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
    public class veobs_rutaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public veobs_rutaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/veobs_ruta
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<veobs_ruta>>> Getveobs_ruta(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.veobs_ruta == null)
                    {
                        return BadRequest(new { resp = "Entidad veobs_ruta es null." });
                    }
                    var result = await _context.veobs_ruta.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/veobs_ruta/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<veobs_ruta>> Getveobs_ruta(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.veobs_ruta == null)
                    {
                        return BadRequest(new { resp = "Entidad veobs_ruta es null." });
                    }
                    var veobs_ruta = await _context.veobs_ruta.FindAsync(codigo);

                    if (veobs_ruta == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }
                    return Ok(veobs_ruta);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}")]
        public async Task<ActionResult<IEnumerable<veobs_ruta>>> Getveobs_ruta_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.veobs_ruta
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion,
                        i.tipo
                    });
                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad veobs_ruta es null." });
                    }
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }

        // PUT: api/veobs_ruta/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putveobs_ruta(string userConn, string codigo, veobs_ruta veobs_ruta)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != veobs_ruta.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }
                if (!veobs_rutaExists(codigo, _context))
                {
                    return NotFound( new { resp = "No existe un registro con ese código" });
                }
                _context.Entry(veobs_ruta).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Problem("Error en el servidor");
                    throw;

                }
                return Ok( new { resp = "206" });   // actualizado con exito
            }
        }

        // POST: api/veobs_ruta
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<veobs_ruta>> Postveobs_ruta(string userConn, veobs_ruta veobs_ruta)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.veobs_ruta == null)
                {
                    return BadRequest(new { resp = "Entidad veobs_ruta es null." });
                }
                if (veobs_rutaExists(veobs_ruta.codigo, _context))
                {
                    return Conflict( new { resp = "Ya existe un registro con ese código" });
                }
                _context.veobs_ruta.Add(veobs_ruta);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                    throw;
                }
                return Ok( new { resp = "204" });   // creado con exito
            }
        }

        // DELETE: api/veobs_ruta/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteveobs_ruta(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.veobs_ruta == null)
                    {
                        return BadRequest(new { resp = "Entidad veobs_ruta es null." });
                    }
                    var veobs_ruta = await _context.veobs_ruta
                        .Where(c => c.codigo == codigo)
                        .FirstOrDefaultAsync();
                    if (veobs_ruta == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }
                    _context.veobs_ruta.Remove(veobs_ruta);
                    await _context.SaveChangesAsync();
                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool veobs_rutaExists(string codigo, DBContext _context)
        {
            return (_context.veobs_ruta?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
