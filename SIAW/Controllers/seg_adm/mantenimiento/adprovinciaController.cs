using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class adprovinciaController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public adprovinciaController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/adprovincia
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<adprovincia>>> Getadprovincia(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adprovincia == null)
                    {
                        return Problem("Entidad adprovincia es null.");
                    }
                    var result = await _context.adprovincia.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/adprovincia/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<adprovincia>> Getadprovincia(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adprovincia == null)
                    {
                        return Problem("Entidad adprovincia es null.");
                    }
                    var adprovincia = await _context.adprovincia.FindAsync(codigo);

                    if (adprovincia == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(adprovincia);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/adprovincia/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putadprovincia(string conexionName, string codigo, adprovincia adprovincia)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != adprovincia.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(adprovincia).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adprovinciaExists(codigo))
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

        // POST: api/adprovincia
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<adprovincia>> Postadprovincia(string conexionName, adprovincia adprovincia)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.adprovincia == null)
                {
                    return Problem("Entidad adprovincia es null.");
                }
                _context.adprovincia.Add(adprovincia);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adprovinciaExists(adprovincia.codigo))
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

        // DELETE: api/adprovincia/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteadprovincia(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adprovincia == null)
                    {
                        return Problem("Entidad adprovincia es null.");
                    }
                    var adprovincia = await _context.adprovincia.FindAsync(codigo);
                    if (adprovincia == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.adprovincia.Remove(adprovincia);
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

        private bool adprovinciaExists(string codigo)
        {
            return (_context.adprovincia?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
