using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/addepto/[controller]")]
    [ApiController]
    public class addeptoController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public addeptoController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/addepto
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<addepto>>> Getaddepto(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.addepto == null)
                    {
                        return Problem("Entidad addepto es null.");
                    }
                    var result = await _context.addepto.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/addepto/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<addepto>> Getaddepto(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.addepto == null)
                    {
                        return Problem("Entidad addepto es null.");
                    }
                    var addepto = await _context.addepto.FindAsync(codigo);

                    if (addepto == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(addepto);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/addepto/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putaddepto(string conexionName, string codigo, addepto addepto)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != addepto.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(addepto).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!addeptoExists(codigo))
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

        // POST: api/addepto
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<addepto>> Postaddepto(string conexionName, addepto addepto)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.addepto == null)
                {
                    return Problem("Entidad addepto es null.");
                }
                _context.addepto.Add(addepto);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (addeptoExists(addepto.codigo))
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

        // DELETE: api/addepto/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteaddepto(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.addepto == null)
                    {
                        return Problem("Entidad addepto es null.");
                    }
                    var addepto = await _context.addepto.FindAsync(codigo);
                    if (addepto == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.addepto.Remove(addepto);
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

        private bool addeptoExists(string codigo)
        {
            return (_context.addepto?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
