Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Datos

''' <summary>
''' Matriz mensual de planificación de menús contra PostgreSQL: factores de participación, raciones, comensales de referencia,
''' bloqueo de jornadas aprobadas y lectura después de volver a abrir.
''' </summary>
Public Class PlanificacionMenuDatosTests

    Private Shared ReadOnly Fecha As New Date(2026, 10, 5)

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    ''' <summary>Almuerzo: sopa al 100 % y dos platos de fondo alternativos (60 % y 40 %), con 520 comensales.</summary>
    Private NotInheritable Class Escenario
        Public Bd As BaseDatosPrueba
        Public Minutas As ServicioMinutas
        Public Matriz As ServicioPlanificacionMenu
        Public Recetas As ServicioRecetas
        Public OperacionServicio, Sopa, Fondo, Minuta As Long
        Public RecetaSopa, RecetaFondoA, RecetaFondoB As Long

        Public Sub New(bd As BaseDatosPrueba)
            Me.Bd = bd
            Dim s = bd.Sesion("A")
            Minutas = New ServicioMinutas(bd.CadenaAplicacion, s)
            Matriz = New ServicioPlanificacionMenu(bd.CadenaAplicacion, s)
            Recetas = New ServicioRecetas(bd.CadenaAplicacion, s)
            Dim almuerzo = Minutas.CrearServicio("ALM", "Almuerzo")
            Sopa = Minutas.CrearEstructura(almuerzo, "SOP", "Sopa", 1)
            Fondo = Minutas.CrearEstructura(almuerzo, "FON", "Plato de fondo", 2)
            OperacionServicio = Minutas.AsignarServicio(almuerzo, Minutas.CrearRegimen("GEN", "General"), Nothing)
            RecetaSopa = Receta("SOPA-POLLO", 0.2D)
            RecetaFondoA = Receta("POLLO-A", 0.15D)
            RecetaFondoB = Receta("PESCADO-B", 0.12D)
            Minuta = Minutas.CrearMinuta(OperacionServicio, Fecha, 520)
            Minutas.AgregarPlatoPorFactor(Minuta, Sopa, RecetaSopa)
            Minutas.AgregarPlatoPorFactor(Minuta, Fondo, RecetaFondoA, repartoBp:=6000)
            Minutas.AgregarPlatoPorFactor(Minuta, Fondo, RecetaFondoB, repartoBp:=4000)
        End Sub

        Private Function Receta(codigo As String, litros As Decimal) As Long
            Dim v = Recetas.CrearReceta(codigo, codigo, Nothing, U(1D), Nothing)
            Recetas.AgregarIngrediente(v, Bd.ProductoAceiteId, U(litros), Nothing, 1)
            Recetas.Aprobar(v)
            Return v
        End Function

        Public Function Plato(receta As Long) As Long
            Return Minutas.ListarPlatos(Minuta).Single(Function(p) p.RecetaVersionId = receta).Id
        End Function

        Public Function Mes() As MatrizMensual
            Return Matriz.Matriz(OperacionServicio, 2026, 10)
        End Function
    End Class

    <FactPostgres>
    Public Sub La_matriz_muestra_raciones_por_factor_y_los_comensales_de_referencia()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim mes = e.Mes()

            Assert.Equal(1, mes.Dias.Count)
            Assert.Equal(520L, mes.Dias(0).Comensales)
            Assert.True(mes.Dias(0).Editable)
            ' Sopa al 100 % = 520 raciones sin aumentar los comensales; fondos al 60 % y 40 % = 312 y 208.
            Dim raciones = mes.Dias(0).Platos.OrderBy(Function(p) p.EstructuraOrden).ThenBy(Function(p) p.Fila).Select(Function(p) p.Raciones).ToList()
            Assert.Equal(520L, raciones(0))
            Assert.Equal(312L, raciones(1))
            Assert.Equal(208L, raciones(2))
            ' Una fila por alternativa: la sopa, y dos filas para el plato de fondo.
            Assert.Equal(3, mes.Filas.Count)
            Assert.Equal(520L, mes.ResumenMes().Comensales)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Cambiar_los_factores_a_70_y_30_da_364_y_156_y_los_comensales_no_cambian()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            e.Matriz.FijarFactor(e.Plato(e.RecetaFondoA), 7000)
            e.Matriz.FijarFactor(e.Plato(e.RecetaFondoB), 3000)

            Dim platos = e.Mes().Dias(0).Platos
            Assert.Equal(364L, platos.Single(Function(p) p.RecetaNombre = "POLLO-A").Raciones)
            Assert.Equal(156L, platos.Single(Function(p) p.RecetaNombre = "PESCADO-B").Raciones)
            Assert.Equal(520L, e.Mes().Dias(0).Comensales)
            Assert.Equal(7000L, platos.Single(Function(p) p.RecetaNombre = "POLLO-A").FactorBp)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Los_comensales_se_cambian_con_accion_explicita_y_recalculan_solo_los_platos_con_factor()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            e.Matriz.FijarComensales(e.Minuta, 400)
            Dim platos = e.Mes().Dias(0).Platos
            Assert.Equal(400L, e.Mes().Dias(0).Comensales)
            Assert.Equal(400L, platos.Single(Function(p) p.RecetaNombre = "SOPA-POLLO").Raciones)
            Assert.Equal(240L, platos.Single(Function(p) p.RecetaNombre = "POLLO-A").Raciones)
            Assert.Equal(160L, platos.Single(Function(p) p.RecetaNombre = "PESCADO-B").Raciones)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Una_jornada_aprobada_no_se_edita_ni_siquiera_llamando_al_servicio()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            e.Minutas.Aprobar(e.Minuta, "PEN")
            Dim platoId = e.Plato(e.RecetaFondoA)

            Dim ex = Assert.Throws(Of ReglaNegocioException)(Sub() e.Matriz.FijarFactor(platoId, 7000))
            Assert.Equal("MINUTA_APROBADA", ex.Codigo)
            Assert.Throws(Of ReglaNegocioException)(Sub() e.Matriz.FijarRaciones(platoId, 10))
            Assert.Throws(Of ReglaNegocioException)(Sub() e.Matriz.FijarComensales(e.Minuta, 600))

            ' Lo aprobado queda como estaba: 312 raciones y 520 comensales.
            Assert.Equal(312L, e.Mes().Dias(0).Platos.Single(Function(p) p.RecetaNombre = "POLLO-A").Raciones)
            Assert.Equal(520L, e.Mes().Dias(0).Comensales)
            Assert.Equal("aprobada", e.Mes().Dias(0).Estado)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Lo_guardado_se_recupera_al_volver_a_abrir_la_matriz()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            e.Matriz.FijarFactor(e.Plato(e.RecetaFondoA), 7000)
            e.Matriz.FijarFactor(e.Plato(e.RecetaFondoB), 3000)

            ' Un servicio nuevo, como al cerrar y abrir la pantalla.
            Dim nuevo As New ServicioPlanificacionMenu(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim mes = nuevo.Matriz(e.OperacionServicio, 2026, 10)
            Assert.Equal(364L, mes.Dias(0).Platos.Single(Function(p) p.RecetaNombre = "POLLO-A").Raciones)
            Assert.Equal(156L, mes.Dias(0).Platos.Single(Function(p) p.RecetaNombre = "PESCADO-B").Raciones)
            Assert.Equal(7000L, mes.Dias(0).Platos.Single(Function(p) p.RecetaNombre = "POLLO-A").FactorBp)
        End Using
    End Sub

End Class
