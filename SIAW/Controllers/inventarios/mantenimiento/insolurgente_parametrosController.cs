using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class insolurgente_parametrosController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public insolurgente_parametrosController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/insolurgente_parametros/5
        [HttpGet("{userConn}/{codalmacen}")]
        public async Task<ActionResult<insolurgente_parametros>> Getinsolurgente_parametros(string userConn, int codalmacen)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.insolurgente_parametros == null)
                    {
                        return BadRequest(new { resp = "Entidad insolurgente_parametros es null." });
                    }
                    var insolurgente_parametros = await _context.insolurgente_parametros
                        .Where(i => i.codalmacen == codalmacen)
                        .OrderBy(i => i.suarea)
                        .ThenBy(i => i.codtarifa) .ToListAsync();

                    if (insolurgente_parametros.Count() == 0)
                    {
                        return NotFound(new { resp = "No se encontro ningun registro con los datos proporcionados" });
                    }

                    return Ok(insolurgente_parametros);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // POST: api/insolurgente_parametros
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<insolurgente_parametros>> Postinsolurgente_parametros(string userConn, insolurgente_parametros insolurgente_parametros)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.insolurgente_parametros == null)
                {
                    return BadRequest(new { resp = "Entidad insolurgente_parametros es null." });
                }
                _context.insolurgente_parametros.Add(insolurgente_parametros);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (insolurgente_parametrosExists(insolurgente_parametros.codigo, _context))
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

        // DELETE: api/insolurgente_parametros/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteinsolurgente_parametros(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.insolurgente_parametros == null)
                    {
                        return BadRequest(new { resp = "Entidad insolurgente_parametros es null." });
                    }
                    var insolurgente_parametros = await _context.insolurgente_parametros.FindAsync(codigo);
                    if (insolurgente_parametros == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.insolurgente_parametros.Remove(insolurgente_parametros);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }
        private bool insolurgente_parametrosExists(int codigo, DBContext _context)
        {
            return (_context.insolurgente_parametros?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }

    }
}
