using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;

namespace SIAW.Controllers.z_pruebas
{
    [Route("api/[controller]")]
    [ApiController]
    public class z_pruebaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Saldos saldos = new Saldos();


        public z_pruebaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // POST: api/acaseguradora
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[Authorize]
        [HttpPost("{userConn}/{codItem}/{descripcorta}/{medida}/{codempresa}/{usuario}")]
        public async Task<ActionResult<acaseguradora>> Postacaseguradora(string userConn, string codItem, string descripcorta, string medida, string codempresa, string usuario)
        {
            //return Ok(RequestValidacion);
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                var infoitem = await saldos.infoitem(userConnectionString, codItem, codempresa, usuario);

                return Ok(infoitem);
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }



        // POST: api/acaseguradora
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[Authorize]
        [HttpPost]
        [Route("ppppp/{userConn}")]
        public async Task<ActionResult<acaseguradora>> prueba (string userConn, RequestValidacion RequestValidacion)
        {
            return Ok(RequestValidacion);
           
        }
    }
}
