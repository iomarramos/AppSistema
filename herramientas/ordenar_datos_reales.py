#!/usr/bin/env python3
"""
Ordena los archivos recibidos del SGP en un juego de datos real y cargable (datos/real/):

  precios_sgp.csv          precio por presentación (inventario o último precio SGP) — solo los que tienen precio y no son atípicos
  precios_atipicos.csv     precios apartados por ser 4 veces mayores o menores que los de su mismo ingrediente
  enlace_manual.csv        (entrada, se edita a mano) enlaces confirmados que mandan sobre el automático
  enlace_complementario.csv ingredientes de receta enlazados por nombre a un ingrediente del catálogo con precio
  ingredientes_por_revisar.csv los que no se enlazaron solos, con el mejor candidato
  recetas_reales.csv       las recetas con el ingrediente del catálogo (lo que se importa)
  recetas_clasificadas.csv cada receta con su componente de menú, servicios, gramaje por ración y costo estimado
  insumos_sin_costo.csv    ingredientes que no se compran (agua para receta): se costean en S/ 0
  contenido_por_revisar.csv presentaciones cuyo contenido cargado no coincide con la medida del nombre (p. ej. 393 GR vs 0,395 KG)
  familias_sgp.csv         familia › subfamilia › grupo del SGP por presentación
  productos_activos.csv    por ingrediente, el producto activo en la operación (con stock o compra más reciente): su precio se costea (D02)
  estructuras_menu.csv     estructura teórica de Desayuno, Almuerzo y Cena (componentes, factor, alternativas)
  ciclo_menu.csv           ciclo de 28 días: receta(s) por día, servicio y componente, con reparto
  resumen.txt              conteos para revisar

Entradas (ya en el repositorio):
  datos/enlace/catalogo_por_ingrediente.csv   ingrediente → productos SGP (contenido por envase en KG/L/UND)
  datos/enlace/recetas_enlazadas.csv          946 recetas con cantidad por ración del ingrediente
  datos/sgp/productos_precios.csv             último precio por producto (precio de una presentación)

Uso: python3 herramientas/ordenar_datos_reales.py
"""
import collections
import csv
import itertools
import os
import re
from decimal import Decimal, ROUND_HALF_UP

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATOS = os.path.join(RAIZ, "datos")
SALIDA = os.path.join(DATOS, "real")


def leer(ruta):
    with open(ruta, encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f, delimiter=";"))


def escribir(nombre, columnas, filas):
    with open(os.path.join(SALIDA, nombre), "w", encoding="utf-8", newline="") as f:
        w = csv.writer(f, delimiter=";", lineterminator="\n")
        w.writerow(columnas)
        w.writerows(filas)


def norm(s):
    return re.sub(r"\s+", " ", (s or "").strip().upper())


def dec(s):
    return Decimal((s or "0").replace(",", "."))


def fmt(d, n=6):
    return str(Decimal(d).quantize(Decimal(1).scaleb(-n), rounding=ROUND_HALF_UP))


# ---------------------------------------------------------------- componentes de menú
# (componente, servicios donde se usa) — el orden de las reglas importa: la primera que coincide gana.
POR_CATEGORIA = {
    "BEB CALIENTES": "BEBIDA CALIENTE",
    "BEB JUG PULPA NATURAL": "JUGO",
    "BEB JUGOS INSTANTANEOS": "REFRESCO",
    "BEB JUG CONCENTRADOS": "REFRESCO",
    "BEB JUGOS REF PULPA CONGELADA": "REFRESCO",
    "BEB REFRESCOS NATURALES": "REFRESCO",
    "GUARN TUBERCULOS": "GUARNICION", "GUARN VERDURAS": "GUARNICION", "GUARN OTROS": "GUARNICION",
    "GUARN ARROZ": "GUARNICION", "GUARN MENESTRAS": "GUARNICION", "GUARN PASTAS": "GUARNICION",
    "ENSALADAS MIXTAS": "ENTRADA", "ENSALADAS FRESCAS": "ENTRADA", "ENSALADA COCIDA": "ENTRADA",
    "SANDW CALIENTES": "SANDWICH", "SANDWICH FRIOS": "SANDWICH", "SAND INTEGRALES LIGHT": "SANDWICH", "SANDWICH": "SANDWICH",
}
REGLAS_NOMBRE = [
    ("SALSA Y AJI", r"^(AJI -|SALSA|MAYONESA|ALCUZA|PEREJIL$|LIMON$)"),
    ("OTROS", r"^(AGUA MINERAL|GASEOSA|SALAD BAR|ALMUERZO TEMATICO|CLASICO$)"),
    ("FONDO", r"^(POLL ?-|RES -|PES -|CER -|PAV -|GALL -|CMIX -|CORD -|MON -|PAT -|HIG -|PUL -|CUY -|CORA -|ALITAS|ANTICUCHO|BROCHETA|ARROZ A LA CUBANA|"
              r"SALCHIPAPA|SALCHIPOLLO|PIZZA|PASTA EN|SPAGUETTI|PASTEL DE PAPA|PASTEL DE ACELGA|CARRUSEL|MENESTRON CON RES|PATA A LA REINA|ZARZA DE PATA|"
              r"QUICHE|SANDWICH CON|TORTILLA DE ATUN|LOCRO|GUISO DE CALABAZA|PICANTE DE OLLUCO|REVUELTO DE CHUÑO)"),
    ("SOPA", r"^(SOPA|CREMA DE|CALDO|CONSOME|AGUADITO|CHUPE|WALLPACHUPE)"),
    ("HUEVO", r"^(OMELETTE|TORTILLA DE VERDURAS|HUEVO|CALDILLO DE HUEVO)"),
    ("PAN", r"^(PAN |PAN-|TOSTADAS)"),
    ("UNTABLE", r"^(MANTEQUILLA|MERMELADA$)"),
    ("COMPLEMENTO", r"^(JAMON|JAMONADA|MORTADELA|QUESO EDAM|QUESO FRESCO|PALTA$|CHICHARRON DE PRENSA|TOCINO$|ACEITUNA|EMPANADA|TEQUEÑOS EN|MAIZ CANCHA)"),
    ("FRUTA", r"^(MANZANA ROJA|PERA$|MANDARINA|NARANJA DE MESA|PLATANO DE SEDA|GRANADILLA|SANDIA|UVA NEGRA|TUNA ROJA)$"),
    ("POSTRE", r"^(GELATINA|MAZAMORRA|ARROZ CON LECHE|FLAN|KEKE|PIONONO|MOUSSE|COMPOTA|DULCE DE|BUDIN|PUDIN|TORTA|PIE |TARTALETA|ALFAJOR|PICARONES|"
               r"CHURROS|BROWNIE|BLONDIE|LECHE ASADA|CREMA VOLTEADA|ENCANELADO|COCADA|STRUDEL|MIL HOJAS|RELAMPAGO|TURCAS|PAÑUELO|NIDITOS|CINNAMON|"
               r"CROCANTE|MERENGADO|MANZANA DELICIA|TOCINO DEL CIELO|QUESO HELADO|DURAZNO EN ALMIBAR|PIÑA EN ALMIBAR|COCKTAIL DE FRUTAS|HIRO DE|"
               r"PASTEL DE CHOCLO DULCE|PASTEL DE MANZANA|CREPES|QUINUA ACARAMELADA|BUÑUELOS)"),
    ("BEBIDA CALIENTE", r"^(INFUSION|LECHE CALIENTE|AVENA|KIWICHA|MACA$|PONCHE|API$|PUNQUI)"),
    ("JUGO", r"^JUGO"),
    ("REFRESCO", r"^REFRESCO"),
    ("ENTRADA", r"^(ENSALADA|ENTRA -|CAUSA|PAPA A LA|PAPA RELLENA|PAPA DORADA|OCOPA|CEVICHE|TIRADITO|SOLTERITO|PALTA RELLENA|CHOCLO CON QUESO|"
                r"CHOCLITO|HUEVOS A LA RUSA|SALPICON|TEQUEÑOS DE LOMO|CROQUETAS|ZAPALLITO)"),
    ("GUARNICION", r"^(GUAR -|ARROZ CON|ARROZ ZAMBITO|PAPAS FRITAS|PAPA CON CHOCLO|REVUELTO DE VERDURAS|CAMOTE GLASEADO|CHOCLO SANCOCHADO)"),
]
# Proteína del fondo (para rotar en el ciclo): prefijo del Recetón o palabra del nombre.
PROTEINAS = [("POLLO", r"^(POLL|ALITAS|SALCHIPOLLO|BROCHETA DE POLLO|ANTICUCHO DE POLLO)|POLLO"), ("RES", r"^(RES -|MENESTRON CON RES|LOCRO)|\bRES\b"),
             ("PESCADO", r"^PES -|PESCADO|TRUCHA|ATUN"), ("CERDO", r"^CER -|CERDO"), ("PAVO", r"^PAV -|PAVO|PAVITA"),
             ("GALLINA", r"^GALL -|GALLINA"), ("OTRAS CARNES", r"^(CMIX|CORD|MON|PAT|HIG|PUL|CUY|CORA) -")]


# Recetas de fichas que por nombre pertenecen al desayuno aunque su hoja sea "GUARN OTROS".
POR_NOMBRE_PRIMERO = [
    ("HUEVO", r"^(HUEVO FRITO|HUEVOS A LA ORDEN|HUEVOS REVUELTOS|TORTILLA DE HUEVO)$"),
    ("CEREAL", r"^(CEREAL|GRANOLA|YOGURT)"),
    ("COMPLEMENTO", r"^(CHORIZO|HOT DOG|SALCHICHA DE HUACHO|ACEITUNA NEGRA|ACEITUNA VERDE)$"),
]


def componente(nombre, categoria):
    for comp, patron in POR_NOMBRE_PRIMERO:
        if re.search(patron, nombre):
            return comp
    if categoria in POR_CATEGORIA:
        return POR_CATEGORIA[categoria]
    for comp, patron in REGLAS_NOMBRE:
        if re.search(patron, nombre):
            return comp
    return "OTROS"


def proteina(nombre):
    for p, patron in PROTEINAS:
        if re.search(patron, nombre):
            return p
    return "VEGETARIANO/OTRO"


# Estructura teórica propuesta (editable en la aplicación y ajustable por cada operación).
# (servicio, orden, código, componente, nombre visible, factor %, repartos de las alternativas)
ESTRUCTURAS = [
    ("DESAYUNO", 1, "BEB", "BEBIDA CALIENTE", "Bebida caliente", 100, [100]),
    ("DESAYUNO", 2, "JUG", "JUGO", "Jugo", 100, [50, 50]),
    ("DESAYUNO", 3, "PAN", "PAN", "Panes", 100, [100]),
    ("DESAYUNO", 4, "FSO", "SOPA", "Fondo o sopa", 30, [100]),
    ("DESAYUNO", 5, "CO1", "COMPLEMENTO", "Complemento 1 (jamon, aceituna, queso, palta, chicharron de prensa)", 70, [100]),
    ("DESAYUNO", 6, "CO2", "COMPLEMENTO", "Complemento 2 (otro complemento distinto al 1)", 30, [100]),
    ("DESAYUNO", 7, "HUE", "HUEVO", "Huevo (frito / a la orden)", 60, [50, 50]),
    ("DESAYUNO", 8, "UNT", "UNTABLE", "Mantequilla / mermelada", 60, [50, 50]),
    ("DESAYUNO", 9, "FRU", "FRUTA", "Fruta", 40, [100]),
    ("DESAYUNO", 10, "CER", "CEREAL", "Cereales / yogurt", 30, [100]),
    ("ALMUERZO", 1, "ENT", "ENTRADA", "Entrada", 100, [100]),
    ("ALMUERZO", 2, "SOP", "SOPA", "Sopa", 100, [100]),
    ("ALMUERZO", 3, "FON", "FONDO", "Plato de fondo", 100, [70, 30]),
    ("ALMUERZO", 4, "GUA", "GUARNICION", "Guarnicion", 100, [100]),
    ("ALMUERZO", 5, "REF", "REFRESCO", "Refresco", 100, [100]),
    ("ALMUERZO", 6, "POS", "POSTRE", "Postre", 100, [100]),
    ("ALMUERZO", 7, "SAL", "SALSA Y AJI", "Salsas y ajies", 50, [100]),
    ("CENA", 1, "SOP", "SOPA", "Sopa", 100, [100]),
    ("CENA", 2, "FON", "FONDO", "Plato de fondo", 100, [100]),
    ("CENA", 3, "GUA", "GUARNICION", "Guarnicion", 100, [100]),
    ("CENA", 4, "BEB", "BEBIDA CALIENTE", "Bebida caliente", 100, [100]),
    ("CENA", 5, "POS", "POSTRE", "Postre", 100, [100]),
]
# Alternativas fijas por indicación del usuario: el huevo del desayuno se ofrece frito y a la orden.
FIJAS = {("DESAYUNO", "HUE"): ["HUEVO FRITO", "HUEVOS A LA ORDEN"]}
ROTACION_FONDOS = ["POLLO", "RES", "PESCADO", "POLLO", "CERDO", "PAVO", "RES", "GALLINA", "POLLO", "PESCADO", "OTRAS CARNES", "RES", "PAVO", "CERDO"]
DIAS = 28
FECHA_INVENTARIO = "2026-10-01"
# Palabras que no distinguen un ingrediente de otro (estado de conservación, envase, unidades).
NEUTRAS = {"DE", "EN", "X", "Y", "LA", "EL", "DEL", "KG", "GR", "LT", "ML", "UND", "-", "CAJA", "CHICA", "REFRIGERADO", "REFRIGERADA",
           "CONGELADO", "CONGELADA", "ENVASADO", "GRANEL", "IMPORTADO", "IMPORTACION", "PRE", "ELABORADO", "RAM", "KGM"}


def tokens(s):
    return {t for t in re.findall(r"[A-ZÑÁÉÍÓÚ0-9/.]+", norm(s)) if t not in NEUTRAS and not re.fullmatch(r"[0-9.,]+", t)}


def familia(c):
    """Grupo para variar el menú: la categoría de la ficha o, en el Recetón, la primera palabra (CAMOTE, ARROZ, SOPA…)."""
    if c["categoria"] != "RECETON SGP":
        return c["categoria"]
    return norm(c["nombre"]).split(" ")[0]


def intercalar(lista):
    """Mantiene el orden de prioridad pero alterna familias (arroz, tubérculo, verdura…) para no repetir días seguidos."""
    salida = []
    for completo in (True, False):
        grupos = collections.OrderedDict()
        for c in lista:
            if c["completo"] == completo:
                grupos.setdefault(familia(c), []).append(c)
        colas = list(grupos.values())
        while any(colas):
            for cola in colas:
                if cola:
                    salida.append(cola.pop(0))
    return salida


def main():
    os.makedirs(SALIDA, exist_ok=True)
    catalogo = leer(os.path.join(DATOS, "enlace", "catalogo_por_ingrediente.csv"))
    precios = leer(os.path.join(DATOS, "sgp", "productos_precios.csv"))
    recetas = leer(os.path.join(DATOS, "enlace", "recetas_enlazadas.csv"))

    # ---- precios por presentación (solo con precio, regla del usuario)
    por_nombre = collections.defaultdict(list)
    for c in catalogo:
        por_nombre[norm(c["descripcion_comercial"])].append(c)
    filas_precio, sin_cruce = [], 0
    precio_variante = {}
    # El inventario inicial trae el precio vigente de la presentación (valorización de apertura): tiene prioridad.
    inventario = leer(os.path.join(DATOS, "inventario", "inventario_inicial.csv"))
    por_codigo = {c["variante_codigo"]: c for c in catalogo}
    for i in inventario:
        v = por_codigo.get(i["variante_codigo"])
        precio = dec(i["precio_envase"])
        if v is None or precio <= 0:
            continue
        precio_variante[v["variante_codigo"]] = precio
        filas_precio.append([v["variante_codigo"], v["descripcion_comercial"], v["producto_codigo"], v["producto_descripcion"], v["unidad_base"],
                             v["contenido_por_envase"], v["empaque_codigo"], fmt(precio), FECHA_INVENTARIO, "inventario"])
    for p in precios:
        precio = dec(p["ultimo_precio"])
        if precio <= 0:
            continue
        vs = por_nombre.get(norm(p["nombre"]))
        if not vs:
            sin_cruce += 1
            continue
        for v in vs:
            if v["variante_codigo"] in precio_variante:
                continue
            precio_variante[v["variante_codigo"]] = precio
            filas_precio.append([v["variante_codigo"], v["descripcion_comercial"], v["producto_codigo"], v["producto_descripcion"], v["unidad_base"],
                                 v["contenido_por_envase"], v["empaque_codigo"], fmt(precio), p["fecha_ultima_compra"] or "2026-01-01", "ultimo precio SGP"])
    filas_precio.sort(key=lambda f: (f[3], f[0]))

    # ---- precios atípicos: costo por unidad base 4 veces mayor o menor que la mediana de las otras presentaciones del mismo
    # ingrediente (p. ej. el precio de una caja registrado como si fuera una unidad). Se apartan para revisar y no se cargan.
    por_ingrediente = collections.defaultdict(list)
    for c in catalogo:
        precio = precio_variante.get(c["variante_codigo"])
        if precio is not None and dec(c["contenido_por_envase"]) > 0:
            por_ingrediente[c["producto_codigo"]].append((precio / dec(c["contenido_por_envase"]), c))
    atipicos = []
    for ing, lista in por_ingrediente.items():
        if len(lista) < 3:
            continue
        for unitario, c in lista:
            otros = sorted(u for u, x in lista if x is not c)
            mediana = otros[len(otros) // 2]
            if mediana > 0 and (unitario > mediana * 4 or unitario * 4 < mediana):
                atipicos.append([c["variante_codigo"], c["descripcion_comercial"], c["producto_descripcion"], c["unidad_base"], fmt(unitario, 4), fmt(mediana, 4)])
    codigos_atipicos = {a[0] for a in atipicos}
    for cod in codigos_atipicos:
        precio_variante.pop(cod, None)
    filas_precio = [f for f in filas_precio if f[0] not in codigos_atipicos]
    escribir("precios_sgp.csv", ["variante_codigo", "descripcion_comercial", "producto_codigo", "producto_descripcion", "unidad_base",
                                 "contenido_por_envase", "empaque_codigo", "precio_envase", "fecha_precio", "fuente"], filas_precio)
    # ---- D02 (decisión del usuario): por ingrediente, el producto ACTIVO en la operación es el que se usa ahí: el que tiene
    # stock en el inventario; si no hay, el de compra más reciente en el SGP. Su precio es el que se costea (no el más barato).
    def preferencia(f):   # inventario primero; luego la fecha de compra más reciente; luego el código
        return (0 if f[9] == "inventario" else 1, -int(f[8].replace("-", "")), f[0])
    activos = {}
    for f in filas_precio:
        if f[2] not in activos or preferencia(f) < preferencia(activos[f[2]]):
            activos[f[2]] = f
    filas_activo = []
    for f in sorted(activos.values(), key=lambda x: (x[3], x[0])):
        motivo = "con stock en el inventario inicial" if f[9] == "inventario" else f"compra mas reciente en el SGP ({f[8]})"
        filas_activo.append([f[0], f[1], f[2], f[3], motivo])
    escribir("productos_activos.csv", ["variante_codigo", "descripcion_comercial", "producto_codigo", "producto_descripcion", "motivo"], filas_activo)

    # ---- familias del SGP (familia › subfamilia › grupo) por presentación, tal como vienen en el listado de precios.
    # Las compras de caja chica no traen subfamilia: quedan solo con su familia.
    filas_familia, vistas = [], set()
    for p in precios:
        familia = (p["familia"] or "").strip()
        if not familia or familia == "SIN CATEGORIA":
            continue
        for v in por_nombre.get(norm(p["nombre"]), []):
            if v["variante_codigo"] in vistas:
                continue
            vistas.add(v["variante_codigo"])
            filas_familia.append([v["variante_codigo"], v["descripcion_comercial"], familia, (p["subfamilia"] or "").strip(), (p["grupo"] or "").strip()])
    filas_familia.sort(key=lambda f: (f[2], f[3], f[4], f[1]))
    escribir("familias_sgp.csv", ["variante_codigo", "descripcion_comercial", "familia", "subfamilia", "grupo"], filas_familia)
    variante_activa = {f[2]: f[0] for f in filas_activo}
    escribir("precios_atipicos.csv", ["variante_codigo", "descripcion_comercial", "ingrediente", "unidad_base", "costo_por_unidad_base", "mediana_del_ingrediente"],
             sorted(atipicos, key=lambda a: a[2]))

    # ---- enlace complementario: ingrediente de receta sin producto con precio → ingrediente del catálogo que sí lo tiene.
    # Automático solo si TODAS las palabras significativas del ingrediente de la receta están en el producto (mismas unidades).
    con_precio_por_ing = collections.defaultdict(list)
    for c in catalogo:
        # Las compras de "CAJA CHICA" son registros sueltos (a veces con el precio de toda la caja como si fuera una unidad):
        # no sirven para decidir a qué producto corresponde un ingrediente.
        if c["variante_codigo"] in precio_variante and not norm(c["descripcion_comercial"]).startswith("CAJA CHICA"):
            con_precio_por_ing[(norm(c["producto_descripcion"]), c["unidad_base"])].append(c)
    necesitan = collections.Counter((norm(r["ingrediente"]), r["unidad"]) for r in recetas
                                    if (norm(r["ingrediente"]), r["unidad"]) not in con_precio_por_ing and not norm(r["ingrediente"]).startswith("AGUA"))
    enlace, pendientes = {}, []
    # Enlaces confirmados a mano (datos/real/enlace_manual.csv) tienen prioridad sobre el automático.
    ruta_manual = os.path.join(SALIDA, "enlace_manual.csv")
    if os.path.exists(ruta_manual):
        for m in leer(ruta_manual):
            enlace[(norm(m["ingrediente_receta"]), m["unidad"])] = m["ingrediente_catalogo"]
    for (nombre, unidad), lineas in necesitan.most_common():
        if (nombre, unidad) in enlace:
            continue
        t = tokens(nombre)
        mejor = None
        for (ing, u), vs in con_precio_por_ing.items():
            if u != unidad or not t:
                continue
            ti = tokens(ing)
            for v in vs:
                tv = tokens(v["descripcion_comercial"])
                puntaje = max(len(t & ti) / len(t), len(t & tv) / len(t))
                # Parecido global (Jaccard) con el ingrediente del catálogo: evita que "MAYONESA" caiga en una mayonesa acevichada.
                parecido = len(t & ti) / len(t | ti) if ti else 0
                if mejor is None or (puntaje, parecido) > (mejor[0], mejor[3]):
                    mejor = (puntaje, ing, v, parecido)
        if mejor and mejor[0] >= 1 and mejor[3] >= 0.5:
            enlace[(nombre, unidad)] = mejor[1]
        else:
            pendientes.append([nombre, unidad, lineas, mejor[1] if mejor else "", mejor[2]["descripcion_comercial"] if mejor else "",
                               f"{mejor[0]:.2f}" if mejor else "0"])
    escribir("enlace_complementario.csv", ["ingrediente_receta", "unidad", "ingrediente_catalogo"],
             sorted([[k[0], k[1], v] for k, v in enlace.items()]))
    escribir("ingredientes_por_revisar.csv", ["ingrediente_receta", "unidad", "lineas_de_receta", "candidato_ingrediente", "candidato_producto", "coincidencia"], pendientes)
    # Recetas con el ingrediente del catálogo (lo que se importa). Si dos líneas quedan en el mismo ingrediente, se suman.
    reales = []
    for codigo, grupo in itertools.groupby(recetas, key=lambda r: r["receta_codigo"]):
        acumulado = collections.OrderedDict()
        for r in grupo:
            nuevo = dict(r)
            nuevo["ingrediente"] = enlace.get((norm(r["ingrediente"]), r["unidad"]), r["ingrediente"])
            clave = (norm(nuevo["ingrediente"]), nuevo["unidad"])
            if clave in acumulado:
                acumulado[clave]["cantidad"] = fmt(dec(acumulado[clave]["cantidad"]) + dec(r["cantidad"]))
            else:
                acumulado[clave] = nuevo
        reales.extend(acumulado.values())
    recetas = reales
    with open(os.path.join(SALIDA, "recetas_reales.csv"), "w", encoding="utf-8", newline="") as f:
        w = csv.DictWriter(f, fieldnames=list(recetas[0].keys()), delimiter=";", lineterminator="\n")
        w.writeheader()
        w.writerows(recetas)

    # ---- costo por unidad base del ingrediente: precio / contenido del producto ACTIVO en la operación (D02)
    costo_base = {}
    for c in catalogo:
        if variante_activa.get(c["producto_codigo"]) != c["variante_codigo"]:
            continue
        precio = precio_variante.get(c["variante_codigo"])
        contenido = dec(c["contenido_por_envase"])
        if precio is None or contenido <= 0:
            continue
        costo_base[(norm(c["producto_descripcion"]), c["unidad_base"])] = precio / contenido

    # ---- recetas: componente, gramaje y costo estimado por ración
    por_receta = collections.OrderedDict()
    for r in recetas:
        por_receta.setdefault(r["receta_codigo"], []).append(r)
    clasificadas = []
    for codigo, lineas in por_receta.items():
        nombre, categoria = lineas[0]["receta_nombre"], lineas[0]["categoria"]
        comp = componente(norm(nombre), categoria)
        gramos = sum(dec(l["cantidad"]) * 1000 for l in lineas if l["unidad"] == "KG")
        mililitros = sum(dec(l["cantidad"]) * 1000 for l in lineas if l["unidad"] == "L")
        unidades = sum(dec(l["cantidad"]) for l in lineas if l["unidad"] == "UND")
        con_precio = [l for l in lineas if (norm(l["ingrediente"]), l["unidad"]) in costo_base]
        costo = sum(dec(l["cantidad"]) * costo_base[(norm(l["ingrediente"]), l["unidad"])] for l in con_precio)
        sin_precio = [l["ingrediente"] for l in lineas if (norm(l["ingrediente"]), l["unidad"]) not in costo_base and not norm(l["ingrediente"]).startswith("AGUA")]
        clasificadas.append({
            "codigo": codigo, "nombre": nombre, "categoria": categoria, "componente": comp,
            "proteina": proteina(norm(nombre)) if comp == "FONDO" else "",
            "gramos": gramos, "ml": mililitros, "und": unidades, "lineas": len(lineas), "con_precio": len(con_precio),
            "costo": costo, "completo": not sin_precio, "sin_precio": sin_precio,
            "dieta": "DIETA" in norm(nombre) or "LIGHT" in norm(nombre), "evento": "ROMPE RUTINA" in norm(nombre) or "EVENTO" in norm(nombre) or "TEMATICO" in norm(nombre),
        })
    servicios_de = collections.defaultdict(set)
    for s, _, _, comp, _, _, _ in ESTRUCTURAS:
        servicios_de[comp].add(s)
    escribir("recetas_clasificadas.csv",
             ["receta_codigo", "receta_nombre", "categoria_origen", "componente", "servicios", "proteina", "gramaje_g_racion", "volumen_ml_racion",
              "unidades_racion", "ingredientes", "ingredientes_con_precio", "costo_estimado_racion", "costo_completo", "ingredientes_sin_precio"],
             [[c["codigo"], c["nombre"], c["categoria"], c["componente"], ",".join(sorted(servicios_de.get(c["componente"], []))), c["proteina"],
               fmt(c["gramos"], 1), fmt(c["ml"], 1), fmt(c["und"], 3), c["lineas"], c["con_precio"], fmt(c["costo"], 4), "SI" if c["completo"] else "NO",
               ", ".join(c["sin_precio"][:6])] for c in sorted(clasificadas, key=lambda x: (x["componente"], x["nombre"]))])

    # ---- insumos sin costo de compra: el agua de las recetas (no se compra; se costea en S/ 0)
    aguas = sorted({r["ingrediente"] for r in recetas if norm(r["ingrediente"]).startswith("AGUA PARA")})
    escribir("insumos_sin_costo.csv", ["producto", "motivo"], [[a, "agua de red para preparar, no se compra"] for a in aguas])

    # ---- contenido por envase: la medida del nombre ("5 LT", "393 GR", "500 ML") contra el contenido cargado (en KG o L).
    # No se corrige nada: lo que no coincide va a revisión, porque cambia el costo por unidad base.
    # Se descartan los que no son error: venta por peso (porciones "120 GR" de un producto que se compra por KG),
    # paquetes múltiples ("6X4 LITROS") y compras de caja chica (no se usan para costear).
    medida = re.compile(r"(\d+(?:[.,]\d+)?)\s*(KGS?|KILOS?|GRS?|G|GRAMOS|LTS?|L|LITROS?|ML|CC)\b")
    a_base = {"KG": ("KG", 1), "KGS": ("KG", 1), "KILO": ("KG", 1), "KILOS": ("KG", 1), "GR": ("KG", Decimal("0.001")), "GRS": ("KG", Decimal("0.001")),
              "G": ("KG", Decimal("0.001")), "GRAMOS": ("KG", Decimal("0.001")), "LT": ("L", 1), "LTS": ("L", 1), "L": ("L", 1), "LITRO": ("L", 1),
              "LITROS": ("L", 1), "ML": ("L", Decimal("0.001")), "CC": ("L", Decimal("0.001"))}
    revisar_contenido, coinciden_contenido = [], 0
    for c in catalogo:
        nombre = c["descripcion_comercial"].upper()
        hallado = medida.findall(nombre)
        if c["unidad_base"] not in ("KG", "L") or not hallado or nombre.startswith("CAJA CHICA") or re.search(r"\d+\s*X\s*\d", nombre):
            continue
        cantidad, unidad = hallado[-1]
        base, factor = a_base[unidad]
        if base != c["unidad_base"]:
            continue   # "1 LT / 946 GR": dos medidas de distinta unidad, el contenido usa la de la unidad base
        declarado = dec(cantidad.replace(",", ".")) * factor
        cargado = dec(c["contenido_por_envase"])
        if abs(declarado - cargado) <= Decimal("0.0005"):
            coinciden_contenido += 1
            continue
        por_peso = cargado == 1 and c["unidad_base"] == "KG" and declarado < 1 and re.search(r"KGM|GRANEL|CONGELAD|CORTE|TROZ|FILETE|CHULETA|ALBONDIGA|PORCION", nombre)
        if por_peso:
            continue
        en_uso = "si" if variante_activa.get(c["producto_codigo"]) == c["variante_codigo"] else "no"
        revisar_contenido.append([c["variante_codigo"], c["descripcion_comercial"], c["producto_descripcion"], c["unidad_base"],
                                  fmt(declarado, 3), fmt(cargado, 3), en_uso])
    escribir("contenido_por_revisar.csv", ["variante_codigo", "descripcion_comercial", "ingrediente", "unidad_base", "contenido_segun_nombre",
                                           "contenido_cargado", "es_producto_activo"], sorted(revisar_contenido, key=lambda x: (x[6] != "si", x[2], x[0])))

    # ---- estructuras
    escribir("estructuras_menu.csv", ["servicio", "orden", "codigo", "componente", "nombre", "factor_consumo_pct", "alternativas_reparto_pct"],
             [[s, o, cod, comp, nom, f, "+".join(map(str, rep))] for s, o, cod, comp, nom, f, rep in ESTRUCTURAS])

    # ---- ciclo de 28 días: recetas del componente con costo completo primero, sin eventos ni dietas; no repetir en 7 días
    pools = collections.defaultdict(list)
    for c in clasificadas:
        if c["evento"] or c["dieta"]:
            continue
        pools[c["componente"]].append(c)
    for comp in pools:
        # Primero las de costo completo, luego las fichas revisadas; "GUARN OTROS" (aceitunas, cancha…) al final de las guarniciones.
        pools[comp].sort(key=lambda c: (not c["completo"], c["categoria"] == "GUARN OTROS", c["codigo"].startswith("RT"), c["nombre"]))
        pools[comp] = intercalar(pools[comp])
    fondos_por_proteina = collections.defaultdict(list)
    for c in pools.get("FONDO", []):
        fondos_por_proteina[c["proteina"]].append(c)
    cursores = collections.Counter()
    usados = collections.defaultdict(list)  # (servicio, comp) -> [(dia, codigo)]
    usados_dia = set()                       # (dia, codigo) en cualquier servicio

    desfase = {"DESAYUNO": 0, "ALMUERZO": 0, "CENA": 0}

    def elegir(servicio, comp, dia, evitar):
        if comp == "FONDO":
            idx = (dia - 1 + (0 if servicio == "ALMUERZO" else 5)) % len(ROTACION_FONDOS)
            prot = ROTACION_FONDOS[idx]
            pool = fondos_por_proteina.get(prot) or pools["FONDO"]
            clave = (servicio, comp, prot)
        else:
            pool = pools.get(comp, [])
            clave = (servicio, comp, "")
        if not pool:
            return None
        if clave not in cursores:
            # Cada servicio empieza en otro punto de la lista: almuerzo y cena no repiten la misma receta el mismo día.
            completas = sum(1 for c in pool if c["completo"]) or len(pool)
            cursores[clave] = {"DESAYUNO": 0, "ALMUERZO": 0, "CENA": completas // 2}[servicio]
        recientes = {cod for d, cod in usados[(servicio, comp)] if dia - d < 7}
        del_dia = {cod for (d, cod) in usados_dia if d == dia}
        # Preferencia: con precio y sin repetir en la semana; con precio aunque se repita; sin precio sin repetir; cualquiera.
        for solo_completas, sin_repetir in ((True, True), (True, False), (False, True), (False, False)):
            for _ in range(len(pool)):
                c = pool[cursores[clave] % len(pool)]
                cursores[clave] += 1
                if c["codigo"] in evitar or c["codigo"] in del_dia:
                    continue
                if (solo_completas and not c["completo"]) or (sin_repetir and c["codigo"] in recientes):
                    continue
                return c
        return None

    ciclo = []
    sin_receta_con_precio = set()
    for dia in range(1, DIAS + 1):
        for s, orden, cod, comp, nom, factor, repartos in ESTRUCTURAS:
            elegidas = set()
            # Un componente sin ninguna receta con precio no entra al ciclo (no se inventa costo); queda informado en el resumen.
            if not any(c["completo"] for c in pools.get(comp, [])):
                sin_receta_con_precio.add(f"{s} / {nom}")
                continue
            # Si el componente no tiene recetas suficientes para todas sus alternativas, la que haya se lleva el 100 %.
            disponibles = sum(1 for c in pools.get(comp, []) if c["completo"])
            if disponibles < len(repartos):
                repartos = [100] * max(disponibles, 0) if disponibles else []
            fijas = [x for x in clasificadas if norm(x["nombre"]) in FIJAS.get((s, cod), [])]
            for n_alt, rep in enumerate(repartos):
                c = fijas[n_alt] if n_alt < len(fijas) else elegir(s, comp, dia, elegidas)
                if c is None:
                    continue
                elegidas.add(c["codigo"])
                usados[(s, comp)].append((dia, c["codigo"]))
                usados_dia.add((dia, c["codigo"]))
                ciclo.append([dia, s, orden, cod, nom, c["codigo"], c["nombre"], rep, fmt(c["gramos"], 1), fmt(c["costo"], 4), "SI" if c["completo"] else "NO"])
    escribir("ciclo_menu.csv", ["dia", "servicio", "orden", "estructura_codigo", "estructura", "receta_codigo", "receta_nombre", "reparto_pct",
                                "gramaje_g_racion", "costo_estimado_racion", "costo_completo"], ciclo)

    # ---- resumen
    comp_count = collections.Counter(c["componente"] for c in clasificadas)
    completas = sum(1 for c in clasificadas if c["completo"])
    with open(os.path.join(SALIDA, "resumen.txt"), "w", encoding="utf-8") as f:
        f.write(f"Precios: {len(filas_precio)} presentaciones con precio ({sum(1 for x in filas_precio if x[-1] == 'inventario')} del inventario, "
                f"el resto ultimo precio SGP; {sin_cruce} precios sin producto en el catalogo).\n")
        f.write(f"Precios atipicos apartados para revisar: {len(atipicos)} (precios_atipicos.csv).\n")
        f.write(f"Ingredientes con costo por unidad base: {len(costo_base)}.\n")
        f.write(f"Enlace complementario: {len(enlace)} ingredientes de receta enlazados por nombre; {len(pendientes)} por revisar ({sum(p[2] for p in pendientes)} lineas).\n")
        f.write(f"Recetas: {len(clasificadas)}; con todos sus ingredientes con precio (salvo agua): {completas}.\n")
        f.write("Recetas por componente: " + ", ".join(f"{k} {v}" for k, v in sorted(comp_count.items())) + "\n")
        f.write(f"Fondos por proteina: " + ", ".join(f"{k} {len(v)}" for k, v in sorted(fondos_por_proteina.items())) + "\n")
        f.write(f"Ciclo: {DIAS} dias, {len(ciclo)} lineas; con costo completo: {sum(1 for x in ciclo if x[-1] == 'SI')}.\n")
        f.write(f"Familias SGP: {len(filas_familia)} presentaciones con familia; {len({(f[2], f[3], f[4]) for f in filas_familia})} grupos.\n")
        f.write(f"Contenido por envase: {coinciden_contenido} presentaciones coinciden con la medida del nombre; {len(revisar_contenido)} por revisar (contenido_por_revisar.csv), {sum(1 for x in revisar_contenido if x[6] == 'si')} de ellas son el producto activo.\n")
        if sin_receta_con_precio:
            f.write("Componentes fuera del ciclo por no tener ninguna receta con precio: " + ", ".join(sorted(sin_receta_con_precio)) + ".\n")
    print(open(os.path.join(SALIDA, "resumen.txt"), encoding="utf-8").read())


if __name__ == "__main__":
    main()
