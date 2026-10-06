Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Carga guiada del juego de datos real (carpeta datos/real, ver LEEME.md) en el orden correcto. Los pasos que ya tienen
''' pantalla propia (catálogo, recetas, inventario inicial) la abren, para no duplicarla; el resto carga el archivo aquí.
''' Todos los pasos son repetibles: lo que ya existe no se duplica ni se pisa. Cada paso exige su propio permiso.
''' </summary>
Partial Public Class FormCargaReal

    Private ReadOnly _cadena As String
    Private ReadOnly _sesion As SesionUsuario
    Private ReadOnly _carga As ServicioCargaReal
    Public Sub New()
        InitializeComponent()
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        _cadena = cadena
        _sesion = sesion
        _carga = New ServicioCargaReal(cadena, sesion)
        Text = "Carga de datos reales - " & sesion.Operacion.Nombre

        Paso(tablaPasos, 1, "Catalogo: productos y presentaciones (listado SGP o catalogo por ingrediente)", Permisos.CatalogoImportar,
             "Abrir importacion de catalogo", Sub() Abrir(New FormImportacion(_cadena, _sesion, ModoImportacion.Catalogo)))
        Paso(tablaPasos, 2, "Familias del SGP: familia, subfamilia y grupo de cada producto (familias_sgp.csv)", Permisos.CatalogoEditar,
             "Cargar familias...", Sub() Cargar("Familias", AddressOf _carga.CargarFamilias))
        Paso(tablaPasos, 3, "Precios por presentacion (precios_sgp.csv). Sin precio no se inventa; un precio ya cargado no se pisa", Permisos.PreciosEditar,
             "Cargar precios...", Sub() Cargar("Precios", AddressOf _carga.ImportarPrecios))
        Paso(tablaPasos, 4, "Producto activo por ingrediente en esta operacion (productos_activos.csv): su precio es el que se costea", Permisos.CatalogoEditar,
             "Liberar productos...", Sub() Cargar("Productos activos", AddressOf _carga.LiberarProductos))
        Paso(tablaPasos, 5, "Recetas con gramaje (recetas_reales.csv)", Permisos.RecetasEditar,
             "Abrir importacion de recetas", Sub() Abrir(New FormImportacion(_cadena, _sesion, ModoImportacion.Recetas)))
        Paso(tablaPasos, 6, "Insumos sin costo de compra, por ejemplo agua para receta (insumos_sin_costo.csv)", Permisos.CatalogoEditar,
             "Marcar insumos...", Sub() Cargar("Insumos sin costo", AddressOf _carga.MarcarInsumosSinCosto))
        Paso(tablaPasos, 7, "Inventario inicial por almacen: en Stock, boton 'Inventario inicial...'", Permisos.StockContabilizar,
             "Abrir stock", Sub() Abrir(New FormStock(_cadena, _sesion)))
        Paso(tablaPasos, 8, "Estructuras de menu con factores de consumo (estructuras_menu.csv)", Permisos.MenusConfigurar,
             "Cargar estructuras...", Sub() Cargar("Estructuras", AddressOf _carga.CargarEstructuras))
        Paso(tablaPasos, 9, "Ciclo de minutas (ciclo_menu.csv) desde una fecha, con los comensales de cada servicio", Permisos.MinutasEditar,
             "Cargar ciclo...", AddressOf CargarCiclo)

        AddHandler Load, Sub() RefrescarEstado()
    End Sub

    ''' <summary>Fila de un paso. Sin el permiso, el botón no aparece y se indica qué permiso falta.</summary>
    Private Sub Paso(tabla As TableLayoutPanel, numero As Integer, descripcion As String, permiso As String, textoBoton As String, accion As Action)
        Dim puede = _sesion.Tiene(permiso)
        tabla.Controls.Add(New Label With {.Text = $"{numero}.", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)})
        tabla.Controls.Add(New Label With {.Text = descripcion & If(puede, "", $" (requiere el permiso {permiso})"), .AutoSize = True,
                                           .Margin = New Padding(3, 9, 12, 3), .Enabled = puede})
        tabla.Controls.Add(Ui.BotonSi(puede, textoBoton, accion))
    End Sub

    Private Sub Abrir(f As Form)
        f.MdiParent = MdiParent
        f.WindowState = FormWindowState.Maximized
        AddHandler f.FormClosed, Sub() RefrescarEstado()
        f.Show()
    End Sub

    Private Function LeerArchivo(titulo As String) As String
        Using d As New OpenFileDialog With {.Title = titulo, .Filter = "CSV (*.csv)|*.csv"}
            If d.ShowDialog(Me) <> DialogResult.OK Then Return Nothing
            Return File.ReadAllText(d.FileName, Encoding.UTF8)
        End Using
    End Function

    Private Sub Cargar(que As String, accion As Func(Of String, ResultadoCargaReal))
        Dim texto = LeerArchivo(que)
        If texto Is Nothing OrElse Not Ui.Confirmar(Me, $"Cargar {que.ToLowerInvariant()} desde el archivo elegido?") Then Return
        Dim r As ResultadoCargaReal = Nothing
        If Ui.Ejecutar(Me, Sub() r = accion(texto)) Then Mostrar(que, r)
        RefrescarEstado()
    End Sub

    Private Sub CargarCiclo()
        Dim texto = LeerArchivo("Ciclo de minutas")
        If texto Is Nothing Then Return
        Dim aprueba = _sesion.Tiene(Permisos.MinutasAprobar)
        Using d As New DialogoCampos("Ciclo de minutas")
            d.Fecha("desde", "Fecha del dia 1", Date.Today).Texto("des", "Comensales desayuno", "0").Texto("alm", "Comensales almuerzo", "0") _
             .Texto("cen", "Comensales cena", "0").Texto("dias", "Dias a cargar (vacio = todo el ciclo)", "")
            If aprueba Then d.Marca("aprobar", "Aprobar las minutas nuevas (costo y venta con los precios vigentes)", True)
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim r As ResultadoCargaReal = Nothing
            If Ui.Ejecutar(Me, Sub()
                                   Dim comensales As New Dictionary(Of String, Long) From {
                                       {"DESAYUNO", Ui.LeerEntero(d.Valor("des"), "Comensales desayuno")},
                                       {"ALMUERZO", Ui.LeerEntero(d.Valor("alm"), "Comensales almuerzo")},
                                       {"CENA", Ui.LeerEntero(d.Valor("cen"), "Comensales cena")}}
                                   Dim dias = If(d.Valor("dias") = "", Integer.MaxValue, CInt(Ui.LeerEntero(d.Valor("dias"), "Dias")))
                                   r = _carga.CargarCiclo(texto, d.FechaElegida("desde").Value, comensales, aprueba AndAlso d.Marcado("aprobar"), dias)
                               End Sub) Then Mostrar("Minutas", r)
        End Using
        RefrescarEstado()
    End Sub

    Private Sub Mostrar(que As String, r As ResultadoCargaReal)
        Dim sb As New StringBuilder()
        sb.AppendLine($"{que}: {r}").AppendLine()
        For Each p In r.Problemas
            sb.AppendLine(p)
        Next
        txtResultado.Text = sb.ToString()
    End Sub

    Private Sub RefrescarEstado()
        If IsDisposed Then Return
        Dim e As EstadoCargaReal = Nothing
        If Not Ui.Ejecutar(Me, Sub() e = _carga.Estado()) Then Return
        lblEstado.Text = $"Empresa: {e.Productos:N0} productos, {e.Presentaciones:N0} presentaciones, {e.PreciosSgp:N0} precios cargados, " &
                       $"{e.RecetasAprobadas:N0} recetas aprobadas, {e.InsumosSinCosto:N0} insumos sin costo." & vbCrLf &
                       $"Presentaciones con familia SGP: {e.PresentacionesConFamilia:N0}. Productos activos en la operacion: {e.ProductosActivos:N0}." & vbCrLf &
                       $"Operacion: inventario inicial en {e.AlmacenesConApertura} de {e.Almacenes} almacen(es), {e.ServiciosAsignados} servicio(s) asignado(s), " &
                       $"{e.Minutas:N0} minutas ({e.MinutasAprobadas:N0} aprobadas)."
    End Sub

End Class
