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
    public class adprovinciaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public adprovinciaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/adprovincia
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<adprovincia>>> Getadprovincia(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adprovincia == null)
                    {
                        return BadRequest(new { resp = "Entidad adprovincia es null." });
                    }
                    var result = await _context.adprovincia.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/adprovincia/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<adprovincia>> Getadprovincia(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adprovincia == null)
                    {
                        return BadRequest(new { resp = "Entidad adprovincia es null." });
                    }
                    var adprovincia = await _context.adprovincia.FindAsync(codigo);

                    if (adprovincia == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(adprovincia);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }



        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo_depto/{userConn}")]
        public async Task<ActionResult<IEnumerable<adprovincia>>> Getadprovincia_depto_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.adprovincia
                    .Join(
                        _context.addepto,
                        p => p.coddepto,
                        d => d.codigo,
                        (p, d) => new { p.codigo, p.nombre, nombreDept = d.nombre }
                    )
                    .OrderBy(x => x.nombre)
                    .ThenBy(x => x.codigo)
                    .Select(x => new { dato1 = x.codigo, dato2 = x.nombre, dato3 = x.nombreDept });

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




        // PUT: api/adprovincia/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putadprovincia(string userConn, string codigo, adprovincia adprovincia)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != adprovincia.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(adprovincia).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adprovinciaExists(codigo, _context))
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

        // POST: api/adprovincia
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<adprovincia>> Postadprovincia(string userConn, adprovincia adprovincia)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adprovincia == null)
                {
                    return BadRequest(new { resp = "Entidad adprovincia es null." });
                }
                _context.adprovincia.Add(adprovincia);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adprovinciaExists(adprovincia.codigo, _context))
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

        // DELETE: api/adprovincia/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteadprovincia(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adprovincia == null)
                    {
                        return BadRequest(new { resp = "Entidad adprovincia es null." });
                    }
                    var adprovincia = await _context.adprovincia.FindAsync(codigo);
                    if (adprovincia == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.adprovincia.Remove(adprovincia);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool adprovinciaExists(string codigo, DBContext _context)
        {
            return (_context.adprovincia?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
