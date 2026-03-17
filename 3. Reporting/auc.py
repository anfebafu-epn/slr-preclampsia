# --- Guardar como auc_hist.png ---
import os
import pandas as pd
import matplotlib.pyplot as plt

# --- Configuración ---
CSV_NAME = "auc_studies.csv"
THIS_DIR = os.path.dirname(os.path.abspath(__file__))
CSV_PATH = os.path.join(THIS_DIR, CSV_NAME)

# --- Definición de columnas según el query SQL ---
cols = [
    "entry_key",
    "auc",
    "auc_ci_low",
    "auc_ci_high",
    "model_family",
    "validation_type",
    "target_json"
]

# --- Leer CSV ---
df = pd.read_csv(
    CSV_PATH,
    sep=';',              # separador punto y coma
    decimal=',',          # coma decimal
    header=None,          # el archivo no tiene encabezado
    names=cols,           # usa los nombres del SELECT
    na_values=['NULL', 'NaN', ''],
    encoding='utf-8-sig',
    quotechar='"',
    engine='python',
    on_bad_lines='warn'
)

# --- Limpieza ---
df['auc'] = pd.to_numeric(df['auc'], errors='coerce')
df = df[df['auc'].notna()]
df = df[(df['auc'] >= 0.5) & (df['auc'] <= 1.0)]

# --- Estadísticos ---
mean_auc   = df['auc'].mean()
median_auc = df['auc'].median()
n = len(df)

# --- Gráfico ---
plt.figure()
plt.hist(df['auc'], bins=10, range=(0.5, 1.0))
plt.axvline(mean_auc,   linestyle='--', linewidth=2, label=f"mean={mean_auc:.3f}")
plt.axvline(median_auc, linestyle='-.', linewidth=2, label=f"median={median_auc:.3f}")
plt.xlim(0.5, 1.0)
plt.xlabel('AUC')
plt.ylabel('Count')
plt.title('Distribution of reported AUCs')
plt.text(0.505, plt.ylim()[1]*0.9, f'n={n}')
plt.legend()
plt.tight_layout()
plt.savefig(os.path.join(THIS_DIR, 'auc_hist.png'), dpi=300)

print(f"✅ Gráfico guardado en {os.path.join(THIS_DIR, 'auc_hist.png')}")
print(f"Media AUC: {mean_auc:.3f}, Mediana AUC: {median_auc:.3f}, n={n}")
