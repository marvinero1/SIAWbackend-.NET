using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class inroscaController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public inroscaController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/inrosca
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<inrosca>>> Getinrosca(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inrosca == null)
                    {
                        return Problem("Entidad inrosca es null.");
                    }
                    var result = await _context.inrosca.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/inrosca/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<inrosca>> Getinrosca(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inrosca == null)
                    {
                        return Problem("Entidad inrosca es null.");
                    }
                    var inrosca = await _context.inrosca.FindAsync(codigo);

                    if (inrosca == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(inrosca);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/inrosca/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putinrosca(string conexionName, string codigo, inrosca inrosca)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != inrosca.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(inrosca).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!inroscaExists(codigo))
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

        // POST: api/inrosca
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<inrosca>> Postinrosca(string conexionName, inrosca inrosca)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.inrosca == null)
                {
                    return Problem("Entidad inrosca es null.");
                }
                _context.inrosca.Add(inrosca);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (inroscaExists(inrosca.codigo))
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

        // DELETE: api/inrosca/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteinrosca(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inrosca == null)
                    {
                        return Problem("Entidad inrosca es null.");
                    }
                    var inrosca = await _context.inrosca.FindAsync(codigo);
                    if (inrosca == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.inrosca.Remove(inrosca);
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

        private bool inroscaExists(string codigo)
        {
            return (_context.inrosca?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
