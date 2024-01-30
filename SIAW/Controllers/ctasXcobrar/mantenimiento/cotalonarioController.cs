using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ctasXcobrar.mantenimiento
{
    [Route("api/ctsxcob/mant/[controller]")]
    [ApiController]
    public class cotalonarioController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cotalonarioController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/cotalonario
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<cotalonario>>> Getcotalonario(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotalonario == null)
                    {
                        return BadRequest(new { resp = "Entidad cotalonario es null." });
                    }
                    var result = await _context.cotalonario
                        .GroupJoin(
                            _context.vevendedor,
                            c => c.codvendedor,
                            t => t.codigo,
                            (c, t) => new { c, t })
                        .SelectMany(
                            x => x.t.DefaultIfEmpty(),
                            (x, vendedor) => new
                            {
                                codigo = x.c.codigo,
                                descripcion = x.c.descripcion,
                                TalDel = x.c.TalDel,
                                TalAl = x.c.TalAl,
                                nroactual = x.c.nroactual,
                                Fecha = x.c.Fecha,
                                horareg = x.c.horareg,
                                fechareg = x.c.fechareg,
                                Usuarioreg = x.c.Usuarioreg,
                                codvendedor = x.c.codvendedor,
                                descVendedor = vendedor != null ? vendedor.descripcion : null
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

        // GET: api/cotalonario/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<cotalonario>> Getcotalonario(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotalonario == null)
                    {
                        return BadRequest(new { resp = "Entidad cotalonario es null." });
                    }
                    var cotalonario = await _context.cotalonario
                        .Where(i => i.codigo == codigo)
                        .GroupJoin(
                            _context.vevendedor,
                            c => c.codvendedor,
                            t => t.codigo,
                            (c, t) => new { c, t })
                        .SelectMany(
                            x => x.t.DefaultIfEmpty(),
                            (x, vendedor) => new
                            {
                                x.c.codigo,
                                x.c.descripcion,
                                x.c.TalDel,
                                x.c.TalAl,
                                x.c.nroactual,
                                x.c.Fecha,
                                x.c.horareg,
                                x.c.fechareg,
                                x.c.Usuarioreg,
                                x.c.codvendedor,
                                descVendedor = vendedor != null ? vendedor.descripcion : null
                            }
                        )
                        .FirstOrDefaultAsync();

                    if (cotalonario == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(cotalonario);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/cotalonario/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putcotalonario(string userConn, string codigo, cotalonario cotalonario)
        {

            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != cotalonario.codigo)
                {
                    return BadRequest(new { resp = "Error con codigo en datos proporcionados." });
                }

                if (cotalonario.TalDel > cotalonario.TalAl)
                {
                    return BadRequest(new { resp = "El numero final no puede ser menor al numero inicial." });
                }
                if (cotalonario.nroactual < cotalonario.TalDel || cotalonario.nroactual > cotalonario.TalAl)
                {
                    return BadRequest(new { resp = "El numero actual esta fuera de los limites del talonario." });
                }
                _context.Entry(cotalonario).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cotalonarioExists(codigo, _context))
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

        // POST: api/cotalonario
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<cotalonario>> Postcotalonario(string userConn, cotalonario cotalonario)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.cotalonario == null)
                {
                    return BadRequest(new { resp = "Entidad cotalonario es null." });
                }
                if (cotalonario.TalDel > cotalonario.TalAl)
                {
                    return BadRequest(new { resp = "El numero final no puede ser menor al numero inicial." });
                }
                if (cotalonario.nroactual < cotalonario.TalDel || cotalonario.nroactual > cotalonario.TalAl)
                {
                    return BadRequest(new { resp = "El numero actual esta fuera de los limites del talonario." });
                }
                _context.cotalonario.Add(cotalonario);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cotalonarioExists(cotalonario.codigo, _context))
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

        // DELETE: api/cotalonario/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletecotalonario(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.cotalonario == null)
                    {
                        return BadRequest(new { resp = "Entidad cotalonario es null." });
                    }
                    var cotalonario = await _context.cotalonario.FindAsync(codigo);
                    if (cotalonario == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.cotalonario.Remove(cotalonario);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool cotalonarioExists(string codigo, DBContext _context)
        {
            return (_context.cotalonario?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
