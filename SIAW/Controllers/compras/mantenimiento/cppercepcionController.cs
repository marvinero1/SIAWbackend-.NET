using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.compras.mantenimiento
{
    [Route("api/compras/mant/[controller]")]
    [ApiController]
    public class cppercepcionController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cppercepcionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cppercepcion
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cppercepcion>>> Getcppercepcion(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cppercepcion == null)
                    {
                        return BadRequest(new { resp = "Entidad cppercepcion es null." });
                    }
                    var result = await _context.cppercepcion
                        .OrderBy(i => i.codigo).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/cppercepcion/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<cppercepcion>> Getcppercepcion(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cppercepcion == null)
                    {
                        return BadRequest(new { resp = "Entidad cppercepcion es null." });
                    }
                    var cppercepcion = await _context.cppercepcion
                        .Where(i => i.codigo == codigo)
                        .FirstOrDefaultAsync();


                    if (cppercepcion == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(cppercepcion);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/cppercepcion/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putcppercepcion(string userConn, int codigo, cppercepcion cppercepcion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != cppercepcion.codigo)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(cppercepcion).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cppercepcionExists(codigo, _context))
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

        // POST: api/cppercepcion
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cppercepcion>> Postcppercepcion(string userConn, cppercepcion cppercepcion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cppercepcion == null)
                {
                    return BadRequest(new { resp = "Entidad cppercepcion es null." });
                }
                // obtenemos ultimo codigo y aumentamos 1
                /*
                var ultimoDato = await _context.cppercepcion.OrderByDescending(i=> i.codigo).FirstOrDefaultAsync();
                var ultimoCodigo = 1;
                if (ultimoDato != null)
                {
                    ultimoCodigo = ultimoDato.codigo + 1;
                }
                cppercepcion.codigo = ultimoCodigo;
                */
                _context.cppercepcion.Add(cppercepcion);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cppercepcionExists(cppercepcion.codigo, _context))
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

        // DELETE: api/cppercepcion/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletecppercepcion(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cppercepcion == null)
                    {
                        return BadRequest(new { resp = "Entidad cppercepcion es null." });
                    }
                    var cppercepcion = await _context.cppercepcion.FindAsync(codigo);
                    if (cppercepcion == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.cppercepcion.Remove(cppercepcion);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool cppercepcionExists(int codigo, DBContext _context)
        {
            return (_context.cppercepcion?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
