#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
doi2bib_pipeline.py
-------------------
Lee doilist.txt (un DOI por línea), genera un referencias.bib y enriquece con abstracts
provenientes de DataCite, Crossref, OpenAlex, Europe PMC, (opcional) Semantic Scholar,
y (opcional, desactivado por defecto) Google Scholar.

Uso:
  1) pip install -r requirements.txt
  2) Edita USER_AGENT con tu email real (recomendación de Crossref)
  3) (Opcional) Exporta SEMANTIC_SCHOLAR_API_KEY si quieres usar esa fuente
  4) (Opcional) Cambia ENABLE_SCHOLAR=True bajo tu propio riesgo (puede violar ToS)
  5) python doi2bib_pipeline.py

Salidas:
  - referencias.bib           (BibTeX enriquecido)
  - faltantes.log             (errores o DOIs sin BibTeX/abstract)
  - enrichment_report.tsv     (doi, fuente del abstract, longitud, notas)
"""

import os
import time
import re
import json
import html
import sys
import random
import logging
from pathlib import Path
from typing import Optional, Tuple
from urllib.parse import quote

import requests

# BeautifulSoup solo se usa si ACTIVAS Google Scholar
try:
    from bs4 import BeautifulSoup  # type: ignore
except Exception:
    BeautifulSoup = None  # Se evaluará más adelante si es necesario

# ----------------- Config -----------------
INPUT = "doilist.txt"
OUT_BIB = "referencias.bib"
OUT_LOG = "faltantes.log"
OUT_TSV = "enrichment_report.tsv"

# Ajusta tu email real (Crossref lo recomienda)
USER_AGENT = "doi2bib-pipeline/1.1 (mailto:tu@correo)"

# Pausas para respetar límites. Puedes subir a 1.0-1.5 si haces muchos DOIs.
PAUSA_S = 0.8

# Orden de preferencia para abstracts
ABSTRACT_SOURCES_ORDER = ("DataCite", "Crossref", "OpenAlex", "EuropePMC", "SemanticScholar", "GoogleScholar")

# Opcionales
ENABLE_SEMANTIC_SCHOLAR = True
ENABLE_SCHOLAR = True   # Desactivado por defecto (posible incumplimiento de ToS). Cámbialo solo si lo aceptas.
SCHOLAR_LANG = "en"      # o "en"
SCHOLAR_MIN_PAUSE_S = 8  # espera respetuosa entre consultas a Scholar
SCHOLAR_MAX_PAUSE_S = 15

SEMANTIC_SCHOLAR_API_KEY = os.environ.get("SEMANTIC_SCHOLAR_API_KEY", "").strip()

# ----------------- Logging -----------------
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s %(levelname)s: %(message)s",
    datefmt="%H:%M:%S",
)

# ----------------- Utils ------------------
TAG_RE = re.compile(r"<[^>]+>")

def clean_abstract(txt: str) -> str:
    if not txt:
        return ""
    txt = TAG_RE.sub("", txt)                 # quitar etiquetas HTML/JATS simples
    txt = html.unescape(txt)
    txt = re.sub(r"\s+", " ", txt).strip()
    return txt

def inject_abstract_in_bib(bibtex: str, abstract: str) -> str:
    """Inserta abstract en la entrada BibTeX, si no existe ya."""
    if not abstract:
        return bibtex
    if re.search(r"(?im)^\s*abstract\s*=", bibtex):
        return bibtex  # ya hay abstract
    safe = abstract.replace("{", "\\{").replace("}", "\\}")
    i = bibtex.rfind("}")
    if i == -1:
        return bibtex + f",\n  abstract = {{{safe}}}\n"
    prefix = bibtex[:i].rstrip()
    if not prefix.endswith(","):
        prefix += ","
    return prefix + f"\n  abstract = {{{safe}}}\n" + bibtex[i:]

def get(url: str, headers=None, timeout=30) -> requests.Response:
    headers = {"User-Agent": USER_AGENT, **(headers or {})}
    return requests.get(url, headers=headers, timeout=timeout)

# ----------------- Sources ----------------

def get_bibtex(doi: str) -> Optional[str]:
    """Intentar obtener BibTeX con doi.org y fallback Crossref transform."""
    # doi.org con content negotiation
    url = f"https://doi.org/{quote(doi)}"
    r = get(url, headers={"Accept": "application/x-bibtex"})
    if r.ok and r.text.strip():
        return r.text.strip()
    # Crossref transform (fallback)
    url2 = f"https://api.crossref.org/works/{quote(doi)}/transform/application/x-bibtex"
    r2 = get(url2)
    if r2.ok and r2.text.strip():
        return r2.text.strip()
    return None

def get_abstract_datacite(doi: str) -> Optional[str]:
    url = f"https://api.datacite.org/dois/{quote(doi)}"
    r = get(url, headers={"Accept": "application/json"})
    if not r.ok:
        return None
    try:
        data = r.json()
        descs = data.get("data", {}).get("attributes", {}).get("descriptions") or []
        cand = None
        for d in descs:
            t = (d.get("descriptionType") or "").lower()
            if t in ("abstract", "other"):
                cand = d.get("description")
                if cand:
                    break
        if not cand and descs:
            cand = descs[0].get("description")
        return clean_abstract(cand) if cand else None
    except Exception:
        return None

def get_abstract_crossref(doi: str) -> Optional[str]:
    url = f"https://api.crossref.org/works/{quote(doi)}"
    r = get(url, headers={"Accept": "application/json"})
    if not r.ok:
        return None
    try:
        msg = r.json().get("message", {})
        abs_jats = msg.get("abstract")
        return clean_abstract(abs_jats) if abs_jats else None
    except Exception:
        return None

def get_abstract_openalex(doi: str) -> Optional[str]:
    """Reconstruir abstract desde abstract_inverted_index si existe."""
    url = f"https://api.openalex.org/works/doi:{quote(doi)}"
    r = get(url, headers={"Accept": "application/json"})
    if not r.ok:
        return None
    try:
        obj = r.json()
        inv = obj.get("abstract_inverted_index")
        if not inv:
            # a veces no hay abstract, o puede venir en otro campo deprecated
            return None
        maxpos = 0
        for positions in inv.values():
            maxpos = max(maxpos, max(positions))
        words = [""] * (maxpos + 1)
        for word, positions in inv.items():
            for p in positions:
                if 0 <= p < len(words):
                    words[p] = word
        txt = " ".join(w for w in words if w).strip()
        return clean_abstract(txt)
    except Exception:
        return None

def get_abstract_europepmc(doi: str) -> Optional[str]:
    url = f"https://www.ebi.ac.uk/europepmc/webservices/rest/search?query=DOI:{quote(doi)}&format=json"
    r = get(url, headers={"Accept": "application/json"})
    if not r.ok:
        return None
    try:
        res = r.json()
        hits = (res.get("resultList") or {}).get("result") or []
        if not hits:
            return None
        abs_txt = hits[0].get("abstractText")
        return clean_abstract(abs_txt) if abs_txt else None
    except Exception:
        return None

def get_abstract_semanticscholar(doi: str) -> Optional[str]:
    if not ENABLE_SEMANTIC_SCHOLAR or not SEMANTIC_SCHOLAR_API_KEY:
        return None
    url = f"https://api.semanticscholar.org/graph/v1/paper/DOI:{quote(doi)}?fields=title,abstract"
    headers = {
        "Accept": "application/json",
        "x-api-key": SEMANTIC_SCHOLAR_API_KEY,
    }
    r = get(url, headers=headers)
    if not r.ok:
        return None
    try:
        data = r.json()
        abs_txt = data.get("abstract")
        return clean_abstract(abs_txt) if abs_txt else None
    except Exception:
        return None

def get_abstract_scholar(doi: str) -> Optional[str]:
    """ÚLTIMO recurso. Puede violar ToS de Google Scholar. USAR BAJO TU PROPIO RIESGO.
    Busca por DOI y toma el snippet de la primera tarjeta (div.gs_rs)."""
    if not ENABLE_SCHOLAR:
        return None
    if BeautifulSoup is None:
        logging.warning("bs4 no está instalado. Instala beautifulsoup4 si deseas usar Scholar.")
        return None

    # Espera aleatoria entre consultas para ser respetuoso
    time.sleep(random.uniform(SCHOLAR_MIN_PAUSE_S, SCHOLAR_MAX_PAUSE_S))

    q = quote(doi)
    url = f"https://scholar.google.com/scholar?hl={SCHOLAR_LANG}&as_sdt=0%2C5&q={q}&btnG="
    # Rotamos un UA simple
    uas = [
        USER_AGENT,
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15",
        "Mozilla/5.0 (X11; Linux x86_64) Gecko/20100101 Firefox/126.0",
    ]
    r = requests.get(url, headers={"User-Agent": random.choice(uas)}, timeout=30)
    if not r.ok:
        return None

    try:
        soup = BeautifulSoup(r.text, "html.parser")
        # Cada resultado típico es un div.gs_ri dentro de div.gs_r(gs_or)
        cards = soup.select("div.gs_r.gs_or")
        if not cards:
            cards = soup.select("div.gs_ri")  # fallback
        for card in cards:
            # Preferimos el primer resultado
            abs_div = card.select_one("div.gs_rs") or card.select_one("div.gs_rs.gs_fma_s")
            if abs_div and abs_div.text:
                txt = abs_div.get_text(separator=" ", strip=True)
                txt = re.sub(r"\s+", " ", txt).strip()
                # A veces Scholar incluye elipsis "[...]" o "…"
                return txt
        return None
    except Exception:
        return None

def get_best_abstract(doi: str) -> Tuple[Optional[str], str]:
    # Orden de preferencia
    for name, fn in (
        ("DataCite", get_abstract_datacite),
        ("Crossref", get_abstract_crossref),
        ("OpenAlex", get_abstract_openalex),
        ("EuropePMC", get_abstract_europepmc),
        ("SemanticScholar", get_abstract_semanticscholar),
        ("GoogleScholar", get_abstract_scholar),
    ):
        try:
            a = fn(doi)
            if a and len(a) >= 40:  # descartar snippets muy cortos
                return a, name
        except Exception:
            pass
    return None, ""

# ----------------- Main -------------------

def main():
    base = Path(".")
    doilist = base / INPUT
    if not doilist.exists():
        logging.error("No encuentro %s", INPUT)
        sys.exit(1)

    dois = [ln.strip() for ln in doilist.read_text(encoding="utf-8").splitlines() if ln.strip() and not ln.strip().startswith("#")]

    out_bibs = []
    misses = []
    report_lines = ["doi\tsource\tlength\tnote"]

    for i, doi in enumerate(dois, 1):
        logging.info("[%d/%d] %s", i, len(dois), doi)
        try:
            bib = get_bibtex(doi)
            if not bib:
                misses.append(f"{doi}\tNoBibTeX")
                time.sleep(PAUSA_S)
                continue

            abstract, source = get_best_abstract(doi)
            if abstract:
                bib = inject_abstract_in_bib(bib, abstract)
                report_lines.append(f"{doi}\t{source}\t{len(abstract)}\tOK")
            else:
                report_lines.append(f"{doi}\t-\t0\tNoAbstractAll")
                misses.append(f"{doi}\tNoAbstractAll")

            out_bibs.append(bib)
        except requests.RequestException as e:
            msg = f"{doi}\tHTTP:{e}"
            misses.append(msg)
            report_lines.append(f"{doi}\t-\t0\t{msg}")
        except Exception as e:
            msg = f"{doi}\tError:{e}"
            misses.append(msg)
            report_lines.append(f"{doi}\t-\t0\t{msg}")

        time.sleep(PAUSA_S)

    Path(OUT_BIB).write_text("\n\n".join(out_bibs) + "\n", encoding="utf-8")
    Path(OUT_LOG).write_text("\n".join(misses) + "\n", encoding="utf-8")
    Path(OUT_TSV).write_text("\n".join(report_lines) + "\n", encoding="utf-8")

    logging.info("Listo.\n- BibTeX: %s\n- Log: %s\n- Reporte: %s", OUT_BIB, OUT_LOG, OUT_TSV)

if __name__ == "__main__":
    main()
