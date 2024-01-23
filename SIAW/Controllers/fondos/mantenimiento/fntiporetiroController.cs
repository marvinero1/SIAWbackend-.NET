using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.fondos.mantenimiento
{
    [Route("api/fondos/mant/[controller]")]
    [ApiController]
    public class fntiporetiroController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public fntiporetiroController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/fntiporetiro
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<fntiporetiro>>> Getfntiporetiro(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fntiporetiro == null)
                    {
                        return BadRequest(new { resp = "Entidad fntiporetiro es null." });
                    }
                    var result = await _context.fntiporetiro
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

        // GET: api/fntiporetiro/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<fntiporetiro>> Getfntiporetiro(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fntiporetiro == null)
                    {
                        return BadRequest(new { resp = "Entidad fntiporetiro es null." });
                    }
                    var fntiporetiro = await _context.fntiporetiro.Where(i => i.id == id)
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

                    if (fntiporetiro == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(fntiporetiro);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/fntiporetiro/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putfntiporetiro(string userConn, string id, fntiporetiro fntiporetiro)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != fntiporetiro.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(fntiporetiro).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!fntiporetiroExists(id, _context))
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

        // POST: api/fntiporetiro
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<fntiporetiro>> Postfntiporetiro(string userConn, fntiporetiro fntiporetiro)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.fntiporetiro == null)
                {
                    return BadRequest(new { resp = "Entidad fntiporetiro es null." });
                }
                _context.fntiporetiro.Add(fntiporetiro);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (fntiporetiroExists(fntiporetiro.id, _context))
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

        // DELETE: api/fntiporetiro/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletefntiporetiro(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fntiporetiro == null)
                    {
                        return BadRequest(new { resp = "Entidad fntiporetiro es null." });
                    }
                    var fntiporetiro = await _context.fntiporetiro.FindAsync(id);
                    if (fntiporetiro == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.fntiporetiro.Remove(fntiporetiro);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool fntiporetiroExists(string id, DBContext _context)
        {
            return (_context.fntiporetiro?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
