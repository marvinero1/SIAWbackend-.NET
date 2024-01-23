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
    public class fntipo_librobancoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public fntipo_librobancoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/fntipo_librobanco
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<fntipo_librobanco>>> Getfntipo_librobanco(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fntipo_librobanco == null)
                    {
                        return BadRequest(new { resp = "Entidad fntipo_librobanco es null." });
                    }
                    var result = await _context.fntipo_librobanco
                        .GroupJoin(
                            _context.cocuentab,
                            c => c.codcuentab,
                            t => t.codigo,
                            (c, t) => new { c, t })
                        .SelectMany(
                            x => x.t.DefaultIfEmpty(),
                            (x, cuentatab) => new
                            {
                                x.c.id,
                                x.c.descripcion,
                                x.c.nroactual,
                                x.c.codcuentab,
                                descCuenta = cuentatab != null ? cuentatab.descripcion : null,
                                x.c.desde,
                                x.c.hasta,
                                x.c.origen,
                                x.c.horareg,
                                x.c.fechareg,
                                x.c.usuarioreg
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

        // GET: api/fntipo_librobanco/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<fntipo_librobanco>> Getfntipo_librobanco(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fntipo_librobanco == null)
                    {
                        return BadRequest(new { resp = "Entidad fntipo_librobanco es null." });
                    }
                    var fntipo_librobanco = await _context.fntipo_librobanco
                          .Where(i => i.id == id)
                        .GroupJoin(
                            _context.cocuentab,
                            c => c.codcuentab,
                            t => t.codigo,
                            (c, t) => new { c, t })
                        .SelectMany(
                            x => x.t.DefaultIfEmpty(),
                            (x, cuentatab) => new
                            {
                                x.c.id,
                                x.c.descripcion,
                                x.c.nroactual,
                                x.c.codcuentab,
                                descCuenta = cuentatab != null ? cuentatab.descripcion : null,
                                x.c.desde,
                                x.c.hasta,
                                x.c.origen,
                                x.c.horareg,
                                x.c.fechareg,
                                x.c.usuarioreg
                            }
                        )
                        .FirstOrDefaultAsync();

                    if (fntipo_librobanco == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(fntipo_librobanco);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/fntipo_librobanco/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putfntipo_librobanco(string userConn, string id, fntipo_librobanco fntipo_librobanco)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != fntipo_librobanco.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(fntipo_librobanco).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!fntipo_librobancoExists(id, _context))
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

        // POST: api/fntipo_librobanco
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<fntipo_librobanco>> Postfntipo_librobanco(string userConn, fntipo_librobanco fntipo_librobanco)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.fntipo_librobanco == null)
                {
                    return BadRequest(new { resp = "Entidad fntipo_librobanco es null." });
                }
                _context.fntipo_librobanco.Add(fntipo_librobanco);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (fntipo_librobancoExists(fntipo_librobanco.id, _context))
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

        // DELETE: api/fntipo_librobanco/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletefntipo_librobanco(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fntipo_librobanco == null)
                    {
                        return BadRequest(new { resp = "Entidad fntipo_librobanco es null." });
                    }
                    var fntipo_librobanco = await _context.fntipo_librobanco.FindAsync(id);
                    if (fntipo_librobanco == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.fntipo_librobanco.Remove(fntipo_librobanco);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool fntipo_librobancoExists(string id, DBContext _context)
        {
            return (_context.fntipo_librobanco?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
