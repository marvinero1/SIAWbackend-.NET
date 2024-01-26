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
    public class fndeudorController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public fndeudorController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/fndeudor
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<fndeudor>>> Getfndeudor(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fndeudor == null)
                    {
                        return BadRequest(new { resp = "Entidad fndeudor es null." });
                    }
                    var result = await _context.fndeudor
                        .GroupJoin(
                            _context.pepersona,
                            c => c.codpersona,
                            t => t.codigo,
                            (c, t) => new { c, t })
                        .SelectMany(
                            x => x.t.DefaultIfEmpty(),
                            (x, persona) => new
                            {
                                x.c.id,
                                x.c.descripcion,
                                x.c.fechareg,
                                x.c.usuarioreg,
                                x.c.horareg,
                                x.c.codpersona,
                                descCuenta = persona != null ? persona.apellido1 + " " + persona.nombre1 : null,
                            }
                        )
                        .OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/fndeudor/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<fndeudor>> Getfndeudor(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fndeudor == null)
                    {
                        return BadRequest(new { resp = "Entidad fndeudor es null." });
                    }
                    var fndeudor = await _context.fndeudor
                        .Where(i => i.id == id)
                        .GroupJoin(
                            _context.pepersona,
                            c => c.codpersona,
                            t => t.codigo,
                            (c, t) => new {c,t})
                        .SelectMany(
                            x => x.t.DefaultIfEmpty(),
                            (x, persona) => new
                            {
                                x.c.id,
                                x.c.descripcion,
                                x.c.fechareg,
                                x.c.usuarioreg,
                                x.c.horareg,
                                x.c.codpersona,
                                descCuenta = persona != null ? persona.apellido1 + " " + persona.nombre1 : null,
                            }
                        )
                        .FirstOrDefaultAsync();

                    if (fndeudor == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(fndeudor);
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
        public async Task<ActionResult<IEnumerable<fndeudor>>> Getfndeudor_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.fndeudor
                    .OrderBy(i => i.id)
                    .Select(i => new
                    {
                        i.id,
                        i.descripcion
                    });

                    var result = query.ToList();

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




        // PUT: api/fndeudor/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putfndeudor(string userConn, string id, fndeudor fndeudor)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != fndeudor.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(fndeudor).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!fndeudorExists(id, _context))
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

        // POST: api/fndeudor
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<fndeudor>> Postfndeudor(string userConn, fndeudor fndeudor)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.fndeudor == null)
                {
                    return BadRequest(new { resp = "Entidad fndeudor es null." });
                }
                _context.fndeudor.Add(fndeudor);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (fndeudorExists(fndeudor.id, _context))
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

        // DELETE: api/fndeudor/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletefndeudor(string userConn, string id)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        if (_context.fndeudor == null)
                        {
                            return BadRequest(new { resp = "Entidad fndeudor es null." });
                        }
                        var fndeudor = await _context.fndeudor.FindAsync(id);
                        if (fndeudor == null)
                        {
                            return NotFound(new { resp = "No existe un registro con ese código" });
                        }

                        var fndeudor_conta = await _context.fndeudor_conta.Where(i => i.iddeudor == id).ToListAsync();
                        if (fndeudor_conta.Count > 0)
                        {
                            _context.fndeudor_conta.RemoveRange(fndeudor_conta);
                            await _context.SaveChangesAsync();
                        }


                        _context.fndeudor.Remove(fndeudor);
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

        private bool fndeudorExists(string id, DBContext _context)
        {
            return (_context.fndeudor?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
