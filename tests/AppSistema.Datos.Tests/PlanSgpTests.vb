Imports System.IO
Imports Xunit
Imports AppSistema.Datos

''' <summary>
''' Plan teórico y real del SGP (V023): el Excel de datos\plan_real queda completo en el servidor y se puede reimportar
''' sin duplicar. Las cifras son las del propio archivo (validacion.txt del conversor).
''' </summary>
Public Class PlanSgpTests

    Private Shared Function CarpetaPlan() As String
        Dim dir = New DirectoryInfo(AppContext.BaseDirectory)
        Do While Not Directory.Exists(Path.Combine(dir.FullName, "datos", "plan_real"))
            dir = dir.Parent
        Loop
        Return Path.Combine(dir.FullName, "datos", "plan_real")
    End Function

    <FactPostgres>
    Public Sub Importa_el_plan_completo_y_reimportar_no_duplica()
        Using bd = BaseDatosPrueba.Crear()
            Dim sesion = bd.Sesion("A")
            Dim primera = New ServicioPlanSgp(bd.CadenaAplicacion, sesion).Importar(CarpetaPlan())

            Assert.Equal(552, primera.PlanDia)
            Assert.Equal(11046, primera.PlanPlato)
            Assert.Equal(2778, primera.Requisicion)
            Assert.Equal(465, primera.ComparativoDia)
            Assert.Equal(11, primera.ComparativoTotal)
            Assert.Equal(18, primera.CostoPisoTecho)
            Assert.Equal(254, primera.Preparaciones)
            Assert.Equal(628, primera.CodigosReceta)
            Assert.Equal(256, primera.CodigosProducto)

            Assert.Equal(11046L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM sgp_plan_plato")))
            Assert.Equal(2778L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM sgp_requisicion")))

            ' Sin recetas cargadas en la base de prueba, ningún código con receta se enlaza: queda la advertencia.
            Assert.Equal(553, primera.Problemas.Where(Function(p) p.StartsWith("Receta del plan")).Count())
            Assert.Equal(628L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM sgp_codigo_receta WHERE receta_id IS NULL")))

            ' Reimportar reemplaza la operación: mismas cifras, sin duplicados.
            Dim segunda = New ServicioPlanSgp(bd.CadenaAplicacion, sesion).Importar(CarpetaPlan())
            Assert.Equal(primera.ToString(), segunda.ToString())
            Assert.Equal(11046L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM sgp_plan_plato")))
            Assert.Equal(2778L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM sgp_requisicion")))
            Assert.Equal(628L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM sgp_codigo_receta")))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Las_cantidades_y_dinero_quedan_en_u6_y_el_porcentaje_en_puntos_basicos()
        Using bd = BaseDatosPrueba.Crear()
            Dim importacion = New ServicioPlanSgp(bd.CadenaAplicacion, bd.Sesion("A")).Importar(CarpetaPlan())
            Assert.Equal(11046, importacion.PlanPlato)
            ' Un plato real del desayuno del 1 de octubre: tiene costo por ración y porcentaje (raciones / comensales) en puntos básicos.
            Dim cifras = Convert.ToString(bd.Escalar(
                "SELECT (costo_racion_u6 IS NOT NULL)::text || '|' || COALESCE(porcentaje_bp::text, 'null') FROM sgp_plan_plato " &
                "WHERE nivel = 'REAL' AND servicio = 'DESAYUNO' AND fecha = '2026-10-01' AND porcentaje_bp IS NOT NULL ORDER BY orden LIMIT 1"))
            ' Orden 2 del desayuno real del 1/10: porcentaje 0,7 = 7000 pb, con costo por ración.
            Assert.Equal("true|7000", cifras)
            Assert.Equal(0L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM sgp_plan_plato WHERE nivel = 'TEORICO' AND porcentaje_bp IS NOT NULL")))
        End Using
    End Sub

End Class
