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
    public class inresistenciaController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public inresistenciaController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/inresistencia
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<inresistencia>>> Getinresistencia(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inresistencia == null)
                    {
                        return Problem("Entidad inresistencia es null.");
                    }
                    var result = await _context.inresistencia.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/inresistencia/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<inresistencia>> Getinresistencia(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inresistencia == null)
                    {
                        return Problem("Entidad inresistencia es null.");
                    }
                    var inresistencia = await _context.inresistencia.FindAsync(codigo);

                    if (inresistencia == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(inresistencia);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/inresistencia/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putinresistencia(string conexionName, string codigo, inresistencia inresistencia)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != inresistencia.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(inresistencia).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!inresistenciaExists(codigo))
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

        // POST: api/inresistencia
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<inresistencia>> Postinresistencia(string conexionName, inresistencia inresistencia)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.inresistencia == null)
                {
                    return Problem("Entidad inresistencia es null.");
                }
                _context.inresistencia.Add(inresistencia);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (inresistenciaExists(inresistencia.codigo))
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

        // DELETE: api/inresistencia/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteinresistencia(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inresistencia == null)
                    {
                        return Problem("Entidad inresistencia es null.");
                    }
                    var inresistencia = await _context.inresistencia.FindAsync(codigo);
                    if (inresistencia == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.inresistencia.Remove(inresistencia);
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

        private bool inresistenciaExists(string codigo)
        {
            return (_context.inresistencia?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
