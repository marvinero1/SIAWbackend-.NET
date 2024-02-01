using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using siaw_DBContext.Models;

namespace SIAW.Controllers.contabilidad.mantenimiento
{
    [Route("api/contab/mant/[controller]")]
    [ApiController]
    public class cncentrocostoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cncentrocostoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}")]
        public async Task<ActionResult<IEnumerable<cncuenta>>> Getcncuenta_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.cncentrocosto
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        codigo = i.codigo.ToString(),
                        descripcion = i.descripcion
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "No se encontraron registros con los datos proporcionados." });
                    }
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }
    }
}
