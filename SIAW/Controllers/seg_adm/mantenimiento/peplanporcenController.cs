using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class peplanporcenController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public peplanporcenController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/peplanporcen
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<peplanporcen>>> Getpeplanporcen(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.peplanporcen == null)
                    {
                        return Problem("Entidad peplanporcen es null.");
                    }
                    var result = await _context.peplanporcen.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/peplanporcen/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<peplanporcen>> Getpeplanporcen(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.peplanporcen == null)
                    {
                        return Problem("Entidad peplanporcen es null.");
                    }
                    var peplanporcen = await _context.peplanporcen.FindAsync(codigo);

                    if (peplanporcen == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(peplanporcen);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/peplanporcen/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putpeplanporcen(string conexionName, int codigo, peplanporcen peplanporcen)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != peplanporcen.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(peplanporcen).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!peplanporcenExists(codigo))
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

        // POST: api/peplanporcen
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<peplanporcen>> Postpeplanporcen(string conexionName, peplanporcen peplanporcen)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.peplanporcen == null)
                {
                    return Problem("Entidad peplanporcen es null.");
                }
                _context.peplanporcen.Add(peplanporcen);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (peplanporcenExists(peplanporcen.codigo))
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

        // DELETE: api/peplanporcen/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deletepeplanporcen(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.peplanporcen == null)
                    {
                        return Problem("Entidad peplanporcen es null.");
                    }
                    var peplanporcen = await _context.peplanporcen.FindAsync(codigo);
                    if (peplanporcen == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.peplanporcen.Remove(peplanporcen);
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

        private bool peplanporcenExists(int codigo)
        {
            return (_context.peplanporcen?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
