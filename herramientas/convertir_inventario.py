#!/usr/bin/env python3
"""
Convierte INVENTARIO_PRODUCTOS.xlsx (Descripción, Unidad, Stock, Precio) al CSV que importa AppSistema
como inventario inicial (documento de apertura).

  Stock  = cantidad en la presentación del SGP (bolsas, latas, kg…)
  Precio = precio de UNA presentación

Se enlaza por descripción con datos/enlace/catalogo_por_ingrediente.csv (descripcion_comercial) y se verifica
que la unidad del Excel corresponda a la presentación del producto.
Salidas (datos/inventario/): inventario_inicial.csv y observaciones_inventario.csv
Uso: python3 herramientas/convertir_inventario.py   (requiere openpyxl)
"""
import csv
import os
from decimal import Decimal, ROUND_HALF_UP

import openpyxl

RAIZ = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "datos")
# Abreviatura del SGP (Uni.Env) → presentación del sistema, según datos/sgp/LEEME.md.
ABREVIATURAS = {"BAR": "GRANO", "BID": "BIDON", "BLD": "BALDE", "BLI": "GRAMO", "BOL": "BOLSA", "BOT": "BOTELLA", "CAJ": "CAJA",
                "FCO": "FRASCO", "GLN": "GALON", "GR": "GRAMO", "KG": "KILOGRAMO", "KIT": "KIT", "LAT": "LATA", "LT": "LITRO",
                "MIL": "MILLAR", "PAR": "PAR", "PCH": "PAQUETE", "POT": "POTE", "PQT": "PAQUETE", "RLL": "ROLLO", "RSM": "PAQUETE",
                "SCH": "SACHET", "SCO": "SACO", "SOB": "SOBRE", "TUB": "TUBO", "UND": "UNIDAD", "VAS": "VASO"}


def norm(s):
    return " ".join(str(s or "").replace("\xa0", " ").split()).upper()


def d6(x):
    return Decimal(str(x)).quantize(Decimal("0.000001"), rounding=ROUND_HALF_UP)


def main():
    catalogo = {}
    with open(os.path.join(RAIZ, "enlace", "catalogo_por_ingrediente.csv"), encoding="utf-8") as fh:
        for r in csv.DictReader(fh, delimiter=";"):
            catalogo.setdefault(norm(r["descripcion_comercial"]), r)
    wb = openpyxl.load_workbook(os.path.join(RAIZ, "inventario", "origen", "INVENTARIO_PRODUCTOS.xlsx"), read_only=True, data_only=True)
    obs, salida, total = [], [], Decimal(0)
    for n, fila in enumerate(wb.worksheets[0].iter_rows(values_only=True), 1):
        if n == 1 or not any(fila):
            continue
        desc, uni, stock, precio = (list(fila) + [None] * 4)[:4]
        nombre = norm(desc)
        c = catalogo.get(nombre)
        if c is None:
            obs.append((n, nombre, "SIN_PRODUCTO", "no esta en el catalogo; no se carga"))
            continue
        if ABREVIATURAS.get(norm(uni)) != c["tipo_envase"]:
            obs.append((n, nombre, "UNIDAD_DISTINTA", f"el Excel dice {uni} y la presentacion del producto es {c['tipo_envase']}; se usa la del producto"))
        try:
            s, p = d6(stock), d6(precio)
        except Exception:
            obs.append((n, nombre, "DATO_INVALIDO", f"stock '{stock}' o precio '{precio}' no es un numero; no se carga"))
            continue
        if s <= 0:
            obs.append((n, nombre, "SIN_STOCK", "stock cero o negativo; no se carga"))
            continue
        contenido = Decimal(c["contenido_por_envase"])
        valor = (s * p).quantize(Decimal("0.000001"), rounding=ROUND_HALF_UP)
        total += valor
        salida.append([c["variante_codigo"], nombre, c["tipo_envase"], f"{s:f}", f"{p:f}", c["unidad_base"], f"{contenido:f}",
                       f"{(s * contenido).normalize():f}", f"{valor:f}"])
    os.makedirs(os.path.join(RAIZ, "inventario"), exist_ok=True)
    with open(os.path.join(RAIZ, "inventario", "inventario_inicial.csv"), "w", encoding="utf-8", newline="") as fh:
        w = csv.writer(fh, delimiter=";", lineterminator="\n")
        w.writerow(["variante_codigo", "descripcion", "presentacion", "stock_envases", "precio_envase", "unidad_base", "contenido_envase", "cantidad_base", "valor"])
        w.writerows(salida)
    with open(os.path.join(RAIZ, "inventario", "observaciones_inventario.csv"), "w", encoding="utf-8", newline="") as fh:
        w = csv.writer(fh, delimiter=";", lineterminator="\n")
        w.writerow(["fila_excel", "producto", "tipo", "detalle"])
        w.writerows(obs)
    print(f"Lineas: {len(salida)}; valor total: {total:.2f}; observaciones: {len(obs)}")


if __name__ == "__main__":
    main()
