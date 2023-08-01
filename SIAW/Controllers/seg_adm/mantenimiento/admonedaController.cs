using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Authorize]
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class admonedaController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public admonedaController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/admoneda
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<admoneda>>> Getadmoneda(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.admoneda == null)
                    {
                        return Problem("Entidad admoneda es null.");
                    }
                    var result = await _context.admoneda.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/admoneda/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<admoneda>> Getadmoneda(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.admoneda == null)
                    {
                        return Problem("Entidad admoneda es null.");
                    }
                    var admoneda = await _context.admoneda.FindAsync(codigo);

                    if (admoneda == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(admoneda);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/admoneda/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putadmoneda(string conexionName, string codigo, admoneda admoneda)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != admoneda.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(admoneda).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!admonedaExists(codigo))
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

        // POST: api/admoneda
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<admoneda>> Postadmoneda(string conexionName, admoneda admoneda)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.admoneda == null)
                {
                    return Problem("Entidad admoneda es null.");
                }
                _context.admoneda.Add(admoneda);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (admonedaExists(admoneda.codigo))
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

        // DELETE: api/admoneda/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteadmoneda(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.admoneda == null)
                    {
                        return Problem("Entidad admoneda es null.");
                    }
                    var admoneda = await _context.admoneda.FindAsync(codigo);
                    if (admoneda == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.admoneda.Remove(admoneda);
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

        private bool admonedaExists(string codigo)
        {
            return (_context.admoneda?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
