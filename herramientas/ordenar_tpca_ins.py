"""
Ordena la Tabla Peruana de Composición de Alimentos (INS/CENAN, PDF) a un CSV por 100 g.

Entrada : datos/nutricion/origen/tpca_ins_2.pdf (texto extraído con `pdftotext` sin disposición de columnas).
Salida  : datos/nutricion/tpca_ins_por_100g.csv   (un alimento por fila, valores con punto decimal)
          datos/nutricion/tpca_ins_revision.csv   (alimentos con valores dudosos o incompletos)

Reglas:
* Cada alimento: código (A51, B97...), nombre, y 20 componentes en el orden fijo de la tabla.
* Los valores van después de la fila "Cantidad". "�" = no reportado o trazas: se guarda vacío, nunca cero.
* Control: la energía (kcal) debe estar cerca de 4·proteína + 9·grasa + 4·carbohidratos disponibles.
  Lo que no cuadra va a la lista de revisión; no se corrige a mano.
"""
import csv
import os
import re
import subprocess
import sys

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PDF = os.path.join(RAIZ, "datos", "nutricion", "origen", "tpca_ins_2.pdf")
SALIDA = os.path.join(RAIZ, "datos", "nutricion", "tpca_ins_por_100g.csv")
REVISION = os.path.join(RAIZ, "datos", "nutricion", "tpca_ins_revision.csv")

# Orden de los componentes en cada ficha (verificado en las primeras páginas de la tabla).
COMPONENTES = [
    "energia_kcal", "energia_kj", "agua_g", "proteina_g", "grasa_g", "carbohidratos_totales_g",
    "carbohidratos_disponibles_g", "fibra_g", "cenizas_g", "calcio_mg", "fosforo_mg", "zinc_mg",
    "hierro_mg", "betacaroteno_equivalentes_ug", "retinol_ug", "vitamina_a_equivalentes_ug",
    "tiamina_mg", "riboflavina_mg", "niacina_mg", "vitamina_c_mg",
]
CODIGO = re.compile(r"^[A-Z]{1,2}\d{1,3}$")
TOKEN_NUMERO = re.compile(r"^\d+(,\d+)?$")
FIN_VALORES = ("el nutriente no se encuentra", "Investigar para proteger")


def texto_pdf():
    if not os.path.exists(PDF):
        sys.exit(f"No existe {PDF}")
    return subprocess.run(["pdftotext", PDF, "-"], capture_output=True, check=True,
                          encoding="utf-8", errors="replace").stdout


def numero(token):
    if token == "�" or token == "":
        return None
    if TOKEN_NUMERO.match(token):
        return float(token.replace(",", "."))
    return None


ENCABEZADO = re.compile(r"^(NOMBRE|C.DIGO|G\W*NERO|GM-|OTROS|Composici|Componente|Unidad$|Actualizado|TABLAS|Nuevo|GRUPO)", re.IGNORECASE)


def alimentos(texto):
    """Recorre las líneas y devuelve (grupo, codigo, nombre, valores) por alimento."""
    lineas = [l.strip() for l in texto.splitlines()]
    grupo = ""
    i = 0
    while i < len(lineas):
        l = lineas[i]
        if l.startswith("GRUPO "):
            grupo = l
        if CODIGO.match(l):
            codigo = l
            # nombre: primera línea no vacía que no sea encabezado de la ficha
            j = i + 1
            while j < len(lineas) and (not lineas[j] or ENCABEZADO.match(lineas[j])):
                j += 1
            nombre = lineas[j] if j < len(lineas) else ""
            # valores: la línea que empieza con "Cantidad" trae los primeros; pueden seguir en líneas siguientes
            k = j
            while k < len(lineas) and not lineas[k].startswith("Cantidad"):
                if CODIGO.match(lineas[k]):
                    break
                k += 1
            tokens = []
            if k < len(lineas) and lineas[k].startswith("Cantidad"):
                tokens.extend(t for t in lineas[k][len("Cantidad"):].split(" ") if t)
                m = k + 1
            else:
                m = k
            while m < len(lineas) and not lineas[m].startswith(FIN_VALORES):
                if CODIGO.match(lineas[m]) or lineas[m].startswith(("GRUPO", "CÓDIGO")):
                    break
                tokens.extend(t for t in lineas[m].split(" ") if t)
                m += 1
            yield grupo, codigo, nombre, tokens
            i = m
            continue
        i += 1


def validar(valores):
    """Devuelve la lista de motivos de revisión (vacía si el alimento está bien)."""
    motivos = []
    if len(valores) != len(COMPONENTES):
        motivos.append(f"componentes {len(valores)} de {len(COMPONENTES)}")
        return motivos
    v = dict(zip(COMPONENTES, valores))
    kcal, prot, grasa, carbo = v["energia_kcal"], v["proteina_g"], v["grasa_g"], v["carbohidratos_disponibles_g"]
    if kcal is None:
        motivos.append("sin energia")
    elif None not in (prot, grasa, carbo):
        calculada = 4 * prot + 9 * grasa + 4 * carbo
        if kcal > 0 and abs(calculada - kcal) > 0.25 * kcal + 10:
            motivos.append(f"energia {kcal} no cuadra con macros ({calculada:.0f})")
    return motivos


def main():
    filas, revision = [], []
    for grupo, codigo, nombre, tokens in alimentos(texto_pdf()):
        valores = [numero(t) for t in tokens[: len(COMPONENTES)]]
        motivos = validar(valores + [None] * (len(COMPONENTES) - len(valores)))
        fila = {"grupo": grupo.replace("GRUPO ", ""), "codigo": codigo, "alimento": nombre}
        for nombre_col, valor in zip(COMPONENTES, valores + [None] * (len(COMPONENTES) - len(valores))):
            fila[nombre_col] = "" if valor is None else valor
        filas.append(fila)
        if motivos:
            revision.append({"codigo": codigo, "alimento": nombre, "motivo": "; ".join(motivos)})
    columnas = ["grupo", "codigo", "alimento"] + COMPONENTES
    with open(SALIDA, "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=columnas, delimiter=";")
        w.writeheader()
        w.writerows(filas)
    with open(REVISION, "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=["codigo", "alimento", "motivo"], delimiter=";")
        w.writeheader()
        w.writerows(revision)
    print(f"alimentos: {len(filas)}  para revisar: {len(revision)}")


if __name__ == "__main__":
    main()
