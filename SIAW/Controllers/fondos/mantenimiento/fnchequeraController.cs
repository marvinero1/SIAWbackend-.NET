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
    public class fnchequeraController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public fnchequeraController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/fnchequera
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<fnchequera>>> Getfnchequera(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fnchequera == null)
                    {
                        return BadRequest(new { resp = "Entidad fnchequera es null." });
                    }
                    var result = await _context.fnchequera
                        .GroupJoin(
                            _context.cocuentab,
                            c => c.codcuentab,
                            t => t.codigo,
                            (c, t) => new { c, t })
                        .SelectMany(
                            x => x.t.DefaultIfEmpty(),
                            (x, Cuenta) => new
                            {
                                x.c.id,
                                x.c.nrodesde,
                                x.c.nrohasta,
                                x.c.nroactual,
                                x.c.descripcion,
                                x.c.codcuentab,
                                descCuenta = Cuenta != null ? Cuenta.descripcion : null,
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

        // GET: api/fnchequera/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<fnchequera>> Getfnchequera(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fnchequera == null)
                    {
                        return BadRequest(new { resp = "Entidad fnchequera es null." });
                    }
                    var fnchequera = await _context.fnchequera
                        .GroupJoin(
                            _context.cocuentab,
                            c => c.codcuentab,
                            t => t.codigo,
                            (c, t) => new { c, t })
                        .SelectMany(
                            x => x.t.DefaultIfEmpty(),
                            (x, Cuenta) => new
                            {
                                x.c.id,
                                x.c.nrodesde,
                                x.c.nrohasta,
                                x.c.nroactual,
                                x.c.descripcion,
                                x.c.codcuentab,
                                descCuenta = Cuenta != null ? Cuenta.descripcion : null,
                                x.c.horareg,
                                x.c.fechareg,
                                x.c.usuarioreg

                            }
                        )
                        .Where(i => i.id == id)
                        .FirstOrDefaultAsync();

                    if (fnchequera == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(fnchequera);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/fnchequera/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putfnchequera(string userConn, string id, fnchequera fnchequera)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != fnchequera.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(fnchequera).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!fnchequeraExists(id, _context))
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

        // POST: api/fnchequera
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<fnchequera>> Postfnchequera(string userConn, fnchequera fnchequera)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.fnchequera == null)
                {
                    return BadRequest(new { resp = "Entidad fnchequera es null." });
                }
                _context.fnchequera.Add(fnchequera);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (fnchequeraExists(fnchequera.id, _context))
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

        // DELETE: api/fnchequera/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletefnchequera(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fnchequera == null)
                    {
                        return BadRequest(new { resp = "Entidad fnchequera es null." });
                    }
                    var fnchequera = await _context.fnchequera.FindAsync(id);
                    if (fnchequera == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.fnchequera.Remove(fnchequera);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool fnchequeraExists(string id, DBContext _context)
        {
            return (_context.fnchequera?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
