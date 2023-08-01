using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.compras.mantenimiento
{
    [Authorize]
    [Route("api/compras/mant/[controller]")]
    [ApiController]
    public class cmtipocompraController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public cmtipocompraController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/cmtipocompra
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<cmtipocompra>>> Getcmtipocompra(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cmtipocompra == null)
                    {
                        return Problem("Entidad cmtipocompra es null.");
                    }
                    var result = await _context.cmtipocompra.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/cmtipocompra/5
        [HttpGet("{conexionName}/{id}")]
        public async Task<ActionResult<cmtipocompra>> Getcmtipocompra(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cmtipocompra == null)
                    {
                        return Problem("Entidad cmtipocompra es null.");
                    }
                    var cmtipocompra = await _context.cmtipocompra.FindAsync(id);

                    if (cmtipocompra == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(cmtipocompra);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/cmtipocompra/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{id}")]
        public async Task<IActionResult> Putcmtipocompra(string conexionName, string id, cmtipocompra cmtipocompra)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (id != cmtipocompra.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(cmtipocompra).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cmtipocompraExists(id))
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

        // POST: api/cmtipocompra
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<cmtipocompra>> Postcmtipocompra(string conexionName, cmtipocompra cmtipocompra)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.cmtipocompra == null)
                {
                    return Problem("Entidad cmtipocompra es null.");
                }
                _context.cmtipocompra.Add(cmtipocompra);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cmtipocompraExists(cmtipocompra.id))
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

        // DELETE: api/cmtipocompra/5
        [HttpDelete("{conexionName}/{id}")]
        public async Task<IActionResult> Deletecmtipocompra(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cmtipocompra == null)
                    {
                        return Problem("Entidad cmtipocompra es null.");
                    }
                    var cmtipocompra = await _context.cmtipocompra.FindAsync(id);
                    if (cmtipocompra == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.cmtipocompra.Remove(cmtipocompra);
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

        private bool cmtipocompraExists(string id)
        {
            return (_context.cmtipocompra?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
