using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ctasXcobrar.mantenimiento
{
    [Route("api/ctasXcobrar/mant/[controller]")]
    [ApiController]
    public class cotipoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cotipoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cotipo
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cotipo>>> Getcotipo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotipo == null)
                    {
                        return BadRequest(new { resp = "Entidad cotipo es null." });
                    }
                    var result = await _context.cotipo.OrderByDescending(id => id.id).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/cotipo/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<cotipo>> Getcotipo(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotipo == null)
                    {
                        return BadRequest(new { resp = "Entidad cotipo es null." });
                    }
                    var cotipo = await _context.cotipo.FindAsync(id);

                    if (cotipo == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(cotipo);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}")]
        public async Task<ActionResult<IEnumerable<cotipo>>> Getcotipo_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.cotipo
                    .OrderBy(i => i.id)
                    .Select(i => new
                    {
                        i.id,
                        i.descripcion
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest( new { resp = "No se encontraron registros con esos datos." });
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

        // PUT: api/cotipo/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putcotipo(string userConn, string id, cotipo cotipo)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != cotipo.id)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(cotipo).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cotipoExists(id, _context))
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok( new { resp = "206" });   // actualizado con exito
            }

        }

        // POST: api/cotipo
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cotipo>> Postcotipo(string userConn, cotipo cotipo)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cotipo == null)
                {
                    return BadRequest(new { resp = "Entidad cotipo es null." });
                }
                _context.cotipo.Add(cotipo);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cotipoExists(cotipo.id, _context))
                    {
                        return Conflict( new { resp = "Ya existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok( new { resp = "204" });   // creado con exito

            }
            
        }

        // DELETE: api/cotipo/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletecotipo(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotipo == null)
                    {
                        return BadRequest(new { resp = "Entidad cotipo es null." });
                    }
                    var cotipo = await _context.cotipo.FindAsync(id);
                    if (cotipo == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.cotipo.Remove(cotipo);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool cotipoExists(string id, DBContext _context)
        {
            return (_context.cotipo?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
