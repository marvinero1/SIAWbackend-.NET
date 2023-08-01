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
    public class intipoinvController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public intipoinvController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/intipoinv
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<intipoinv>>> Getintipoinv(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intipoinv == null)
                    {
                        return Problem("Entidad intipoinv es null.");
                    }
                    var result = await _context.intipoinv.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/intipoinv/5
        [HttpGet("{conexionName}/{id}")]
        public async Task<ActionResult<intipoinv>> Getintipoinv(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intipoinv == null)
                    {
                        return Problem("Entidad intipoinv es null.");
                    }
                    var intipoinv = await _context.intipoinv.FindAsync(id);

                    if (intipoinv == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(intipoinv);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/intipoinv/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{id}")]
        public async Task<IActionResult> Putintipoinv(string conexionName, string id, intipoinv intipoinv)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (id != intipoinv.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(intipoinv).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!intipoinvExists(id))
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

        // POST: api/intipoinv
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<intipoinv>> Postintipoinv(string conexionName, intipoinv intipoinv)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.intipoinv == null)
                {
                    return Problem("Entidad intipoinv es null.");
                }
                _context.intipoinv.Add(intipoinv);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (intipoinvExists(intipoinv.id))
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

        // DELETE: api/intipoinv/5
        [HttpDelete("{conexionName}/{id}")]
        public async Task<IActionResult> Deleteintipoinv(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intipoinv == null)
                    {
                        return Problem("Entidad intipoinv es null.");
                    }
                    var intipoinv = await _context.intipoinv.FindAsync(id);
                    if (intipoinv == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.intipoinv.Remove(intipoinv);
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

        private bool intipoinvExists(string id)
        {
            return (_context.intipoinv?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
