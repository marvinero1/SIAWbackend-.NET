using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.contabilidad.mantenimiento
{
    [Route("api/contab/mant/[controller]")]
    [ApiController]
    public class cnnumeracionController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public cnnumeracionController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/cnnumeracion
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<cnnumeracion>>> Getcnnumeracion(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cnnumeracion == null)
                    {
                        return Problem("Entidad cnnumeracion es null.");
                    }
                    var result = await _context.cnnumeracion.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/cnnumeracion/5
        [HttpGet("{conexionName}/{id}")]
        public async Task<ActionResult<cnnumeracion>> Getcnnumeracion(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cnnumeracion == null)
                    {
                        return Problem("Entidad cnnumeracion es null.");
                    }
                    var cnnumeracion = await _context.cnnumeracion.FindAsync(id);

                    if (cnnumeracion == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(cnnumeracion);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/cnnumeracion/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{conexionName}/{id}")]
        public async Task<IActionResult> Putcnnumeracion(string conexionName, string id, cnnumeracion cnnumeracion)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (id != cnnumeracion.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(cnnumeracion).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cnnumeracionExists(id))
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
            return BadRequest("Se perdio la conexion con el servidor");


        }

        // POST: api/cnnumeracion
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<cnnumeracion>> Postcnnumeracion(string conexionName, cnnumeracion cnnumeracion)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.cnnumeracion == null)
                {
                    return Problem("Entidad cnnumeracion es null.");
                }
                _context.cnnumeracion.Add(cnnumeracion);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cnnumeracionExists(cnnumeracion.id))
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
            return BadRequest("Se perdio la conexion con el servidor");
        }

        // DELETE: api/cnnumeracion/5
        [Authorize]
        [HttpDelete("{conexionName}/{id}")]
        public async Task<IActionResult> Deletecnnumeracion(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cnnumeracion == null)
                    {
                        return Problem("Entidad cnnumeracion es null.");
                    }
                    var cnnumeracion = await _context.cnnumeracion.FindAsync(id);
                    if (cnnumeracion == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.cnnumeracion.Remove(cnnumeracion);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
                return BadRequest("Se perdio la conexion con el servidor");

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool cnnumeracionExists(string id)
        {
            return (_context.cnnumeracion?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
