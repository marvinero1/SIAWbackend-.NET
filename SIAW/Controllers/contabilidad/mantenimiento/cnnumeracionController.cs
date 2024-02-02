using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.contabilidad.mantenimiento
{
    [Route("api/contab/mant/[controller]")]
    [ApiController]
    public class cnnumeracionController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cnnumeracionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cnnumeracion
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cnnumeracion>>> Getcnnumeracion(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cnnumeracion == null)
                    {
                        return BadRequest(new { resp = "Entidad cnnumeracion es null." });
                    }
                    var result = await _context.cnnumeracion.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/cnnumeracion/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<cnnumeracion>> Getcnnumeracion(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cnnumeracion == null)
                    {
                        return BadRequest(new { resp = "Entidad cnnumeracion es null." });
                    }
                    var cnnumeracion = await _context.cnnumeracion.FindAsync(id);

                    if (cnnumeracion == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(cnnumeracion);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // PUT: api/cnnumeracion/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putcnnumeracion(string userConn, string id, cnnumeracion cnnumeracion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            if (cnnumeracion.hasta < cnnumeracion.desde)
            {
                return BadRequest(new { resp = "El rango de fechas esta mal definido, el valor desde es mayor al valor hasta." });
            }
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != cnnumeracion.id)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(cnnumeracion).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cnnumeracionExists(id, _context))
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok( new { resp = "206" });   // actualizado con exito
            }
        }

        // POST: api/cnnumeracion
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cnnumeracion>> Postcnnumeracion(string userConn, cnnumeracion cnnumeracion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            if (cnnumeracion.hasta < cnnumeracion.desde)
            {
                return BadRequest(new { resp = "El rango de fechas esta mal definido, el valor desde es mayor al valor hasta." });
            }
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cnnumeracion == null)
                {
                    return BadRequest(new { resp = "Entidad cnnumeracion es null." });
                }
                _context.cnnumeracion.Add(cnnumeracion);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cnnumeracionExists(cnnumeracion.id, _context))
                    {
                        return Conflict( new { resp = "Ya existe un registro con ese código" });
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok( new { resp = "204" });   // creado con exito

            }
        }

        // DELETE: api/cnnumeracion/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletecnnumeracion(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cnnumeracion == null)
                    {
                        return BadRequest(new { resp = "Entidad cnnumeracion es null." });
                    }
                    var cnnumeracion = await _context.cnnumeracion.FindAsync(id);
                    if (cnnumeracion == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.cnnumeracion.Remove(cnnumeracion);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool cnnumeracionExists(string id, DBContext _context)
        {
            return (_context.cnnumeracion?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
