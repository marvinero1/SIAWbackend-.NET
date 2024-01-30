using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ctasXcobrar.mantenimiento
{
    [Route("api/ctsxcob/mant/[controller]")]

    [ApiController]
    public class cocuentabController : ControllerBase
    {

        private readonly UserConnectionManager _userConnectionManager;
        public cocuentabController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cocuentab
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cocuentab>>> Getcocuentab(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cocuentab == null)
                    {
                        return BadRequest(new { resp = "Entidad cocuentab es null." });
                    }
                    var result = await _context.cocuentab
                        .Join(
                            _context.cobanco,
                            cc => cc.codbanco,
                            cb => cb.codigo,
                            (cc, cb) => new { cc, cb }
                        )
                        .Join(
                            _context.admoneda,
                            cc_cb => cc_cb.cc.codmoneda,
                            am => am.codigo,
                            (cc_cb, am) => new
                            {
                                cc_cb.cc.codigo,
                                cc_cb.cc.descripcion,
                                cc_cb.cc.codbanco,
                                nombreBanco = cc_cb.cb.nombre,
                                cc_cb.cc.codmoneda,
                                descripcionMoneda = am.descripcion,
                                cc_cb.cc.autorizados,
                                cc_cb.cc.obs,
                                cc_cb.cc.horareg,
                                cc_cb.cc.fechareg,
                                cc_cb.cc.usuarioreg,
                                cc_cb.cc.itf,
                                cc_cb.cc.itf_porcentaje,
                                cc_cb.cc.itf_desde,
                                cc_cb.cc.balance,
                                cc_cb.cc.fecha,
                                cc_cb.cc.trunca,
                                cc_cb.cc.nroactual,
                                cc_cb.cc.id
                            }
                        )
                        .OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/cocuentab/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<cocuentab>> Getcocuentab(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cocuentab == null)
                    {
                        return BadRequest(new { resp = "Entidad cocuentab es null." });
                    }
                    var cocuentab = await _context.cocuentab
                        .Where(i => i.codigo == codigo)
                        .Join(
                            _context.cobanco,
                            cc => cc.codbanco,
                            cb => cb.codigo,
                            (cc, cb) => new { cc, cb }
                        )
                        .Join(
                            _context.admoneda,
                            cc_cb => cc_cb.cc.codmoneda,
                            am => am.codigo,
                            (cc_cb, am) => new
                            {
                                cc_cb.cc.codigo,
                                cc_cb.cc.descripcion,
                                cc_cb.cc.codbanco,
                                nombreBanco = cc_cb.cb.nombre,
                                cc_cb.cc.codmoneda,
                                descripcionMoneda = am.descripcion,
                                cc_cb.cc.autorizados,
                                cc_cb.cc.obs,
                                cc_cb.cc.horareg,
                                cc_cb.cc.fechareg,
                                cc_cb.cc.usuarioreg,
                                cc_cb.cc.itf,
                                cc_cb.cc.itf_porcentaje,
                                cc_cb.cc.itf_desde,
                                cc_cb.cc.balance,
                                cc_cb.cc.fecha,
                                cc_cb.cc.trunca,
                                cc_cb.cc.nroactual,
                                cc_cb.cc.id
                            }
                        )
                        .FirstOrDefaultAsync();

                    if (cocuentab == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(cocuentab);
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
        public async Task<ActionResult<IEnumerable<cocuentab>>> Getcocuentab_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.cocuentab
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion,
                        i.codbanco
                    }).ToListAsync();


                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "No se encontraron registros con esos datos." });
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

        // PUT: api/cocuentab/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putcocuentab(string userConn, string codigo, cocuentab cocuentab)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != cocuentab.codigo)
                {
                    return BadRequest(new { resp = "Error con codigo en datos proporcionados." });
                }

                _context.Entry(cocuentab).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cocuentabExists(codigo, _context))
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

        // POST: api/cocuentab
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cocuentab>> Postcocuentab(string userConn, cocuentab cocuentab)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cocuentab == null)
                {
                    return BadRequest(new { resp = "Entidad cocuentab es null." });
                }
                _context.cocuentab.Add(cocuentab);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cocuentabExists(cocuentab.codigo, _context))
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

        // DELETE: api/cocuentab/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletecocuentab(string userConn, string codigo)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        if (_context.cocuentab == null)
                        {
                            return BadRequest(new { resp = "Entidad cocuentab es null." });
                        }
                        var cocuentab = await _context.cocuentab.FindAsync(codigo);
                        if (cocuentab == null)
                        {
                            return NotFound(new { resp = "No existe un registro con ese código" });
                        }


                        var cocuentab_conta = await _context.cocuentab_conta.Where(i => i.codcuentab == codigo).ToListAsync();
                        if (cocuentab_conta.Count()>0)
                        {
                            _context.cocuentab_conta.RemoveRange(cocuentab_conta);
                            await _context.SaveChangesAsync();
                        }

                        _context.cocuentab.Remove(cocuentab);
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

        private bool cocuentabExists(string codigo, DBContext _context)
        {
            return (_context.cocuentab?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
