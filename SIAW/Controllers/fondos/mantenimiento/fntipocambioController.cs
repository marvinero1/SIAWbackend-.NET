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
    public class fntipocambioController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public fntipocambioController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/fntipocambio
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<fntipocambio>>> Getfntipocambio(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fntipocambio == null)
                    {
                        return BadRequest(new { resp = "Entidad fntipocambio es null." });
                    }
                    var result = await _context.fntipocambio
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
                                descUnidad = unidad != null ? unidad.descripcion : null,
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

        // GET: api/fntipocambio/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<fntipocambio>> Getfntipocambio(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fntipocambio == null)
                    {
                        return BadRequest(new { resp = "Entidad fntipocambio es null." });
                    }
                    var fntipocambio = await _context.fntipocambio
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
                                descUnidad = unidad != null ? unidad.descripcion : null,
                            }
                        )
                        .FirstOrDefaultAsync();

                    if (fntipocambio == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(fntipocambio);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/fntipocambio/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putfntipocambio(string userConn, string id, fntipocambio fntipocambio)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != fntipocambio.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(fntipocambio).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!fntipocambioExists(id, _context))
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

        // POST: api/fntipocambio
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<fntipocambio>> Postfntipocambio(string userConn, fntipocambio fntipocambio)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.fntipocambio == null)
                {
                    return BadRequest(new { resp = "Entidad fntipocambio es null." });
                }
                _context.fntipocambio.Add(fntipocambio);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (fntipocambioExists(fntipocambio.id, _context))
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

        // DELETE: api/fntipocambio/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletefntipocambio(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fntipocambio == null)
                    {
                        return BadRequest(new { resp = "Entidad fntipocambio es null." });
                    }
                    var fntipocambio = await _context.fntipocambio.FindAsync(id);
                    if (fntipocambio == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.fntipocambio.Remove(fntipocambio);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool fntipocambioExists(string id, DBContext _context)
        {
            return (_context.fntipocambio?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
