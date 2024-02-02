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
    public class cpidproformaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cpidproformaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cpidproforma
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cpidproforma>>> Getcpidproforma(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cpidproforma == null)
                    {
                        return BadRequest(new { resp = "Entidad cpidproforma es null." });
                    }
                    var result = await _context.cpidproforma
                        .OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/cpidproforma/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<cpidproforma>> Getcpidproforma(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cpidproforma == null)
                    {
                        return BadRequest(new { resp = "Entidad cpidproforma es null." });
                    }
                    var cpidproforma = await _context.cpidproforma
                        .Where(i => i.id == id)
                        .FirstOrDefaultAsync();

                    if (cpidproforma == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(cpidproforma);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/cpidproforma/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putcpidproforma(string userConn, string id, cpidproforma cpidproforma)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != cpidproforma.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(cpidproforma).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cpidproformaExists(id, _context))
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

        // POST: api/cpidproforma
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cpidproforma>> Postcpidproforma(string userConn, cpidproforma cpidproforma)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cpidproforma == null)
                {
                    return BadRequest(new { resp = "Entidad cpidproforma es null." });
                }
                _context.cpidproforma.Add(cpidproforma);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cpidproformaExists(cpidproforma.id, _context))
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

        // DELETE: api/cpidproforma/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletecpidproforma(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cpidproforma == null)
                    {
                        return BadRequest(new { resp = "Entidad cpidproforma es null." });
                    }
                    var cpidproforma = await _context.cpidproforma.FindAsync(id);
                    if (cpidproforma == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.cpidproforma.Remove(cpidproforma);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool cpidproformaExists(string id, DBContext _context)
        {
            return (_context.cpidproforma?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
