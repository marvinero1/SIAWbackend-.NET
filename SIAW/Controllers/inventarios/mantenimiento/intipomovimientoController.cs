using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class intipomovimientoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public intipomovimientoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/intipomovimiento
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<intipomovimiento>>> Getintipomovimiento(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.intipomovimiento == null)
                    {
                        return BadRequest(new { resp = "Entidad intipomovimiento es null." });
                    }
                    var result = await _context.intipomovimiento.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/intipomovimiento/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<intipomovimiento>> Getintipomovimiento(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.intipomovimiento == null)
                    {
                        return BadRequest(new { resp = "Entidad intipomovimiento es null." });
                    }
                    var intipomovimiento = await _context.intipomovimiento.FindAsync(id);

                    if (intipomovimiento == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(intipomovimiento);
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
        public async Task<ActionResult<IEnumerable<intipomovimiento>>> Getintipomovimiento_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.intipomovimiento
                    .OrderBy(i => i.id)
                    .Select(i => new
                    {
                        codigo = i.id,
                        descrip = i.descripcion,
                        nroactual = i.nroactual
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad intipomovimiento es null." });
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


        // PUT: api/intipomovimiento/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putintipomovimiento(string userConn, string id, intipomovimiento intipomovimiento)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != intipomovimiento.id)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(intipomovimiento).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!intipomovimientoExists(id, _context))
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

        // POST: api/intipomovimiento
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<intipomovimiento>> Postintipomovimiento(string userConn, intipomovimiento intipomovimiento)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.intipomovimiento == null)
                {
                    return BadRequest(new { resp = "Entidad intipomovimiento es null." });
                }
                _context.intipomovimiento.Add(intipomovimiento);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (intipomovimientoExists(intipomovimiento.id, _context))
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

        // DELETE: api/intipomovimiento/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deleteintipomovimiento(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.intipomovimiento == null)
                    {
                        return BadRequest(new { resp = "Entidad intipomovimiento es null." });
                    }
                    var intipomovimiento = await _context.intipomovimiento.FindAsync(id);
                    if (intipomovimiento == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.intipomovimiento.Remove(intipomovimiento);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool intipomovimientoExists(string id, DBContext _context)
        {
            return (_context.intipomovimiento?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
