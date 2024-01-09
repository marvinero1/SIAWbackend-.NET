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
    public class intipopedidoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public intipopedidoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/intipopedido
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<intipopedido>>> Getintipopedido(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.intipopedido == null)
                    {
                        return BadRequest(new { resp = "Entidad intipopedido es null." });
                    }
                    var result = await _context.intipopedido.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/intipopedido/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<intipopedido>> Getintipopedido(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.intipopedido == null)
                    {
                        return BadRequest(new { resp = "Entidad intipopedido es null." });
                    }
                    var intipopedido = await _context.intipopedido.FindAsync(id);

                    if (intipopedido == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(intipopedido);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // PUT: api/intipopedido/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putintipopedido(string userConn, string id, intipopedido intipopedido)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != intipopedido.id)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(intipopedido).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!intipopedidoExists(id, _context))
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

        // POST: api/intipopedido
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<intipopedido>> Postintipopedido(string userConn, intipopedido intipopedido)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.intipopedido == null)
                {
                    return BadRequest(new { resp = "Entidad intipopedido es null." });
                }
                _context.intipopedido.Add(intipopedido);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (intipopedidoExists(intipopedido.id, _context))
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

        // DELETE: api/intipopedido/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deleteintipopedido(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.intipopedido == null)
                    {
                        return BadRequest(new { resp = "Entidad intipopedido es null." });
                    }
                    var intipopedido = await _context.intipopedido.FindAsync(id);
                    if (intipopedido == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.intipopedido.Remove(intipopedido);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool intipopedidoExists(string id, DBContext _context)
        {
            return (_context.intipopedido?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
