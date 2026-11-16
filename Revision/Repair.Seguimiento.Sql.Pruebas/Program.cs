using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Repair.Seguimiento.Sql.Pruebas
{
    internal static class Program
    {
        private const string Diagnostico = "Prueba ficticia: ventilador detenido bajo carga.";
        private const string Observaciones = "Prueba ficticia: verificar temperatura tras la reparación.";
        private static readonly StringBuilder informe = new StringBuilder();

        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            bool procesoNuevo = args.Length != 0;
            int resultado;
            try
            {
                var configuracion = new SqlConnectionStringBuilder(ConsultaOrdenesSql.CadenaConfigurada());
                var persistencia = new PersistenciaSeguimientoSql(configuracion.ConnectionString);
                if (procesoNuevo)
                {
                    if (args.Length != 3 || args[0] != "--reabrir" ||
                        !int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out int numero) || numero <= 0 ||
                        !long.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out long ticks))
                    {
                        throw new ArgumentException("Argumentos de reapertura inválidos.");
                    }

                    var reabierta = ConsultaOrdenesSql.ConsultarAsync(configuracion.ConnectionString, numero).GetAwaiter().GetResult();
                    Confirmar(ConservaEntrega(reabierta, ticks), "Otro proceso recuperó el diagnóstico, el estado y la primera fecha desde SQL.");
                }
                else
                {
                    Probar(configuracion, persistencia).GetAwaiter().GetResult();
                }

                resultado = 0;
            }
            catch (SqlException ex)
            {
                Anotar($"PENDIENTE · SQL Server devolvió el código {ex.Number}. Usa tu sesión normal de Windows y confirma la conexión al motor y el acceso a la base de pruebas.");
                resultado = 1;
            }
            catch (TypeInitializationException ex)
            {
                Anotar($"PENDIENTE · Windows no pudo iniciar el cliente SQL en esta sesión ({ex.GetBaseException().GetType().Name}).");
                resultado = 1;
            }
            catch (Exception ex)
            {
                Anotar("PENDIENTE · " + ex.Message);
                resultado = 1;
            }

            if (!procesoNuevo)
            {
                string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resultado-sql.txt");
                File.WriteAllText(ruta, informe.ToString(), Encoding.UTF8);
            }

            return resultado;
        }

        private static async Task Probar(SqlConnectionStringBuilder configuracion, PersistenciaSeguimientoSql persistencia)
        {
            Anotar($"Verificación SQL de Repair · {DateTimeOffset.Now:g}");
            string cadena = configuracion.ConnectionString;
            configuracion.InitialCatalog = "master";
            await Ejecutar(configuracion.ConnectionString, LeerScript("BaseExclusiva.sql"));
            await Ejecutar(cadena, LeerScript("EsquemaDePruebas.sql"));
            Anotar("OK · Base exclusiva y esquema de pruebas comprobados.");

            var (numero, numeroLegado) = await CrearOrdenesFicticias(cadena);
            Anotar($"Órdenes ficticias para la revisión: vinculada {numero}, sin vínculo {numeroLegado}.");
            var vinculada = await ConsultaOrdenesSql.ConsultarAsync(cadena, numero);
            var sinVinculo = await ConsultaOrdenesSql.ConsultarAsync(cadena, numeroLegado);
            Confirmar(vinculada?.Estado == FlujoOrden.EnDiagnostico && vinculada.Equipo == "Lenovo ThinkPad de prueba" &&
                vinculada.Diagnostico == Diagnostico && vinculada.EntregadaUtc == null,
                "Lectura SQL recupera equipo vinculado y diagnóstico en la orden inicial.");
            Confirmar(sinVinculo != null && sinVinculo.Equipo == null && vinculada.Cliente == sinVinculo.Cliente,
                "La orden antigua conserva el cliente sin recibir un equipo por aproximación.");

            Confirmar(!await persistencia.IntentarAvanzarAsync(numero, FlujoOrden.EnDiagnostico, FlujoOrden.Entregada), "Se rechaza entregar directamente desde diagnóstico.");
            Confirmar(!await persistencia.IntentarAvanzarAsync(numeroLegado, FlujoOrden.EnDiagnostico, FlujoOrden.EnReparacion), "SQL rechaza avanzar la orden sin vínculo.");
            Confirmar(await persistencia.IntentarAvanzarAsync(numero, FlujoOrden.EnDiagnostico, FlujoOrden.EnReparacion), "SQL registra En reparación.");
            Confirmar(!await persistencia.IntentarAvanzarAsync(numero, FlujoOrden.EnDiagnostico, FlujoOrden.EnReparacion), "SQL rechaza un estado anterior desactualizado.");
            Confirmar(!await persistencia.IntentarAvanzarAsync(numero, FlujoOrden.EnReparacion, FlujoOrden.Entregada), "SQL rechaza saltar Lista para entrega.");
            Confirmar(await persistencia.IntentarAvanzarAsync(numero, FlujoOrden.EnReparacion, FlujoOrden.Lista), "SQL registra Lista para entrega.");

            var entregas = await Task.WhenAll(
                persistencia.IntentarAvanzarAsync(numero, FlujoOrden.Lista, FlujoOrden.Entregada),
                persistencia.IntentarAvanzarAsync(numero, FlujoOrden.Lista, FlujoOrden.Entregada));
            Confirmar(entregas[0] != entregas[1], "Dos intentos concurrentes solo registran una entrega.");
            var entregada = await ConsultaOrdenesSql.ConsultarAsync(cadena, numero);
            Confirmar(entregada?.EntregadaUtc.HasValue == true && ConservaEntrega(entregada, entregada.EntregadaUtc.Value.Ticks), "SQL devuelve estado, fecha UTC, equipo y diagnóstico conservados.");
            long ticks = entregada.EntregadaUtc.Value.Ticks;

            Confirmar(!await persistencia.IntentarAvanzarAsync(numero, FlujoOrden.Lista, FlujoOrden.Entregada), "SQL rechaza volver a entregar la misma orden.");
            Confirmar(ConservaEntrega(await ConsultaOrdenesSql.ConsultarAsync(cadena, numero), ticks), "La consulta posterior conserva la primera fecha.");

            SqlConnection.ClearAllPools();
            ProbarDesdeOtroProceso(numero, ticks);
            Anotar("RESULTADO · Persistencia comprobada en la base aislada. Repair.Revision mantiene sus escrituras desactivadas.");
        }

        private static bool ConservaEntrega(OrdenSeguimiento orden, long ticks)
        {
            return orden?.Estado == FlujoOrden.Entregada && orden.EntregadaUtc?.Ticks == ticks &&
                orden.EntregadaUtc.Value.Kind == DateTimeKind.Utc && orden.Diagnostico == Diagnostico &&
                orden.Observaciones == Observaciones && orden.Equipo == "Lenovo ThinkPad de prueba";
        }

        private static async Task<(int numero, int numeroLegado)> CrearOrdenesFicticias(string cadena)
        {
            const string sql = @"
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'RepairSeguimientoDesarrollo_20261115' OR NOT EXISTS (
    SELECT 1 FROM sys.extended_properties WHERE class = 0 AND name = N'RepairAvance'
    AND CONVERT(nvarchar(100), value) = N'Seguimiento-20261115')
    THROW 51002, 'La base no pertenece a Seguimiento.', 1;
BEGIN TRANSACTION;
INSERT INTO dbo.Clientes (Nombre, Telefono, Correo) VALUES (@Nombre, N'809-555-0199', N'seguimiento@example.test');
DECLARE @ClienteId int = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO dbo.Equipos (ClienteId, Tipo, Marca, Modelo, Serie, Condicion)
VALUES (@ClienteId, N'Laptop', N'Lenovo', N'ThinkPad de prueba', @Serie, N'Dato ficticio para Seguimiento');
DECLARE @EquipoId int = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO dbo.Ordenes (ClienteId, EquipoId, Estado, Diagnostico, Observaciones)
VALUES (@ClienteId, @EquipoId, @Estado, @Diagnostico, @Observaciones);
DECLARE @Numero int = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO dbo.Ordenes (ClienteId, Estado, Diagnostico) VALUES (@ClienteId, @Estado, @Diagnostico);
DECLARE @NumeroLegado int = CONVERT(int, SCOPE_IDENTITY());
COMMIT TRANSACTION;
SELECT @Numero, @NumeroLegado;";

            string marca = Guid.NewGuid().ToString("N");
            using (var conexion = new SqlConnection(cadena))
            using (var comando = new SqlCommand(sql, conexion))
            {
                comando.CommandTimeout = 15;
                comando.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value = "Cliente ficticio Seguimiento " + marca;
                comando.Parameters.Add("@Serie", SqlDbType.NVarChar, 100).Value = "PRUEBA-" + marca;
                comando.Parameters.Add("@Estado", SqlDbType.NVarChar, 50).Value = FlujoOrden.EnDiagnostico;
                comando.Parameters.Add("@Diagnostico", SqlDbType.NVarChar, 1000).Value = Diagnostico;
                comando.Parameters.Add("@Observaciones", SqlDbType.NVarChar, 1000).Value = Observaciones;
                await conexion.OpenAsync();
                using (var lector = await comando.ExecuteReaderAsync())
                {
                    if (!await lector.ReadAsync()) throw new InvalidOperationException("El alta de prueba no devolvió los números de orden.");
                    return (lector.GetInt32(0), lector.GetInt32(1));
                }
            }
        }

        private static void ProbarDesdeOtroProceso(int numero, long ticks)
        {
            string argumentos = FormattableString.Invariant($"--reabrir {numero} {ticks}");
            using (var proceso = Process.Start(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location, argumentos)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8
            }))
            {
                string informeNuevo = proceso.StandardOutput.ReadToEnd().Trim();
                proceso.WaitForExit();
                Anotar(informeNuevo);
                if (proceso.ExitCode != 0) throw new InvalidOperationException("Falló la lectura SQL desde el segundo proceso.");
            }
        }

        private static async Task Ejecutar(string cadena, string sql)
        {
            using (var conexion = new SqlConnection(cadena))
            using (var comando = new SqlCommand(sql, conexion))
            {
                comando.CommandTimeout = 45;
                await conexion.OpenAsync();
                await comando.ExecuteNonQueryAsync();
            }
        }

        private static string LeerScript(string nombre)
        {
            var ensamblado = Assembly.GetExecutingAssembly();
            using (var script = ensamblado.GetManifestResourceStream("Repair.Seguimiento.Sql.Pruebas." + nombre))
            {
                if (script == null) throw new InvalidOperationException("Falta el script incorporado " + nombre);
                using (var lector = new StreamReader(script)) return lector.ReadToEnd();
            }
        }

        private static void Confirmar(bool cumple, string descripcion)
        {
            if (!cumple) throw new InvalidOperationException(descripcion);
            Anotar("OK · " + descripcion);
        }

        private static void Anotar(string texto)
        {
            Console.WriteLine(texto);
            informe.AppendLine(texto);
        }
    }
}
