using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Models;

namespace SIAW.Controllers.compras.mantenimiento
{
    [Route("api/compras/mant/[controller]")]
    [ApiController]
    public class cpproveedorController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cpproveedorController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}")]
        public async Task<ActionResult<IEnumerable<cpproveedor>>> Getcpproveedor_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.cpproveedor
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.razonsocial
                    }).ToListAsync();


                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad cpproveedor es null." });
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
