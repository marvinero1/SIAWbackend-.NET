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
        [Authorize]
        [HttpPost]
        [Route("verifPermisoEsp/{userConn}/{servicio}/{codpersona}/{password}/{codempresa}/{dato_a}/{dato_b}")]
        public async Task<ActionResult<bool>> verifPermisoEsp(string userConn, int servicio, int codpersona, string password, string codempresa, string dato_a, string dato_b)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            int codalmacen = await empresa.CodAlmacen(userConnectionString, codempresa);
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    var autoriza = await _context.adautorizacion
                        .Where(i => i.nivel == servicio && i.codpersona == codpersona)
                        .FirstOrDefaultAsync();
                    if (autoriza != null)
                    {
                        string passEncript = await funciones.EncriptarMD5(password);
                        if (passEncript == autoriza.password) 
                        {
                            return Ok(new { resp = autoriza.obs }); ///  autorizado a cierta persona
                        }
                        return BadRequest( new { resp = "La contraseña no es correcta para su usuario" });
                    }
                    string pass = await genSpecialAut(codalmacen, dato_a, dato_b, servicio.ToString());
                    if (pass == password)
                    {
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
}
