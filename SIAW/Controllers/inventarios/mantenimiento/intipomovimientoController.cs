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
    public class intipomovimientoController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public intipomovimientoController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/intipomovimiento
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<intipomovimiento>>> Getintipomovimiento(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intipomovimiento == null)
                    {
                        return Problem("Entidad intipomovimiento es null.");
                    }
                    var result = await _context.intipomovimiento.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/intipomovimiento/5
        [HttpGet("{conexionName}/{id}")]
        public async Task<ActionResult<intipomovimiento>> Getintipomovimiento(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intipomovimiento == null)
                    {
                        return Problem("Entidad intipomovimiento es null.");
                    }
                    var intipomovimiento = await _context.intipomovimiento.FindAsync(id);

                    if (intipomovimiento == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(intipomovimiento);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/intipomovimiento/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{id}")]
        public async Task<IActionResult> Putintipomovimiento(string conexionName, string id, intipomovimiento intipomovimiento)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (id != intipomovimiento.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(intipomovimiento).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!intipomovimientoExists(id))
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

        // POST: api/intipomovimiento
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<intipomovimiento>> Postintipomovimiento(string conexionName, intipomovimiento intipomovimiento)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.intipomovimiento == null)
                {
                    return Problem("Entidad intipomovimiento es null.");
                }
                _context.intipomovimiento.Add(intipomovimiento);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (intipomovimientoExists(intipomovimiento.id))
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

        // DELETE: api/intipomovimiento/5
        [HttpDelete("{conexionName}/{id}")]
        public async Task<IActionResult> Deleteintipomovimiento(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intipomovimiento == null)
                    {
                        return Problem("Entidad intipomovimiento es null.");
                    }
                    var intipomovimiento = await _context.intipomovimiento.FindAsync(id);
                    if (intipomovimiento == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.intipomovimiento.Remove(intipomovimiento);
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

        private bool intipomovimientoExists(string id)
        {
            return (_context.intipomovimiento?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
