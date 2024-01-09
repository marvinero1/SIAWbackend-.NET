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
    public class addeptoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public addeptoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/addepto
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<addepto>>> Getaddepto(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.addepto == null)
                    {
                        return BadRequest(new { resp = "Entidad addepto es null." });
                    }
                    var result = await _context.addepto.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/addepto/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<addepto>> Getaddepto(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.addepto == null)
                    {
                        return BadRequest(new { resp = "Entidad addepto es null." });
                    }
                    var addepto = await _context.addepto.FindAsync(codigo);

                    if (addepto == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(addepto);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // PUT: api/addepto/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putaddepto(string userConn, string codigo, addepto addepto)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != addepto.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(addepto).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!addeptoExists(codigo, _context))
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

        // POST: api/addepto
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<addepto>> Postaddepto(string userConn, addepto addepto)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.addepto == null)
                {
                    return BadRequest(new { resp = "Entidad addepto es null." });
                }
                _context.addepto.Add(addepto);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (addeptoExists(addepto.codigo, _context))
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

        // DELETE: api/addepto/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteaddepto(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.addepto == null)
                    {
                        return BadRequest(new { resp = "Entidad addepto es null." });
                    }
                    var addepto = await _context.addepto.FindAsync(codigo);
                    if (addepto == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.addepto.Remove(addepto);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool addeptoExists(string codigo, DBContext _context)
        {
            return (_context.addepto?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
