"""Verifica contra la base todas las consultas SQL literales del código VB (AppSistema.Datos y Escritorio).

Pasos:
1. Extrae cada consulta: une los literales concatenados con & o + (con continuación " _") y toma las que empiezan
   por SELECT, INSERT, UPDATE, DELETE o WITH. Las que se arman con variables (trozos no literales) se marcan como
   parciales y no se cuentan como error.
2. Ejecuta EXPLAIN de cada una (no modifica datos) con psql: los parámetros @nombre se sustituyen por NULL. EXPLAIN
   valida tablas, columnas, funciones y operadores. No valida el tipo de los valores reales ni las filas.

Uso (con la base migrada, por ejemplo creada con el Instalador `migrar`):
    set PGPASSWORD=...   (la clave del usuario indicado; no se guarda en el repositorio)
    python herramientas/verificar_consultas_sql.py BASE [USUARIO] [ROL]

ROL opcional: SET ROLE antes de verificar (app_stock, app_sede, app_sincronizacion) para revisar permisos.
La ruta de psql se toma de PSQL o de la ruta habitual de PostgreSQL 17 en Windows.
Salida: lista de errores por archivo y línea. Código de salida 1 si hay errores.
"""
import os
import re
import subprocess
import sys

RAIZ = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'src')
PSQL = os.environ.get('PSQL', 'C:/Program Files/PostgreSQL/17/bin/psql.exe')
PALABRAS = re.compile(r'^(SELECT|INSERT|UPDATE|DELETE|WITH)\s')


def tokens(texto):
    """Devuelve (tipo, valor, linea, interpolada): 'str' con el contenido, 'op' para & y +, 'otro' para lo demás."""
    i, n, linea = 0, len(texto), 1
    salida = []
    while i < n:
        c = texto[i]
        if c == '\n':
            linea += 1
            i += 1
            continue
        if c in ' \t\r':
            i += 1
            continue
        if c == "'":                                   # comentario hasta fin de línea
            while i < n and texto[i] != '\n':
                i += 1
            continue
        if c == '_' and i > 0 and texto[i - 1] in ' \t' and (i + 1 >= n or texto[i + 1] in ' \t\r\n'):
            i += 1                                     # continuación de línea
            continue
        interpolada = c == '$' and i + 1 < n and texto[i + 1] == '"'
        if c == '"' or interpolada:
            inicio_linea = linea
            i += 2 if interpolada else 1
            buf = []
            profundidad = 0
            while i < n:
                ch = texto[i]
                if interpolada:
                    if ch == '{':
                        profundidad += 1
                    elif ch == '}':
                        profundidad -= 1
                    elif profundidad > 0 and ch == '"':   # cadena anidada dentro de {...}
                        i += 1
                        while i < n and texto[i] != '"':
                            i += 1
                        i += 1
                        continue
                if ch == '\n':
                    linea += 1
                if ch == '"':
                    if i + 1 < n and texto[i + 1] == '"' and profundidad == 0:
                        buf.append('"')
                        i += 2
                        continue
                    break
                buf.append(ch)
                i += 1
            i += 1
            salida.append(('str', ''.join(buf), inicio_linea, interpolada))
            continue
        if c in '&+':
            salida.append(('op', c, linea, False))
            i += 1
            continue
        if c.isalnum() or c in '_.':
            j = i
            while j < n and (texto[j].isalnum() or texto[j] in '_.'):
                j += 1
            salida.append(('otro', texto[i:j], linea, False))
            i = j
            continue
        salida.append(('otro', c, linea, False))
        i += 1
    return salida


def consultas_de(ruta):
    with open(ruta, encoding='utf-8', errors='replace') as f:
        toks = tokens(f.read())
    grupos = []
    k = 0
    while k < len(toks):
        if toks[k][0] != 'str':
            k += 1
            continue
        valor, linea, dinamica = toks[k][1], toks[k][2], toks[k][3]
        k += 1
        while k + 1 < len(toks) and toks[k][0] == 'op' and toks[k + 1][0] == 'str':
            valor += toks[k + 1][1]
            dinamica = dinamica or toks[k + 1][3]
            k += 2
        if k + 1 < len(toks) and toks[k][0] == 'op' and toks[k + 1][0] != 'str':
            dinamica = True                          # sigue un trozo variable: la consulta está incompleta
        grupos.append((valor, linea, dinamica))
    return [{'archivo': os.path.basename(ruta), 'linea': linea, 'sql': valor, 'dinamica': bool(dinamica)}
            for valor, linea, dinamica in grupos if PALABRAS.match(valor.lstrip().upper())]


def ejecutar_explain(consultas, base, usuario, rol):
    lineas = ['\\set ON_ERROR_STOP 0', '\\pset pager off']
    if rol:
        lineas.append('SET ROLE ' + rol + ';')
    for i, c in enumerate(consultas):
        sql = re.sub(r'@[A-Za-z_]\w*', 'NULL', c['sql']).strip().rstrip(';')
        lineas.append(f'\\echo @@{i}')
        lineas.append('EXPLAIN ' + sql.replace('\n', ' ') + ';')
    archivo = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'verificar_tmp.sql')
    with open(archivo, 'w', encoding='utf-8') as f:
        f.write('\n'.join(lineas) + '\n')
    try:
        proc = subprocess.run([PSQL, '-X', '-h', 'localhost', '-U', usuario, '-d', base, '-f', archivo],
                              stdout=subprocess.PIPE, stderr=subprocess.STDOUT, env=os.environ.copy())
    finally:
        os.remove(archivo)
    errores = {}
    actual = None
    for linea in proc.stdout.decode('utf-8', errors='replace').splitlines():
        m = re.match(r'^@@(\d+)$', linea.strip())
        if m:
            actual = int(m.group(1))
        elif actual is not None and 'ERROR:' in linea:
            errores.setdefault(actual, linea.strip())
    return errores


def main(argv):
    if len(argv) < 2:
        print(__doc__)
        return 2
    base = argv[1]
    usuario = argv[2] if len(argv) > 2 else 'postgres'
    rol = argv[3] if len(argv) > 3 else ''
    consultas = []
    for carpeta in ('AppSistema.Datos', 'AppSistema.Escritorio'):
        for ruta in sorted(os.listdir(os.path.join(RAIZ, carpeta))):
            if ruta.endswith('.vb'):
                consultas += consultas_de(os.path.join(RAIZ, carpeta, ruta))
    errores = ejecutar_explain(consultas, base, usuario, rol)
    informe = [dict(c, error=errores.get(i)) for i, c in enumerate(consultas)]
    completas = [x for x in informe if not x['dinamica']]
    con_error = [x for x in completas if x['error']]
    print(f'consultas: {len(consultas)}  completas: {len(completas)}  parciales (no verificadas): {len(consultas) - len(completas)}')
    print(f'con error: {len(con_error)}')
    for x in con_error:
        print(f"  {x['archivo']}:{x['linea']}  {re.sub(r'^psql:.*?: ', '', x['error'])[:160]}")
    return 1 if con_error else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
