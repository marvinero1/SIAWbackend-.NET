using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/ingrupomer/[controller]")]
    [ApiController]
    public class ingrupomerController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public ingrupomerController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/ingrupomer
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<ingrupomer>>> Getingrupomer(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.ingrupomer == null)
                    {
                        return Problem("Entidad ingrupomer es null.");
                    }
                    var result = await _context.ingrupomer.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/ingrupomer/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<ingrupomer>> Getingrupomer(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.ingrupomer == null)
                    {
                        return Problem("Entidad ingrupomer es null.");
                    }
                    var ingrupomer = await _context.ingrupomer.FindAsync(codigo);

                    if (ingrupomer == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(ingrupomer);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/ingrupomer/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putingrupomer(string conexionName, int codigo, ingrupomer ingrupomer)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != ingrupomer.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(ingrupomer).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ingrupomerExists(codigo))
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

        // POST: api/ingrupomer
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<ingrupomer>> Postingrupomer(string conexionName, ingrupomer ingrupomer)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.ingrupomer == null)
                {
                    return Problem("Entidad ingrupomer es null.");
                }
                _context.ingrupomer.Add(ingrupomer);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (ingrupomerExists(ingrupomer.codigo))
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

        // DELETE: api/ingrupomer/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteingrupomer(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.ingrupomer == null)
                    {
                        return Problem("Entidad ingrupomer es null.");
                    }
                    var ingrupomer = await _context.ingrupomer.FindAsync(codigo);
                    if (ingrupomer == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.ingrupomer.Remove(ingrupomer);
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

        private bool ingrupomerExists(int codigo)
        {
            return (_context.ingrupomer?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
