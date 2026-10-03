#!/usr/bin/env python3
"""
Convierte las recetas exportadas del SGP a un CSV normalizado que importa AppSistema.

Fuentes (datos/recetas/origen/):
  - RECETAS_SGP_REVISADAS_DEL_TOTAL_3.xlsx: fichas técnicas revisadas (una hoja por categoría) con
    ingredientes, cantidad por ración, unidad, técnica de corte y preparación. TIENEN PRIORIDAD.
  - Receta_-_Receton.csv: receta;ingrediente;unidad;cantidad por ración (Latin-1). Cada receta aparece
    en varios bloques repetidos; se toma la versión más frecuente (empate: la última del archivo).

Salidas (datos/recetas/):
  - recetas_normalizadas.csv: receta_codigo;receta_nombre;categoria;fuente;rendimiento;ingrediente;
    unidad;cantidad;tecnica;instrucciones   (instrucciones solo en la primera fila de cada receta)
  - ingredientes.csv: ingrediente;unidad;recetas;producto_sgp_exacto;candidatos_sgp
  - observaciones_recetas.csv: receta;tipo;detalle   (todo lo que se corrigió o conviene revisar)

Uso: python3 herramientas/convertir_recetas_sgp.py   (requiere openpyxl)
Mismo origen → mismo resultado (códigos deterministas).
"""
import collections
import csv
import io
import os
import re
from decimal import Decimal, ROUND_HALF_UP

import openpyxl

RAIZ = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "datos")
ORIGEN = os.path.join(RAIZ, "recetas", "origen")
SALIDA = os.path.join(RAIZ, "recetas")
UNIDADES = {"KG": "KG", "LT": "L", "L": "L", "UND": "UND", "UNIDAD": "UND"}
RX_CODIGO_SGP = re.compile(r"^(\d+(?:\.\d+)+)\s+(.*)$")

obs = []


def norm(s):
    return " ".join(str(s or "").replace("\xa0", " ").split()).upper()


def cantidad6(x):
    """Cantidad con 6 decimales (u6), mitad alejándose de cero."""
    return Decimal(str(x)).quantize(Decimal("0.000001"), rounding=ROUND_HALF_UP)


def es_numero(x):
    if isinstance(x, (int, float)):
        return True
    try:
        Decimal(str(x))
        return True
    except Exception:
        return False


# ---------- Fichas técnicas (Excel) ----------

def leer_fichas():
    wb = openpyxl.load_workbook(os.path.join(ORIGEN, "RECETAS_SGP_REVISADAS_DEL_TOTAL_3.xlsx"), read_only=True, data_only=True)
    fichas = []
    for ws in wb.worksheets:
        cur, modo = None, None
        for n, fila in enumerate(ws.iter_rows(values_only=True), 1):
            r = list(fila) + [None] * (8 - len(fila))
            t = [str(x).strip() if x is not None else "" for x in r]
            if t[2].upper() == "FICHA TECNICA DE RECETA":
                cur = {"hoja": ws.title, "fila": n, "nombre": None, "id": None, "codigo_sgp": None, "ing": [], "prep": []}
                fichas.append(cur)
                modo = "cab"
                continue
            if cur is None:
                continue
            if modo == "cab" and t[2] and t[2].upper() != "INGREDIENTES":
                nombre = norm(t[2])
                m = RX_CODIGO_SGP.match(nombre)
                if m:
                    cur["codigo_sgp"], nombre = m.group(1), m.group(2)
                cur["nombre"] = nombre
                for x in r[3:8]:
                    if isinstance(x, (int, float)) and x >= 1:
                        cur["id"] = int(x)
                continue
            if t[2].upper() == "INGREDIENTES":
                modo = "ing"
                if cur["id"] is None:
                    for x in r[5:8]:
                        if isinstance(x, (int, float)):
                            cur["id"] = int(x)
                continue
            if t[2].upper() == "PREPARACION":
                modo = "prep"
                continue
            if modo == "ing" and t[2]:
                cur["ing"].append({"nombre": norm(t[2]), "cantidad": r[3], "unidad": t[4].upper(), "tecnica": t[5], "fila": n})
            elif modo == "prep" and t[2]:
                cur["prep"].append(t[2])
    return fichas


# ---------- Recetón (CSV) ----------

def leer_receton():
    texto = open(os.path.join(ORIGEN, "Receta_-_Receton.csv"), encoding="latin-1").read()
    bloques = collections.defaultdict(list)
    anterior = None
    for linea in texto.replace("\r\n", "\n").split("\n"):
        if not linea.strip():
            continue
        receta, ingrediente, unidad, cant = linea.split(";")
        k = norm(receta)
        if k != anterior:
            bloques[k].append([])
            anterior = k
        bloques[k][-1].append((norm(ingrediente), unidad.strip().upper(), cantidad6(cant)))
    recetas = {}
    for k, bl in bloques.items():
        versiones = collections.Counter(tuple(sorted(b)) for b in bl)
        maximo = max(versiones.values())
        # La más frecuente; en empate la que aparece última en el archivo.
        elegida = [tuple(sorted(b)) for b in bl if versiones[tuple(sorted(b))] == maximo][-1]
        if len(versiones) > 1:
            otras = [v for v in versiones if v != elegida]
            dif = sorted(set(otras[0]) ^ set(elegida))
            obs.append((k, "RECETON_VARIAS_VERSIONES",
                        f"{len(versiones)} versiones en {len(bl)} bloques; se toma la que aparece {maximo} veces. Diferencias: "
                        + ", ".join(f"{i} {c} {u}" for i, u, c in dif[:6])))
        recetas[k] = [(i, u, c, "") for i, u, c in elegida]
    return recetas


def main():
    fichas = leer_fichas()
    receton = leer_receton()

    # Unidad habitual de cada ingrediente (para completar unidades vacías o PQT).
    habitual = collections.defaultdict(collections.Counter)
    for f in fichas:
        for i in f["ing"]:
            if i["unidad"] in UNIDADES:
                habitual[i["nombre"]][UNIDADES[i["unidad"]]] += 1
    for lineas in receton.values():
        for i, u, c, _ in lineas:
            if u in UNIDADES:
                habitual[i][UNIDADES[u]] += 1

    recetas = {}   # nombre -> dict
    vistos_id = {}
    for f in fichas:
        nombre = f["nombre"]
        if f["codigo_sgp"]:
            obs.append((nombre, "CODIGO_EN_NOMBRE", f"el nombre traia el codigo SGP {f['codigo_sgp']}; se quito del nombre"))
        if nombre in recetas:
            previo = recetas[nombre]
            igual = sorted((i["nombre"], i["cantidad"]) for i in f["ing"]) == previo["_firma"]
            obs.append((nombre, "FICHA_DUPLICADA", f"hoja {f['hoja']} fila {f['fila']} repite la ficha de la fila {previo['_fila']}"
                        + ("" if igual else " CON OTROS INGREDIENTES; se usa la primera")))
            continue
        lineas = collections.OrderedDict()
        for i in f["ing"]:
            cant, uni = i["cantidad"], i["unidad"]
            if not es_numero(cant) and es_numero(uni):
                cant, uni = uni, str(cant).upper()
                obs.append((nombre, "COLUMNAS_INVERTIDAS", f"{i['nombre']}: cantidad y unidad estaban intercambiadas"))
            if uni not in UNIDADES:
                deducida = habitual[i["nombre"]].most_common(1)
                if deducida:
                    obs.append((nombre, "UNIDAD_DEDUCIDA", f"{i['nombre']}: unidad '{uni}' no valida; se usa {deducida[0][0]} (la habitual del ingrediente)"))
                    uni = deducida[0][0]
                else:
                    obs.append((nombre, "UNIDAD_INVALIDA", f"{i['nombre']}: unidad '{uni}' sin equivalente; se usa UND"))
                    uni = "UND"
            else:
                uni = UNIDADES[uni]
            if not es_numero(cant) or Decimal(str(cant)) <= 0:
                obs.append((nombre, "CANTIDAD_INVALIDA", f"{i['nombre']}: cantidad '{cant}' no valida; se omite el ingrediente"))
                continue
            c = cantidad6(cant)
            if c == 0:
                obs.append((nombre, "CANTIDAD_MENOR_A_U6", f"{i['nombre']}: {cant} redondea a 0 con 6 decimales; se usa 0.000001"))
                c = Decimal("0.000001")
            if i["nombre"] in lineas:
                u0, c0, t0 = lineas[i["nombre"]]
                obs.append((nombre, "INGREDIENTE_REPETIDO", f"{i['nombre']} aparece dos veces; se suman {c0} + {c}"))
                lineas[i["nombre"]] = (u0, c0 + c, " / ".join(x for x in (t0, i["tecnica"]) if x))
            else:
                lineas[i["nombre"]] = (uni, c, i["tecnica"])
        if f["id"] in vistos_id:
            obs.append((nombre, "ID_REPETIDO", f"el numero {f['id']} ya lo usa '{vistos_id[f['id']]}'"))
        vistos_id.setdefault(f["id"], nombre)
        recetas[nombre] = {"codigo": f"F{f['id']:05d}", "categoria": f["hoja"], "fuente": "FICHA",
                           "lineas": [(k, *v) for k, v in lineas.items()], "prep": f["prep"],
                           "_firma": sorted((i["nombre"], i["cantidad"]) for i in f["ing"]), "_fila": f["fila"]}

    # Comparación con el Recetón de las recetas que están en ambos.
    for nombre, r in recetas.items():
        if nombre in receton:
            a = {i: (u, c) for i, u, c, _ in r["lineas"]}
            b = {i: (UNIDADES.get(u, u), c) for i, u, c, _ in receton[nombre]}
            if a != b:
                solo_f = sorted(set(a) - set(b))
                solo_r = sorted(set(b) - set(a))
                cambia = sorted(k for k in set(a) & set(b) if a[k] != b[k])
                partes = []
                if solo_f: partes.append("solo en ficha: " + ", ".join(solo_f[:5]))
                if solo_r: partes.append("solo en receton: " + ", ".join(solo_r[:5]))
                if cambia: partes.append("cantidad distinta: " + ", ".join(f"{k} {b[k][1]}->{a[k][1]}" for k in cambia[:5]))
                obs.append((nombre, "FICHA_DIFIERE_DE_RECETON", "se usa la ficha revisada; " + "; ".join(partes)))

    # Recetas solo del Recetón, con código correlativo por nombre.
    for n, nombre in enumerate(sorted(k for k in receton if k not in recetas), 1):
        lineas = [(i, UNIDADES[u], c, "") for i, u, c, _ in sorted(receton[nombre])]
        recetas[nombre] = {"codigo": f"RT{n:04d}", "categoria": "RECETON SGP", "fuente": "RECETON", "lineas": lineas, "prep": []}

    # Un ingrediente es un producto base con UNA unidad. Si aparece con otra unidad (KG y L no se convierten
    # sin una regla), el uso minoritario pasa a un ingrediente aparte "NOMBRE (UNIDAD)" y queda observado.
    conteo = collections.defaultdict(collections.Counter)
    for r in recetas.values():
        for ing, uni, _, _ in r["lineas"]:
            conteo[ing][uni] += 1
    for ing, c in sorted(conteo.items()):
        if len(c) > 1:
            principal = c.most_common(1)[0][0]
            obs.append((ing, "INGREDIENTE_VARIAS_UNIDADES",
                        ", ".join(f"{u} en {n} recetas" for u, n in c.most_common())
                        + f"; se mantiene {principal} y los otros usos pasan a '{ing} (UNIDAD)'"))
            for nombre, r in recetas.items():
                for k, (i2, uni, cant, tec) in enumerate(r["lineas"]):
                    if i2 == ing and uni != principal:
                        r["lineas"][k] = (f"{ing} ({uni})", uni, cant, tec)
                        obs.append((nombre, "UNIDAD_DISTINTA_A_LA_HABITUAL", f"{ing} en {uni} (lo habitual es {principal}); revisar la cantidad"))

    # ---------- Escritura ----------
    with open(os.path.join(SALIDA, "recetas_normalizadas.csv"), "w", encoding="utf-8", newline="") as fh:
        w = csv.writer(fh, delimiter=";", lineterminator="\n")
        w.writerow(["receta_codigo", "receta_nombre", "categoria", "fuente", "rendimiento", "ingrediente", "unidad", "cantidad", "tecnica", "instrucciones"])
        for nombre, r in sorted(recetas.items(), key=lambda x: x[1]["codigo"]):
            prep = "\n".join(r["prep"])
            for k, (ing, uni, cant, tec) in enumerate(r["lineas"]):
                w.writerow([r["codigo"], nombre, r["categoria"], r["fuente"], "1", ing, uni, f"{cant:f}", tec, prep if k == 0 else ""])

    sgp = {}
    with open(os.path.join(RAIZ, "sgp", "productos_sgp_original.tsv"), encoding="utf-8") as fh:
        next(fh)
        for linea in fh:
            if linea.strip():
                sgp.setdefault(norm(linea.rsplit("\t", 2)[0]), True)
    usos = collections.Counter()
    unidad_de = {}
    for r in recetas.values():
        for ing, uni, _, _ in r["lineas"]:
            usos[ing] += 1
            unidad_de.setdefault(ing, collections.Counter())[uni] += 1
    with open(os.path.join(SALIDA, "ingredientes.csv"), "w", encoding="utf-8", newline="") as fh:
        w = csv.writer(fh, delimiter=";", lineterminator="\n")
        w.writerow(["ingrediente", "unidad", "recetas", "producto_sgp_exacto", "candidatos_sgp"])
        for ing in sorted(usos):
            palabras = ing.split()[:2]
            candidatos = [p for p in sgp if p != ing and p.split()[:2] == palabras][:5]
            w.writerow([ing, unidad_de[ing].most_common(1)[0][0], usos[ing], "SI" if ing in sgp else "", " | ".join(candidatos)])

    with open(os.path.join(SALIDA, "observaciones_recetas.csv"), "w", encoding="utf-8", newline="") as fh:
        w = csv.writer(fh, delimiter=";", lineterminator="\n")
        w.writerow(["receta", "tipo", "detalle"])
        for o in sorted(obs):
            w.writerow(o)

    fuentes = collections.Counter(r["fuente"] for r in recetas.values())
    print(f"Recetas: {len(recetas)} (fichas {fuentes['FICHA']}, receton {fuentes['RECETON']}); "
          f"lineas {sum(len(r['lineas']) for r in recetas.values())}; ingredientes {len(usos)}; observaciones {len(obs)}")
    for tipo, n in collections.Counter(o[1] for o in obs).most_common():
        print(f"  {tipo}: {n}")


if __name__ == "__main__":
    main()
