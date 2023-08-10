using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.contabilidad.mantenimiento
{
    [Route("api/contab/mant/[controller]")]
    [ApiController]
    public class cntipoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cntipoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cntipo
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cntipo>>> Getcntipo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cntipo == null)
                    {
                        return Problem("Entidad cntipo es null.");
                    }
                    var result = await _context.cntipo.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/cntipo/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<cntipo>> Getcntipo(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cntipo == null)
                    {
                        return Problem("Entidad cntipo es null.");
                    }
                    var cntipo = await _context.cntipo.FindAsync(codigo);

                    if (cntipo == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(cntipo);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/cntipo/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putcntipo(string userConn, string codigo, cntipo cntipo)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != cntipo.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(cntipo).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cntipoExists(codigo, _context))
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

        // POST: api/cntipo
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cntipo>> Postcntipo(string userConn, cntipo cntipo)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cntipo == null)
                {
                    return Problem("Entidad cntipo es null.");
                }
                _context.cntipo.Add(cntipo);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cntipoExists(cntipo.codigo, _context))
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

        // DELETE: api/cntipo/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletecntipo(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cntipo == null)
                    {
                        return Problem("Entidad cntipo es null.");
                    }
                    var cntipo = await _context.cntipo.FindAsync(codigo);
                    if (cntipo == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.cntipo.Remove(cntipo);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool cntipoExists(string codigo, DBContext _context)
        {
            return (_context.cntipo?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
