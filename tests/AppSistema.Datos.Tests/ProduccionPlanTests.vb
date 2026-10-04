Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Datos

''' <summary>
''' Plan de producción del chef (V025): puede cambiar las raciones a producir desde 3 días atrás en adelante y mientras el
''' día no esté cerrado. Las fechas son relativas a hoy para que la prueba no dependa del día en que corra.
''' </summary>
Public Class ProduccionPlanTests

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    ''' <summary>Una minuta aprobada del almuerzo para la fecha dada, con un plato de sopa de 50 comensales.</summary>
    Private Shared Function MinutaAprobada(bd As BaseDatosPrueba, s As SesionUsuario, fecha As Date, versionReceta As Long) As Long
        Dim minutas As New ServicioMinutas(bd.CadenaAplicacion, s)
        ' El servicio, su asignación y la estructura se crean una sola vez por prueba.
        Dim servicio As Long
        Dim existente = bd.Escalar("SELECT id FROM servicio WHERE codigo = 'ALM'")
        If existente Is Nothing Then servicio = minutas.CrearServicio("ALM", "Almuerzo") Else servicio = Convert.ToInt64(existente)
        Dim osExistente = bd.Escalar($"SELECT id FROM operacion_servicio WHERE servicio_id = {servicio}")
        Dim osId As Long
        If osExistente Is Nothing Then osId = minutas.AsignarServicio(servicio, minutas.CrearRegimen("GEN", "General"), Nothing) Else osId = Convert.ToInt64(osExistente)
        Dim estructuraExistente = bd.Escalar($"SELECT id FROM estructura_servicio WHERE servicio_id = {servicio} AND codigo = 'SOPA'")
        Dim estructura As Long
        If estructuraExistente Is Nothing Then estructura = minutas.CrearEstructura(servicio, "SOPA", "Sopa", 1) Else estructura = Convert.ToInt64(estructuraExistente)
        Dim minuta = minutas.CrearMinuta(osId, fecha, 50)
        minutas.AgregarPlato(minuta, estructura, versionReceta, 50)
        minutas.Aprobar(minuta, "PEN")
        Return minuta
    End Function

    <FactPostgres>
    Public Sub El_chef_cambia_las_raciones_desde_3_dias_atras_y_queda_historial()
        Using bd = BaseDatosPrueba.Crear()
            Dim s = bd.Sesion("A")
            Dim recetas As New ServicioRecetas(bd.CadenaAplicacion, s)
            Dim version = recetas.CrearReceta("SOPA", "Sopa", Nothing, U(1D), Nothing)
            recetas.AgregarIngrediente(version, bd.ProductoAceiteId, U(0.1D), Nothing, 1)
            recetas.Aprobar(version)
            Dim hoyLima = Convert.ToDateTime(bd.Escalar("SELECT (now() AT TIME ZONE 'America/Lima')::date::text"), Globalization.CultureInfo.InvariantCulture).Date
            Dim hoy = MinutaAprobada(bd, s, hoyLima, version)
            Dim detalle = Convert.ToInt64(bd.Escalar($"SELECT id FROM minuta_detalle WHERE minuta_id = {hoy}"))

            Dim produccion As New ServicioProduccion(bd.CadenaAplicacion, s)
            produccion.FijarRacionesProducir(detalle, 60)
            produccion.FijarRacionesProducir(detalle, 61)
            Assert.Equal(61L, Convert.ToInt64(bd.Escalar($"SELECT raciones_producir FROM produccion_plan WHERE minuta_detalle_id = {detalle}")))
            ' Alta (50 de la minuta) + dos cambios: el historial guarda cada uno con su anterior y su nuevo valor.
            Assert.Equal(2L, Convert.ToInt64(bd.Escalar($"SELECT count(*) FROM produccion_plan_cambio WHERE minuta_detalle_id = {detalle}")))
            Assert.Equal(50L, Convert.ToInt64(bd.Escalar($"SELECT raciones_anterior FROM produccion_plan_cambio WHERE minuta_detalle_id = {detalle} ORDER BY id LIMIT 1")))
            ' La pantalla del chef ve el plan vigente: 61 raciones a producir para ese plato.
            Assert.Equal(61L, produccion.PlatosProducibles().Single(Function(x) x.MinutaDetalleId = detalle).RacionesProducir)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Fuera_de_la_ventana_o_con_el_dia_cerrado_no_se_cambia()
        Using bd = BaseDatosPrueba.Crear()
            Dim s = bd.Sesion("A")
            Dim recetas As New ServicioRecetas(bd.CadenaAplicacion, s)
            Dim version = recetas.CrearReceta("SOPA", "Sopa", Nothing, U(1D), Nothing)
            recetas.AgregarIngrediente(version, bd.ProductoAceiteId, U(0.1D), Nothing, 1)
            recetas.Aprobar(version)
            ' "Hoy" es el dia de Lima, el mismo que usa la regla de la base (no el del equipo).
            Dim hoyLima = Convert.ToDateTime(bd.Escalar("SELECT (now() AT TIME ZONE 'America/Lima')::date::text"), Globalization.CultureInfo.InvariantCulture).Date
            Dim antiguo = MinutaAprobada(bd, s, hoyLima.AddDays(-4), version)
            Dim detalleAntiguo = Convert.ToInt64(bd.Escalar($"SELECT id FROM minuta_detalle WHERE minuta_id = {antiguo}"))
            Dim produccion As New ServicioProduccion(bd.CadenaAplicacion, s)

            ' 4 días atrás: fuera de la ventana (solo 3 días atrás en adelante).
            Assert.Equal("FUERA_DE_VENTANA", Assert.Throws(Of ReglaNegocioException)(Sub() produccion.FijarRacionesProducir(detalleAntiguo, 60)).Codigo)

            ' Hoy, con el día cerrado: no se cambia.
            Dim hoy = MinutaAprobada(bd, s, hoyLima, version)
            Dim detalleHoy = Convert.ToInt64(bd.Escalar($"SELECT id FROM minuta_detalle WHERE minuta_id = {hoy}"))
            bd.EjecutarAdmin($"INSERT INTO cierre_diario(empresa_id, operacion_id, fecha, estado) VALUES ({bd.A.EmpresaId}, {bd.A.OperacionId}, '{hoyLima:yyyy-MM-dd}', 'cerrado')")
            Assert.Equal("DIA_CERRADO", Assert.Throws(Of ReglaNegocioException)(Sub() produccion.FijarRacionesProducir(detalleHoy, 60)).Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub El_almacen_no_cambia_la_produccion()
        Using bd = BaseDatosPrueba.Crear()
            Dim hoyLima = Convert.ToDateTime(bd.Escalar("SELECT (now() AT TIME ZONE 'America/Lima')::date::text"), Globalization.CultureInfo.InvariantCulture).Date
            Dim admin = New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            admin.CrearUsuario("almacen", "Almacenero", "Clave-Persona-2026", bd.A.OperacionId, "ALMACEN")
            Dim s = bd.Sesion("A")
            Dim recetas As New ServicioRecetas(bd.CadenaAplicacion, s)
            Dim version = recetas.CrearReceta("SOPA", "Sopa", Nothing, U(1D), Nothing)
            recetas.AgregarIngrediente(version, bd.ProductoAceiteId, U(0.1D), Nothing, 1)
            recetas.Aprobar(version)
            Dim hoy = MinutaAprobada(bd, s, hoyLima, version)
            Dim detalle = Convert.ToInt64(bd.Escalar($"SELECT id FROM minuta_detalle WHERE minuta_id = {hoy}"))
            Dim almacen As New ServicioProduccion(bd.CadenaAplicacion, bd.Sesion("A", "almacen", "Clave-Persona-2026"))
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Sub() almacen.FijarRacionesProducir(detalle, 60)).Codigo)
        End Using
    End Sub

End Class
