using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ctasXcobrar.mantenimiento
{
    [Route("api/ctsxcob/mant/[controller]")]
    [ApiController]
    public class cotipodescuento_moraController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cotipodescuento_moraController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cotipodescuento_mora
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cotipodescuento_mora>>> Getcotipodescuento_mora(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotipodescuento_mora == null)
                    {
                        return BadRequest(new { resp = "Entidad cotipodescuento_mora es null." });
                    }
                    var result = await _context.cotipodescuento_mora
                        .GroupJoin(
                            _context.adunidad,
                            c => c.codunidad,
                            t => t.codigo,
                            (c, t) => new { c, t })
                        .SelectMany(
                            x => x.t.DefaultIfEmpty(),
                            (x, unidad) => new
                            {
                                x.c.id,
                                x.c.descripcion,
                                x.c.nroactual,
                                x.c.horareg,
                                x.c.fechareg,
                                x.c.usuarioreg,
                                x.c.codunidad,
                                descUnidad = unidad != null ? unidad.descripcion : null
                            }
                        )
                        .OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/cotipodescuento_mora/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<cotipodescuento_mora>> Getcotipodescuento_mora(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotipodescuento_mora == null)
                    {
                        return BadRequest(new { resp = "Entidad cotipodescuento_mora es null." });
                    }
                    var cotipodescuento_mora = await _context.cotipodescuento_mora
                        .Where(i => i.id == id)
                        .GroupJoin(
                            _context.adunidad,
                            c => c.codunidad,
                            t => t.codigo,
                            (c, t) => new { c, t })
                        .SelectMany(
                            x => x.t.DefaultIfEmpty(),
                            (x, unidad) => new
                            {
                                x.c.id,
                                x.c.descripcion,
                                x.c.nroactual,
                                x.c.horareg,
                                x.c.fechareg,
                                x.c.usuarioreg,
                                x.c.codunidad,
                                descUnidad = unidad != null ? unidad.descripcion : null
                            }
                        )
                        .FirstOrDefaultAsync();

                    if (cotipodescuento_mora == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(cotipodescuento_mora);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/cotipodescuento_mora/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putcotipodescuento_mora(string userConn, string id, cotipodescuento_mora cotipodescuento_mora)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != cotipodescuento_mora.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(cotipodescuento_mora).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cotipodescuento_moraExists(id, _context))
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

        // POST: api/cotipodescuento_mora
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cotipodescuento_mora>> Postcotipodescuento_mora(string userConn, cotipodescuento_mora cotipodescuento_mora)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cotipodescuento_mora == null)
                {
                    return BadRequest(new { resp = "Entidad cotipodescuento_mora es null." });
                }
                _context.cotipodescuento_mora.Add(cotipodescuento_mora);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cotipodescuento_moraExists(cotipodescuento_mora.id, _context))
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

        // DELETE: api/cotipodescuento_mora/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletecotipodescuento_mora(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotipodescuento_mora == null)
                    {
                        return BadRequest(new { resp = "Entidad cotipodescuento_mora es null." });
                    }
                    var cotipodescuento_mora = await _context.cotipodescuento_mora.FindAsync(id);
                    if (cotipodescuento_mora == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.cotipodescuento_mora.Remove(cotipodescuento_mora);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool cotipodescuento_moraExists(string id, DBContext _context)
        {
            return (_context.cotipodescuento_mora?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
