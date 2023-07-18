using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/ingrupo/[controller]")]
    [ApiController]
    public class ingrupoController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public ingrupoController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/ingrupo
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<ingrupo>>> Getingrupo(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.ingrupo == null)
                    {
                        return Problem("Entidad ingrupo es null.");
                    }
                    var result = await _context.ingrupo.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/ingrupo/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<ingrupo>> Getingrupo(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.ingrupo == null)
                    {
                        return Problem("Entidad ingrupo es null.");
                    }
                    var ingrupo = await _context.ingrupo.FindAsync(codigo);

                    if (ingrupo == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(ingrupo);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/ingrupo/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putingrupo(string conexionName, int codigo, ingrupo ingrupo)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != ingrupo.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(ingrupo).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ingrupoExists(codigo))
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

        // POST: api/ingrupo
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<ingrupo>> Postingrupo(string conexionName, ingrupo ingrupo)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.ingrupo == null)
                {
                    return Problem("Entidad ingrupo es null.");
                }
                _context.ingrupo.Add(ingrupo);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (ingrupoExists(ingrupo.codigo))
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

        // DELETE: api/ingrupo/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteingrupo(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.ingrupo == null)
                    {
                        return Problem("Entidad ingrupo es null.");
                    }
                    var ingrupo = await _context.ingrupo.FindAsync(codigo);
                    if (ingrupo == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.ingrupo.Remove(ingrupo);
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

        private bool ingrupoExists(int codigo)
        {
            return (_context.ingrupo?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
