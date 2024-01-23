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
    public class fntiporeposicionController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public fntiporeposicionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/fntiporeposicion
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<fntiporeposicion>>> Getfntiporeposicion(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fntiporeposicion == null)
                    {
                        return BadRequest(new { resp = "Entidad fntiporeposicion es null." });
                    }
                    var result = await _context.fntiporeposicion
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

        // GET: api/fntiporeposicion/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<fntiporeposicion>> Getfntiporeposicion(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fntiporeposicion == null)
                    {
                        return BadRequest(new { resp = "Entidad fntiporeposicion es null." });
                    }
                    var fntiporeposicion = await _context.fntiporeposicion.Where(i => i.id == id)
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


                    if (fntiporeposicion == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(fntiporeposicion);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/fntiporeposicion/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putfntiporeposicion(string userConn, string id, fntiporeposicion fntiporeposicion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != fntiporeposicion.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(fntiporeposicion).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!fntiporeposicionExists(id, _context))
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

        // POST: api/fntiporeposicion
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<fntiporeposicion>> Postfntiporeposicion(string userConn, fntiporeposicion fntiporeposicion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.fntiporeposicion == null)
                {
                    return BadRequest(new { resp = "Entidad fntiporeposicion es null." });
                }
                _context.fntiporeposicion.Add(fntiporeposicion);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (fntiporeposicionExists(fntiporeposicion.id, _context))
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

        // DELETE: api/fntiporeposicion/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletefntiporeposicion(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fntiporeposicion == null)
                    {
                        return BadRequest(new { resp = "Entidad fntiporeposicion es null." });
                    }
                    var fntiporeposicion = await _context.fntiporeposicion.FindAsync(id);
                    if (fntiporeposicion == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.fntiporeposicion.Remove(fntiporeposicion);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool fntiporeposicionExists(string id, DBContext _context)
        {
            return (_context.fntiporeposicion?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
