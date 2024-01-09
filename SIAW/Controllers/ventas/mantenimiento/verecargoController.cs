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
    public class verecargoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public verecargoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/verecargo
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<verecargo>>> Getverecargo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.verecargo == null)
                    {
                        return BadRequest(new { resp = "Entidad verecargo es null." });
                    }
                    var result = await _context.verecargo
                        .Join(_context.admoneda,
                        vr => vr.moneda,
                        am => am.codigo,
                        (vr, am) => new
                        {
                            codigo = vr.codigo,
                            descorta = vr.descorta,
                            descripcion = vr.descripcion,
                            porcentaje = vr.porcentaje,
                            monto = vr.monto,
                            moneda = vr.moneda,
                            mondesc = am.descripcion,
                            montopor = vr.montopor,
                            modificable = vr.modificable,
                            horareg = vr.horareg,
                            fechareg = vr.fechareg,
                            usuarioreg = vr.usuarioreg
                        })
                        .OrderBy(codigo => codigo.codigo)
                        .ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/verecargo/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<verecargo>> Getverecargo(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.verecargo == null)
                    {
                        return BadRequest(new { resp = "Entidad verecargo es null." });
                    }
                    var verecargo = await _context.verecargo
                        .Join(_context.admoneda,
                        vr => vr.moneda,
                        am => am.codigo,
                        (vr, am) => new
                        {
                            codigo = vr.codigo,
                            descorta = vr.descorta,
                            descripcion = vr.descripcion,
                            porcentaje = vr.porcentaje,
                            monto = vr.monto,
                            moneda = vr.moneda,
                            mondesc = am.descripcion,
                            montopor = vr.montopor,
                            modificable = vr.modificable,
                            horareg = vr.horareg,
                            fechareg = vr.fechareg,
                            usuarioreg = vr.usuarioreg
                        })
                        .Where(c => c.codigo == codigo)
                        .FirstOrDefaultAsync();

                    if (verecargo == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }
                    return Ok(verecargo);
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
        public async Task<ActionResult<IEnumerable<verecargo>>> Getverecargo_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.verecargo
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion
                    });
                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad verecargo es null." });
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

        // PUT: api/verecargo/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putverecargo(string userConn, int codigo, verecargo verecargo)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != verecargo.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }
                if (!verecargoExists(codigo, _context))
                {
                    return NotFound( new { resp = "No existe un registro con ese código" });
                }
                _context.Entry(verecargo).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Problem("Error en el servidor");
                    throw;

                }
                return Ok( new { resp = "206" });   // actualizado con exito
            }
        }

        // POST: api/verecargo
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<verecargo>> Postverecargo(string userConn, verecargo verecargo)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.verecargo == null)
                {
                    return BadRequest(new { resp = "Entidad verecargo es null." });
                }
                if (verecargoExists(verecargo.codigo, _context))
                {
                    return Conflict( new { resp = "Ya existe un registro con ese código" });
                }
                _context.verecargo.Add(verecargo);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                    throw;
                }
                return Ok( new { resp = "204" });   // creado con exito
            }
        }

        // DELETE: api/verecargo/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteverecargo(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.verecargo == null)
                    {
                        return BadRequest(new { resp = "Entidad verecargo es null." });
                    }
                    var verecargo = await _context.verecargo.FindAsync(codigo);
                    if (verecargo == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }
                    _context.verecargo.Remove(verecargo);
                    await _context.SaveChangesAsync();
                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool verecargoExists(int codigo, DBContext _context)
        {
            return (_context.verecargo?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
