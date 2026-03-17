# --- Guardar como auc_forest.png ---
import os
import pandas as pd
import matplotlib.pyplot as plt
import numpy as np

# --- Configuración ---
CSV_NAME = "auc_studies.csv"
THIS_DIR = os.path.dirname(os.path.abspath(__file__))
CSV_PATH = os.path.join(THIS_DIR, CSV_NAME)

# Columnas según tu SELECT
cols = [
    "entry_key",
    "auc",
    "auc_ci_low",
    "auc_ci_high",
    "model_family",
    "validation_type",
    "target_json"
]

# --- Lectura robusta (punto y coma + coma decimal, sin encabezado) ---
df = pd.read_csv(
    CSV_PATH,
    sep=';',
    decimal=',',
    header=None,
    names=cols,
    na_values=['NULL', 'NaN', ''],
    encoding='utf-8-sig',
    quotechar='"',
    engine='python',
    on_bad_lines='warn'
)

# --- Tipos numéricos y filtro de filas válidas ---
for c in ["auc", "auc_ci_low", "auc_ci_high"]:
    df[c] = pd.to_numeric(df[c], errors='coerce')

df = df[df['auc'].notna()].copy()
df['has_ci'] = df['auc_ci_low'].notna() & df['auc_ci_high'].notna()

# (opcional) acotar a rango razonable de AUC
df = df[(df['auc'] >= 0.5) & (df['auc'] <= 1.0)].copy()

# Ordenar por AUC (si prefieres mejor arriba, invierte después el eje Y)
df.sort_values('auc', inplace=True)
df.reset_index(drop=True, inplace=True)

# --- Forest plot ---
y = np.arange(len(df))
plt.figure(figsize=(7, max(4, 0.3*len(df))))

# puntos
plt.plot(df['auc'], y, 'o')

# barras de error solo donde haya IC
with_ci = df['has_ci'].values
if with_ci.any():
    plt.hlines(
        y[with_ci],
        df.loc[with_ci, 'auc_ci_low'],
        df.loc[with_ci, 'auc_ci_high']
    )

# Etiquetas y formato
plt.yticks(y, df['entry_key'])
plt.xlabel('AUC')
plt.xlim(0.5, 1.0)
plt.title('Study-level AUC (95% CI if available)')
plt.grid(axis='x', linestyle=':', alpha=0.5)

# (opcional) poner el mejor AUC arriba
plt.gca().invert_yaxis()

plt.tight_layout()
out_path = os.path.join(THIS_DIR, 'auc_forest.png')
plt.savefig(out_path, dpi=300)
print(f"✅ Forest plot guardado en: {out_path}")
