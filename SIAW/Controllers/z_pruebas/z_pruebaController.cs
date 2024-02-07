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
        private readonly Cobranzas cobranzas = new Cobranzas();

        public z_pruebaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // POST: api/acaseguradora
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[Authorize]
        [HttpGet]
        [Route("prueCobranza/{userConn}")]
        public async Task<ActionResult<consultCocobranza>> Postacaseguradora(string userConn)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    var prueba = await cobranzas.Consulta_Deposito_Cobranzas_Credito_Sin_Aplicar(_context, "cliente", "", "", "300023", false, "APLICAR_DESCTO", "41182", false, new DateTime(2015, 5, 13));
                    return Ok(prueba);   // creado con exito
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                }

                

            }
        }





        // POST: api/acaseguradora
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<acaseguradora>> Postacaseguradora(string userConn, veptoventa veptoventa)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.veptoventa == null)
                {
                    return BadRequest(new { resp = "Entidad veptoventa es null." });
                }
                return Ok(veptoventa);
                _context.veptoventa.Add(veptoventa);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                }

                return Ok(new { resp = "204" });   // creado con exito

            }
        }

        // POST: api/veptoventa
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        // [Authorize]
        [HttpPost]
        [Route("aaa/{userConn}")]
        public async Task<ActionResult<adunidad>> Postadunidaefgqwegrvwd(string userConn, adunidad adunidad)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adunidad == null)
                {
                    return BadRequest(new { resp = "Entidad adunidad es null." });
                }
                _context.adunidad.Add(adunidad);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                }

                return Ok(new { resp = "204" });   // creado con exito

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
