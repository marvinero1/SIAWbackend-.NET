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
    public class inudemedController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public inudemedController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/inudemed
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<inudemed>>> Getinudemed(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inudemed == null)
                    {
                        return BadRequest(new { resp = "Entidad inudemed es null." });
                    }
                    var result = await _context.inudemed.OrderBy(Codigo => Codigo.Codigo).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/inudemed/5
        [HttpGet("{userConn}/{Codigo}")]
        public async Task<ActionResult<inudemed>> Getinudemed(string userConn, string Codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inudemed == null)
                    {
                        return BadRequest(new { resp = "Entidad inudemed es null." });
                    }
                    var inudemed = await _context.inudemed.FindAsync(Codigo);

                    if (inudemed == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(inudemed);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // PUT: api/inudemed/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{Codigo}")]
        public async Task<IActionResult> Putinudemed(string userConn, string Codigo, inudemed inudemed)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (Codigo != inudemed.Codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(inudemed).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!inudemedExists(Codigo, _context))
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

        // POST: api/inudemed
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<inudemed>> Postinudemed(string userConn, inudemed inudemed)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.inudemed == null)
                {
                    return BadRequest(new { resp = "Entidad inudemed es null." });
                }
                _context.inudemed.Add(inudemed);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (inudemedExists(inudemed.Codigo, _context))
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

        // DELETE: api/inudemed/5
        [Authorize]
        [HttpDelete("{userConn}/{Codigo}")]
        public async Task<IActionResult> Deleteinudemed(string userConn, string Codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inudemed == null)
                    {
                        return BadRequest(new { resp = "Entidad inudemed es null." });
                    }
                    var inudemed = await _context.inudemed.FindAsync(Codigo);
                    if (inudemed == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.inudemed.Remove(inudemed);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool inudemedExists(string Codigo, DBContext _context)
        {
            return (_context.inudemed?.Any(e => e.Codigo == Codigo)).GetValueOrDefault();

        }
    }
}
