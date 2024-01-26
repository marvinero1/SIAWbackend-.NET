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
    public class fndeudor_compuestoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public fndeudor_compuestoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/fndeudor_compuesto
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<fndeudor_compuesto>>> Getfndeudor_compuesto(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fndeudor_compuesto == null)
                    {
                        return BadRequest(new { resp = "Entidad fndeudor_compuesto es null." });
                    }
                    var result = await _context.fndeudor_compuesto.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/fndeudor_compuesto/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<fndeudor_compuesto>> Getfndeudor_compuesto(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fndeudor_compuesto == null)
                    {
                        return BadRequest(new { resp = "Entidad fndeudor_compuesto es null." });
                    }
                    var fndeudor_compuesto = await _context.fndeudor_compuesto.FindAsync(id);

                    if (fndeudor_compuesto == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(fndeudor_compuesto);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/fndeudor_compuesto/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putfndeudor_compuesto(string userConn, string id, fndeudor_compuesto fndeudor_compuesto)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != fndeudor_compuesto.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(fndeudor_compuesto).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!fndeudor_compuestoExists(id, _context))
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

        // POST: api/fndeudor_compuesto
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<fndeudor_compuesto>> Postfndeudor_compuesto(string userConn, fndeudor_compuesto fndeudor_compuesto)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.fndeudor_compuesto == null)
                {
                    return BadRequest(new { resp = "Entidad fndeudor_compuesto es null." });
                }
                _context.fndeudor_compuesto.Add(fndeudor_compuesto);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (fndeudor_compuestoExists(fndeudor_compuesto.id, _context))
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

        // DELETE: api/fndeudor_compuesto/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletefndeudor_compuesto(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fndeudor_compuesto == null)
                    {
                        return BadRequest(new { resp = "Entidad fndeudor_compuesto es null." });
                    }
                    var fndeudor_compuesto = await _context.fndeudor_compuesto.FindAsync(id);
                    if (fndeudor_compuesto == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.fndeudor_compuesto.Remove(fndeudor_compuesto);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool fndeudor_compuestoExists(string id, DBContext _context)
        {
            return (_context.fndeudor_compuesto?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }










    [Route("api/fondos/mant/[controller]")]
    [ApiController]
    public class fndeudor_compuesto1Controller : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public fndeudor_compuesto1Controller(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/fndeudor_compuesto1/5
        [HttpGet("{userConn}/{iddeudor_compuesto}")]
        public async Task<ActionResult<fndeudor_compuesto1>> Getfndeudor_compuesto1(string userConn, string iddeudor_compuesto)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fndeudor_compuesto1 == null)
                    {
                        return BadRequest(new { resp = "Entidad fndeudor_compuesto1 es null." });
                    }
                    var fndeudor_compuesto1 = await _context.fndeudor_compuesto1
                        .Where(i => i.iddeudor_compuesto == iddeudor_compuesto) .ToListAsync();

                    if (fndeudor_compuesto1.Count() == 0)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(fndeudor_compuesto1);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        

        // POST: api/fndeudor_compuesto1
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<fndeudor_compuesto1>> Postfndeudor_compuesto1(string userConn, fndeudor_compuesto1 fndeudor_compuesto1)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        if (_context.fndeudor_compuesto1 == null)
                        {
                            return BadRequest(new { resp = "Entidad fndeudor_compuesto1 es null." });
                        }
                        var deudorExite = await _context.fndeudor_compuesto1
                            .Where(i => i.iddeudor_compuesto ==  fndeudor_compuesto1.iddeudor_compuesto && i.iddeudor == fndeudor_compuesto1.iddeudor)
                            .ToListAsync();
                        // si hay deudores con mismos codigos elimina primero
                        if (deudorExite.Count() > 0)
                        {
                            _context.fndeudor_compuesto1.RemoveRange(deudorExite);
                            await _context.SaveChangesAsync();
                        }
                        // graba luego
                        _context.fndeudor_compuesto1.Add(fndeudor_compuesto1);
                        await _context.SaveChangesAsync();



                        dbContexTransaction.Commit();
                        return Ok(new { resp = "204" });   // creado con exito
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

        // DELETE: api/fndeudor_compuesto1/5
        [Authorize]
        [HttpDelete("{userConn}/{iddeudor_compuesto}/{iddeudor}")]
        public async Task<IActionResult> Deletefndeudor_compuesto1(string userConn, string iddeudor_compuesto, string iddeudor)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fndeudor_compuesto1 == null)
                    {
                        return BadRequest(new { resp = "Entidad fndeudor_compuesto1 es null." });
                    }
                    var fndeudor_compuesto1 = await _context.fndeudor_compuesto1
                            .Where(i => i.iddeudor_compuesto == iddeudor_compuesto && i.iddeudor == iddeudor)
                            .FirstOrDefaultAsync();
                    if (fndeudor_compuesto1 == null)
                    {
                        return NotFound(new { resp = "No existe un registro con los datos proporcionados" });
                    }

                    _context.fndeudor_compuesto1.Remove(fndeudor_compuesto1);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

    }
}
