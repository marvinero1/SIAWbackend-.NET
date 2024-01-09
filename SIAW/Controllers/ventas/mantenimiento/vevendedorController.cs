using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class vevendedorController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public vevendedorController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/vevendedor
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<vevendedor>>> Getvevendedor(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vevendedor == null)
                    {
                        return BadRequest(new { resp = "Entidad vevendedor es null." });
                    }
                    var result = await _context.vevendedor.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/vevendedor/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<vevendedor>> Getvevendedor(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vevendedor == null)
                    {
                        return BadRequest(new { resp = "Entidad vevendedor es null." });
                    }
                    var vevendedor = await _context.vevendedor.FindAsync(codigo);

                    if (vevendedor == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(vevendedor);
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
        public async Task<ActionResult<IEnumerable<vevendedor>>> Getvevendedor_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.vevendedor
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest( new { resp = "No se encontraron registros con esos datos." });
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



        // PUT: api/vevendedor/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putvevendedor(string userConn, int codigo, vevendedor vevendedor)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != vevendedor.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(vevendedor).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!vevendedorExists(codigo, _context))
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

        // POST: api/vevendedor
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<vevendedor>> Postvevendedor(string userConn, vevendedor vevendedor)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.vevendedor == null)
                {
                    return BadRequest(new { resp = "Entidad vevendedor es null." });
                }
                _context.vevendedor.Add(vevendedor);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (vevendedorExists(vevendedor.codigo, _context))
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

        // DELETE: api/vevendedor/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletevevendedor(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vevendedor == null)
                    {
                        return BadRequest(new { resp = "Entidad vevendedor es null." });
                    }
                    var vevendedor = await _context.vevendedor.FindAsync(codigo);
                    if (vevendedor == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.vevendedor.Remove(vevendedor);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool vevendedorExists(int codigo, DBContext _context)
        {
            return (_context.vevendedor?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
