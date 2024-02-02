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
    public class cpidrecepcionController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cpidrecepcionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cpidrecepcion
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cpidrecepcion>>> Getcpidrecepcion(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cpidrecepcion == null)
                    {
                        return BadRequest(new { resp = "Entidad cpidrecepcion es null." });
                    }
                    var result = await _context.cpidrecepcion
                        .OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/cpidrecepcion/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<cpidrecepcion>> Getcpidrecepcion(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cpidrecepcion == null)
                    {
                        return BadRequest(new { resp = "Entidad cpidrecepcion es null." });
                    }
                    var cpidrecepcion = await _context.cpidrecepcion
                        .Where(i => i.id == id)
                        .FirstOrDefaultAsync();

                    if (cpidrecepcion == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(cpidrecepcion);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/cpidrecepcion/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putcpidrecepcion(string userConn, string id, cpidrecepcion cpidrecepcion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != cpidrecepcion.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(cpidrecepcion).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cpidrecepcionExists(id, _context))
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

        // POST: api/cpidrecepcion
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cpidrecepcion>> Postcpidrecepcion(string userConn, cpidrecepcion cpidrecepcion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cpidrecepcion == null)
                {
                    return BadRequest(new { resp = "Entidad cpidrecepcion es null." });
                }
                _context.cpidrecepcion.Add(cpidrecepcion);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cpidrecepcionExists(cpidrecepcion.id, _context))
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

        // DELETE: api/cpidrecepcion/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletecpidrecepcion(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cpidrecepcion == null)
                    {
                        return BadRequest(new { resp = "Entidad cpidrecepcion es null." });
                    }
                    var cpidrecepcion = await _context.cpidrecepcion.FindAsync(id);
                    if (cpidrecepcion == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.cpidrecepcion.Remove(cpidrecepcion);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool cpidrecepcionExists(string id, DBContext _context)
        {
            return (_context.cpidrecepcion?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
