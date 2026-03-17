import time, re, json, html, sys, pathlib
from urllib.parse import quote
import requests

# Configuración
INPUT = "doilist.txt"
OUT_BIB = "referencias.bib"
OUT_LOG = "faltantes.log"
PAUSA_S = 0.6  # respeta rate limits (~1–2 req/seg)
USER_AGENT = "doi2bib-script/1.0 (mailto:andres.bastidas@smartwork.com.ec)"

# Utilidades
TAG_RE = re.compile(r"<[^>]+>")
def clean_abstract(txt: str) -> str:
    if not txt: return ""
    # Quitar etiquetas JATS/HTML simples y normalizar espacios
    txt = TAG_RE.sub("", txt)
    txt = html.unescape(txt)
    # Quitar espacios repetidos
    txt = re.sub(r"\s+", " ", txt).strip()
    return txt

def get_bibtex(doi: str) -> str | None:
    # 1) Content negotiation via doi.org
    url = f"https://doi.org/{quote(doi)}"
    headers = {"Accept": "application/x-bibtex", "User-Agent": USER_AGENT}
    r = requests.get(url, headers=headers, timeout=20)
    if r.ok and r.text.strip():
        return r.text.strip()

    # 2) Fallback: Crossref transform endpoint
    url2 = f"https://api.crossref.org/works/{quote(doi)}/transform/application/x-bibtex"
    r2 = requests.get(url2, headers={"User-Agent": USER_AGENT}, timeout=20)
    if r2.ok and r2.text.strip():
        return r2.text.strip()

    return None

def get_abstract(doi: str) -> str | None:
    # 1) DataCite
    url_dc = f"https://api.datacite.org/dois/{quote(doi)}"
    r = requests.get(url_dc, headers={"User-Agent": USER_AGENT, "Accept": "application/json"}, timeout=20)
    if r.ok:
        try:
            data = r.json()
            descs = data.get("data", {}).get("attributes", {}).get("descriptions") or []
            # elegir el abstract preferentemente con type 'Abstract' o 'Other'
            cand = None
            for d in descs:
                t = (d.get("descriptionType") or "").lower()
                if t in ("abstract", "other"):
                    cand = d.get("description")
                    if cand: break
            if not cand and descs:
                cand = descs[0].get("description")
            if cand:
                return clean_abstract(cand)
        except Exception:
            pass

    # 2) Crossref (campo 'abstract' en JATS)
    url_cr = f"https://api.crossref.org/works/{quote(doi)}"
    r2 = requests.get(url_cr, headers={"User-Agent": USER_AGENT, "Accept": "application/json"}, timeout=20)
    if r2.ok:
        try:
            msg = r2.json().get("message", {})
            abs_jats = msg.get("abstract")
            if abs_jats:
                return clean_abstract(abs_jats)
        except Exception:
            pass

    return None

def inject_abstract_in_bib(bibtex: str, abstract: str) -> str:
    if not abstract: return bibtex
    # Si ya hay campo abstract, no duplicar
    if re.search(r"\babstract\s*=", bibtex, flags=re.I):
        return bibtex
    # Insertar antes de la última '}' de la entrada
    # Aseguramos llaves balanceadas para lo típico
    # Escapar llaves en el abstract (rudimentario)
    safe = abstract.replace("{", "\\{").replace("}", "\\}")
    # Buscar cierre final de la entrada
    i = bibtex.rfind("}")
    if i == -1:
        # fallback simple
        return bibtex + f",\n  abstract = {{{safe}}}\n"
    # Si antes del cierre ya hay coma, perfecto; si no, añadir
    prefix = bibtex[:i].rstrip()
    if not prefix.endswith(","):
        prefix += ","
    return prefix + f"\n  abstract = {{{safe}}}\n" + bibtex[i:]

def main():
    base = pathlib.Path(".")
    doilist = base / INPUT
    if not doilist.exists():
        print(f"No encuentro {INPUT}.")
        sys.exit(1)

    lines = [ln.strip() for ln in doilist.read_text(encoding="utf-8").splitlines()]
    dois = [ln for ln in lines if ln and not ln.startswith("#")]

    out_b = []
    misses = []

    for idx, doi in enumerate(dois, 1):
        print(f"[{idx}/{len(dois)}] {doi}")
        try:
            bib = get_bibtex(doi)
            if not bib:
                misses.append(f"{doi}\tNoBibTeX")
                time.sleep(PAUSA_S)
                continue

            abs_txt = get_abstract(doi)
            if abs_txt:
                bib = inject_abstract_in_bib(bib, abs_txt)
            else:
                misses.append(f"{doi}\tNoAbstract")

            out_b.append(bib)
        except requests.HTTPError as e:
            misses.append(f"{doi}\tHTTPError:{e}")
        except Exception as e:
            misses.append(f"{doi}\tError:{e}")

        time.sleep(PAUSA_S)

    pathlib.Path(OUT_BIB).write_text("\n\n".join(out_b) + "\n", encoding="utf-8")
    pathlib.Path(OUT_LOG).write_text("\n".join(misses) + "\n", encoding="utf-8")

    print(f"\nListo.\n- BibTeX: {OUT_BIB}\n- Log: {OUT_LOG}")

if __name__ == "__main__":
    main()
