using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class interminacionController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public interminacionController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/interminacion
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<interminacion>>> Getinterminacion(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.interminacion == null)
                    {
                        return Problem("Entidad interminacion es null.");
                    }
                    var result = await _context.interminacion.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/interminacion/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<interminacion>> Getinterminacion(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.interminacion == null)
                    {
                        return Problem("Entidad interminacion es null.");
                    }
                    var interminacion = await _context.interminacion.FindAsync(codigo);

                    if (interminacion == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(interminacion);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/interminacion/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putinterminacion(string conexionName, string codigo, interminacion interminacion)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != interminacion.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(interminacion).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!interminacionExists(codigo))
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

        // POST: api/interminacion
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<interminacion>> Postinterminacion(string conexionName, interminacion interminacion)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.interminacion == null)
                {
                    return Problem("Entidad interminacion es null.");
                }
                _context.interminacion.Add(interminacion);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (interminacionExists(interminacion.codigo))
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

        // DELETE: api/interminacion/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteinterminacion(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.interminacion == null)
                    {
                        return Problem("Entidad interminacion es null.");
                    }
                    var interminacion = await _context.interminacion.FindAsync(codigo);
                    if (interminacion == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.interminacion.Remove(interminacion);
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

        private bool interminacionExists(string codigo)
        {
            return (_context.interminacion?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
