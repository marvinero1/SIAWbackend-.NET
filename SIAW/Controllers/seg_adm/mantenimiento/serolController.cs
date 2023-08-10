using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class serolController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public serolController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/serol
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<serol>>> Getserol(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.serol == null)
                    {
                        return Problem("Entidad serol es null.");
                    }
                    var result = await _context.serol.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/serol/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<serol>> Getserol(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.serol == null)
                    {
                        return Problem("Entidad serol es null.");
                    }
                    var serol = await _context.serol.FindAsync(codigo);

                    if (serol == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(serol);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/serol/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putserol(string userConn, string codigo, serol serol)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != serol.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(serol).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!serolExists(codigo, _context))
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

        // POST: api/serol
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<serol>> Postserol(string userConn, serol serol)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.serol == null)
                {
                    return Problem("Entidad serol es null.");
                }
                _context.serol.Add(serol);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (serolExists(serol.codigo, _context))
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

        // DELETE: api/serol/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteserol(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.serol == null)
                    {
                        return Problem("Entidad serol es null.");
                    }
                    var serol = await _context.serol.FindAsync(codigo);
                    if (serol == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.serol.Remove(serol);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool serolExists(string codigo, DBContext _context)
        {
            return (_context.serol?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
