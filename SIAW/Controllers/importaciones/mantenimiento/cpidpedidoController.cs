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
    public class cpidpedidoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cpidpedidoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cpidpedido
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cpidpedido>>> Getcpidpedido(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cpidpedido == null)
                    {
                        return BadRequest(new { resp = "Entidad cpidpedido es null." });
                    }
                    var result = await _context.cpidpedido
                        .OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/cpidpedido/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<cpidpedido>> Getcpidpedido(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cpidpedido == null)
                    {
                        return BadRequest(new { resp = "Entidad cpidpedido es null." });
                    }
                    var cpidpedido = await _context.cpidpedido
                        .Where(i => i.id == id)
                        .FirstOrDefaultAsync();

                    if (cpidpedido == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(cpidpedido);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/cpidpedido/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putcpidpedido(string userConn, string id, cpidpedido cpidpedido)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != cpidpedido.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(cpidpedido).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cpidpedidoExists(id, _context))
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

        // POST: api/cpidpedido
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cpidpedido>> Postcpidpedido(string userConn, cpidpedido cpidpedido)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cpidpedido == null)
                {
                    return BadRequest(new { resp = "Entidad cpidpedido es null." });
                }
                _context.cpidpedido.Add(cpidpedido);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cpidpedidoExists(cpidpedido.id, _context))
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

        // DELETE: api/cpidpedido/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletecpidpedido(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cpidpedido == null)
                    {
                        return BadRequest(new { resp = "Entidad cpidpedido es null." });
                    }
                    var cpidpedido = await _context.cpidpedido.FindAsync(id);
                    if (cpidpedido == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.cpidpedido.Remove(cpidpedido);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool cpidpedidoExists(string id, DBContext _context)
        {
            return (_context.cpidpedido?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
