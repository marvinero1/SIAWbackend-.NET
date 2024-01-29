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
    public class cotipopagomoraController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cotipopagomoraController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cotipopagomora
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cotipopagomora>>> Getcotipopagomora(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotipopagomora == null)
                    {
                        return BadRequest(new { resp = "Entidad cotipopagomora es null." });
                    }
                    var result = await _context.cotipopagomora
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

        // GET: api/cotipopagomora/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<cotipopagomora>> Getcotipopagomora(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotipopagomora == null)
                    {
                        return BadRequest(new { resp = "Entidad cotipopagomora es null." });
                    }
                    var cotipopagomora = await _context.cotipopagomora
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

                    if (cotipopagomora == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(cotipopagomora);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/cotipopagomora/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putcotipopagomora(string userConn, string id, cotipopagomora cotipopagomora)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != cotipopagomora.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(cotipopagomora).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cotipopagomoraExists(id, _context))
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

        // POST: api/cotipopagomora
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cotipopagomora>> Postcotipopagomora(string userConn, cotipopagomora cotipopagomora)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cotipopagomora == null)
                {
                    return BadRequest(new { resp = "Entidad cotipopagomora es null." });
                }
                _context.cotipopagomora.Add(cotipopagomora);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cotipopagomoraExists(cotipopagomora.id, _context))
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

        // DELETE: api/cotipopagomora/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletecotipopagomora(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotipopagomora == null)
                    {
                        return BadRequest(new { resp = "Entidad cotipopagomora es null." });
                    }
                    var cotipopagomora = await _context.cotipopagomora.FindAsync(id);
                    if (cotipopagomora == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.cotipopagomora.Remove(cotipopagomora);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool cotipopagomoraExists(string id, DBContext _context)
        {
            return (_context.cotipopagomora?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
