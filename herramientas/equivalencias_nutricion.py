"""
Equivalencias entre los ingredientes de las recetas y las dos tablas de composición:
  * Tabla Peruana (INS/CENAN 2013): fuente principal.
  * Tabla kCAL del Excel "kCAL MENÚ DIARIO ACTUAL.xls": complementaria.

Entrada : datos/real/recetas_reales.csv (ingredientes de las recetas ordenadas)
          datos/nutricion/tpca_ins_por_100g.csv
          datos/nutricion/composicion_por_100g.csv
Salida  : datos/nutricion/equivalencias.csv
  estado: AUTO      puntaje >= 0.80 (revisar solo por muestreo)
          REVISAR   0.55 a 0.80: hay que confirmar la equivalencia a mano
          SIN       sin alimento parecido en ninguna tabla: buscar fuente externa

El puntaje combina semejanza de texto y coincidencia de palabras. Se prefiere la tabla INS cuando empatan.
No se asigna ningún valor nutricional aquí: solo se propone el alimento de referencia.
"""
import csv
import difflib
import os
import re
import unicodedata
from collections import Counter

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RECETAS = os.path.join(RAIZ, "datos", "real", "recetas_reales.csv")
INS = os.path.join(RAIZ, "datos", "nutricion", "tpca_ins_por_100g.csv")
KCAL = os.path.join(RAIZ, "datos", "nutricion", "composicion_por_100g.csv")
SALIDA = os.path.join(RAIZ, "datos", "nutricion", "equivalencias.csv")

PALABRAS_VACIAS = {"DE", "DEL", "LA", "EL", "Y", "EN", "CON", "SIN", "PARA", "A", "O", "ENTERO", "ENTERA", "ENVASADO",
                   "MOLIDO", "MOLIDA", "FRESCO", "CONGELADO", "REFRIGERADO", "TETRA", "CAJA"}


def norm(texto):
    t = unicodedata.normalize("NFKD", str(texto)).encode("ascii", "ignore").decode().upper()
    t = re.sub(r"[^A-Z0-9 ]+", " ", t)
    return re.sub(r"\s+", " ", t).strip()


def palabras(texto):
    return {p for p in norm(texto).split() if p not in PALABRAS_VACIAS and len(p) > 1}


def puntaje(ingrediente, alimento):
    a, b = norm(ingrediente), norm(alimento)
    if not a or not b:
        return 0.0
    texto = difflib.SequenceMatcher(None, a, b).ratio()
    pa, pb = palabras(a), palabras(b)
    if not pa or not pb:
        return round(texto * 0.8, 3)
    interseccion = len(pa & pb)
    cubre = interseccion / len(pa)                # cuántas palabras del ingrediente aparecen en el alimento
    return round(0.5 * texto + 0.5 * cubre, 3)


def cargar_alimentos():
    alimentos = []
    with open(INS, encoding="utf-8") as f:
        for r in csv.DictReader(f, delimiter=";"):
            alimentos.append({"fuente": "INS_2013", "codigo": r["codigo"], "alimento": r["alimento"]})
    with open(KCAL, encoding="utf-8") as f:
        for r in csv.DictReader(f, delimiter=";"):
            alimentos.append({"fuente": "KCAL_EXCEL", "codigo": r["codigo"], "alimento": r["alimento"]})
    return alimentos


def ingredientes():
    usos = Counter()
    with open(RECETAS, encoding="utf-8", errors="replace") as f:
        for r in csv.DictReader(f, delimiter=";"):
            usos[norm(r["ingrediente"])] += 1
    return usos


def main():
    alimentos = cargar_alimentos()
    filas = []
    for ing, usadas in ingredientes().most_common():
        mejor = None
        for al in alimentos:
            p = puntaje(ing, al["alimento"])
            # a igual puntaje, gana la tabla INS (fuente principal)
            if mejor is None or p > mejor[0] or (p == mejor[0] and al["fuente"] == "INS_2013" and mejor[1]["fuente"] != "INS_2013"):
                mejor = (p, al)
        p, al = mejor if mejor else (0.0, {"fuente": "", "codigo": "", "alimento": ""})
        estado = "AUTO" if p >= 0.80 else ("REVISAR" if p >= 0.55 else "SIN")
        filas.append({"ingrediente": ing, "recetas": usadas, "fuente": al["fuente"], "codigo": al["codigo"],
                      "alimento_propuesto": al["alimento"], "puntaje": p, "estado": estado})
    with open(SALIDA, "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=list(filas[0].keys()), delimiter=";")
        w.writeheader()
        w.writerows(filas)
    resumen = Counter(f["estado"] for f in filas)
    print(f"ingredientes: {len(filas)}  AUTO: {resumen['AUTO']}  REVISAR: {resumen['REVISAR']}  SIN: {resumen['SIN']}")


if __name__ == "__main__":
    main()
