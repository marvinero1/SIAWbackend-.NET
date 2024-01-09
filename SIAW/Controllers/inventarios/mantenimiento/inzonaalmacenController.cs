using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/[controller]")]
    [ApiController]
    public class inzonaalmacenController : ControllerBase
    {


        private readonly UserConnectionManager _userConnectionManager;
        public inzonaalmacenController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}/{codAlmacen}")]
        public async Task<ActionResult<IEnumerable<inzonaalmacen>>> Getinzonaalmacen_catalogo(string userConn, int codAlmacen)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.inzonaalmacen
                        .Where(i => i.codalmacen == codAlmacen)
                        .OrderBy(i => i.codzona)
                        .Select(i => new
                        {
                            codigo = i.codzona,
                            descripcion = i.codzona
                        });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad inzonaalmacen es null." });
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
