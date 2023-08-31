using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Route("api/venta/mant/[controller]")]
    [ApiController]
    public class veclienteController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public veclienteController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/vecliente
        /*
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<vecliente>>> Getvecliente(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vecliente == null)
                    {
                        return Problem("Entidad vecliente es null.");
                    }
                    var result = await _context.vecliente.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }
        */


        // GET: api/vecliente/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<vecliente>> Getvecliente(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vecliente == null)
                    {
                        return Problem("Entidad vecliente es null.");
                    }
                    //var vecliente = await _context.vecliente.FindAsync(codigo);
                    var vecliente = await _context.vecliente
                        .Join(_context.vetienda, c => c.codigo, v => v.codcliente, (cliente, vivienda) => new { Cliente = cliente, Vivienda = vivienda })
                        .Where(joined => joined.Cliente.codigo == codigo && joined.Vivienda.central == true)
                        .FirstOrDefaultAsync();

                    if (vecliente == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(vecliente);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        /// <summary>
        /// Obtiene algunos datos de todos los registros de la tabla vecliente para catalogo
        /// </summary>
        /// <param name="userConn"></param>
        /// <returns></returns>
        // GET: api/vecliente/5
        [HttpGet]
        [Route("catalogo/{userConn}")]
        public async Task<ActionResult<vecliente>> Getvecliente_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vecliente == null)
                    {
                        return Problem("Entidad vecliente es null.");
                    }
                    //var vecliente = await _context.vecliente.FindAsync(codigo);
                    var query = _context.vecliente
                    .OrderBy(c => c.codigo)
                    .ToList() // Cargamos todos los registros en memoria
                    //.Where(c => IsNumeric(c.codigo)) // Filtramos en memoria
                    .Select(c => new
                    {
                        codigo=c.codigo,
                        nombre=c.nombre_comercial+" - "+c.razonsocial,
                        nit=c.nit,
                        habilitado = c.habilitado,
                        codvendedor = c.codvendedor,
                        nombre_comercial = c.nombre_comercial,
                        direccion_titular = c.direccion_titular
                    });

                    var result = query.ToList();

                    if (result == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }


        // PUT: api/vecliente/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putvecliente(string userConn, string codigo, vecliente vecliente)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != vecliente.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(vecliente).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!veclienteExists(codigo, _context))
                    {
                        return NotFound("No existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("206");   // actualizado con exito
            }
            


        }

        // POST: api/vecliente
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<vecliente>> Postvecliente(string userConn, vecliente vecliente)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.vecliente == null)
                {
                    return Problem("Entidad vecliente es null.");
                }
                _context.vecliente.Add(vecliente);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (veclienteExists(vecliente.codigo, _context))
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

        // DELETE: api/vecliente/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletevecliente(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vecliente == null)
                    {
                        return Problem("Entidad vecliente es null.");
                    }
                    var vecliente = await _context.vecliente.FindAsync(codigo);
                    if (vecliente == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.vecliente.Remove(vecliente);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool veclienteExists(string codigo, DBContext _context)
        {
            return (_context.vecliente?.Any(e => e.codigo == codigo)).GetValueOrDefault();
        }
        private static bool IsNumeric(string input)
        {
            return int.TryParse(input, out _);
        }
    }
}
