from pathlib import Path
import sqlite3, json, zipfile
root=Path(__file__).resolve().parents[1]/'referencia'
c=sqlite3.connect(':memory:')
c.executescript((root/'Esquema_Sistema.sql').read_text())
results=[]
def check(label,fn):
    fn(); results.append({'prueba':label,'resultado':'correcto'})
def rejected(statement,args=()):
    try: c.execute(statement,args)
    except sqlite3.IntegrityError: return
    raise AssertionError('Operación inválida admitida')
def seed():
    c.execute("INSERT INTO empresa(id,codigo,nombre) VALUES(1,'A','Ejemplo'),(2,'B','Otra empresa')")
    c.execute("INSERT INTO usuario(id,empresa_id,nombre,login,password_hash) VALUES(1,1,'Ejemplo','ejemplo','NO_ES_CREDENCIAL')")
    c.execute("INSERT INTO operacion(id,empresa_id,codigo,nombre) VALUES(1,1,'ORC','Orcopampa'),(2,2,'OTR','Otra')")
    c.execute("INSERT INTO almacen(id,empresa_id,operacion_id,codigo,nombre) VALUES(1,1,1,'P','Principal')")
    c.execute("INSERT INTO unidad_medida(id,empresa_id,codigo,nombre,dimension) VALUES(1,1,'L','Litro','volumen')")
    c.execute("INSERT INTO producto_base(id,empresa_id,codigo,descripcion,unidad_base_id) VALUES(1,1,'ACE','Aceite',1)")
    c.execute("INSERT INTO variante_producto(id,empresa_id,producto_base_id,codigo,descripcion_comercial,tipo_envase,contenido_base_por_envase_u6) VALUES(1,1,1,'ACE4','Aceite 4L','envase',4000000)")
    c.execute("INSERT INTO empaque_compra(id,empresa_id,variante_id,codigo,descripcion,envases_por_empaque) VALUES(1,1,1,'CAJA','Caja 4x4L',4)")
seed()
check('Caja 4 × 4 litros equivale a 16 litros',lambda:exec("assert c.execute('SELECT contenido_base_total_u6 FROM v_empaque_conversion').fetchone()[0]==16000000"))
check('Aislamiento de referencias entre empresas',lambda:rejected("INSERT INTO almacen(empresa_id,operacion_id,codigo,nombre) VALUES(1,2,'X','Inválido')"))
def post(i,qty,value,sign,date='2026-10-02'):
    c.execute("INSERT INTO documento_stock(id,empresa_id,almacen_id,numero,fecha,tipo,estado,usuario_id) VALUES(?,1,1,?,?,?,'confirmado',1)",(i,str(i),date,'apertura' if sign==1 else 'salida_produccion'))
    c.execute("INSERT INTO documento_stock_detalle(id,empresa_id,documento_id,variante_id,cantidad_base_u6,costo_unitario_base_u6,valor_u6) VALUES(?,1,?,1,?,8000000,?)",(i,i,qty,value))
    c.execute("INSERT INTO movimiento_stock(empresa_id,documento_detalle_id,almacen_id,variante_id,fecha,secuencia,signo,cantidad_base_u6,costo_unitario_base_u6,valor_u6,usuario_id) VALUES(1,?,1,1,?,?,?,?,8000000,?,1)",(i,date,i,sign,qty,value))
def stock():
    post(1,32000000,256000000,1)
    post(2,5000000,40000000,-1)
    assert c.execute('SELECT cantidad_base_u6,valor_u6 FROM saldo_stock').fetchone()==(27000000,216000000)
    assert c.execute('SELECT saldo_cantidad_u6,saldo_valor_u6 FROM v_kardex ORDER BY secuencia DESC LIMIT 1').fetchone()==(27000000,216000000)
check('Entrada y salida concilian stock y kárdex',stock)
check('Movimiento duplicado bloqueado',lambda:rejected("INSERT INTO movimiento_stock(empresa_id,documento_detalle_id,almacen_id,variante_id,fecha,secuencia,signo,cantidad_base_u6,costo_unitario_base_u6,valor_u6,usuario_id) SELECT empresa_id,documento_detalle_id,almacen_id,variante_id,fecha,99,signo,cantidad_base_u6,costo_unitario_base_u6,valor_u6,usuario_id FROM movimiento_stock WHERE id=1"))
check('Libro de movimientos inmutable',lambda:rejected('UPDATE movimiento_stock SET valor_u6=1 WHERE id=1'))
def too_much():
    c.execute('SAVEPOINT prueba')
    try:
        post(3,50000000,400000000,-1)
    except sqlite3.IntegrityError:
        c.execute('ROLLBACK TO prueba');c.execute('RELEASE prueba');return
    raise AssertionError('Stock negativo permitido')
check('Salida mayor que existencias bloqueada',too_much)
def closed():
    c.execute("INSERT INTO cierre_diario(empresa_id,operacion_id,fecha,estado,usuario_cierre_id) VALUES(1,1,'2026-10-02','cerrado',1)")
    c.execute('SAVEPOINT prueba')
    try: post(4,1000000,8000000,1)
    except sqlite3.IntegrityError:
        c.execute('ROLLBACK TO prueba');c.execute('RELEASE prueba');return
    raise AssertionError('Día cerrado admitió movimiento')
check('Día cerrado bloquea contabilización',closed)
def inventory():
    c.execute("INSERT INTO inventario(id,empresa_id,almacen_id,numero,fecha_corte,tipo,usuario_id) VALUES(1,1,1,'INV1','2026-10-02','general',1)")
    before=c.execute('SELECT cantidad_base_u6,valor_u6 FROM saldo_stock').fetchone()
    c.execute('INSERT INTO inventario_detalle(empresa_id,inventario_id,variante_id,stock_sistema_u6,fisico_u6,costo_corte_u6) VALUES(1,1,1,27000000,26000000,8000000)')
    assert c.execute('SELECT diferencia_u6,resultado FROM v_diferencias_inventario').fetchone()==(-1000000,'faltante')
    assert c.execute('SELECT cantidad_base_u6,valor_u6 FROM saldo_stock').fetchone()==before
check('Conteo genera diferencia sin ajustar stock',inventory)
check('Integridad y claves foráneas',lambda:exec("assert c.execute('PRAGMA integrity_check').fetchone()[0]=='ok'; assert c.execute('PRAGMA foreign_key_check').fetchall()==[]"))

# Diagnósticos de brechas; cada prueba revierte sus cambios en memoria.
brechas=[]
for nombre,sql in [
 ('Detalle de documento confirmado editable por SQL directo', 'UPDATE documento_stock_detalle SET cantidad_base_u6=31000000 WHERE id=1'),
 ('Saldo editable por SQL directo', 'UPDATE saldo_stock SET cantidad_base_u6=26000000 WHERE variante_id=1'),
 ('Cabecera confirmada editable por SQL directo', "UPDATE documento_stock SET fecha='2026-10-03' WHERE id=1")]:
    c.execute('SAVEPOINT diagnostico')
    try:
        c.execute(sql)
        brechas.append({'hallazgo':nombre,'resultado':'permitido; requiere protección adicional'})
    except sqlite3.IntegrityError:
        brechas.append({'hallazgo':nombre,'resultado':'bloqueado'})
    finally:
        c.execute('ROLLBACK TO diagnostico'); c.execute('RELEASE diagnostico')
print(json.dumps({'tablas':c.execute("SELECT COUNT(*) FROM sqlite_master WHERE type='table'").fetchone()[0], 'pruebas':results,'brechas':brechas,'alcance':'SQLite en memoria; no valida API, interfaz, concurrencia ni sincronización'},ensure_ascii=False,indent=2))
