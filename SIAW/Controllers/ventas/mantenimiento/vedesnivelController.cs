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
    public class vedesnivelController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public vedesnivelController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/vedesnivel
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<vedesnivel>>> Getvedesnivel(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedesnivel == null)
                    {
                        return BadRequest(new { resp = "Entidad vedesnivel es null." });
                    }
                    var result = await _context.vedesnivel.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/vedesnivel/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<vedesnivel>> Getvedesnivel(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedesnivel == null)
                    {
                        return BadRequest(new { resp = "Entidad vedesnivel es null." });
                    }
                    var vedesnivel = await _context.vedesnivel.FindAsync(codigo);

                    if (vedesnivel == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(vedesnivel);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}")]
        public async Task<ActionResult<IEnumerable<vedesnivel>>> Getvedesnivel_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.vedesnivel
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad vedesnivel es null." });
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

        // POST: api/vedesnivel
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<vedesnivel>> Postvedesnivel(string userConn, vedesnivel vedesnivel)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.vedesnivel == null)
                {
                    return BadRequest(new { resp = "Entidad vedesnivel es null." });
                }
                _context.vedesnivel.Add(vedesnivel);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (vedesnivelExists(vedesnivel.codigo, _context))
                    {
                        return Conflict( new { resp = "Ya existe un registro con ese código" });
                    }
                    return Problem("Error en el servidor");
                    throw;
                }

                return Ok( new { resp = "204" });   // creado con exito

            }

        }

        // DELETE: api/vedesnivel/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletevedesnivel(string userConn, string codigo)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        if (_context.vedesnivel == null)
                        {
                            return BadRequest(new { resp = "Entidad vedesnivel es null." });
                        }

                        // Eliminar vedesnivel
                        var vedesnivel = await _context.vedesnivel.FindAsync(codigo);
                        if (vedesnivel == null)
                        {
                            return NotFound( new { resp = "No existe un registro con ese código" });
                        }

                        _context.vedesnivel.Remove(vedesnivel);
                        await _context.SaveChangesAsync();

                        // eliminar vedesnivel_clasificacion
                        var vedesnivel_clasificacion = await _context.vedesnivel_clasificacion
                            .Where(i => i.codvedesnivel == codigo)
                            .ToListAsync();
                        if (vedesnivel_clasificacion.Count > 0)
                        {
                            _context.vedesnivel_clasificacion.RemoveRange(vedesnivel_clasificacion);
                            await _context.SaveChangesAsync();
                        }
                        dbContexTransaction.Commit();
                        return Ok( new { resp = "208" });   // eliminado con exito
                    }
                    catch (Exception)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

            }
        }

        private bool vedesnivelExists(string codigo, DBContext _context)
        {
            return (_context.vedesnivel?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }










        // GET: api/vedesnivel_clasificacion
        [HttpGet]
        [Route("vedesnivel_clasificacion/{userConn}/{codvedesnivel}")]
        public async Task<ActionResult<IEnumerable<vedesnivel_clasificacion>>> Getvedesnivel_clasificacion(string userConn, string codvedesnivel)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedesnivel_clasificacion == null)
                    {
                        return BadRequest(new { resp = "Entidad vedesnivel_clasificacion es null." });
                    }
                    var result = await _context.vedesnivel_clasificacion
                        .Where(i => i.codvedesnivel == codvedesnivel)
                        .OrderBy(codigo => codigo.clasificacion).ToListAsync();
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }


        // POST: api/vedesnivel_clasificacion
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost]
        [Route("vedesnivel_clasificacion/{userConn}")]
        public async Task<ActionResult<vedesnivel_clasificacion>> Postvedesnivel_clasificacion(string userConn, vedesnivel_clasificacion vedesnivel_clasificacion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var valida = await _context.vedesnivel_clasificacion
                    .Where(i => i.codvedesnivel == vedesnivel_clasificacion.codvedesnivel && i.clasificacion == vedesnivel_clasificacion.clasificacion)
                    .FirstOrDefaultAsync();
                if (valida != null)
                {
                    return Conflict( new { resp = "Ya existe un registro con los datos proporcionados"});
                }
                _context.vedesnivel_clasificacion.Add(vedesnivel_clasificacion);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                }

                return Ok( new { resp = "204" });   // creado con exito
            }
        }

        // DELETE: api/vedesnivel_clasificacion/5
        [Authorize]
        [HttpDelete]
        [Route("vedesnivel_clasificacion/{userConn}/{codvedesnivel}/{clasificacion}")]
        public async Task<IActionResult> Deletevedesnivel_clasificacion(string userConn, string codvedesnivel, string clasificacion)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var vedesnivel_clasificacion = await _context.vedesnivel_clasificacion
                        .Where(i => i.codvedesnivel == codvedesnivel && i.clasificacion == clasificacion)
                        .FirstOrDefaultAsync();
                    if (vedesnivel_clasificacion == null)
                    {
                        return NotFound( new { resp = "No se encontraron registros con los datos proporcionados." });
                    }

                    _context.vedesnivel_clasificacion.Remove(vedesnivel_clasificacion);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

    }
}
