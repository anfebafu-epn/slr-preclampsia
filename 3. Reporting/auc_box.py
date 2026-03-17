# --- Guardar como auc_box_by_model.png ---
import os
import pandas as pd
import matplotlib.pyplot as plt

# --- Configuración ---
CSV_NAME = "auc_studies.csv"
THIS_DIR = os.path.dirname(os.path.abspath(__file__))
CSV_PATH = os.path.join(THIS_DIR, CSV_NAME)

# Columnas según el SELECT SQL
cols = [
    "entry_key",
    "auc",
    "auc_ci_low",
    "auc_ci_high",
    "model_family",
    "validation_type",
    "target_json"
]

# --- Leer CSV correctamente ---
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

# --- Limpieza y preparación ---
df['auc'] = pd.to_numeric(df['auc'], errors='coerce')
df = df[df['auc'].notna()].copy()
df['model_family'] = df['model_family'].fillna('NR')

# --- Agrupar por tipo de modelo ---
groups = []
labels = []
for name, g in df.groupby('model_family'):
    vals = g['auc'].dropna().values
    if len(vals) >= 2:  # evita cajas con 1 solo valor
        groups.append(vals)
        labels.append(name)

# --- Gráfico tipo boxplot ---
plt.figure(figsize=(6, 4))
plt.boxplot(groups, labels=labels, showfliers=True)
plt.ylim(0.5, 1.0)
plt.xticks(rotation=30, ha='right')
plt.ylabel('AUC')
plt.title('AUC by model family')
plt.tight_layout()
plt.savefig(os.path.join(THIS_DIR, 'auc_box_by_model.png'), dpi=300)

print(f"✅ Boxplot guardado en {os.path.join(THIS_DIR, 'auc_box_by_model.png')}")
