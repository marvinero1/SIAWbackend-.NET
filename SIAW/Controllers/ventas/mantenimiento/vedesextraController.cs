using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Route("api/[controller]")]
    [ApiController]
    public class vedesextraController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public vedesextraController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/vedesextra
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<vedesextra>>> Getvedesextra(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedesextra == null)
                    {
                        return BadRequest(new { resp = "Entidad vedesextra es null." });
                    }
                    var result = await _context.vedesextra.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/vedesextra/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<vedesextra>> Getvedesextra(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedesextra == null)
                    {
                        return BadRequest(new { resp = "Entidad vedesextra es null." });
                    }
                    var vedesextra = await _context.vedesextra.FindAsync(codigo);

                    if (vedesextra == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(vedesextra);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // PUT: api/vedesextra/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putvedesextra(string userConn, int codigo, vedesextra vedesextra)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != vedesextra.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(vedesextra).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!vedesextraExists(codigo, _context))
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

        // POST: api/vedesextra
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<vedesextra>> Postvedesextra(string userConn, vedesextra vedesextra)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.vedesextra == null)
                {
                    return BadRequest(new { resp = "Entidad vedesextra es null." });
                }
                _context.vedesextra.Add(vedesextra);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (vedesextraExists(vedesextra.codigo, _context))
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

        // DELETE: api/vedesextra/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletevedesextra(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedesextra == null)
                    {
                        return BadRequest(new { resp = "Entidad vedesextra es null." });
                    }
                    var vedesextra = await _context.vedesextra.FindAsync(codigo);
                    if (vedesextra == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.vedesextra.Remove(vedesextra);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool vedesextraExists(int codigo, DBContext _context)
        {
            return (_context.vedesextra?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
