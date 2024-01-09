using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class peplanporcenController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public peplanporcenController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/peplanporcen
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<peplanporcen>>> Getpeplanporcen(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.peplanporcen == null)
                    {
                        return BadRequest(new { resp = "Entidad peplanporcen es null." });
                    }
                    var result = await _context.peplanporcen.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/peplanporcen/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<peplanporcen>> Getpeplanporcen(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.peplanporcen == null)
                    {
                        return BadRequest(new { resp = "Entidad peplanporcen es null." });
                    }
                    var peplanporcen = await _context.peplanporcen.FindAsync(codigo);

                    if (peplanporcen == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(peplanporcen);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // PUT: api/peplanporcen/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putpeplanporcen(string userConn, int codigo, peplanporcen peplanporcen)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != peplanporcen.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(peplanporcen).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!peplanporcenExists(codigo, _context))
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

        // POST: api/peplanporcen
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<peplanporcen>> Postpeplanporcen(string userConn, peplanporcen peplanporcen)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.peplanporcen == null)
                {
                    return BadRequest(new { resp = "Entidad peplanporcen es null." });
                }
                _context.peplanporcen.Add(peplanporcen);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (peplanporcenExists(peplanporcen.codigo, _context))
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

        // DELETE: api/peplanporcen/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletepeplanporcen(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.peplanporcen == null)
                    {
                        return BadRequest(new { resp = "Entidad peplanporcen es null." });
                    }
                    var peplanporcen = await _context.peplanporcen.FindAsync(codigo);
                    if (peplanporcen == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.peplanporcen.Remove(peplanporcen);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool peplanporcenExists(int codigo, DBContext _context)
        {
            return (_context.peplanporcen?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
