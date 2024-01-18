using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Route("api/venta/mant/[controller]")]
    [ApiController]
    public class vecartaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public vecartaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/vecarta
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<vecarta>>> Getvecarta(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vecarta == null)
                    {
                        return BadRequest(new { resp = "Entidad vecarta es null." });
                    }
                    var result = await _context.vecarta.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/vecarta/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<vecarta>> Getvecarta(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vecarta == null)
                    {
                        return BadRequest(new { resp = "Entidad vecarta es null." });
                    }
                    var vecarta = await _context.vecarta.FindAsync(codigo);

                    if (vecarta == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(vecarta);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/vecarta/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putvecarta(string userConn, int codigo, vecarta vecarta)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != vecarta.codigo)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(vecarta).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!vecartaExists(codigo, _context))
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok(new { resp = "206" });   // actualizado con exito
            }



        }

        // POST: api/vecarta
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<vecarta>> Postvecarta(string userConn, vecarta vecarta)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.vecarta == null)
                {
                    return BadRequest(new { resp = "Entidad vecarta es null." });
                }
                _context.vecarta.Add(vecarta);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (vecartaExists(vecarta.codigo, _context))
                    {
                        return Conflict(new { resp = "Ya existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok(new { resp = "204" });   // creado con exito

            }

        }

        // DELETE: api/vecarta/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletevecarta(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vecarta == null)
                    {
                        return BadRequest(new { resp = "Entidad vecarta es null." });
                    }
                    var vecarta = await _context.vecarta.FindAsync(codigo);
                    if (vecarta == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.vecarta.Remove(vecarta);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool vecartaExists(int codigo, DBContext _context)
        {
            return (_context.vecarta?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
