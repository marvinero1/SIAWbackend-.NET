using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;

namespace SIAW.Controllers.compras.mantenimiento
{
    [Route("api/compras/mant/[controller]")]
    [ApiController]
    public class cmtipocompraController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cmtipocompraController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cmtipocompra
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cmtipocompra>>> Getcmtipocompra(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cmtipocompra == null)
                    {
                        return BadRequest(new { resp = "Entidad cmtipocompra es null." });
                    }
                    var result = await _context.cmtipocompra.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/cmtipocompra/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<cmtipocompra>> Getcmtipocompra(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cmtipocompra == null)
                    {
                        return BadRequest(new { resp = "Entidad cmtipocompra es null." });
                    }
                    var cmtipocompra = await _context.cmtipocompra.FindAsync(id);

                    if (cmtipocompra == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(cmtipocompra);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // PUT: api/cmtipocompra/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putcmtipocompra(string userConn, string id, cmtipocompra cmtipocompra)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != cmtipocompra.id)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(cmtipocompra).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cmtipocompraExists(id, _context))
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok( new { resp = "206" });   // actualizado con exito
            }
            


        }

        // POST: api/cmtipocompra
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cmtipocompra>> Postcmtipocompra(string userConn, cmtipocompra cmtipocompra)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cmtipocompra == null)
                {
                    return BadRequest(new { resp = "Entidad cmtipocompra es null." });
                }
                _context.cmtipocompra.Add(cmtipocompra);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cmtipocompraExists(cmtipocompra.id, _context))
                    {
                        return Conflict( new { resp = "Ya existe un registro con ese código" });
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok( new { resp = "204" });   // creado con exito

            }
            
        }

        // DELETE: api/cmtipocompra/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletecmtipocompra(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cmtipocompra == null)
                    {
                        return BadRequest(new { resp = "Entidad cmtipocompra es null." });
                    }
                    var cmtipocompra = await _context.cmtipocompra.FindAsync(id);
                    if (cmtipocompra == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.cmtipocompra.Remove(cmtipocompra);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool cmtipocompraExists(string id, DBContext _context)
        {
            return (_context.cmtipocompra?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}
