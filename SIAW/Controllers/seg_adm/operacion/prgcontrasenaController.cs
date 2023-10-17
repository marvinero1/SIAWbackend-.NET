using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Configuration;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;
using System.Net;

namespace SIAW.Controllers.seg_adm.operacion
{
    [Route("api/seg_adm/oper/[controller]")]
    [ApiController]
    public class prgcontrasenaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Funciones funciones = new Funciones();
        public prgcontrasenaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }



        // GET: api/semodulo
        [HttpGet]
        [Route("passwordAut/{userConn}/{codempresa}/{servicio}/{password}")]
        public async Task<ActionResult<object>> genAutEspecial(string userConn, string codempresa, int servicio, string password)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                DateTime fechaAct = DateTime.Now;

                string respuesta = await verificar(userConnectionString, codempresa, servicio, fechaAct, password);
                if (respuesta == "")
                {
                    return NotFound("713");
                }
                return Ok(respuesta);
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }




        private async Task<string> verificar(string userConnectionString, string codempresa, int servicio, DateTime fechaAct, string password)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                string resultado = "";
                var autorizaciones = await _context.adautorizacion
                    .Where(item => item.codempresa == codempresa && item.nivel == servicio && item.vencimiento >= fechaAct)
                    .Select(item => new
                    {
                        nivel = item.nivel,
                        password = item.password,
                        obs = item.obs,
                        codpersona = item.codpersona
                    })
                    .ToListAsync();

                foreach (var item in autorizaciones)
                {
                    if (item.password == await funciones.EncriptarMD5(password))
                    {
                        resultado = item.obs; break;
                    }
                }
                return resultado;
            }
        }
    }
}
