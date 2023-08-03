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
    public class adusparametrosController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public adusparametrosController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/adusparametros
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<adusparametros>>> Getadusparametros(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adusparametros == null)
                    {
                        return Problem("Entidad adusparametros es null.");
                    }
                    var result = await _context.adusparametros.OrderBy(usuario => usuario.usuario).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/adusparametros/5
        [HttpGet("{userConn}/{usuario}")]
        public async Task<ActionResult<adusparametros>> Getadusparametros(string userConn, string usuario)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adusparametros == null)
                    {
                        return Problem("Entidad adusparametros es null.");
                    }
                    var adusparametros = await _context.adusparametros.FindAsync(usuario);

                    if (adusparametros == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(adusparametros);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/adusparametros/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{usuario}")]
        public async Task<IActionResult> Putadusparametros(string userConn, string usuario, adusparametros adusparametros)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (usuario != adusparametros.usuario)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(adusparametros).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adusparametrosExists(usuario, _context))
                    {
                        return NotFound("No existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("Datos actualizados correctamente.");
            }
            


        }

        // POST: api/adusparametros
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<adusparametros>> Postadusparametros(string userConn, adusparametros adusparametros)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adusparametros == null)
                {
                    return Problem("Entidad adusparametros es null.");
                }
                _context.adusparametros.Add(adusparametros);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adusparametrosExists(adusparametros.usuario, _context))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("Registrado con Exito :D");

            }
            
        }

        // DELETE: api/adusparametros/5
        [Authorize]
        [HttpDelete("{userConn}/{usuario}")]
        public async Task<IActionResult> Deleteadusparametros(string userConn, string usuario)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adusparametros == null)
                    {
                        return Problem("Entidad adusparametros es null.");
                    }
                    var adusparametros = await _context.adusparametros.FindAsync(usuario);
                    if (adusparametros == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.adusparametros.Remove(adusparametros);
                    await _context.SaveChangesAsync();

                    return Ok("Datos eliminados con exito");
                }
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool adusparametrosExists(string usuario, DBContext _context)
        {
            return (_context.adusparametros?.Any(e => e.usuario == usuario)).GetValueOrDefault();

        }
    }
}
