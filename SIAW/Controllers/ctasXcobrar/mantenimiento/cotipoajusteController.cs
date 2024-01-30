using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ctasXcobrar.mantenimiento
{
    [Route("api/ctsxcob/mant/[controller]")]
    [ApiController]
    public class cotipoajusteController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cotipoajusteController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cotipoajuste
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cotipoajuste>>> Getcotipoajuste(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotipoajuste == null)
                    {
                        return BadRequest(new { resp = "Entidad cotipoajuste es null." });
                    }
                    var result = await _context.cotipoajuste
                        .OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/cotipoajuste/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<cotipoajuste>> Getcotipoajuste(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotipoajuste == null)
                    {
                        return BadRequest(new { resp = "Entidad cotipoajuste es null." });
                    }
                    var cotipoajuste = await _context.cotipoajuste
                        .Where(i => i.id == id)
                        .FirstOrDefaultAsync();

                    if (cotipoajuste == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(cotipoajuste);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/cotipoajuste/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putcotipoajuste(string userConn, string id, cotipoajuste cotipoajuste)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != cotipoajuste.id)
                {
                    return BadRequest(new { resp = "Error con id en datos proporcionados." });
                }

                _context.Entry(cotipoajuste).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cotipoajusteExists(id, _context))
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok(new { resp = "206" });   // actualizado con exito
            }



        }

        // POST: api/cotipoajuste
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cotipoajuste>> Postcotipoajuste(string userConn, cotipoajuste cotipoajuste)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cotipoajuste == null)
                {
                    return BadRequest(new { resp = "Entidad cotipoajuste es null." });
                }
                _context.cotipoajuste.Add(cotipoajuste);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cotipoajusteExists(cotipoajuste.id, _context))
                    {
                        return Conflict(new { resp = "Ya existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok(new { resp = "204" });   // creado con exito

            }

        }

        // DELETE: api/cotipoajuste/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletecotipoajuste(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotipoajuste == null)
                    {
                        return BadRequest(new { resp = "Entidad cotipoajuste es null." });
                    }
                    var cotipoajuste = await _context.cotipoajuste.FindAsync(id);
                    if (cotipoajuste == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.cotipoajuste.Remove(cotipoajuste);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool cotipoajusteExists(string id, DBContext _context)
        {
            return (_context.cotipoajuste?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
