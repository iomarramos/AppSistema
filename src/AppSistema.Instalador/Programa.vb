Imports System.IO
Imports System.Text
Imports AppSistema.Datos
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Importacion

''' <summary>
''' Herramienta de instalación de una sede. Usa la conexión del PROPIETARIO de la base
''' (variable APPSISTEMA_CONEXION_PROPIETARIO o se solicita). No guarda claves.
'''   migrar                      aplica las migraciones pendientes
'''   crear-empresa               crea empresa, primera operación, almacén y administrador
'''   crear-usuario-sede NOMBRE   crea el usuario de base que usan las computadoras de la sede
'''   convertir-sgp ARCHIVO [DIR] convierte el listado de productos del SGP (sin base de datos)
'''   importar-sgp ARCHIVO        carga ese listado en el catálogo de una empresa (conexión de sede + usuario)
'''   importar-catalogo ARCHIVO   carga un CSV de catálogo (p. ej. datos/enlace/catalogo_por_ingrediente.csv)
'''   importar-recetas ARCHIVO [--aprobar]  carga recetas_normalizadas.csv (ingredientes como productos base)
''' </summary>
Public Module Programa

    Public Function Main(args As String()) As Integer
        Console.OutputEncoding = Encoding.UTF8
        If args.Length = 0 OrElse args(0) = "-h" OrElse args(0) = "--ayuda" Then
            Ayuda()
            Return If(args.Length = 0, 1, 0)
        End If
        Try
            Select Case args(0)
                Case "convertir-sgp"
                    If args.Length < 2 Then Throw New ReglaNegocioException("DATO_OBLIGATORIO", "Indique el archivo del SGP.")
                    Return ConvertirSgp(args(1), If(args.Length > 2, args(2), Path.GetDirectoryName(Path.GetFullPath(args(1)))))
                Case "importar-sgp"
                    If args.Length < 2 Then Throw New ReglaNegocioException("DATO_OBLIGATORIO", "Indique el archivo del SGP.")
                    Return ImportarSgp(args(1))
                Case "importar-catalogo"
                    If args.Length < 2 Then Throw New ReglaNegocioException("DATO_OBLIGATORIO", "Indique el archivo de catalogo.")
                    Return ImportarSgp(args(1), esListadoSgp:=False)
                Case "importar-recetas"
                    If args.Length < 2 Then Throw New ReglaNegocioException("DATO_OBLIGATORIO", "Indique el archivo de recetas normalizadas.")
                    Return ImportarRecetas(args(1), args.Contains("--aprobar"))
            End Select

            Dim conexion = Environment.GetEnvironmentVariable("APPSISTEMA_CONEXION_PROPIETARIO")
            If String.IsNullOrWhiteSpace(conexion) Then conexion = Pedir("Conexion del propietario (Host=...;Database=...;Username=...;Password=...)", oculto:=True)

            Select Case args(0)
                Case "migrar"
                    For Each m In New Migrador(conexion).Migrar()
                        Console.WriteLine($"  {m.Archivo,-45} {If(m.Aplicada, "APLICADA", "ya estaba")}")
                    Next
                    Dim nuevos = New ServicioInstalacion(conexion).SincronizarPermisos()
                    If nuevos > 0 Then Console.WriteLine($"  {nuevos} permisos nuevos creados y asignados al rol ADMIN.")
                    Console.WriteLine("Migraciones al dia.")

                Case "crear-empresa"
                    Dim d As New DatosInstalacion With {
                        .EmpresaCodigo = Pedir("Codigo de empresa"), .EmpresaNombre = Pedir("Nombre de empresa"),
                        .OperacionCodigo = Pedir("Codigo de la primera operacion"), .OperacionNombre = Pedir("Nombre de la operacion"),
                        .AlmacenCodigo = Pedir("Codigo del almacen"), .AlmacenNombre = Pedir("Nombre del almacen"),
                        .AdminLogin = Pedir("Usuario administrador"), .AdminNombre = Pedir("Nombre del administrador")}
                    d.AdminClave = PedirClaveConfirmada("Clave del administrador")
                    Dim r = New ServicioInstalacion(conexion).CrearEmpresa(d)
                    Console.WriteLine($"Empresa creada (id {r.EmpresaId}). El administrador ya puede iniciar sesion.")

                Case "crear-usuario-sede"
                    If args.Length < 2 Then Throw New ReglaNegocioException("DATO_OBLIGATORIO", "Indique el nombre del usuario de sede.")
                    Call New Migrador(conexion).CrearUsuarioSede(args(1), PedirClaveConfirmada("Clave del usuario de sede"))
                    Console.WriteLine($"Usuario de sede '{args(1)}' listo. Configure esa cuenta en cada computadora (menu Sesion > Conexion).")

                Case Else
                    Ayuda()
                    Return 1
            End Select
            Return 0
        Catch ex As ReglaNegocioException
            Console.Error.WriteLine("ERROR: " & ex.Message)
            Return 2
        Catch ex As Exception
            Console.Error.WriteLine("ERROR inesperado: " & ex.Message)
            Return 3
        End Try
    End Function

    Private Sub Ayuda()
        Console.WriteLine("Uso: AppSistema.Instalador <migrar | crear-empresa | crear-usuario-sede NOMBRE | convertir-sgp ARCHIVO [DIR] | importar-sgp ARCHIVO | importar-catalogo ARCHIVO | importar-recetas ARCHIVO [--aprobar]>")
        Console.WriteLine("La conexion del propietario se toma de APPSISTEMA_CONEXION_PROPIETARIO o se solicita.")
        Console.WriteLine("importar-sgp, importar-catalogo e importar-recetas usan la conexion de sede (APPSISTEMA_CONEXION o se solicita) y un usuario con permiso CATALOGO_IMPORTAR.")
    End Sub

    ''' <summary>Escribe catalogo_sgp.csv (formato del importador) y observaciones_sgp.txt sin tocar la base.</summary>
    Private Function ConvertirSgp(archivo As String, directorio As String) As Integer
        Dim r = ConversorSgp.Convertir(File.ReadAllText(archivo))
        Directory.CreateDirectory(directorio)
        File.WriteAllText(Path.Combine(directorio, "catalogo_sgp.csv"), ConversorSgp.GenerarCsvCatalogo(r), New UTF8Encoding(True))
        Dim obs As New StringBuilder()
        obs.AppendLine("linea;codigo;producto;presentacion;factor;unidad_base;observacion")
        For Each p In r.ConObservacion
            obs.AppendLine(String.Join(";", p.Linea, p.Codigo, p.Nombre.Replace(";", ","), p.Presentacion, p.FactorTexto, p.UnidadBase, p.Observacion.Replace(";", ",")))
        Next
        File.WriteAllText(Path.Combine(directorio, "observaciones_sgp.csv"), obs.ToString(), New UTF8Encoding(True))
        For Each e In r.Errores
            Console.Error.WriteLine("  " & e.ToString())
        Next
        Console.WriteLine($"Productos: {r.Productos.Count} (repetidos identicos omitidos: {r.RepetidasIdenticas}); " &
                          $"KG {r.Productos.Where(Function(p) p.UnidadBase = ConversorSgp.UnidadKg).Count()}, " &
                          $"L {r.Productos.Where(Function(p) p.UnidadBase = ConversorSgp.UnidadLitro).Count()}, " &
                          $"UND {r.Productos.Where(Function(p) p.UnidadBase = ConversorSgp.UnidadConteo).Count()}; " &
                          $"con observacion {r.ConObservacion.Count()}; con error {r.Errores.Count}.")
        Console.WriteLine("Archivos en " & directorio)
        Return If(r.Errores.Count = 0, 0, 2)
    End Function

    ''' <summary>Conexión de sede (APPSISTEMA_CONEXION o se solicita) e inicio de sesión con selección de operación.</summary>
    Private Function IniciarSesionSede(ByRef conexion As String) As SesionUsuario
        conexion = Environment.GetEnvironmentVariable("APPSISTEMA_CONEXION")
        If String.IsNullOrWhiteSpace(conexion) Then conexion = Pedir("Conexion de sede (Host=...;Database=...;Username=...;Password=...)", oculto:=True)
        Dim acceso As New ServicioAcceso(conexion)
        Dim sesion = acceso.IniciarSesion(Pedir("Codigo de empresa"), Pedir("Usuario"), Pedir("Clave", oculto:=True))
        ' El catálogo es de la empresa; la operación solo se pide si el usuario tiene varias.
        If sesion.Operaciones.Count = 0 Then Throw New ReglaNegocioException("OPERACION_NO_SELECCIONADA", "El usuario no tiene operaciones asignadas.")
        Dim operacion = sesion.Operaciones(0)
        If sesion.Operaciones.Count > 1 Then
            Dim codigo = Pedir("Operacion (" & String.Join(", ", sesion.Operaciones.Select(Function(o) o.Codigo)) & ")")
            operacion = sesion.Operaciones.FirstOrDefault(Function(o) o.Codigo = codigo)
            If operacion Is Nothing Then Throw New ReglaNegocioException("DATO_INVALIDO", $"La operacion '{codigo}' no esta disponible.")
        End If
        sesion = acceso.SeleccionarOperacion(sesion, operacion.Id)
        Return sesion
    End Function

    Private Function ImportarRecetas(archivo As String, aprobar As Boolean) As Integer
        Dim texto = File.ReadAllText(archivo)
        Dim conexion As String = Nothing
        Dim sesion = IniciarSesionSede(conexion)
        Dim servicio As New ServicioImportacionRecetas(conexion, sesion)
        Dim vista = servicio.VistaPrevia(texto)
        For Each f In vista.Filas.Where(Function(x) x.Estado = EstadoFilaImportacion.ConError)
            Console.Error.WriteLine($"  Linea {f.Numero}: {f.Detalle}")
        Next
        Console.WriteLine("Vista previa: " & vista.Resumen)
        If vista.HayErrores Then Return 2
        If vista.RecetasNuevas = 0 Then
            Console.WriteLine("Nada que importar: las recetas ya estan cargadas.")
            Return 0
        End If
        If Not Pedir("Escriba SI para importar" & If(aprobar, " y APROBAR las recetas nuevas", " (quedan en borrador)")).Equals("SI", StringComparison.OrdinalIgnoreCase) Then
            Console.WriteLine("Cancelado. No se importo nada.")
            Return 1
        End If
        Console.WriteLine("Importado: " & servicio.Aplicar(texto, aprobar).Resumen)
        Return 0
    End Function

    Private Function ImportarSgp(archivo As String, Optional esListadoSgp As Boolean = True) As Integer
        Dim texto = File.ReadAllText(archivo)
        Dim conexion As String = Nothing
        Dim sesion = IniciarSesionSede(conexion)
        Dim servicio As New ServicioImportacionCatalogo(conexion, sesion)
        Dim vista = If(esListadoSgp, servicio.VistaPreviaSgp(texto), servicio.VistaPrevia(texto, crearUnidadesBase:=True))
        For Each f In vista.Filas.Where(Function(x) x.Estado = EstadoFilaImportacion.ConError)
            Console.Error.WriteLine($"  Linea {f.Numero}: {f.Detalle}")
        Next
        For Each o In vista.Observaciones
            Console.WriteLine("  OBS " & o)
        Next
        Console.WriteLine("Vista previa: " & vista.Resumen)
        If vista.HayErrores Then Return 2
        If Not vista.Filas.Any(Function(f) f.Estado = EstadoFilaImportacion.Nueva) Then
            Console.WriteLine("Nada que importar: el catalogo ya tiene este listado.")
            Return 0
        End If
        If Not Pedir("Escriba SI para importar").Equals("SI", StringComparison.OrdinalIgnoreCase) Then
            Console.WriteLine("Cancelado. No se importo nada.")
            Return 1
        End If
        Console.WriteLine("Importado: " & If(esListadoSgp, servicio.AplicarSgp(texto), servicio.Aplicar(texto, crearUnidadesBase:=True)).Resumen)
        Return 0
    End Function

    Private Function Pedir(texto As String, Optional oculto As Boolean = False) As String
        Console.Write(texto & ": ")
        If Not oculto OrElse Console.IsInputRedirected Then Return If(Console.ReadLine(), "").Trim()
        Dim sb As New StringBuilder()
        Do
            Dim k = Console.ReadKey(True)
            If k.Key = ConsoleKey.Enter Then Exit Do
            If k.Key = ConsoleKey.Backspace Then
                If sb.Length > 0 Then sb.Length -= 1
            ElseIf Not Char.IsControl(k.KeyChar) Then
                sb.Append(k.KeyChar)
            End If
        Loop
        Console.WriteLine()
        Return sb.ToString()
    End Function

    Private Function PedirClaveConfirmada(texto As String) As String
        Dim c1 = Pedir(texto, oculto:=True)
        Dim c2 = Pedir("Repita la clave", oculto:=True)
        If c1 <> c2 Then Throw New ReglaNegocioException("DATO_INVALIDO", "Las claves no coinciden.")
        Return c1
    End Function

End Module
