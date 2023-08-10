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
    public class intipoinvController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public intipoinvController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/intipoinv
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<intipoinv>>> Getintipoinv(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.intipoinv == null)
                    {
                        return Problem("Entidad intipoinv es null.");
                    }
                    var result = await _context.intipoinv.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/intipoinv/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<intipoinv>> Getintipoinv(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.intipoinv == null)
                    {
                        return Problem("Entidad intipoinv es null.");
                    }
                    var intipoinv = await _context.intipoinv.FindAsync(id);

                    if (intipoinv == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(intipoinv);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/intipoinv/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putintipoinv(string userConn, string id, intipoinv intipoinv)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != intipoinv.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(intipoinv).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!intipoinvExists(id, _context))
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

        // POST: api/intipoinv
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<intipoinv>> Postintipoinv(string userConn, intipoinv intipoinv)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.intipoinv == null)
                {
                    return Problem("Entidad intipoinv es null.");
                }
                _context.intipoinv.Add(intipoinv);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (intipoinvExists(intipoinv.id, _context))
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

        // DELETE: api/intipoinv/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deleteintipoinv(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.intipoinv == null)
                    {
                        return Problem("Entidad intipoinv es null.");
                    }
                    var intipoinv = await _context.intipoinv.FindAsync(id);
                    if (intipoinv == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.intipoinv.Remove(intipoinv);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool intipoinvExists(string id, DBContext _context)
        {
            return (_context.intipoinv?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
