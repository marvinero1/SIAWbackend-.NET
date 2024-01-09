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
    public class veptoventaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public veptoventaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/veptoventa
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<veptoventa>>> Getveptoventa(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.veptoventa == null)
                    {
                        return BadRequest(new { resp = "Entidad veptoventa es null." });
                    }
                    var result = await _context.veptoventa.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/veptoventa/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<veptoventa>> Getveptoventa(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.veptoventa == null)
                    {
                        return BadRequest(new { resp = "Entidad veptoventa es null." });
                    }
                    var veptoventa = await _context.veptoventa.FindAsync(codigo);

                    if (veptoventa == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(veptoventa);
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
        public async Task<ActionResult<IEnumerable<veptoventa>>> Getveptoventa_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = _context.veptoventa
                    .Join(_context.adprovincia,
                        p => p.codprovincia,
                        v => v.codigo,
                        (p, v) => new
                        {
                            codigo = p.codigo.ToString(),
                            descripcion = p.descripcion,
                            ubicacion = v.coddepto + " - " + v.nombre
                        })
                    .OrderBy(p => p.codigo)
                    .ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad veptoventa es null." });
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

        // PUT: api/veptoventa/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putveptoventa(string userConn, int codigo, veptoventa veptoventa)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != veptoventa.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(veptoventa).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!veptoventaExists(codigo, _context))
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok( new { resp = "206" });   // actualizado con exito
            }



        }

        // POST: api/veptoventa
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<veptoventa>> Postveptoventa(string userConn, veptoventa veptoventa)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.veptoventa == null)
                {
                    return BadRequest(new { resp = "Entidad veptoventa es null." });
                }
                _context.veptoventa.Add(veptoventa);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (veptoventaExists(veptoventa.codigo, _context))
                    {
                        return Conflict( new { resp = "Ya existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok( new { resp = "204" });   // creado con exito

            }

        }

        // DELETE: api/veptoventa/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteveptoventa(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.veptoventa == null)
                    {
                        return BadRequest(new { resp = "Entidad veptoventa es null." });
                    }
                    var veptoventa = await _context.veptoventa.FindAsync(codigo);
                    if (veptoventa == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.veptoventa.Remove(veptoventa);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool veptoventaExists(int codigo, DBContext _context)
        {
            return (_context.veptoventa?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
