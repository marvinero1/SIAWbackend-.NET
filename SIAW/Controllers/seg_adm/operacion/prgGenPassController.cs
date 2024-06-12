using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;
using siaw_DBContext.Models_Extra;

namespace SIAW.Controllers.seg_adm.operacion
{
    [Route("api/seg_adm/oper/[controller]")]
    [ApiController]
    public class prgGenPassController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Funciones funciones = new Funciones();
        private readonly Seguridad seguridad = new Seguridad();
        private readonly Empresa empresa = new Empresa();   
        public prgGenPassController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/
        [HttpGet]
        [Route("genAutEspecial/{codalmacen}/{dato_a}/{dato_b}/{servicio}")]
        public async Task<ActionResult<object>> genAutEspecial(int codalmacen, string dato_a, string dato_b, string servicio)
        {
            string pass = await genSpecialAut(codalmacen, dato_a, dato_b, servicio);

            if (pass != "0")
            {
                return Ok(new { resp = pass });
            }

            return Problem("Error en el servidor");
        }



        // GET: api/
        [HttpGet]
        [Route("verificaAutHabilitada/{userConn}/{TipoPermiso}")]
        public async Task<ActionResult<bool>> VerificaAutHabilitada(string userConn, int TipoPermiso)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            try
            {
                // Verificar si el Permiso especial esta activado
                bool autHabilitada = await seguridad.AutorizacionEstaHabilitada(userConnectionString, TipoPermiso);
                
                return Ok(autHabilitada);  // si es false la autorizacion estadeshabilitada

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // POST: api/
        //[Authorize]
        [HttpPost]
        [Route("verifPermisoEsp/{userConn}")]
        public async Task<ActionResult<bool>> verifPermisoEsp(string userConn, requestPermisoEsp requestPermisoEsp)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            int codalmacen = await empresa.CodAlmacen(userConnectionString, requestPermisoEsp.codempresa);
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    var autorizaList = await _context.adautorizacion
                        .Where(i => i.nivel == requestPermisoEsp.servicio && i.codempresa == requestPermisoEsp.codempresa && i.vencimiento >= DateTime.Today.Date)
                        .ToListAsync();
                    if (autorizaList.Count() > 0)
                    {
                        string passEncript = await funciones.EncriptarMD5(requestPermisoEsp.password);
                        var confirmacion = autorizaList.Where(i => i.password == passEncript).FirstOrDefault();
                        if (confirmacion != null)
                        {
                            // antes de devolver OK se debe guardar logs de autorizacion
                            await seguridad.grabar_log_permisos(_context, requestPermisoEsp.servicio + " - " + requestPermisoEsp.descServicio, requestPermisoEsp.obs, requestPermisoEsp.datos_documento, requestPermisoEsp.usuario, requestPermisoEsp.fechareg, requestPermisoEsp.horareg);
                            return Ok(new { resp = confirmacion.obs }); ///  autorizado a cierta persona
                        }
                        //return BadRequest(new { resp = "La contraseña no es correcta para su usuario" });
                    }

                    // tratar el password otp
                    string pass = await genSpecialAut(codalmacen, requestPermisoEsp.dato_a, requestPermisoEsp.dato_b, requestPermisoEsp.servicio.ToString());
                    if (pass == requestPermisoEsp.password)
                    {
                        // antes de devolver OK se debe guardar logs de autorizacion
                        await seguridad.grabar_log_permisos(_context, requestPermisoEsp.servicio + " - " + requestPermisoEsp.descServicio, requestPermisoEsp.obs, requestPermisoEsp.datos_documento, requestPermisoEsp.usuario, requestPermisoEsp.fechareg, requestPermisoEsp.horareg);
                        return Ok(new { resp = "Autorizado" });
                    }
                    return BadRequest(new { resp = "La contraseña no es correcta" });
                }
                catch (Exception)
                {
                    return Problem("Error en el servidor");
                    throw;
                }
            }
        }


        private async Task<string> genSpecialAut(int codalmacen, string dato_a, string dato_b, string servicio)
        {
            try
            {
                DateTime fechaAct = DateTime.Now;
                int hora = fechaAct.Hour;
                string pass = await funciones.SP(fechaAct, hora, codalmacen, dato_a, dato_b, servicio);
                return pass;
            }
            catch (Exception)
            {
                return "0";
            }
        }


    }

    public class requestPermisoEsp
    {
        public int servicio { get; set; }
        public string descServicio { get; set; }
        public int codpersona { get; set; }
        public string password { get; set; }
        public string codempresa { get; set; }
        public string dato_a { get; set; }
        public string dato_b { get; set; }
        public DateTime fechareg { get; set; }
        public string horareg { get; set; }
        public string usuario { get; set; }
        public string obs { get; set; }
        public string datos_documento { get; set; }
    }
}
