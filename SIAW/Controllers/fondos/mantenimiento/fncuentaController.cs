using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.fondos.mantenimiento
{
    [Route("api/fondos/mant/[controller]")]
    [ApiController]
    public class fncuentaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public fncuentaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/fncuenta
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<fncuenta>>> Getfncuenta(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fncuenta == null)
                    {
                        return BadRequest(new { resp = "Entidad fncuenta es null." });
                    }
                    var result = await _context.fncuenta
                        .GroupJoin(
                            _context.admoneda,
                            c => c.codmoneda,
                            t => t.codigo,
                            (c, t) => new { c, t })
                        .SelectMany(
                            x => x.t.DefaultIfEmpty(),
                            (x, moneda) => new
                            {
                                x.c.id,
                                x.c.descripcion,
                                x.c.fechareg,
                                x.c.usuarioreg,
                                x.c.horareg,
                                x.c.balance,
                                x.c.fecha,
                                x.c.codmoneda,
                                descUnidad = moneda != null ? moneda.descripcion : null,
                                x.c.tipo_movimiento
                            }
                        )
                        .OrderBy(id => id.id)
                        .ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/fncuenta/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<fncuenta>> Getfncuenta(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fncuenta == null)
                    {
                        return BadRequest(new { resp = "Entidad fncuenta es null." });
                    }
                    var fncuenta = await _context.fncuenta
                        .GroupJoin(
                            _context.admoneda,
                            c => c.codmoneda,
                            t => t.codigo,
                            (c, t) => new { c, t })
                        .SelectMany(
                            x => x.t.DefaultIfEmpty(),
                            (x, moneda) => new
                            {
                                x.c.id,
                                x.c.descripcion,
                                x.c.fechareg,
                                x.c.usuarioreg,
                                x.c.horareg,
                                x.c.balance,
                                x.c.fecha,
                                x.c.codmoneda,
                                descUnidad = moneda != null ? moneda.descripcion : null,
                                x.c.tipo_movimiento
                            }
                        )
                        .Where(i => i.id == id)
                        .FirstOrDefaultAsync();

                    if (fncuenta == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(fncuenta);
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
        public async Task<ActionResult<IEnumerable<fncuenta>>> Getfncuenta_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.fncuenta
                    .OrderBy(i => i.id)
                    .Select(i => new
                    {
                        i.id,
                        i.descripcion
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad fncuenta es null." });
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

        // PUT: api/fncuenta/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putfncuenta(string userConn, string id, fncuenta fncuenta)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != fncuenta.id)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(fncuenta).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!fncuentaExists(id, _context))
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

        // POST: api/fncuenta
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<fncuenta>> Postfncuenta(string userConn, fncuenta fncuenta)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.fncuenta == null)
                {
                    return BadRequest(new { resp = "Entidad fncuenta es null." });
                }
                _context.fncuenta.Add(fncuenta);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (fncuentaExists(fncuenta.id, _context))
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

        // DELETE: api/fncuenta/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletefncuenta(string userConn, string id)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        if (_context.fncuenta == null)
                        {
                            return BadRequest(new { resp = "Entidad fncuenta es null." });
                        }
                        var fncuenta = await _context.fncuenta.FindAsync(id);
                        if (fncuenta == null)
                        {
                            return NotFound(new { resp = "No existe un registro con ese código" });
                        }

                        var fncuenta_conta = await _context.fncuenta_conta.Where(i => i.idcuenta == id).ToListAsync();
                        if (fncuenta_conta.Count > 0)
                        {
                            _context.fncuenta_conta.RemoveRange(fncuenta_conta);
                            await _context.SaveChangesAsync();
                        }

                        _context.fncuenta.Remove(fncuenta);
                        await _context.SaveChangesAsync();

                        dbContexTransaction.Commit();
                        return Ok(new { resp = "208" });   // eliminado con exito
                    }
                    catch (Exception)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor");
                        throw;
                    }
                }
            }

        }

        private bool fncuentaExists(string id, DBContext _context)
        {
            return (_context.fncuenta?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
