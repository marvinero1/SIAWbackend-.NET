using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/adunidad/[controller]")]
    [ApiController]
    public class adunidadController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public adunidadController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/adunidad
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<adunidad>>> Getadunidad(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adunidad == null)
                    {
                        return Problem("Entidad adunidad es null.");
                    }
                    var result = await _context.adunidad.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/adunidad/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<adunidad>> Getadunidad(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adunidad == null)
                    {
                        return Problem("Entidad adunidad es null.");
                    }
                    var adunidad = await _context.adunidad.FindAsync(codigo);

                    if (adunidad == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(adunidad);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/adunidad/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putadunidad(string conexionName, string codigo, adunidad adunidad)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != adunidad.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(adunidad).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adunidadExists(codigo))
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

        // POST: api/adunidad
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<adunidad>> Postadunidad(string conexionName, adunidad adunidad)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.adunidad == null)
                {
                    return Problem("Entidad adunidad es null.");
                }
                _context.adunidad.Add(adunidad);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adunidadExists(adunidad.codigo))
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

        // DELETE: api/adunidad/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteadunidad(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adunidad == null)
                    {
                        return Problem("Entidad adunidad es null.");
                    }
                    var adunidad = await _context.adunidad.FindAsync(codigo);
                    if (adunidad == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.adunidad.Remove(adunidad);
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

        private bool adunidadExists(string codigo)
        {
            return (_context.adunidad?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
