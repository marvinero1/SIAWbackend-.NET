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
    public class inlineaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public inlineaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/inlinea
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<inlinea>>> Getinlinea(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inlinea == null)
                    {
                        return BadRequest(new { resp = "Entidad inlinea es null." });
                    }
                    var result = await _context.inlinea.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/inlinea/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<inlinea>> Getinlinea(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inlinea == null)
                    {
                        return BadRequest(new { resp = "Entidad inlinea es null." });
                    }
                    var inlinea = await _context.inlinea.FindAsync(codigo);

                    if (inlinea == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(inlinea);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // PUT: api/inlinea/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putinlinea(string userConn, string codigo, inlinea inlinea)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != inlinea.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(inlinea).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!inlineaExists(codigo, _context))
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

        // POST: api/inlinea
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<inlinea>> Postinlinea(string userConn, inlinea inlinea)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.inlinea == null)
                {
                    return BadRequest(new { resp = "Entidad inlinea es null." });
                }
                _context.inlinea.Add(inlinea);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (inlineaExists(inlinea.codigo, _context))
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

        // DELETE: api/inlinea/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteinlinea(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inlinea == null)
                    {
                        return BadRequest(new { resp = "Entidad inlinea es null." });
                    }
                    var inlinea = await _context.inlinea.FindAsync(codigo);
                    if (inlinea == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.inlinea.Remove(inlinea);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool inlineaExists(string codigo, DBContext _context)
        {
            return (_context.inlinea?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }








    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class insubgrupo_vtaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public insubgrupo_vtaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/insubgrupo_vta
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<insubgrupo_vta>>> Getinsubgrupo_vta(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.insubgrupo_vta == null)
                    {
                        return BadRequest(new { resp = "Entidad insubgrupo_vta es null." });
                    }
                    var result = await _context.insubgrupo_vta.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/insubgrupo_vta/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<insubgrupo_vta>> Getinsubgrupo_vta(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.insubgrupo_vta == null)
                    {
                        return BadRequest(new { resp = "Entidad insubgrupo_vta es null." });
                    }
                    var insubgrupo_vta = await _context.insubgrupo_vta.FindAsync(codigo);

                    if (insubgrupo_vta == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(insubgrupo_vta);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // PUT: api/insubgrupo_vta/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putinsubgrupo_vta(string userConn, string codigo, insubgrupo_vta insubgrupo_vta)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != insubgrupo_vta.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(insubgrupo_vta).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!insubgrupo_vtaExists(codigo, _context))
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

        // POST: api/insubgrupo_vta
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<insubgrupo_vta>> Postinsubgrupo_vta(string userConn, insubgrupo_vta insubgrupo_vta)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.insubgrupo_vta == null)
                {
                    return BadRequest(new { resp = "Entidad insubgrupo_vta es null." });
                }
                _context.insubgrupo_vta.Add(insubgrupo_vta);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (insubgrupo_vtaExists(insubgrupo_vta.codigo, _context))
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

        // DELETE: api/insubgrupo_vta/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteinsubgrupo_vta(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.insubgrupo_vta == null)
                    {
                        return BadRequest(new { resp = "Entidad insubgrupo_vta es null." });
                    }
                    var insubgrupo_vta = await _context.insubgrupo_vta.FindAsync(codigo);
                    if (insubgrupo_vta == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.insubgrupo_vta.Remove(insubgrupo_vta);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool insubgrupo_vtaExists(string codigo, DBContext _context)
        {
            return (_context.insubgrupo_vta?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }

    }


}
