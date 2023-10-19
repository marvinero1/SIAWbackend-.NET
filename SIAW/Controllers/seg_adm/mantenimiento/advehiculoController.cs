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
    public class advehiculoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public advehiculoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/advehiculo
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<advehiculo>>> Getadvehiculo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.advehiculo == null)
                    {
                        return Problem("Entidad advehiculo es null.");
                    }
                    var result = await _context.advehiculo.ToListAsync();
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/advehiculo/5
        [HttpGet("{userConn}/{placa}")]
        public async Task<ActionResult<advehiculo>> Getadvehiculo(string userConn, string placa)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.advehiculo == null)
                    {
                        return Problem("Entidad advehiculo es null.");
                    }
                    var advehiculo = await _context.advehiculo.FindAsync(placa);

                    if (advehiculo == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(advehiculo);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/advehiculo/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{placa}")]
        public async Task<IActionResult> Putadvehiculo(string userConn, string placa, advehiculo advehiculo)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (placa != advehiculo.placa)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(advehiculo).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!advehiculoExists(placa, _context))
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

        // POST: api/advehiculo
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<advehiculo>> Postadvehiculo(string userConn, advehiculo advehiculo)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.advehiculo == null)
                {
                    return Problem("Entidad advehiculo es null.");
                }
                _context.advehiculo.Add(advehiculo);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (advehiculoExists(advehiculo.placa, _context))
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

        // DELETE: api/advehiculo/5
        [Authorize]
        [HttpDelete("{userConn}/{placa}")]
        public async Task<IActionResult> Deleteadvehiculo(string userConn, string placa)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.advehiculo == null)
                    {
                        return Problem("Entidad advehiculo es null.");
                    }
                    var advehiculo = await _context.advehiculo.FindAsync(placa);
                    if (advehiculo == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.advehiculo.Remove(advehiculo);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool advehiculoExists(string placa, DBContext _context)
        {
            return (_context.advehiculo?.Any(e => e.placa == placa)).GetValueOrDefault();

        }
    }
}
