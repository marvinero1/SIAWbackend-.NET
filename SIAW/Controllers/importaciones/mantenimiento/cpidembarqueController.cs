using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.importaciones.mantenimiento
{
    [Route("api/importaciones/mant/[controller]")]
    [ApiController]
    public class cpidembarqueController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cpidembarqueController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cpidembarque
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cpidembarque>>> Getcpidembarque(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cpidembarque == null)
                    {
                        return BadRequest(new { resp = "Entidad cpidembarque es null." });
                    }
                    var result = await _context.cpidembarque
                        .OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/cpidembarque/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<cpidembarque>> Getcpidembarque(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cpidembarque == null)
                    {
                        return BadRequest(new { resp = "Entidad cpidembarque es null." });
                    }
                    var cpidembarque = await _context.cpidembarque
                        .Where(i => i.id == id)
                        .FirstOrDefaultAsync();

                    if (cpidembarque == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(cpidembarque);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/cpidembarque/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putcpidembarque(string userConn, string id, cpidembarque cpidembarque)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != cpidembarque.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(cpidembarque).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cpidembarqueExists(id, _context))
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

        // POST: api/cpidembarque
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cpidembarque>> Postcpidembarque(string userConn, cpidembarque cpidembarque)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cpidembarque == null)
                {
                    return BadRequest(new { resp = "Entidad cpidembarque es null." });
                }
                _context.cpidembarque.Add(cpidembarque);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cpidembarqueExists(cpidembarque.id, _context))
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

        // DELETE: api/cpidembarque/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletecpidembarque(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cpidembarque == null)
                    {
                        return BadRequest(new { resp = "Entidad cpidembarque es null." });
                    }
                    var cpidembarque = await _context.cpidembarque.FindAsync(id);
                    if (cpidembarque == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.cpidembarque.Remove(cpidembarque);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool cpidembarqueExists(string id, DBContext _context)
        {
            return (_context.cpidembarque?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
