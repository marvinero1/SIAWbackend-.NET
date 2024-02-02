using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Route("api/[controller]")]
    [ApiController]
    public class vetiposoldsctosController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public vetiposoldsctosController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}")]
        public async Task<ActionResult<IEnumerable<vetiposoldsctos>>> catalogoVetiposoldsctos(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.vetiposoldsctos
                    .Select(i => new
                    {
                        codigo = i.id,
                        descripcion = i.descripcion
                    })
                    .ToListAsync();

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
