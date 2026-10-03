#!/usr/bin/env python3
"""
Enlaza productos comerciales del SGP con los ingredientes de las recetas usando PRODUCTO_INGREDIENTE.csv.

Modelo resultante (el del sistema):
  ingrediente  = producto base (unidad KG, L o UND; código INGnnnnn)
  producto SGP = variante del ingrediente (código SGPnnnnn, contenido = pro_facing en la unidad del ingrediente)
  presentación SGP = empaque de compra (1 envase, mínimo 1, múltiplo 1)

Entradas:
  datos/enlace/origen/PRODUCTO_INGREDIENTE.csv   Producto;Ingrediente;Categoria (Windows-1252)
  datos/sgp/catalogo_sgp.csv                      generado por "AppSistema.Instalador convertir-sgp"
  datos/recetas/recetas_normalizadas.csv          generado por herramientas/convertir_recetas_sgp.py
Salidas (datos/enlace/):
  catalogo_por_ingrediente.csv   formato del importador de catálogo (importar-catalogo)
  recetas_enlazadas.csv          recetas con el ingrediente del enlace (importar-recetas)
  ingredientes_sin_enlace.csv    ingredientes de recetas que no se pudieron enlazar
  observaciones_enlace.csv       conflictos y decisiones automáticas

Uso: python3 herramientas/enlazar_sgp.py
"""
import collections
import csv
import os

RAIZ = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "datos")
SALIDA = os.path.join(RAIZ, "enlace")
obs = []


def norm(s):
    return " ".join(str(s or "").replace("\xa0", " ").split()).upper()


def variantes_de_nombre(n):
    """Mismo nombre con la otra terminación de género (REFRIGERADA/REFRIGERADO, CONGELADA/CONGELADO, ENTERA/ENTERO)."""
    pares = [("REFRIGERADA", "REFRIGERADO"), ("CONGELADA", "CONGELADO"), ("ENTERA", "ENTERO")]
    salida = []
    for a, b in pares:
        for x, y in ((a, b), (b, a)):
            palabras = n.split()
            if x in palabras:
                salida.append(" ".join(y if p == x else p for p in palabras))
    return salida


def leer_csv(ruta, encoding="utf-8"):
    with open(ruta, encoding=encoding, newline="") as fh:
        return list(csv.DictReader(fh, delimiter=";"))


def main():
    os.makedirs(SALIDA, exist_ok=True)
    # ---------- Enlace producto → ingrediente ----------
    enlace, categoria_de = {}, {}
    with open(os.path.join(RAIZ, "enlace", "origen", "PRODUCTO_INGREDIENTE.csv"), encoding="cp1252", newline="") as fh:
        for fila in csv.DictReader(fh, delimiter=";"):
            p, i, c = norm(fila["Producto"]), norm(fila["Ingrediente"]), fila["Categoria"].strip()
            if not p or not i:
                continue
            if p in enlace and enlace[p] != i:
                obs.append((p, "PRODUCTO_CON_DOS_INGREDIENTES", f"enlazado a '{enlace[p]}' y a '{i}'; se usa el primero"))
                continue
            enlace.setdefault(p, i)
            categoria_de.setdefault(i, c)
    ingredientes_enlace = set(enlace.values())

    # ---------- Productos SGP (con unidad y factor ya deducidos) ----------
    sgp = leer_csv(os.path.join(RAIZ, "sgp", "catalogo_sgp.csv"), "utf-8-sig")
    recetas = leer_csv(os.path.join(RAIZ, "recetas", "recetas_normalizadas.csv"))

    # Unidad de cada ingrediente: la de las recetas manda; si no se usa en recetas, la más común de sus productos.
    def resolver(nombre):
        """Ingrediente del enlace para un nombre de receta: él mismo, el de su producto, o variante de género."""
        if nombre in ingredientes_enlace:
            return nombre, "ingrediente"
        if nombre in enlace:
            return enlace[nombre], "producto"
        for v in variantes_de_nombre(nombre):
            if v in ingredientes_enlace:
                return v, "variante de nombre"
            if v in enlace:
                return enlace[v], "variante de nombre de un producto"
        return None, None

    unidad_receta = collections.defaultdict(collections.Counter)
    resolucion = {}
    for r in recetas:
        ing, como = resolver(r["ingrediente"])
        resolucion[r["ingrediente"]] = (ing, como)
        if ing:
            unidad_receta[ing][r["unidad"]] += 1
    unidad_productos = collections.defaultdict(collections.Counter)
    for p in sgp:
        ing = enlace.get(norm(p["producto_descripcion"]))
        if ing:
            unidad_productos[ing][p["unidad_base"]] += 1

    def unidad_de(ing):
        if unidad_receta[ing]:
            return unidad_receta[ing].most_common(1)[0][0]
        return unidad_productos[ing].most_common(1)[0][0]

    # ---------- Catálogo por ingrediente ----------
    base_de = {}            # (nombre producto base, unidad) -> código ING
    filas = []
    sin_enlace = 0
    for p in sgp:
        nombre_sgp = norm(p["producto_descripcion"])
        ing = enlace.get(nombre_sgp)
        if ing is None:
            ing = nombre_sgp   # sin enlace: el producto es su propio ingrediente
            sin_enlace += 1
            obs.append((nombre_sgp, "PRODUCTO_SGP_SIN_INGREDIENTE", "no esta en PRODUCTO_INGREDIENTE; queda como su propio ingrediente"))
            unidad = p["unidad_base"]
        else:
            unidad = unidad_de(ing)
        base = ing
        if p["unidad_base"] != unidad:
            obs.append((nombre_sgp, "UNIDAD_DISTINTA_AL_INGREDIENTE",
                        f"el producto esta en {p['unidad_base']} (factor {p['contenido_por_envase']}) y el ingrediente '{ing}' en {unidad}; "
                        f"queda bajo '{ing} ({p['unidad_base']})' hasta definir la conversion"))
            base, unidad = f"{ing} ({p['unidad_base']})", p["unidad_base"]
        base_de.setdefault((base, unidad), None)
        filas.append((base, unidad, p))

    for n, clave in enumerate(sorted(base_de), 1):
        base_de[clave] = f"ING{n:05d}"

    with open(os.path.join(SALIDA, "catalogo_por_ingrediente.csv"), "w", encoding="utf-8", newline="") as fh:
        w = csv.writer(fh, delimiter=";", lineterminator="\n")
        w.writerow(["producto_codigo", "producto_descripcion", "unidad_base", "categoria", "variante_codigo", "marca", "descripcion_comercial",
                    "tipo_envase", "contenido_por_envase", "empaque_codigo", "empaque_descripcion", "envases_por_empaque", "minimo", "multiplo"])
        for base, unidad, p in sorted(filas, key=lambda x: (base_de[(x[0], x[1])], x[2]["variante_codigo"])):
            # La categoría es del producto base (ingrediente), no de cada producto comercial.
            categoria = categoria_de.get(base.split(" (")[0]) or ("CAJA CHICA" if base.startswith("CAJA CHICA") else "")
            w.writerow([base_de[(base, unidad)], base, unidad, categoria, p["variante_codigo"], "", p["descripcion_comercial"],
                        p["tipo_envase"], p["contenido_por_envase"], p["empaque_codigo"], p["empaque_descripcion"], "1", "1", "1"])

    # ---------- Recetas con el ingrediente del enlace ----------
    no_enlazados = collections.Counter()
    with open(os.path.join(SALIDA, "recetas_enlazadas.csv"), "w", encoding="utf-8", newline="") as fh:
        w = csv.writer(fh, delimiter=";", lineterminator="\n")
        campos = ["receta_codigo", "receta_nombre", "categoria", "fuente", "rendimiento", "ingrediente", "unidad", "cantidad", "tecnica", "instrucciones"]
        w.writerow(campos)
        vistos = collections.defaultdict(dict)   # receta -> ingrediente -> índice de fila
        salida = []
        for r in recetas:
            ing, como = resolucion[r["ingrediente"]]
            nuevo = r["ingrediente"]
            if ing:
                unidad = unidad_de(ing)
                if r["unidad"] == unidad:
                    nuevo = ing
                    if como != "ingrediente":
                        obs.append((r["ingrediente"], "ENLACE_" + como.upper().replace(" ", "_"), f"se usa el ingrediente '{ing}'"))
                else:
                    nuevo = f"{ing} ({r['unidad']})"
                    obs.append((r["receta_nombre"], "UNIDAD_DE_RECETA_DISTINTA", f"{r['ingrediente']} en {r['unidad']}; el ingrediente '{ing}' es {unidad}"))
            else:
                no_enlazados[(r["ingrediente"], r["unidad"])] += 1
            fila = dict(r, ingrediente=nuevo)
            previo = vistos[r["receta_codigo"]].get(nuevo)
            if previo is not None:
                # Dos ingredientes de la receta enlazan al mismo: se suman.
                from decimal import Decimal
                salida[previo]["cantidad"] = f"{Decimal(salida[previo]['cantidad']) + Decimal(r['cantidad']):f}"
                obs.append((r["receta_nombre"], "INGREDIENTES_UNIDOS", f"{r['ingrediente']} se suma a '{nuevo}'"))
                continue
            vistos[r["receta_codigo"]][nuevo] = len(salida)
            salida.append(fila)
        for f in salida:
            w.writerow([f[c] for c in campos])

    with open(os.path.join(SALIDA, "ingredientes_sin_enlace.csv"), "w", encoding="utf-8", newline="") as fh:
        w = csv.writer(fh, delimiter=";", lineterminator="\n")
        w.writerow(["ingrediente", "unidad", "lineas_de_receta"])
        for (i, u), n in sorted(no_enlazados.items(), key=lambda x: (-x[1], x[0])):
            w.writerow([i, u, n])

    vistas = set()
    with open(os.path.join(SALIDA, "observaciones_enlace.csv"), "w", encoding="utf-8", newline="") as fh:
        w = csv.writer(fh, delimiter=";", lineterminator="\n")
        w.writerow(["elemento", "tipo", "detalle"])
        for o in sorted(obs):
            if o not in vistas:
                vistas.add(o)
                w.writerow(o)

    tipos = collections.Counter(o[1] for o in vistas)
    print(f"Productos base (ingredientes): {len(base_de)}; productos SGP como variantes: {len(filas)} (sin enlace {sin_enlace}); "
          f"ingredientes de receta sin enlace: {len(no_enlazados)} ({sum(no_enlazados.values())} lineas)")
    for t, n in tipos.most_common():
        print(f"  {t}: {n}")


if __name__ == "__main__":
    main()
