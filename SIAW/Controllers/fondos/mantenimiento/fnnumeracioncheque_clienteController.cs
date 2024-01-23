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
    public class fnnumeracioncheque_clienteController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public fnnumeracioncheque_clienteController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/fnnumeracioncheque_cliente
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<fnnumeracioncheque_cliente>>> Getfnnumeracioncheque_cliente(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fnnumeracioncheque_cliente == null)
                    {
                        return BadRequest(new { resp = "Entidad fnnumeracioncheque_cliente es null." });
                    }
                    var result = await _context.fnnumeracioncheque_cliente
                        .GroupJoin(
                            _context.adunidad,
                            c => c.codunidad,
                            t => t.codigo,
                            (c, t) => new { c, t })
                        .SelectMany(
                            x => x.t.DefaultIfEmpty(),
                            (x, unidad) => new
                            {
                                x.c.id,
                                x.c.descripcion,
                                x.c.nroactual,
                                x.c.horareg,
                                x.c.fechareg,
                                x.c.usuarioreg,
                                x.c.codunidad,
                                descUnidad = unidad != null ? unidad.descripcion : null
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

        // GET: api/fnnumeracioncheque_cliente/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<fnnumeracioncheque_cliente>> Getfnnumeracioncheque_cliente(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fnnumeracioncheque_cliente == null)
                    {
                        return BadRequest(new { resp = "Entidad fnnumeracioncheque_cliente es null." });
                    }
                    var fnnumeracioncheque_cliente = await _context.fnnumeracioncheque_cliente
                        .Where(i => i.id == id)
                        .GroupJoin(
                            _context.adunidad,
                            c => c.codunidad,
                            t => t.codigo,
                            (c, t) => new { c, t })
                        .SelectMany(
                            x => x.t.DefaultIfEmpty(),
                            (x, unidad) => new
                            {
                                x.c.id,
                                x.c.descripcion,
                                x.c.nroactual,
                                x.c.horareg,
                                x.c.fechareg,
                                x.c.usuarioreg,
                                x.c.codunidad,
                                descUnidad = unidad != null ? unidad.descripcion : null
                            }
                        )
                        .FirstOrDefaultAsync();


                    if (fnnumeracioncheque_cliente == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(fnnumeracioncheque_cliente);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/fnnumeracioncheque_cliente/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putfnnumeracioncheque_cliente(string userConn, string id, fnnumeracioncheque_cliente fnnumeracioncheque_cliente)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != fnnumeracioncheque_cliente.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(fnnumeracioncheque_cliente).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!fnnumeracioncheque_clienteExists(id, _context))
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

        // POST: api/fnnumeracioncheque_cliente
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<fnnumeracioncheque_cliente>> Postfnnumeracioncheque_cliente(string userConn, fnnumeracioncheque_cliente fnnumeracioncheque_cliente)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.fnnumeracioncheque_cliente == null)
                {
                    return BadRequest(new { resp = "Entidad fnnumeracioncheque_cliente es null." });
                }
                _context.fnnumeracioncheque_cliente.Add(fnnumeracioncheque_cliente);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (fnnumeracioncheque_clienteExists(fnnumeracioncheque_cliente.id, _context))
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

        // DELETE: api/fnnumeracioncheque_cliente/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletefnnumeracioncheque_cliente(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fnnumeracioncheque_cliente == null)
                    {
                        return BadRequest(new { resp = "Entidad fnnumeracioncheque_cliente es null." });
                    }
                    var fnnumeracioncheque_cliente = await _context.fnnumeracioncheque_cliente.FindAsync(id);
                    if (fnnumeracioncheque_cliente == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.fnnumeracioncheque_cliente.Remove(fnnumeracioncheque_cliente);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool fnnumeracioncheque_clienteExists(string id, DBContext _context)
        {
            return (_context.fnnumeracioncheque_cliente?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
