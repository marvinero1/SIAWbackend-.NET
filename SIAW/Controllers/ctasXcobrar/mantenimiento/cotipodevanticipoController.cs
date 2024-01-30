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
    public class cotipodevanticipoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cotipodevanticipoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cotipodevanticipo
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cotipodevanticipo>>> Getcotipodevanticipo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotipodevanticipo == null)
                    {
                        return BadRequest(new { resp = "Entidad cotipodevanticipo es null." });
                    }
                    var result = await _context.cotipodevanticipo
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

        // GET: api/cotipodevanticipo/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<cotipodevanticipo>> Getcotipodevanticipo(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotipodevanticipo == null)
                    {
                        return BadRequest(new { resp = "Entidad cotipodevanticipo es null." });
                    }
                    var cotipodevanticipo = await _context.cotipodevanticipo
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

                    if (cotipodevanticipo == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(cotipodevanticipo);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/cotipodevanticipo/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putcotipodevanticipo(string userConn, string id, cotipodevanticipo cotipodevanticipo)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != cotipodevanticipo.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(cotipodevanticipo).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cotipodevanticipoExists(id, _context))
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

        // POST: api/cotipodevanticipo
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cotipodevanticipo>> Postcotipodevanticipo(string userConn, cotipodevanticipo cotipodevanticipo)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cotipodevanticipo == null)
                {
                    return BadRequest(new { resp = "Entidad cotipodevanticipo es null." });
                }
                _context.cotipodevanticipo.Add(cotipodevanticipo);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cotipodevanticipoExists(cotipodevanticipo.id, _context))
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

        // DELETE: api/cotipodevanticipo/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletecotipodevanticipo(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotipodevanticipo == null)
                    {
                        return BadRequest(new { resp = "Entidad cotipodevanticipo es null." });
                    }
                    var cotipodevanticipo = await _context.cotipodevanticipo.FindAsync(id);
                    if (cotipodevanticipo == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.cotipodevanticipo.Remove(cotipodevanticipo);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool cotipodevanticipoExists(string id, DBContext _context)
        {
            return (_context.cotipodevanticipo?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
