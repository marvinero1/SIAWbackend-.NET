using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.personal_planillas.mantenimiento
{
    [Authorize]
    [Route("api/pers_plan/mant/[controller]")]
    [ApiController]
    public class pepersonaController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public pepersonaController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/pepersona
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<pepersona>>> Getpepersona(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.pepersona == null)
                    {
                        return Problem("Entidad pepersona es null.");
                    }
                    var result = await _context.pepersona.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/pepersona/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<pepersona>> Getpepersona(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.pepersona == null)
                    {
                        return Problem("Entidad pepersona es null.");
                    }
                    var pepersona = await _context.pepersona.FindAsync(codigo);

                    if (pepersona == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(pepersona);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/pepersona/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putpepersona(string conexionName, int codigo, pepersona pepersona)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != pepersona.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(pepersona).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!pepersonaExists(codigo))
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

        // POST: api/pepersona
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<pepersona>> Postpepersona(string conexionName, pepersona pepersona)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.pepersona == null)
                {
                    return Problem("Entidad pepersona es null.");
                }
                _context.pepersona.Add(pepersona);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (pepersonaExists(pepersona.codigo))
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

        // DELETE: api/pepersona/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deletepepersona(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.pepersona == null)
                    {
                        return Problem("Entidad pepersona es null.");
                    }
                    var pepersona = await _context.pepersona.FindAsync(codigo);
                    if (pepersona == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.pepersona.Remove(pepersona);
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

        private bool pepersonaExists(int codigo)
        {
            return (_context.pepersona?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
