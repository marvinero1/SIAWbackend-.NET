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
    public class fntipotransferenciaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public fntipotransferenciaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/fntipotransferencia
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<fntipotransferencia>>> Getfntipotransferencia(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fntipotransferencia == null)
                    {
                        return BadRequest(new { resp = "Entidad fntipotransferencia es null." });
                    }
                    var result = await _context.fntipotransferencia
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

        // GET: api/fntipotransferencia/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<fntipotransferencia>> Getfntipotransferencia(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fntipotransferencia == null)
                    {
                        return BadRequest(new { resp = "Entidad fntipotransferencia es null." });
                    }
                    var fntipotransferencia = await _context.fntipotransferencia.Where(i => i.id == id)
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

                    if (fntipotransferencia == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(fntipotransferencia);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/fntipotransferencia/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putfntipotransferencia(string userConn, string id, fntipotransferencia fntipotransferencia)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != fntipotransferencia.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(fntipotransferencia).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!fntipotransferenciaExists(id, _context))
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

        // POST: api/fntipotransferencia
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<fntipotransferencia>> Postfntipotransferencia(string userConn, fntipotransferencia fntipotransferencia)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.fntipotransferencia == null)
                {
                    return BadRequest(new { resp = "Entidad fntipotransferencia es null." });
                }
                _context.fntipotransferencia.Add(fntipotransferencia);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (fntipotransferenciaExists(fntipotransferencia.id, _context))
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

        // DELETE: api/fntipotransferencia/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletefntipotransferencia(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fntipotransferencia == null)
                    {
                        return BadRequest(new { resp = "Entidad fntipotransferencia es null." });
                    }
                    var fntipotransferencia = await _context.fntipotransferencia.FindAsync(id);
                    if (fntipotransferencia == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.fntipotransferencia.Remove(fntipotransferencia);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool fntipotransferenciaExists(string id, DBContext _context)
        {
            return (_context.fntipotransferencia?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
