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
    public class inalmacen_reservaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public inalmacen_reservaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/inalmacen_reserva/5
        [HttpGet("{userConn}/{codalmacen}")]
        public async Task<ActionResult<inalmacen_reserva>> Getinalmacen_reserva(string userConn, int codalmacen)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inalmacen_reserva == null)
                    {
                        return BadRequest(new { resp = "Entidad inalmacen_reserva es null." });
                    }

                    var codigosAlmacenReserva = await _context.inalmacen_reserva
                        .Where(r => r.codalmacen == codalmacen)
                        .Select(r => r.codalmacen_reserva).ToListAsync();

                    if (codigosAlmacenReserva.Count() == 0)
                    {
                        return NotFound(new { resp = "No se encontro ningun registro con los datos proporcionados" });
                    }

                    var result = await _context.inalmacen
                        .Where(a => codigosAlmacenReserva.Contains(a.codigo))
                        .Select(a => new
                        {
                            Codigo = a.codigo,
                            Descripcion = a.descripcion
                        })
                        .ToListAsync();

                    if (result.Count() == 0)
                    {
                        return NotFound(new { resp = "No se encontro ningun registro con los datos proporcionados" });
                    }

                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // POST: api/inalmacen_reserva
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<inalmacen_reserva>> Postinalmacen_reserva(string userConn, inalmacen_reserva inalmacen_reserva)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.inalmacen_reserva == null)
                {
                    return BadRequest(new { resp = "Entidad inalmacen_reserva es null." });
                }

                var valida = await _context.inalmacen_reserva
                    .Where(i => i.codalmacen == inalmacen_reserva.codalmacen && i.codalmacen_reserva == inalmacen_reserva.codalmacen_reserva)
                    .FirstOrDefaultAsync();
                if (valida != null)
                {
                    return BadRequest(new { resp = "Este almacen ya esta en la lista." });
                }

                _context.inalmacen_reserva.Add(inalmacen_reserva);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                    throw;
                }

                return Ok(new { resp = "204" });   // creado con exito
            }
        }

        // DELETE: api/inalmacen_reserva/5
        [Authorize]
        [HttpDelete("{userConn}/{codalmacen}/{codalmacen_reserva}")]
        public async Task<IActionResult> Deleteinalmacen_reserva(string userConn, int codalmacen, int codalmacen_reserva)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inalmacen_reserva == null)
                    {
                        return BadRequest(new { resp = "Entidad inalmacen_reserva es null." });
                    }
                    var inalmacen_reserva = await _context.inalmacen_reserva
                    .Where(i => i.codalmacen == codalmacen && i.codalmacen_reserva == codalmacen_reserva)
                    .FirstOrDefaultAsync();
                    if (inalmacen_reserva == null)
                    {
                        return NotFound(new { resp = "No existe un registro con los datos proporcionados" });
                    }

                    _context.inalmacen_reserva.Remove(inalmacen_reserva);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }
    }
}
