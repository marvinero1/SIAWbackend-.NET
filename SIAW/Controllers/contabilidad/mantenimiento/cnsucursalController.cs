using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.contabilidad.mantenimiento
{
    [Route("api/contab/mant/[controller]")]
    [ApiController]
    public class cnsucursalController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cnsucursalController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cnsucursal
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cnsucursal>>> Getcnsucursal(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cnsucursal == null)
                    {
                        return BadRequest(new { resp = "Entidad cnsucursal es null." });
                    }
                    var result = await _context.cnsucursal
                        .Join( _context.inalmacen,
                        cs => cs.codalmacen,
                        ia => ia.codigo,
                        (cs, ia) => new
                        {
                            codigo = cs.codigo,
                            codalmacen = cs.codalmacen,
                            almDesc = ia.descripcion,
                            descripcion = cs.descripcion,
                            horareg = cs.horareg,
                            fechareg = cs.fechareg,
                            usuarioreg = cs.usuarioreg,

                        })
                        .OrderBy(i => i.codigo).ToListAsync();
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/cnsucursal/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<cnsucursal>> Getcnsucursal(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cnsucursal == null)
                    {
                        return BadRequest(new { resp = "Entidad cnsucursal es null." });
                    }
                    var cnsucursal = await _context.cnsucursal
                        .Join(_context.inalmacen,
                        cs => cs.codalmacen,
                        ia => ia.codigo,
                        (cs, ia) => new
                        {
                            codigo = cs.codigo,
                            codalmacen = cs.codalmacen,
                            almDesc = ia.descripcion,
                            descripcion = cs.descripcion,
                            horareg = cs.horareg,
                            fechareg = cs.fechareg,
                            usuarioreg = cs.usuarioreg,

                        })
                        .Where(i => i.codigo == codigo)
                        .FirstOrDefaultAsync();

                    if (cnsucursal == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(cnsucursal);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // PUT: api/cnsucursal/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putcnsucursal(string userConn, string codigo, cnsucursal cnsucursal)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != cnsucursal.codigo)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }
                if (!cnsucursalExists(codigo, _context))
                {
                    return NotFound(new { resp = "No existe un registro con ese código" });
                }

                var validaAlmacen = await _context.cnsucursal
                    .Where(i => i.codalmacen == cnsucursal.codalmacen)
                    .FirstOrDefaultAsync();
                if (validaAlmacen != null)
                {
                    if (validaAlmacen.codigo != codigo)
                    {
                        return BadRequest(new { resp = "Ya hay una sucursal asignada al almacen que usted eligio." });
                    }
                }

                _context.Entry(cnsucursal).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Problem("Error en el servidor");
                    throw;
                }

                return Ok(new { resp = "206" });   // actualizado con exito
            }



        }

        // POST: api/cnsucursal
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cnsucursal>> Postcnsucursal(string userConn, cnsucursal cnsucursal)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cnsucursal == null)
                {
                    return BadRequest(new { resp = "Entidad cnsucursal es null." });
                }

                if (cnsucursalExists(cnsucursal.codigo, _context))
                {
                    return Conflict(new { resp = "Ya existe un registro con ese código" });
                }

                var validaAlmacen = await _context.cnsucursal
                    .Where(i => i.codalmacen == cnsucursal.codalmacen)
                    .FirstOrDefaultAsync();
                if (validaAlmacen != null)
                {
                    return BadRequest(new { resp = "Ya hay una sucursal asignada al almacen que usted eligio." });
                }

                _context.cnsucursal.Add(cnsucursal);
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

        // DELETE: api/cnsucursal/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletecnsucursal(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cnsucursal == null)
                    {
                        return BadRequest(new { resp = "Entidad cnsucursal es null." });
                    }
                    var cnsucursal = await _context.cnsucursal.FindAsync(codigo);
                    if (cnsucursal == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.cnsucursal.Remove(cnsucursal);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool cnsucursalExists(string codigo, DBContext _context)
        {
            return (_context.cnsucursal?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
