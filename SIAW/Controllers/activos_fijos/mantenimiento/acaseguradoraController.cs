using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;


namespace SIAW.Controllers.activos_fijos.mantenimiento
{
    [Route("api/act_fij/mant/[controller]")]
    [ApiController]
    public class acaseguradoraController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;

        public acaseguradoraController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/acaseguradora
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<acaseguradora>>> Getacaseguradora(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.acaseguradora == null)
                    {
                        return Problem("Entidad acaseguradora es null.");
                    }

                    var result = await _context.acaseguradora.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // GET: api/acaseguradora/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<acaseguradora>> Getacaseguradora(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.acaseguradora == null)
                    {
                        return Problem("Entidad acaseguradora es null.");
                    }
                    var acaseguradora = await _context.acaseguradora.FindAsync(codigo);

                    if (acaseguradora == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(acaseguradora);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/acaseguradora/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putacaseguradora(string userConn, int codigo, acaseguradora acaseguradora)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != acaseguradora.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(acaseguradora).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!acaseguradoraExists(codigo, _context))
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

        // POST: api/acaseguradora
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<acaseguradora>> Postacaseguradora(string userConn, acaseguradora acaseguradora)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.acaseguradora == null)
                {
                    return Problem("Entidad acaseguradora es null.");
                }
                _context.acaseguradora.Add(acaseguradora);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (acaseguradoraExists(acaseguradora.codigo, _context))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        return BadRequest("Error en el servidor");
                        throw;
                    }
                }

                return Ok("204");   // creado con exito

            }
            
        }

        // DELETE: api/acaseguradora/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteacaseguradora(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.acaseguradora == null)
                    {
                        return Problem("Entidad acaseguradora es null.");
                    }
                    var acaseguradora = await _context.acaseguradora.FindAsync(codigo);
                    if (acaseguradora == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.acaseguradora.Remove(acaseguradora);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool acaseguradoraExists(int codigo, DBContext _context)
        {
            return (_context.acaseguradora?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }

    }
}
