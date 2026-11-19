using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Repair.Core.Datos;
using Repair.Core.Logica;
using Repair.Core.Modelos;
using Repair.Core.Seguridad;

namespace Repair.Instalacion
{
    internal static class PruebasEntrega
    {
        internal static async Task EjecutarAsync(string cadena, Action<string> log)
        {
            if(new SqlConnectionStringBuilder(cadena).InitialCatalog!=Program.BasePruebas)throw new InvalidOperationException("Prueba fuera de la base exclusiva.");
            int casos=0;
            Action<bool,string> exigir=(cumple,texto)=>{if(!cumple)throw new InvalidOperationException(texto);casos++;log("OK · "+texto);};
            var auth=new AutenticacionSql(cadena);
            var admin=await auth.IniciarAsync("admin.demo","Repair-Admin-2026!");
            var recepcion=await auth.IniciarAsync("recepcion.demo","Repair-Recepcion-2026!");
            var tecnico=await auth.IniciarAsync("tecnico.demo","Repair-Tecnico-2026!");
            exigir(admin?.Rol=="Administrador" && recepcion?.Rol=="Recepcionista" && tecnico?.Rol=="Técnico","Inicio de sesión real de los tres roles.");
            exigir(await auth.IniciarAsync("admin.demo","clave errónea")==null,"Contraseña incorrecta rechazada.");
            string cuentaTemporal = "prueba." + Guid.NewGuid().ToString("N").Substring(0,12);
            byte[] salTemporal = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salTemporal);
            using (var conexion = new SqlConnection(cadena))
            using (var comando = new SqlCommand("INSERT dbo.Usuarios(Usuario,Rol,Sal,Hash,Iteraciones) VALUES(@Usuario,N'Técnico',@Sal,@Hash,210000);", conexion))
            {
                comando.Parameters.Add("@Usuario",SqlDbType.NVarChar,60).Value=cuentaTemporal;
                comando.Parameters.Add("@Sal",SqlDbType.VarBinary,16).Value=salTemporal;
                comando.Parameters.Add("@Hash",SqlDbType.VarBinary,32).Value=AutenticacionSql.Derivar("Prueba-Anterior-2026!",salTemporal,210000);
                await conexion.OpenAsync(); await comando.ExecuteNonQueryAsync();
            }
            var sesionTemporal = await auth.IniciarAsync(cuentaTemporal,"Prueba-Anterior-2026!");
            await auth.CambiarClaveAsync(sesionTemporal,"Prueba-Anterior-2026!","Prueba-Nueva-2026!");
            exigir(await auth.IniciarAsync(cuentaTemporal,"Prueba-Anterior-2026!")==null &&
                await auth.IniciarAsync(cuentaTemporal,"Prueba-Nueva-2026!")!=null,"Cambiar contraseña invalida la anterior y permite la nueva.");
            using (var conexion = new SqlConnection(cadena))
            using (var comando = new SqlCommand("UPDATE dbo.Usuarios SET Activo=0 WHERE Usuario=@Usuario;",conexion))
            {
                comando.Parameters.Add("@Usuario",SqlDbType.NVarChar,60).Value=cuentaTemporal;
                await conexion.OpenAsync(); await comando.ExecuteNonQueryAsync();
            }
            exigir(await auth.IniciarAsync(cuentaTemporal,"Prueba-Nueva-2026!")==null,"Cuenta desactivada no puede iniciar sesión.");
            var clienteServicio=new ClienteServicio(new ClientesAutorizados(new ClienteSqlRepositorio(cadena),recepcion));
            string token=Guid.NewGuid().ToString("N").Substring(0,12);
            int cliente=await clienteServicio.RegistrarAsync(new Cliente{Nombre="Cliente ficticio cierre "+token,Telefono="809-555-0199",Correo="cierre@example.test",Notas="Datos ficticios de aceptación"});
            var equipos=new EquipoServicio(new EquiposAutorizados(new EquipoSqlRepositorio(cadena),recepcion));
            int equipo=await equipos.RegistrarAsync(new Equipo{ClienteId=cliente,Tipo="Laptop",Marca="Lenovo",Modelo="ThinkPad prueba RC1",Serie="RC1-"+token,Accesorios="Cargador",Condicion="Prueba ficticia, ventilador averiado"});
            var equipoLeido=await equipos.ObtenerAsync(equipo);
            exigir(equipoLeido?.Tipo=="Laptop","Alta de cliente y equipo con Tipo en SQL.");
            var ordenRepo=new OrdenSqlRepositorio(cadena);
            var ordenes=await ordenRepo.BuscarAsync("");
            exigir(ordenes.Count(o=>o.ClienteId==cliente)==1,"El alta de cliente no genera una orden vacía adicional.");
            var orden=ordenes.Single(o=>o.EquipoId==equipo);
            exigir(orden.ClienteId==cliente && orden.Estado=="Pendiente","Alta automática de una orden vinculada al equipo.");
            var servicioTecnico=new OrdenServicio(new OrdenesAutorizadas(ordenRepo,tecnico));
            await servicioTecnico.GuardarDiagnosticoAsync(orden.Id,"Ventilador detenido, sustituir conjunto.","Severidad media, dato ficticio");
            exigir((await ordenRepo.ObtenerAsync(orden.Id)).Estado=="En diagnóstico","Diagnóstico de Oliver pasa la orden a En diagnóstico.");
            bool rechazado=false;
            try{await new OrdenServicio(new OrdenesAutorizadas(ordenRepo,recepcion)).GuardarDiagnosticoAsync(orden.Id,"Intento no autorizado","");}catch(UnauthorizedAccessException){rechazado=true;}
            exigir(rechazado,"Recepcionista no puede guardar diagnóstico.");
            rechazado=false;
            try{await new EquipoServicio(new EquiposAutorizados(new EquipoSqlRepositorio(cadena),tecnico)).EliminarAsync(equipo);}catch(UnauthorizedAccessException){rechazado=true;}
            exigir(rechazado,"Técnico no puede eliminar equipos.");
            var flujoTecnico=new SeguimientoServicio(cadena,tecnico);
            var flujoRecepcion=new SeguimientoServicio(cadena,recepcion);
            rechazado=false;
            try{await flujoRecepcion.AvanzarAsync(orden.Id,"En diagnóstico");}catch(UnauthorizedAccessException){rechazado=true;}
            exigir(rechazado,"Recepcionista no puede iniciar una reparación.");
            exigir(!await flujoRecepcion.AvanzarAsync(orden.Id,"Lista para entrega"),"Entregar antes de tiempo se rechaza por estado esperado.");
            exigir(await flujoTecnico.AvanzarAsync(orden.Id,"En diagnóstico"),"Transición a En reparación.");
            exigir(!await flujoTecnico.AvanzarAsync(orden.Id,"En diagnóstico"),"Reintento con estado antiguo no sobrescribe el estado.");
            await servicioTecnico.GuardarDiagnosticoAsync(orden.Id,"Ventilador sustituido y comprobado.","Prueba ficticia de temperatura correcta");
            exigir((await ordenRepo.ObtenerAsync(orden.Id)).Estado=="En reparación","Guardar de nuevo el diagnóstico conserva En reparación.");
            exigir(await flujoTecnico.AvanzarAsync(orden.Id,"En reparación"),"Transición a Lista para entrega.");
            rechazado=false;
            try{await flujoTecnico.AvanzarAsync(orden.Id,"Lista para entrega");}catch(UnauthorizedAccessException){rechazado=true;}
            exigir(rechazado,"Técnico no puede registrar la entrega.");
            var cierres=await Task.WhenAll(flujoRecepcion.AvanzarAsync(orden.Id,"Lista para entrega"),flujoRecepcion.AvanzarAsync(orden.Id,"Lista para entrega"));
            exigir(cierres.Count(v=>v)==1,"Dos intentos concurrentes registran una sola entrega.");
            var consulta=new ConsultaSeguimientoSql(cadena);
            var entregada=await consulta.BuscarPorIdAsync(orden.Id);
            long fecha=entregada.EntregadaUtc.Value.Ticks;
            exigir(entregada.Estado=="Entregada" && entregada.EquipoId==equipo && entregada.EntregadaUtc.Value.Kind==DateTimeKind.Utc,"Consulta de Seguimiento devuelve entrega y hora UTC reales.");
            await servicioTecnico.GuardarDiagnosticoAsync(orden.Id,"Ventilador sustituido y comprobado.","Prueba ficticia de temperatura correcta");
            var final=await consulta.BuscarPorIdAsync(orden.Id);
            exigir(final.Estado=="Entregada" && final.EntregadaUtc.Value.Ticks==fecha,"Guardar diagnóstico después de entregar conserva estado y primera fecha.");
            exigir(!await flujoRecepcion.AvanzarAsync(orden.Id,"Lista para entrega"),"La entrega repetida no se confirma otra vez.");
            var reingresos=await Task.WhenAll(flujoRecepcion.ReingresarAsync(equipo),flujoRecepcion.ReingresarAsync(equipo));
            exigir(reingresos[0]==reingresos[1] && reingresos[0]!=orden.Id,"Dos reingresos del mismo equipo reutilizan una sola orden activa nueva.");
            var nueva=await ordenRepo.ObtenerAsync(reingresos[0]);
            exigir(nueva.EquipoId==equipo && nueva.ClienteId==cliente && nueva.Estado=="Pendiente" && nueva.EntregadaUtc==null,"El nuevo ingreso del mismo equipo queda pendiente y sin fecha de entrega.");
            var tabla=await new ReporteOrdenesSql(cadena).ConsultarAsync("Entregada",DateTime.Today.AddDays(-1),DateTime.Today,true);
            exigir(tabla.Rows.Cast<DataRow>().Any(r=>(int)r["Orden"]==orden.Id),"El reporte por estado y fecha incluye la entrega.");
            await Program.PrepararAsync(new SqlConnectionStringBuilder(cadena));
            exigir((await consulta.BuscarPorIdAsync(orden.Id)).EntregadaUtc.Value.Ticks==fecha,"Repetir la migración conserva datos y fecha.");
            SqlConnection.ClearAllPools();
            using(var proceso=Process.Start(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location,$"--reabrir {orden.Id} {fecha}")
            {UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,StandardOutputEncoding=Encoding.UTF8}))
            {
                string lectura=await proceso.StandardOutput.ReadToEndAsync();proceso.WaitForExit();
                exigir(proceso.ExitCode==0,"Segundo proceso recupera estado, equipo, diagnóstico y primera fecha desde SQL.");
                log(lectura.Trim());
            }
            log($"RESULTADO: {casos} comprobaciones SQL reales aprobadas.");
            log($"DATOS FICTICIOS PARA LA UI: cliente {cliente}; equipo {equipo}; orden entregada {orden.Id}; nuevo ingreso {nueva.Id}.");
        }

        internal static int LeerDesdeOtroProceso(string cadena,int id,long ticks)
        {
            var orden=new ConsultaSeguimientoSql(cadena).BuscarPorIdAsync(id).GetAwaiter().GetResult();
            bool coincide=orden?.Estado=="Entregada" && orden.EquipoId.HasValue && orden.EntregadaUtc?.Ticks==ticks && orden.Diagnostico=="Ventilador sustituido y comprobado.";
            Console.WriteLine(coincide ? "Lectura SQL desde otro proceso: correcta." : "Lectura SQL desde otro proceso: fallida.");
            return coincide?0:1;
        }
    }
}
