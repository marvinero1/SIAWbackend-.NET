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
    public class vetiendaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public vetiendaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/vetienda
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<vetienda>>> Getvetienda(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vetienda == null)
                    {
                        return BadRequest(new { resp = "Entidad vetienda es null." });
                    }
                    var result = await _context.vetienda.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/vetienda/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<vetienda>> Getvetienda(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vetienda == null)
                    {
                        return BadRequest(new { resp = "Entidad vetienda es null." });
                    }
                    var vetienda = await _context.vetienda.FindAsync(codigo);

                    if (vetienda == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(vetienda);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }



        // GET: api/vetienda/5
        [HttpGet]
        [Route("getCentral/{userConn}/{codcliente}")]
        public async Task<ActionResult<vetienda>> GetvetiendaCentral(string userConn, string codcliente)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vetienda == null)
                    {
                        return BadRequest(new { resp = "Entidad vetienda es null." });
                    }
                    var vetienda = await _context.vetienda
                        .Where(v => v.codcliente == codcliente && v.central == true)
                        .Join(_context.veptoventa, v => v.codptoventa, p => p.codigo, (v, p) => new
                        {
                            telefono = v.telefono,
                            direccion = v.direccion + " (" + p.descripcion + " - " + p.codprovincia + ")",
                            central = v.central
                        })
                        .FirstOrDefaultAsync();

                    if (vetienda == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(vetienda);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }



        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}/{codcliente}")]
        public async Task<ActionResult<IEnumerable<vetienda>>> Getvetienda_catalogo(string userConn, string codcliente)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.vetienda
                    .Where(v => v.codcliente == codcliente)
                    .Join(_context.veptoventa, v => v.codptoventa, p => p.codigo, (v, p) => new
                    {
                        telefono = v.telefono,
                        direccion = v.direccion + " (" + p.descripcion + " - " + p.codprovincia + ")",
                        central = v.central,
                        latitud = v.latitud,
                        longitud = v.longitud
                    })
                    .ToListAsync();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad vetienda es null." });
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

        // PUT: api/vetienda/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putvetienda(string userConn, int codigo, vetienda vetienda)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != vetienda.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(vetienda).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!vetiendaExists(codigo, _context))
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

        // POST: api/vetienda
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<vetienda>> Postvetienda(string userConn, vetienda vetienda)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.vetienda == null)
                {
                    return BadRequest(new { resp = "Entidad vetienda es null." });
                }
                _context.vetienda.Add(vetienda);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (vetiendaExists(vetienda.codigo, _context))
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

        // DELETE: api/vetienda/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletevetienda(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vetienda == null)
                    {
                        return BadRequest(new { resp = "Entidad vetienda es null." });
                    }
                    var vetienda = await _context.vetienda.FindAsync(codigo);
                    if (vetienda == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.vetienda.Remove(vetienda);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool vetiendaExists(int codigo, DBContext _context)
        {
            return (_context.vetienda?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
