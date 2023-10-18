using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;
using System.Net;

namespace SIAW.Controllers.seg_adm.operacion
{
    [Route("api/seg_adm/oper/[controller]")]
    [ApiController]
    public class prgGenPassController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Funciones funciones = new Funciones();
        public prgGenPassController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/semodulo
        [HttpGet]
        [Route("genAutEspecial/{codalmacen}/{dato_a}/{dato_b}/{servicio}")]
        public async Task<ActionResult<object>> genAutEspecial(int codalmacen, string dato_a, string dato_b, string servicio)
        {
            try
            {
                DateTime fechaAct = DateTime.Now;
                int hora = fechaAct.Hour;
                string pass = await funciones.SP(fechaAct, hora, codalmacen, dato_a, dato_b, servicio);
                return Ok(new {resp = pass});
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

    }
}
