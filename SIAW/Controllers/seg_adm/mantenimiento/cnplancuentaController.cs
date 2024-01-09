using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class cnplancuentaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cnplancuentaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cnplancuenta
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cnplancuenta>>> Getcnplancuenta(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cnplancuenta == null)
                    {
                        return BadRequest(new { resp = "Entidad cnplancuenta es null." });
                    }
                    var result = await _context.cnplancuenta.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/cnplancuenta/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<cnplancuenta>> Getcnplancuenta(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cnplancuenta == null)
                    {
                        return BadRequest(new { resp = "Entidad cnplancuenta es null." });
                    }
                    var cnplancuenta = await _context.cnplancuenta.FindAsync(codigo);

                    if (cnplancuenta == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(cnplancuenta);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // PUT: api/cnplancuenta/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putcnplancuenta(string userConn, int codigo, cnplancuenta cnplancuenta)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != cnplancuenta.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(cnplancuenta).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cnplancuentaExists(codigo, _context))
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok( new { resp = "206" });   // actualizado con exito
            }
            


        }

        // POST: api/cnplancuenta
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cnplancuenta>> Postcnplancuenta(string userConn, cnplancuenta cnplancuenta)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cnplancuenta == null)
                {
                    return BadRequest(new { resp = "Entidad cnplancuenta es null." });
                }
                _context.cnplancuenta.Add(cnplancuenta);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cnplancuentaExists(cnplancuenta.codigo, _context))
                    {
                        return Conflict( new { resp = "Ya existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok( new { resp = "204" });   // creado con exito

            }
            
        }

        // DELETE: api/cnplancuenta/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletecnplancuenta(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cnplancuenta == null)
                    {
                        return BadRequest(new { resp = "Entidad cnplancuenta es null." });
                    }
                    var cnplancuenta = await _context.cnplancuenta.FindAsync(codigo);
                    if (cnplancuenta == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.cnplancuenta.Remove(cnplancuenta);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool cnplancuentaExists(int codigo, DBContext _context)
        {
            return (_context.cnplancuenta?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
