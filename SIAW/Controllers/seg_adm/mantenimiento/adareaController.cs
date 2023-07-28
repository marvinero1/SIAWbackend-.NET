using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class adareaController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public adareaController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/adarea
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<adarea>>> Getadarea(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adarea == null)
                    {
                        return Problem("Entidad adarea es null.");
                    }
                    var result = await _context.adarea.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/adarea/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<adarea>> Getadarea(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adarea == null)
                    {
                        return Problem("Entidad adarea es null.");
                    }
                    var adarea = await _context.adarea.FindAsync(codigo);

                    if (adarea == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(adarea);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/adarea/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putadarea(string conexionName, int codigo, adarea adarea)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != adarea.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(adarea).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adareaExists(codigo))
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

        // POST: api/adarea
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<adarea>> Postadarea(string conexionName, adarea adarea)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.adarea == null)
                {
                    return Problem("Entidad adarea es null.");
                }
                _context.adarea.Add(adarea);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adareaExists(adarea.codigo))
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

        // DELETE: api/adarea/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteadarea(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adarea == null)
                    {
                        return Problem("Entidad adarea es null.");
                    }
                    var adarea = await _context.adarea.FindAsync(codigo);
                    if (adarea == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.adarea.Remove(adarea);
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

        private bool adareaExists(int codigo)
        {
            return (_context.adarea?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
