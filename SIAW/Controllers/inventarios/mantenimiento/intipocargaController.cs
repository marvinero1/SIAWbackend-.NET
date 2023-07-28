using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class intipocargaController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public intipocargaController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/intipocarga
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<intipocarga>>> Getintipocarga(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intipocarga == null)
                    {
                        return Problem("Entidad intipocarga es null.");
                    }
                    var result = await _context.intipocarga.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/intipocarga/5
        [HttpGet("{conexionName}/{id}")]
        public async Task<ActionResult<intipocarga>> Getintipocarga(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intipocarga == null)
                    {
                        return Problem("Entidad intipocarga es null.");
                    }
                    var intipocarga = await _context.intipocarga.FindAsync(id);

                    if (intipocarga == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(intipocarga);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/intipocarga/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{id}")]
        public async Task<IActionResult> Putintipocarga(string conexionName, string id, intipocarga intipocarga)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (id != intipocarga.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(intipocarga).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!intipocargaExists(id))
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

        // POST: api/intipocarga
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<intipocarga>> Postintipocarga(string conexionName, intipocarga intipocarga)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.intipocarga == null)
                {
                    return Problem("Entidad intipocarga es null.");
                }
                _context.intipocarga.Add(intipocarga);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (intipocargaExists(intipocarga.id))
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

        // DELETE: api/intipocarga/5
        [HttpDelete("{conexionName}/{id}")]
        public async Task<IActionResult> Deleteintipocarga(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intipocarga == null)
                    {
                        return Problem("Entidad intipocarga es null.");
                    }
                    var intipocarga = await _context.intipocarga.FindAsync(id);
                    if (intipocarga == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.intipocarga.Remove(intipocarga);
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

        private bool intipocargaExists(string id)
        {
            return (_context.intipocarga?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
