using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Storage;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class ingrupoperController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public ingrupoperController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/ingrupoper
        [HttpGet("{userConn}/{codinvconsol}")]
        public async Task<ActionResult<IEnumerable<ingrupoper>>> Getingrupoper(string userConn, int codinvconsol)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.ingrupoper == null)
                    {
                        return Problem("Entidad ingrupoper es null.");
                    }
                    var result = await _context.ingrupoper
                        .Where(i => i.codinvconsol == codinvconsol)
                        .OrderBy(codigo => codigo.codigo)
                        .ToListAsync();
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }



        // POST: api/ingrupoper
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<ingrupoper>> Postingrupoper(string userConn, ingrupoper ingrupoper)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.ingrupoper == null)
                {
                    return Problem("Entidad ingrupoper es null.");
                }
                _context.ingrupoper.Add(ingrupoper);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (ingrupoperExists(ingrupoper.codigo, _context))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("204");   // creado con exito

            }

        }

        // DELETE: api/ingrupoper/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteingrupoper(string userConn, int codigo)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // elimina grupo cabecera
                        var ingrupoper = await _context.ingrupoper.FindAsync(codigo);
                        if (ingrupoper == null)
                        {
                            return NotFound("No existe un registro con ese código");
                        }
                        _context.ingrupoper.Remove(ingrupoper);
                        await _context.SaveChangesAsync();

                        // elimina integrantes del grupo si es que hay
                        var ingrupoper1 = await _context.ingrupoper1
                            .Where(i => i.codgrupoper == ingrupoper.codigo)
                            .ToListAsync();
                        
                        if (ingrupoper1.Count()>0)
                        {
                            _context.ingrupoper1.RemoveRange(ingrupoper1);
                            await _context.SaveChangesAsync();
                        }
                       
                        dbContexTransaction.Commit();
                        return Ok("208");   // eliminado con exito
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

        private bool ingrupoperExists(int codigo, DBContext _context)
        {
            return (_context.ingrupoper?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }






    // cuerpo de los grupos, es decir detalle o quienes estan 

    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class ingrupoper1Controller : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public ingrupoper1Controller(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/ingrupoper1
        [HttpGet("{userConn}/{codgrupoper}")]
        public async Task<ActionResult<IEnumerable<ingrupoper1>>> Getingrupoper1(string userConn, int codgrupoper)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.ingrupoper1 == null)
                    {
                        return Problem("Entidad ingrupoper1 es null.");
                    }
                    var result = await _context.ingrupoper1
                        .Join(
                            _context.pepersona,
                            ing => ing.codpersona,
                            pe => pe.codigo,
                            (ing, pe) => new { ing, pe }
                        )
                        .Where(i => i.ing.codgrupoper == codgrupoper)
                        .OrderBy(i => i.ing.codpersona)
                        .Select(i => new
                        {
                            codgrupoper = i.ing.codgrupoper,
                            codpersona = i.ing.codpersona,
                            nomPersona = i.pe.nombre1 + " " + i.pe.apellido1 + " " + i.pe.apellido2
                        })
                        .ToListAsync();
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }


        // POST: api/ingrupoper1
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<ingrupoper1>> Postingrupoper1(string userConn, ingrupoper1 ingrupoper1)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.ingrupoper1 == null)
                {
                    return Problem("Entidad ingrupoper1 es null.");
                }
                _context.ingrupoper1.Add(ingrupoper1);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (ingrupoper1Exists(ingrupoper1.codgrupoper, ingrupoper1.codpersona, _context))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("204");   // creado con exito

            }

        }

        // DELETE: api/ingrupoper1/5
        [Authorize]
        [HttpDelete("{userConn}/{codgrupoper}/{codpersona}")]
        public async Task<IActionResult> Deleteingrupoper1(string userConn, int codgrupoper, int codpersona)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.ingrupoper1 == null)
                    {
                        return Problem("Entidad ingrupoper1 es null.");
                    }
                    var ingrupoper1 = await _context.ingrupoper1
                        .Where(i => i.codgrupoper==codgrupoper && i.codpersona == codpersona)
                        .FirstOrDefaultAsync();
                    if (ingrupoper1 == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.ingrupoper1.Remove(ingrupoper1);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool ingrupoper1Exists(int codgrupoper, int codpersona, DBContext _context)
        {
            return (_context.ingrupoper1?.Any(e => e.codgrupoper == codgrupoper && e.codpersona == codpersona)).GetValueOrDefault();

        }
    }
}
