Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad

''' <summary>Stock por variante del almacén (cantidad base, valor y costo promedio) e inventario inicial valorizado.</summary>
Public Class FormStock
    Inherits Form

    Private ReadOnly _stock As ServicioStock
    Private ReadOnly _apertura As ServicioInventarioInicial
    Private ReadOnly _almacen As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 220}
    Private ReadOnly _buscar As New TextBox With {.Width = 200}
    Private ReadOnly _saldos As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _total As New Label With {.Dock = DockStyle.Bottom, .Height = 28, .Padding = New Padding(6)}

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _stock = New ServicioStock(cadena, sesion)
        _apertura = New ServicioInventarioInicial(cadena, sesion)
        Dim admin As New ServicioAdministracion(cadena, sesion)
        Text = "Stock - " & sesion.Operacion.Nombre
        Dim inicial = Ui.Boton("Inventario inicial...", AddressOf InventarioInicial)
        inicial.Visible = sesion.Tiene(Permisos.StockContabilizar)
        Controls.Add(_saldos)
        Controls.Add(_total)
        Controls.Add(Ui.BarraBotones(New Label With {.Text = "Almacen", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _almacen,
                                     New Label With {.Text = "Buscar", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _buscar,
                                     Ui.Boton("Ver", AddressOf Cargar), inicial))
        AddHandler _almacen.SelectedIndexChanged, Sub() Cargar()
        AddHandler _buscar.KeyDown, Sub(s, e) If e.KeyCode = Keys.Enter Then Cargar()
        AddHandler Load, Sub() Ui.Ejecutar(Me,
                                    Sub()
                                        For Each a In admin.ListarAlmacenes()
                                            _almacen.Items.Add(New Opcion(Of AlmacenResumen)(a, $"{a.Codigo} - {a.Nombre}"))
                                        Next
                                        If _almacen.Items.Count > 0 Then _almacen.SelectedIndex = 0
                                    End Sub)
    End Sub

    Private ReadOnly Property Almacen As AlmacenResumen
        Get
            Return TryCast(_almacen.SelectedItem, Opcion(Of AlmacenResumen))?.Valor
        End Get
    End Property

    Private Sub Cargar()
        If Almacen Is Nothing Then Return
        Ui.Ejecutar(Me,
            Sub()
                Dim saldos = _stock.ConsultarSaldos(Almacen.Id, _buscar.Text)
                Ui.Mostrar(_saldos, saldos, "ProductoDescripcion|Producto", "VarianteDescripcion|Presentacion comercial", "CantidadBaseU6|Cantidad",
                           "Unidad|Unidad", "ValorU6|Valor", "CostoPromedioU6|Costo promedio")
                _total.Text = $"{saldos.Count} variantes con stock; valor total {Ui.Dinero(saldos.Sum(Function(x) x.ValorU6))}"
            End Sub)
    End Sub

    Private Sub InventarioInicial()
        If Almacen Is Nothing Then Return
        Dim texto As String = Nothing
        Using a As New OpenFileDialog With {.Filter = "Inventario inicial (*.csv)|*.csv"}
            If a.ShowDialog(Me) <> DialogResult.OK Then Return
            texto = File.ReadAllText(a.FileName, Encoding.UTF8)
        End Using
        Using d As New DialogoCampos("Inventario inicial de " & Almacen.Nombre)
            d.Fecha("fecha", "Fecha del inventario", Date.Today)
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me,
                Sub()
                    Dim vista = _apertura.VistaPrevia(Almacen.Id, texto)
                    Ui.MostrarLista(Me, "Vista previa del inventario inicial", vista.Resumen, vista.Lineas,
                                    "Linea|Fila", "VarianteCodigo|Codigo", "Descripcion|Producto", "Presentacion|Presentacion", "StockEnvasesU6|Envases",
                                    "PrecioEnvaseU6|Precio envase", "CantidadBaseU6|Cantidad base", "Unidad|Unidad", "ValorU6|Valor", "Problema|Problema")
                    If vista.HayErrores Then Ui.Informar(Me, "No se puede registrar: " & vista.Resumen) : Return
                    If Not Ui.Confirmar(Me, "Registrar la apertura? " & vista.Resumen) Then Return
                    Dim r = _apertura.Aplicar(Almacen.Id, d.FechaElegida("fecha").Value, texto)
                    Ui.Informar(Me, "Inventario inicial registrado. " & r.Resumen)
                End Sub)
        End Using
        Cargar()
    End Sub
End Class
