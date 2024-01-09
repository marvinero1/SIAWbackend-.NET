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
    public class vetipocreditoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public vetipocreditoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/vetipocredito
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<vetipocredito>>> Getvetipocredito(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vetipocredito == null)
                    {
                        return BadRequest(new { resp = "Entidad vetipocredito es null." });
                    }
                    var result = await _context.vetipocredito.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/vetipocredito/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<vetipocredito>> Getvetipocredito(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vetipocredito == null)
                    {
                        return BadRequest(new { resp = "Entidad vetipocredito es null." });
                    }
                    var vetipocredito = await _context.vetipocredito.FindAsync(codigo);

                    if (vetipocredito == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }
                    return Ok(vetipocredito);
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
        public async Task<ActionResult<IEnumerable<vetipocredito>>> Getvetipocredito_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.vetipocredito
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion,
                        i.duracion
                    });
                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad vetipocredito es null." });
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

        // PUT: api/vetipocredito/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putvetipocredito(string userConn, string codigo, vetipocredito vetipocredito)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != vetipocredito.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }
                if (!vetipocreditoExists(codigo, _context))
                {
                    return NotFound( new { resp = "No existe un registro con ese código" });
                }
                _context.Entry(vetipocredito).State = EntityState.Modified;

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

        // POST: api/vetipocredito
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<vetipocredito>> Postvetipocredito(string userConn, vetipocredito vetipocredito)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.vetipocredito == null)
                {
                    return BadRequest(new { resp = "Entidad vetipocredito es null." });
                }
                if (vetipocreditoExists(vetipocredito.codigo, _context))
                {
                    return Conflict( new { resp = "Ya existe un registro con ese código" });
                }
                _context.vetipocredito.Add(vetipocredito);
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

        // DELETE: api/vetipocredito/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletevetipocredito(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vetipocredito == null)
                    {
                        return BadRequest(new { resp = "Entidad vetipocredito es null." });
                    }
                    var vetipocredito = await _context.vetipocredito.FindAsync(codigo);
                    if (vetipocredito == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }
                    _context.vetipocredito.Remove(vetipocredito);
                    await _context.SaveChangesAsync();
                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool vetipocreditoExists(string codigo, DBContext _context)
        {
            return (_context.vetipocredito?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
