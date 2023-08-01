using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Authorize]
    [Route("api/venta/mant/[controller]")]
    [ApiController]
    public class veclienteController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public veclienteController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }


        // GET: api/vecliente
        /*
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<vecliente>>> Getvecliente(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.vecliente == null)
                    {
                        return Problem("Entidad vecliente es null.");
                    }
                    var result = await _context.vecliente.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }
        */


        // GET: api/vecliente/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<vecliente>> Getvecliente(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.vecliente == null)
                    {
                        return Problem("Entidad vecliente es null.");
                    }
                    var vecliente = await _context.vecliente.FindAsync(codigo);

                    if (vecliente == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(vecliente);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        /// <summary>
        /// Obtiene algunos datos de todos los registros de la tabla vecliente para catalogo
        /// </summary>
        /// <param name="conexionName"></param>
        /// <returns></returns>
        // GET: api/vecliente/5
        [HttpGet]
        [Route("catalogo/{conexionName}")]
        public async Task<ActionResult<vecliente>> Getvecliente_catalogo(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.vecliente == null)
                    {
                        return Problem("Entidad vecliente es null.");
                    }
                    //var vecliente = await _context.vecliente.FindAsync(codigo);
                    var query = _context.vecliente
                    .Where(c => IsNumeric(c.codigo))
                    .OrderBy(c => c.codigo)
                    .Select(c => new
                    {
                        c.codigo,
                        c.razonsocial,
                        c.nit,
                        c.habilitado,
                        c.codvendedor,
                        c.nombre_comercial
                    });

                    var result = query.ToList();

                    if (result == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }


        // PUT: api/vecliente/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putvecliente(string conexionName, string codigo, vecliente vecliente)
        {
            if (verificador.VerConnection(conexionName, connectionString))
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
                    if (!veclienteExists(codigo))
                    {
                        return NotFound("No existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("Datos actualizados correctamente.");
            }
            return BadRequest("Se perdio la conexion con el servidor");


        }

        // POST: api/vecliente
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<vecliente>> Postvecliente(string conexionName, vecliente vecliente)
        {
            if (verificador.VerConnection(conexionName, connectionString))
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
                    if (veclienteExists(vecliente.codigo))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("Registrado con Exito :D");

            }
            return BadRequest("Se perdio la conexion con el servidor");
        }

        // DELETE: api/vecliente/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deletevecliente(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
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

                    return Ok("Datos eliminados con exito");
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool veclienteExists(string codigo)
        {
            return (_context.vecliente?.Any(e => e.codigo == codigo)).GetValueOrDefault();
        }
        private static bool IsNumeric(string input)
        {
            return int.TryParse(input, out _);
        }
    }
}
