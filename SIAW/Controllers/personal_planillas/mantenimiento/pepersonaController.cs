using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.personal_planillas.mantenimiento
{
    [Route("api/pers_plan/mant/[controller]")]
    [ApiController]
    public class pepersonaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public pepersonaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/pepersona
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<pepersona>>> Getpepersona(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.pepersona == null)
                    {
                        return Problem("Entidad pepersona es null.");
                    }
                    var result = await _context.pepersona.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/pepersona/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<pepersona>> Getpepersona(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.pepersona == null)
                    {
                        return Problem("Entidad pepersona es null.");
                    }
                    var pepersona = await _context.pepersona.FindAsync(codigo);

                    if (pepersona == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(pepersona);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }



        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}")]
        public async Task<ActionResult<IEnumerable<pepersona>>> Getpepersona_catalogo_resumido(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.pepersona
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        codigo = i.codigo,
                        descrip = i.nombre1 + " " + i.apellido1 + " " + i.apellido2
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return Problem("Entidad pepersona es null.");
                    }
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }



        // PUT: api/pepersona/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putpepersona(string userConn, int codigo, pepersona pepersona)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != pepersona.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(pepersona).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!pepersonaExists(codigo, _context))
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

        // POST: api/pepersona
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<pepersona>> Postpepersona(string userConn, pepersona pepersona)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.pepersona == null)
                {
                    return Problem("Entidad pepersona es null.");
                }
                _context.pepersona.Add(pepersona);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (pepersonaExists(pepersona.codigo, _context))
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

        // DELETE: api/pepersona/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletepepersona(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.pepersona == null)
                    {
                        return Problem("Entidad pepersona es null.");
                    }
                    var pepersona = await _context.pepersona.FindAsync(codigo);
                    if (pepersona == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.pepersona.Remove(pepersona);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool pepersonaExists(int codigo, DBContext _context)
        {
            return (_context.pepersona?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
