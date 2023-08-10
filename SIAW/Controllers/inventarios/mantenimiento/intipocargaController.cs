using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class intipocargaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public intipocargaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/intipocarga
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<intipocarga>>> Getintipocarga(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.intipocarga == null)
                    {
                        return Problem("Entidad intipocarga es null.");
                    }
                    var result = await _context.intipocarga.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/intipocarga/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<intipocarga>> Getintipocarga(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.intipocarga == null)
                    {
                        return Problem("Entidad intipocarga es null.");
                    }
                    var intipocarga = await _context.intipocarga.FindAsync(id);

                    if (intipocarga == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(intipocarga);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/intipocarga/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putintipocarga(string userConn, string id, intipocarga intipocarga)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != intipocarga.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(intipocarga).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!intipocargaExists(id, _context))
                    {
                        return NotFound("No existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("206");   // actualizado con exito
            }
            


        }

        // POST: api/intipocarga
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<intipocarga>> Postintipocarga(string userConn, intipocarga intipocarga)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.intipocarga == null)
                {
                    return Problem("Entidad intipocarga es null.");
                }
                _context.intipocarga.Add(intipocarga);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (intipocargaExists(intipocarga.id, _context))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("204");   // creado con exito

            }
            
        }

        // DELETE: api/intipocarga/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deleteintipocarga(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.intipocarga == null)
                    {
                        return Problem("Entidad intipocarga es null.");
                    }
                    var intipocarga = await _context.intipocarga.FindAsync(id);
                    if (intipocarga == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.intipocarga.Remove(intipocarga);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool intipocargaExists(string id, DBContext _context)
        {
            return (_context.intipocarga?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
