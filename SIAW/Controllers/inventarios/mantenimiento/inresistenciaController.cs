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
    public class inresistenciaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public inresistenciaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/inresistencia
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<inresistencia>>> Getinresistencia(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inresistencia == null)
                    {
                        return BadRequest(new { resp = "Entidad inresistencia es null." });
                    }
                    var result = await _context.inresistencia.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/inresistencia/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<inresistencia>> Getinresistencia(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inresistencia == null)
                    {
                        return BadRequest(new { resp = "Entidad inresistencia es null." });
                    }
                    var inresistencia = await _context.inresistencia.FindAsync(codigo);

                    if (inresistencia == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(inresistencia);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // PUT: api/inresistencia/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putinresistencia(string userConn, string codigo, inresistencia inresistencia)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != inresistencia.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(inresistencia).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!inresistenciaExists(codigo, _context))
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

        // POST: api/inresistencia
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<inresistencia>> Postinresistencia(string userConn, inresistencia inresistencia)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.inresistencia == null)
                {
                    return BadRequest(new { resp = "Entidad inresistencia es null." });
                }
                _context.inresistencia.Add(inresistencia);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (inresistenciaExists(inresistencia.codigo, _context))
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

        // DELETE: api/inresistencia/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteinresistencia(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inresistencia == null)
                    {
                        return BadRequest(new { resp = "Entidad inresistencia es null." });
                    }
                    var inresistencia = await _context.inresistencia.FindAsync(codigo);
                    if (inresistencia == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.inresistencia.Remove(inresistencia);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool inresistenciaExists(string codigo, DBContext _context)
        {
            return (_context.inresistencia?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
