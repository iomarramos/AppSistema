Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports AppSistema.Datos
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Importacion
Imports AppSistema.Dominio.Seguridad
Imports Npgsql

''' <summary>
''' Asistente de instalación: es lo que hace AppSistema.Instalador.exe sin argumentos (o con «instalar»). Crea la base de
''' datos en el PostgreSQL del servidor, aplica las migraciones, crea el usuario con el que trabajan las computadoras,
''' la empresa, la operación, el almacén y el superusuario, y carga todos los datos de la carpeta datos\ (los CSV que
''' se generan a partir de los Excel con herramientas\). Todo se hace desde .NET: no necesita psql, dotnet ni PowerShell.
''' Las claves se piden en la consola y no se guardan ni se muestran al final.
''' </summary>
Public NotInheritable Class AsistenteInstalacion

    Private Shared ReadOnly PatronIdentificador As New Regex("^[a-z][a-z0-9_]{0,62}$")
    Private Shared ReadOnly PatronCodigo As New Regex("^[A-Za-z0-9_-]{1,20}$")

    ''' <summary>Archivos que se cargan, en orden (relativos a la carpeta de datos).</summary>
    Private Shared ReadOnly Archivos As String() = {
        "enlace\catalogo_por_ingrediente.csv",
        "real\familias_sgp.csv",
        "real\precios_sgp.csv",
        "real\recetas_reales.csv",
        "real\insumos_sin_costo.csv",
        "real\productos_activos.csv",
        "inventario\inventario_inicial.csv",
        "real\estructuras_menu.csv",
        "real\ciclo_menu.csv"}

    Private Const ArchivoCatalogo As Integer = 0
    Private Const ArchivoFamilias As Integer = 1
    Private Const ArchivoPrecios As Integer = 2
    Private Const ArchivoRecetas As Integer = 3
    Private Const ArchivoSinCosto As Integer = 4
    Private Const ArchivoActivos As Integer = 5
    Private Const ArchivoInventario As Integer = 6
    Private Const ArchivoEstructuras As Integer = 7
    Private Const ArchivoCiclo As Integer = 8

    ''' <summary>Archivos del plan teórico y real del SGP (datos\plan_real, convertidos del Excel), cargados en la referencia del plan (V023).</summary>
    Private Shared ReadOnly ArchivosPlan As String() = {
        ServicioPlanSgp.ArchivoCodigosRecetas, ServicioPlanSgp.ArchivoCodigosProductos, ServicioPlanSgp.ArchivoMenuDias,
        ServicioPlanSgp.ArchivoMenuPlanificado, ServicioPlanSgp.ArchivoRequisicion, ServicioPlanSgp.ArchivoComparativoCostos,
        ServicioPlanSgp.ArchivoComparativoTotales, ServicioPlanSgp.ArchivoCostoPisoTecho, ServicioPlanSgp.ArchivoPreparacion}

    ''' <summary>Avisos que no detienen la instalación (p. ej. filas con problema) y se muestran al final.</summary>
    Private Shared ReadOnly _avisos As New List(Of String)

    Public Shared Function Ejecutar() As Integer
        Dim codigo As Integer
        Try
            codigo = Instalar()
        Catch ex As ReglaNegocioException
            Console.Error.WriteLine()
            Console.Error.WriteLine("ERROR: " & ex.Message)
            codigo = 2
        Catch ex As Exception
            Console.Error.WriteLine()
            Console.Error.WriteLine("ERROR inesperado: " & ex.Message)
            codigo = 3
        End Try
        Console.WriteLine()
        If Not Console.IsInputRedirected Then
            Console.Write("Presione Enter para salir...")
            Console.ReadLine()
        End If
        Return codigo
    End Function

    Private Shared Function Instalar() As Integer
        Console.WriteLine("============================================================")
        Console.WriteLine("  AppSistema - asistente de instalacion")
        Console.WriteLine("============================================================")
        Console.WriteLine("Crea la base de datos, la conecta y carga los datos. Se puede repetir: lo que ya existe se conserva.")

        ' 1. Servidor
        Paso("1. Servidor PostgreSQL")
        Dim host = Leer("Servidor", "localhost")
        Dim puerto As Integer
        If Not Integer.TryParse(Leer("Puerto", "5432"), puerto) Then Throw New ReglaNegocioException("DATO_INVALIDO", "El puerto debe ser un numero.")
        Dim admin = Leer("Usuario administrador de PostgreSQL", "postgres")
        Dim claveAdmin = Leer("Clave de " & admin & " (Enter si el servidor no la pide)", "", oculto:=True)
        Dim cadenaServidor = Cadena(host, puerto, "postgres", admin, claveAdmin)
        Dim version = CInt(Consulta(cadenaServidor, "SHOW server_version_num"))
        If version < 160000 Then Throw New ReglaNegocioException("VERSION_NO_SOPORTADA", "Se requiere PostgreSQL 16 o superior.")
        Console.WriteLine($"Conectado a PostgreSQL {version \ 10000}.{version Mod 10000}.")

        ' 2. Base de datos
        Paso("2. Base de datos")
        Dim bd = Leer("Nombre de la base de datos", "appsistema")
        Validar(bd, "nombre de la base de datos")
        If Existe(cadenaServidor, "SELECT 1 FROM pg_database WHERE datname = " & Lit(bd)) Then
            Console.WriteLine($"La base '{bd}' ya existe: se conserva y solo se aplican las migraciones pendientes.")
            If Leer("Para borrarla y empezar de cero escriba RECREAR (Enter para conservarla)", "") = "RECREAR" Then
                Comando(cadenaServidor, $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = {Lit(bd)} AND pid <> pg_backend_pid()")
                Comando(cadenaServidor, $"DROP DATABASE ""{bd}""")
                Comando(cadenaServidor, $"CREATE DATABASE ""{bd}""")
                Console.WriteLine($"Base '{bd}' recreada desde cero.")
            End If
        Else
            Comando(cadenaServidor, $"CREATE DATABASE ""{bd}""")
            Console.WriteLine($"Base '{bd}' creada.")
        End If

        ' 3. Migraciones
        Paso("3. Estructura de la base (migraciones)")
        Dim cadenaDueno = Cadena(host, puerto, bd, admin, claveAdmin)
        For Each m In New Migrador(cadenaDueno).Migrar()
            Console.WriteLine($"  {m.Archivo,-45} {If(m.Aplicada, "APLICADA", "ya estaba")}")
        Next
        Dim permisosNuevos = New ServicioInstalacion(cadenaDueno).SincronizarPermisos()
        If permisosNuevos > 0 Then Console.WriteLine($"  {permisosNuevos} permisos nuevos asignados al rol ADMIN.")
        Console.WriteLine("Migraciones al dia.")

        ' 4. Usuario de la aplicación
        Paso("4. Usuario de la base para las computadoras")
        Dim usuarioApp = Leer("Usuario de la base", "app_sede")
        Validar(usuarioApp, "usuario de la base")
        Dim claveApp = PedirClaveConfirmada("Clave del usuario " & usuarioApp)
        Dim migrador As New Migrador(cadenaDueno)
        migrador.CrearUsuarioSede(usuarioApp, claveApp)
        Comando(cadenaDueno, $"GRANT CONNECT ON DATABASE ""{bd}"" TO ""{usuarioApp}""")
        Console.WriteLine($"Usuario '{usuarioApp}' listo y con acceso a la base '{bd}'.")

        ' 5. Empresa
        Paso("5. Empresa, operacion y almacen")
        Dim empresa = LeerCodigo("Codigo de la empresa", "DEMO")
        If Existe(cadenaDueno, "SELECT 1 FROM empresa WHERE codigo = " & Lit(empresa)) Then
            Console.WriteLine($"La empresa '{empresa}' ya existe; se conserva.")
        Else
            Dim d As New DatosInstalacion With {
                .EmpresaCodigo = empresa,
                .EmpresaNombre = Leer("Nombre de la empresa", "Empresa Demo"),
                .OperacionCodigo = LeerCodigo("Codigo de la primera operacion", "ORC"),
                .OperacionNombre = Leer("Nombre de la operacion", "Orcopampa"),
                .AlmacenCodigo = LeerCodigo("Codigo del almacen", "ALM"),
                .AlmacenNombre = Leer("Nombre del almacen", "Almacen Principal"),
                .AdminLogin = Leer("Usuario administrador de AppSistema", "admin"),
                .AdminNombre = Leer("Nombre del administrador", "Administrador")}
            d.AdminClave = PedirClaveConfirmada("Clave del administrador " & d.AdminLogin)
            Dim r = New ServicioInstalacion(cadenaDueno).CrearEmpresa(d)
            Console.WriteLine($"Empresa '{empresa}' creada (id {r.EmpresaId}); el administrador ya puede entrar.")
        End If

        ' 6. Superusuario
        Paso("6. Superusuario del sistema (dueno)")
        Dim dueno = Leer("Usuario del dueno del sistema")
        Validar(dueno, "usuario del dueno")
        Dim nombreDueno = Leer("Nombre del dueno del sistema")
        If Existe(cadenaDueno, $"SELECT 1 FROM usuario u JOIN empresa e ON e.id = u.empresa_id WHERE e.codigo = {Lit(empresa)} AND u.login = {Lit(dueno)}") Then
            Console.WriteLine($"El dueno '{dueno}' ya existe; se conserva.")
        Else
            Dim claveDueno = PedirClaveConfirmada("Clave personal del dueno " & dueno)
            Dim instalacion As New ServicioInstalacion(cadenaDueno)
            instalacion.CrearDueno(empresa, dueno, nombreDueno, claveDueno)
            Console.WriteLine($"Dueno '{dueno}' creado: entra a todas las operaciones.")
        End If

        ' 7. Datos
        Paso("7. Datos de la empresa (carpeta datos\)")
        Dim carpeta = Leer("Carpeta de datos", CarpetaDatosPorDefecto())
        Dim faltan = Archivos.Where(Function(a) Not File.Exists(RutaArchivo(carpeta, a))).ToList()
        faltan.AddRange(ArchivosPlan.Where(Function(a) Not File.Exists(Path.Combine(carpeta, "plan_real", a))).Select(Function(a) "plan_real\" & a))
        If faltan.Count > 0 Then
            Throw New ReglaNegocioException("ARCHIVO_FALTANTE", "Faltan en la carpeta de datos: " & String.Join(", ", faltan))
        End If
        If Leer("Cargar ahora los datos (SI/NO)", "SI").ToUpperInvariant() = "SI" Then
            Dim claveSesion = Leer("Clave del dueno " & dueno & " (solo para cargar los datos; no se guarda)", oculto:=True)
            Dim cadenaApp = Cadena(host, puerto, bd, usuarioApp, claveApp)
            Dim sesion = New ServicioAcceso(cadenaApp).IniciarSesion(empresa, dueno, claveSesion)
            sesion = New ServicioAcceso(cadenaApp).SeleccionarOperacion(sesion, sesion.Operaciones(0).Id)
            CargarDatos(cadenaApp, sesion, carpeta)
        Else
            Console.WriteLine("Carga de datos omitida. Puede hacerla despues con el mismo asistente.")
        End If

        ' 8. Conciliación
        Paso("8. Conciliacion de la base")
        Console.Write(ServicioContinuidad.TextoInstantanea(New ServicioContinuidad(cadenaDueno).Instantanea()))

        ' Resumen
        Console.WriteLine()
        Console.WriteLine("============================================================")
        Console.WriteLine("  INSTALACION TERMINADA")
        Console.WriteLine("============================================================")
        If _avisos.Count > 0 Then
            Console.WriteLine("Avisos (revisar):")
            For Each a In _avisos
                Console.WriteLine("  - " & a)
            Next
            Console.WriteLine()
        End If
        Console.WriteLine("En cada computadora, en la ventana Conexion, ingrese:")
        Console.WriteLine($"  Servidor: {host}   Puerto: {puerto}   Base: {bd}")
        Console.WriteLine($"  Usuario de la base: {usuarioApp}   (la clave que eligio)")
        Console.WriteLine($"  Empresa: {empresa}   Usuario: el que eligio (administrador o dueno)")
        Return 0
    End Function

    ''' <summary>Carga los datos en el orden de dependencias: catálogo, familias, precios, recetas, insumos, productos activos, inventario, estructuras y ciclo.</summary>
    Private Shared Sub CargarDatos(cadena As String, sesion As SesionUsuario, carpeta As String)
        Dim Texto = Function(indice As Integer) File.ReadAllText(RutaArchivo(carpeta, Archivos(indice)), Encoding.UTF8)

        Paso("7.1 Catalogo de productos e ingredientes")
        Dim catalogo = New ServicioImportacionCatalogo(cadena, sesion)
        Dim textoCatalogo = Texto(ArchivoCatalogo)
        Dim vistaCatalogo = catalogo.VistaPrevia(textoCatalogo, crearUnidadesBase:=True)
        ComprobarSinErrores(vistaCatalogo.Filas.Where(Function(f) f.Estado = EstadoFilaImportacion.ConError).Select(Function(f) $"Linea {f.Numero}: {f.Detalle}"), vistaCatalogo.Resumen)
        If vistaCatalogo.Filas.Any(Function(f) f.Estado = EstadoFilaImportacion.Nueva) Then
            Console.WriteLine("Importado: " & catalogo.Aplicar(textoCatalogo, crearUnidadesBase:=True).Resumen)
        Else
            Console.WriteLine("Ya estaba cargado.")
        End If

        Paso("7.2 Familias de productos")
        Mostrar("Familias", New ServicioCargaReal(cadena, sesion).CargarFamilias(Texto(ArchivoFamilias)))

        Paso("7.3 Precios de compra")
        Mostrar("Precios", New ServicioCargaReal(cadena, sesion).ImportarPrecios(Texto(ArchivoPrecios)))

        Paso("7.4 Recetas")
        Dim recetas = New ServicioImportacionRecetas(cadena, sesion)
        Dim textoRecetas = Texto(ArchivoRecetas)
        Dim vistaRecetas = recetas.VistaPrevia(textoRecetas)
        ComprobarSinErrores(vistaRecetas.Filas.Where(Function(f) f.Estado = EstadoFilaImportacion.ConError).Select(Function(f) $"Linea {f.Numero}: {f.Detalle}"), vistaRecetas.Resumen)
        If vistaRecetas.RecetasNuevas = 0 Then
            Console.WriteLine("Ya estaban cargadas.")
        Else
            Console.WriteLine("Importado (aprobadas): " & recetas.Aplicar(textoRecetas, aprobar:=True).Resumen)
        End If

        Paso("7.5 Insumos sin costo y productos activos")
        Mostrar("Insumos sin costo", New ServicioCargaReal(cadena, sesion).MarcarInsumosSinCosto(Texto(ArchivoSinCosto)))
        Mostrar("Productos activos", New ServicioCargaReal(cadena, sesion).LiberarProductos(Texto(ArchivoActivos)))

        Paso("7.6 Inventario inicial (apertura)")
        Dim almacen = New ServicioAdministracion(cadena, sesion).ListarAlmacenes().First()
        Dim textoInventario = Texto(ArchivoInventario)
        Dim inventario = New ServicioInventarioInicial(cadena, sesion)
        Dim apertura = inventario.VistaPrevia(almacen.Id, textoInventario)
        If apertura.Bloqueo IsNot Nothing Then
            Console.WriteLine("Omitido: " & apertura.Bloqueo)
        Else
            ComprobarSinErrores(apertura.Lineas.Where(Function(l) l.Problema IsNot Nothing).Select(Function(l) $"Linea {l.Linea}: {l.Problema}"), apertura.Resumen)
            Dim fecha = LeerFecha("Fecha del inventario (aaaa-mm-dd)")
            Console.WriteLine($"Apertura del almacen {almacen.Codigo}: " & inventario.Aplicar(almacen.Id, fecha, textoInventario).Resumen)
        End If

        Paso("7.7 Estructuras de servicio")
        Mostrar("Estructuras", New ServicioCargaReal(cadena, sesion).CargarEstructuras(Texto(ArchivoEstructuras)))

        Paso("7.8 Ciclo de menu (minutas)")
        Dim desde = LeerFecha("Inicio del ciclo (aaaa-mm-dd)", "2026-10-05")
        Dim comensales As New Dictionary(Of String, Long) From {
            {"DESAYUNO", LeerEntero("Comensales de desayuno", 500)},
            {"ALMUERZO", LeerEntero("Comensales de almuerzo", 500)},
            {"CENA", LeerEntero("Comensales de cena", 300)}}
        Mostrar("Minutas del ciclo", New ServicioCargaReal(cadena, sesion).CargarCiclo(Texto(ArchivoCiclo), desde, comensales, aprobar:=True))

        Paso("7.9 Plan teorico y real (Excel de plan_real)")
        Dim plan = New ServicioPlanSgp(cadena, sesion).Importar(Path.Combine(carpeta, "plan_real"))
        For Each p In plan.Problemas.Take(30)
            Console.Error.WriteLine("  " & p)
        Next
        If plan.Problemas.Count > 0 Then _avisos.Add($"Plan teorico y real: {plan.Problemas.Count} codigos sin enlace a AppSistema (ver la lista arriba).")
        Console.WriteLine("Plan teorico y real: " & plan.ToString())
    End Sub

    ' ---------- Entrada y salida de consola ----------

    Private Shared Sub Paso(titulo As String)
        Console.WriteLine()
        Console.WriteLine("---- " & titulo)
    End Sub

    ''' <summary>Pide un dato. Sin defecto es obligatorio; con defecto "" es opcional.</summary>
    Private Shared Function Leer(texto As String, Optional defecto As String = Nothing, Optional oculto As Boolean = False) As String
        Do
            Console.Write(If(defecto Is Nothing, texto & ": ", $"{texto} [{defecto}]: "))
            Dim valor = LeerLinea(oculto).Trim()
            If valor <> "" Then Return valor
            If defecto IsNot Nothing Then Return defecto
            Console.WriteLine("  Este dato es obligatorio.")
        Loop
    End Function

    Private Shared Function LeerLinea(oculto As Boolean) As String
        If Not oculto OrElse Console.IsInputRedirected Then
            Dim linea = Console.ReadLine()
            If linea Is Nothing Then Throw New ReglaNegocioException("ENTRADA_CERRADA", "Se cerró la entrada antes de completar la instalación.")
            Return linea
        End If
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

    Private Shared Function LeerCodigo(texto As String, defecto As String) As String
        Do
            Dim valor = Leer(texto, defecto)
            If PatronCodigo.IsMatch(valor) Then Return valor
            Console.WriteLine("  Use letras, números, guion o guion bajo (máximo 20).")
        Loop
    End Function

    Private Shared Function LeerFecha(texto As String, Optional defecto As String = Nothing) As Date
        Do
            Dim valor = Leer(texto, defecto)
            Dim fecha As Date
            If Date.TryParseExact(valor, "yyyy-MM-dd", Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.None, fecha) Then Return fecha
            Console.WriteLine("  Use el formato aaaa-mm-dd.")
        Loop
    End Function

    Private Shared Function LeerEntero(texto As String, defecto As Long) As Long
        Do
            Dim valor As Long
            If Long.TryParse(Leer(texto, defecto.ToString()), valor) AndAlso valor >= 0 Then Return valor
            Console.WriteLine("  Escriba un número mayor o igual a cero.")
        Loop
    End Function

    Private Shared Function PedirClaveConfirmada(texto As String) As String
        Do
            Dim c1 = Leer(texto, oculto:=True)
            Try
                PoliticaClave.Validar(c1)
            Catch ex As ReglaNegocioException
                Console.WriteLine("  " & ex.Message)
                Continue Do
            End Try
            If Leer("Repita la clave", oculto:=True) = c1 Then Return c1
            Console.WriteLine("  Las claves no coinciden.")
        Loop
    End Function

    Private Shared Sub Validar(valor As String, que As String)
        If Not PatronIdentificador.IsMatch(valor) Then
            Throw New ReglaNegocioException("DATO_INVALIDO", $"El {que} debe empezar con letra minúscula y tener solo letras minúsculas, números o guion bajo.")
        End If
    End Sub

    Private Shared Sub ComprobarSinErrores(errores As IEnumerable(Of String), resumen As String)
        Console.WriteLine("Vista previa: " & resumen)
        Dim lista = errores.ToList()
        For Each e In lista.Take(30)
            Console.Error.WriteLine("  " & e)
        Next
        If lista.Count > 0 Then
            Throw New ReglaNegocioException("DATOS_CON_ERROR", $"Hay {lista.Count} filas con error; corríjalas en el archivo y repita la instalación.")
        End If
    End Sub

    Private Shared Sub Mostrar(nombre As String, r As ResultadoCargaReal)
        For Each p In r.Problemas.Take(30)
            Console.Error.WriteLine("  " & p)
        Next
        If r.Problemas.Count > 30 Then Console.Error.WriteLine($"  ... y {r.Problemas.Count - 30} más")
        Console.WriteLine($"{nombre}: {r}")
        If r.Problemas.Count > 0 Then _avisos.Add($"{nombre}: {r.Problemas.Count} filas con problema (se omitieron).")
    End Sub

    ' ---------- Conexión ----------

    Private Shared Function Cadena(host As String, puerto As Integer, bd As String, usuario As String, clave As String) As String
        Return New NpgsqlConnectionStringBuilder With {
            .Host = host, .Port = puerto, .Database = bd, .Username = usuario, .Password = clave}.ConnectionString
    End Function

    Private Shared Function Consulta(cadena As String, sql As String) As String
        Try
            Using cn As New NpgsqlConnection(cadena)
                cn.Open()
                Using cmd As New NpgsqlCommand(sql, cn)
                    Return Convert.ToString(cmd.ExecuteScalar(), Globalization.CultureInfo.InvariantCulture)
                End Using
            End Using
        Catch ex As NpgsqlException
            Throw New ReglaNegocioException("SIN_CONEXION", "No se pudo conectar a PostgreSQL. Verifique que el servidor esté instalado y en ejecución, el servidor, el puerto y la clave. Detalle: " & ex.Message)
        End Try
    End Function

    Private Shared Function Existe(cadena As String, sql As String) As Boolean
        Return Consulta(cadena, $"SELECT EXISTS ({sql})") = "True"
    End Function

    Private Shared Sub Comando(cadena As String, sql As String)
        Try
            Using cn As New NpgsqlConnection(cadena)
                cn.Open()
                Using cmd As New NpgsqlCommand(sql, cn)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As NpgsqlException
            Throw New ReglaNegocioException("BASE_DE_DATOS", ex.Message)
        End Try
    End Sub

    Private Shared Function Lit(valor As String) As String
        Return "'" & valor.Replace("'", "''") & "'"
    End Function

    ''' <summary>Ruta de un archivo de la lista (escrita con \) dentro de la carpeta de datos, con el separador del sistema.</summary>
    Private Shared Function RutaArchivo(carpeta As String, relativo As String) As String
        Return Path.Combine(carpeta, relativo.Replace("\"c, Path.DirectorySeparatorChar))
    End Function

    ''' <summary>La carpeta datos\ junto al ejecutable, la actual o una superior (desarrollo).</summary>
    Private Shared Function CarpetaDatosPorDefecto() As String
        Dim candidatas As New List(Of String) From {Path.Combine(AppContext.BaseDirectory, "datos"), Path.Combine(Directory.GetCurrentDirectory(), "datos")}
        Dim dir = New DirectoryInfo(AppContext.BaseDirectory)
        For i = 1 To 5
            If dir.Parent Is Nothing Then Exit For
            dir = dir.Parent
            candidatas.Add(Path.Combine(dir.FullName, "datos"))
        Next
        Dim encontrada = candidatas.FirstOrDefault(Function(c) File.Exists(Path.Combine(c, "real", "ciclo_menu.csv")))
        Return If(encontrada, candidatas(0))
    End Function

End Class
