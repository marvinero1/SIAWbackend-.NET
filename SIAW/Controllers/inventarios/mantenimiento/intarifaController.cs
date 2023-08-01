using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Authorize]
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class intarifaController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public intarifaController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/intarifa
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<intarifa>>> Getintarifa(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intarifa == null)
                    {
                        return Problem("Entidad intarifa es null.");
                    }
                    var result = await _context.intarifa.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/intarifa/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<intarifa>> Getintarifa(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intarifa == null)
                    {
                        return Problem("Entidad intarifa es null.");
                    }
                    var intarifa = await _context.intarifa.FindAsync(codigo);

                    if (intarifa == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(intarifa);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{conexionName}")]
        public async Task<ActionResult<IEnumerable<intarifa>>> Getintarifa_catalogo(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    var query = _context.intarifa
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return Problem("No se encontraron registros con esos datos.");
                    }
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }

        // PUT: api/intarifa/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putintarifa(string conexionName, int codigo, intarifa intarifa)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != intarifa.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(intarifa).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!intarifaExists(codigo))
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

        // POST: api/intarifa
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<intarifa>> Postintarifa(string conexionName, intarifa intarifa)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.intarifa == null)
                {
                    return Problem("Entidad intarifa es null.");
                }
                _context.intarifa.Add(intarifa);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (intarifaExists(intarifa.codigo))
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

        // DELETE: api/intarifa/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteintarifa(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intarifa == null)
                    {
                        return Problem("Entidad intarifa es null.");
                    }
                    var intarifa = await _context.intarifa.FindAsync(codigo);
                    if (intarifa == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.intarifa.Remove(intarifa);
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

        private bool intarifaExists(int codigo)
        {
            return (_context.intarifa?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
