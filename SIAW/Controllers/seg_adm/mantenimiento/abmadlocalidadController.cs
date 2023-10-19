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
    public class abmadlocalidadController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public abmadlocalidadController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/adlocalidad
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<adlocalidad>>> Getadlocalidad(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adlocalidad == null)
                    {
                        return Problem("Entidad adlocalidad es null.");
                    }
                    var result = await _context.adlocalidad.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/adlocalidad/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<adlocalidad>> Getadlocalidad(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adlocalidad == null)
                    {
                        return Problem("Entidad adlocalidad es null.");
                    }
                    var adlocalidad = await _context.adlocalidad.FindAsync(codigo);

                    if (adlocalidad == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(adlocalidad);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/adlocalidad/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putadlocalidad(string userConn, string codigo, adlocalidad adlocalidad)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != adlocalidad.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(adlocalidad).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adlocalidadExists(codigo, _context))
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

        // POST: api/adlocalidad
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<adlocalidad>> Postadlocalidad(string userConn, adlocalidad adlocalidad)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adlocalidad == null)
                {
                    return Problem("Entidad adlocalidad es null.");
                }
                _context.adlocalidad.Add(adlocalidad);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adlocalidadExists(adlocalidad.codigo, _context))
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

        // DELETE: api/adlocalidad/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteadlocalidad(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adlocalidad == null)
                    {
                        return Problem("Entidad adlocalidad es null.");
                    }
                    var adlocalidad = await _context.adlocalidad.FindAsync(codigo);
                    if (adlocalidad == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.adlocalidad.Remove(adlocalidad);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool adlocalidadExists(string codigo, DBContext _context)
        {
            return (_context.adlocalidad?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
