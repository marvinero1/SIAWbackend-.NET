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
    public class serolController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public serolController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/serol
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<serol>>> Getserol(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.serol == null)
                    {
                        return Problem("Entidad serol es null.");
                    }
                    var result = await _context.serol.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/serol/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<serol>> Getserol(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.serol == null)
                    {
                        return Problem("Entidad serol es null.");
                    }
                    var serol = await _context.serol.FindAsync(codigo);

                    if (serol == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(serol);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/serol/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putserol(string conexionName, string codigo, serol serol)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != serol.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(serol).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!serolExists(codigo))
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

        // POST: api/serol
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<serol>> Postserol(string conexionName, serol serol)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.serol == null)
                {
                    return Problem("Entidad serol es null.");
                }
                _context.serol.Add(serol);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (serolExists(serol.codigo))
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

        // DELETE: api/serol/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteserol(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.serol == null)
                    {
                        return Problem("Entidad serol es null.");
                    }
                    var serol = await _context.serol.FindAsync(codigo);
                    if (serol == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.serol.Remove(serol);
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

        private bool serolExists(string codigo)
        {
            return (_context.serol?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
