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
    public class interminacionController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public interminacionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/interminacion
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<interminacion>>> Getinterminacion(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.interminacion == null)
                    {
                        return Problem("Entidad interminacion es null.");
                    }
                    var result = await _context.interminacion.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/interminacion/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<interminacion>> Getinterminacion(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.interminacion == null)
                    {
                        return Problem("Entidad interminacion es null.");
                    }
                    var interminacion = await _context.interminacion.FindAsync(codigo);

                    if (interminacion == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(interminacion);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/interminacion/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putinterminacion(string userConn, string codigo, interminacion interminacion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != interminacion.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(interminacion).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!interminacionExists(codigo, _context))
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

        // POST: api/interminacion
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<interminacion>> Postinterminacion(string userConn, interminacion interminacion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.interminacion == null)
                {
                    return Problem("Entidad interminacion es null.");
                }
                _context.interminacion.Add(interminacion);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (interminacionExists(interminacion.codigo, _context))
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

        // DELETE: api/interminacion/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteinterminacion(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.interminacion == null)
                    {
                        return Problem("Entidad interminacion es null.");
                    }
                    var interminacion = await _context.interminacion.FindAsync(codigo);
                    if (interminacion == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.interminacion.Remove(interminacion);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool interminacionExists(string codigo, DBContext _context)
        {
            return (_context.interminacion?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
