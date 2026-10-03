Imports System.IO
Imports System.Text
Imports AppSistema.Datos
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Importacion
Imports AppSistema.Dominio.Numerico

''' <summary>
''' Herramienta de instalación de una sede. Usa la conexión del PROPIETARIO de la base
''' (variable APPSISTEMA_CONEXION_PROPIETARIO o se solicita). No guarda claves.
'''   migrar                      aplica las migraciones pendientes
'''   crear-empresa               crea empresa, primera operación, almacén y administrador
'''   crear-usuario-sede NOMBRE   crea el usuario de base que usan las computadoras de la sede
'''   convertir-sgp ARCHIVO [DIR] convierte el listado de productos del SGP (sin base de datos)
'''   importar-sgp ARCHIVO        carga ese listado en el catálogo de una empresa (conexión de sede + usuario)
'''   importar-catalogo ARCHIVO   carga un CSV de catálogo (p. ej. datos/enlace/catalogo_por_ingrediente.csv)
'''   importar-inventario ARCHIVO carga el inventario inicial valorizado (documento de apertura) de un almacén
'''   importar-recetas ARCHIVO [--aprobar]  carga recetas_normalizadas.csv (ingredientes como productos base)
''' Continuidad (etapa 8):
'''   configurar-sede EMPRESA SEDE        activa la cola de salida de esta sede y anota la historia confirmada
'''   sincronizar EMPRESA                 envía la cola a la central (APPSISTEMA_CONEXION_CENTRAL, APPSISTEMA_CREDENCIAL_SEDE)
'''   estado-sincronizacion EMPRESA       pendientes, errores y último envío de la sede
'''   registrar-sede EMPRESA SEDE NOMBRE  (central) registra la sede y muestra su credencial UNA vez
'''   desactivar-sede EMPRESA SEDE        (central) deja de aceptar eventos de esa sede
'''   crear-usuario-sincronizacion NOMBRE (central) usuario de base del agente: solo puede entregar eventos
'''   reporte-central EMPRESA             (central) última sincronización, retenidos, conflictos y stock por sede
'''   respaldar ARCHIVO                   pg_dump consistente + ARCHIVO.conciliacion
'''   restaurar ARCHIVO                   restaura en una base vacía y concilia con ARCHIVO.conciliacion
'''   conciliar                           muestra la fotografía de conciliación de la base
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
                Case "importar-inventario"
                    If args.Length < 2 Then Throw New ReglaNegocioException("DATO_OBLIGATORIO", "Indique el archivo de inventario inicial.")
                    Return ImportarInventario(args(1))
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

                Case "configurar-sede"
                    Requiere(args, 3, "Indique EMPRESA y SEDE.")
                    Dim n = New ServicioContinuidad(conexion).ConfigurarOrigen(args(1), args(2))
                    Console.WriteLine($"Sede '{args(2)}' configurada; {n} eventos historicos anotados en la cola de salida.")

                Case "sincronizar"
                    Requiere(args, 2, "Indique EMPRESA.")
                    Dim central = Environment.GetEnvironmentVariable("APPSISTEMA_CONEXION_CENTRAL")
                    If String.IsNullOrWhiteSpace(central) Then central = Pedir("Conexion de la central (usuario de sincronizacion)", oculto:=True)
                    Dim credencial = Environment.GetEnvironmentVariable("APPSISTEMA_CREDENCIAL_SEDE")
                    If String.IsNullOrWhiteSpace(credencial) Then credencial = Pedir("Credencial de la sede", oculto:=True)
                    Dim r = New ServicioContinuidad(conexion).Enviar(args(1), central, credencial, ignorarEspera:=args.Contains("--ahora"))
                    Console.WriteLine("Sincronizacion: " & r.ToString())
                    If r.Problema IsNot Nothing Then Return 2

                Case "estado-sincronizacion"
                    Requiere(args, 2, "Indique EMPRESA.")
                    Dim e = New ServicioContinuidad(conexion).EstadoCola(args(1))
                    Console.WriteLine($"Sede {If(e.Origen, "(sin configurar)")}: {e.Pendientes} pendientes, {e.ConError} con error, {e.EnConflicto} en conflicto, " &
                                      $"{e.Enviados} enviados; ultimo envio {If(e.UltimoEnvio.HasValue, e.UltimoEnvio.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), "nunca")}" &
                                      If(e.UltimoError Is Nothing, "", "; ultimo error: " & e.UltimoError))

                Case "registrar-sede"
                    Requiere(args, 4, "Indique EMPRESA, SEDE y NOMBRE.")
                    Dim credencial = New ServicioContinuidad(conexion).RegistrarSede(args(1), args(2), String.Join(" ", args.Skip(3)))
                    Console.WriteLine($"Sede '{args(2)}' registrada. Credencial (se muestra solo esta vez; configurela en el servidor de la sede):")
                    Console.WriteLine(credencial)

                Case "desactivar-sede"
                    Requiere(args, 3, "Indique EMPRESA y SEDE.")
                    Call New ServicioContinuidad(conexion).DesactivarSede(args(1), args(2))
                    Console.WriteLine($"Sede '{args(2)}' desactivada: sus envios seran rechazados.")

                Case "crear-usuario-sincronizacion"
                    Requiere(args, 2, "Indique el nombre del usuario.")
                    Call New Migrador(conexion).CrearUsuarioSincronizacion(args(1), PedirClaveConfirmada("Clave del usuario de sincronizacion"))
                    Console.WriteLine($"Usuario '{args(1)}' listo: solo puede entregar eventos a la central.")

                Case "reporte-central"
                    Requiere(args, 2, "Indique EMPRESA.")
                    For Each s In New ServicioContinuidad(conexion).ResumenCentral(args(1))
                        Console.WriteLine($"  {s.Sede,-10} {If(s.Activa, "activa  ", "inactiva")} ultima sincronizacion " &
                                          $"{If(s.UltimaSincronizacion.HasValue, s.UltimaSincronizacion.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), "nunca"),-16} " &
                                          $"secuencia {s.UltimaSecuenciaAplicada}, {s.Documentos} documentos, stock S/ {EscalaU6.ADecimal(s.ValorStockU6):N2}, " &
                                          $"{s.Retenidos} retenidos, {s.Conflictos} en conflicto" & If(s.MotivoRetencion Is Nothing, "", " (" & s.MotivoRetencion & ")"))
                    Next

                Case "respaldar"
                    Requiere(args, 2, "Indique el archivo de respaldo.")
                    Dim foto = New ServicioContinuidad(conexion).Respaldar(args(1))
                    Console.WriteLine($"Respaldo listo: {args(1)} ({foto.Where(Function(kv) kv.Key.StartsWith("filas.")).Sum(Function(kv) Long.Parse(kv.Value))} filas; " &
                                      $"stock valor {foto("saldo.valor_u6")} u6). Conciliacion en {args(1)}.conciliacion")

                Case "restaurar"
                    Requiere(args, 2, "Indique el archivo de respaldo.")
                    Dim diferencias = New ServicioContinuidad(conexion).Restaurar(args(1))
                    If diferencias.Count = 0 Then
                        Console.WriteLine("Restauracion conciliada: mismos recuentos, saldos y referencias que el respaldo.")
                    Else
                        For Each d In diferencias
                            Console.Error.WriteLine("  DIFERENCIA " & d)
                        Next
                        Return 2
                    End If

                Case "conciliar"
                    Console.Write(ServicioContinuidad.TextoInstantanea(New ServicioContinuidad(conexion).Instantanea()))

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

    Private Sub Requiere(args As String(), n As Integer, mensaje As String)
        If args.Length < n Then Throw New ReglaNegocioException("DATO_OBLIGATORIO", mensaje)
    End Sub

    Private Sub Ayuda()
        Console.WriteLine("Uso: AppSistema.Instalador <migrar | crear-empresa | crear-usuario-sede NOMBRE | convertir-sgp ARCHIVO [DIR] | importar-sgp ARCHIVO | importar-catalogo ARCHIVO | importar-recetas ARCHIVO [--aprobar] | importar-inventario ARCHIVO>")
        Console.WriteLine("Continuidad: <configurar-sede EMPRESA SEDE | sincronizar EMPRESA [--ahora] | estado-sincronizacion EMPRESA | registrar-sede EMPRESA SEDE NOMBRE | desactivar-sede EMPRESA SEDE | crear-usuario-sincronizacion NOMBRE | reporte-central EMPRESA | respaldar ARCHIVO | restaurar ARCHIVO | conciliar>")
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

    Private Function ImportarInventario(archivo As String) As Integer
        Dim texto = File.ReadAllText(archivo)
        Dim conexion As String = Nothing
        Dim sesion = IniciarSesionSede(conexion)
        Dim almacenes = New ServicioAdministracion(conexion, sesion).ListarAlmacenes()
        If almacenes.Count = 0 Then Throw New ReglaNegocioException("DATO_OBLIGATORIO", "La operacion no tiene almacenes.")
        Dim almacen = almacenes(0)
        If almacenes.Count > 1 Then
            Dim codigo = Pedir("Almacen (" & String.Join(", ", almacenes.Select(Function(a) a.Codigo)) & ")")
            almacen = almacenes.FirstOrDefault(Function(a) a.Codigo = codigo)
            If almacen Is Nothing Then Throw New ReglaNegocioException("DATO_INVALIDO", $"El almacen '{codigo}' no existe.")
        End If
        Dim textoFecha = Pedir("Fecha del inventario (aaaa-mm-dd)")
        Dim fecha As Date
        If Not Date.TryParseExact(textoFecha, "yyyy-MM-dd", Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.None, fecha) Then
            Throw New ReglaNegocioException("DATO_INVALIDO", "Fecha invalida; use aaaa-mm-dd.")
        End If
        Dim servicio As New ServicioInventarioInicial(conexion, sesion)
        Dim vista = servicio.VistaPrevia(almacen.Id, texto)
        For Each l In vista.Lineas.Where(Function(x) x.Problema IsNot Nothing)
            Console.Error.WriteLine($"  Linea {l.Linea}: {l.Problema}")
        Next
        Console.WriteLine($"Vista previa ({almacen.Codigo}): " & vista.Resumen)
        If vista.HayErrores Then Return 2
        If Not Pedir("Escriba SI para registrar la apertura").Equals("SI", StringComparison.OrdinalIgnoreCase) Then
            Console.WriteLine("Cancelado. No se registro nada.")
            Return 1
        End If
        Dim r = servicio.Aplicar(almacen.Id, fecha, texto)
        Console.WriteLine($"Apertura registrada (documento {r.DocumentoId}): " & r.Resumen)
        Return 0
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
