using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/intipopedido/[controller]")]
    [ApiController]
    public class intipopedidoController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public intipopedidoController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/intipopedido
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<intipopedido>>> Getintipopedido(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intipopedido == null)
                    {
                        return Problem("Entidad intipopedido es null.");
                    }
                    var result = await _context.intipopedido.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/intipopedido/5
        [HttpGet("{conexionName}/{id}")]
        public async Task<ActionResult<intipopedido>> Getintipopedido(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intipopedido == null)
                    {
                        return Problem("Entidad intipopedido es null.");
                    }
                    var intipopedido = await _context.intipopedido.FindAsync(id);

                    if (intipopedido == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(intipopedido);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/intipopedido/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{id}")]
        public async Task<IActionResult> Putintipopedido(string conexionName, string id, intipopedido intipopedido)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (id != intipopedido.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(intipopedido).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!intipopedidoExists(id))
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

        // POST: api/intipopedido
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<intipopedido>> Postintipopedido(string conexionName, intipopedido intipopedido)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.intipopedido == null)
                {
                    return Problem("Entidad intipopedido es null.");
                }
                _context.intipopedido.Add(intipopedido);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (intipopedidoExists(intipopedido.id))
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

        // DELETE: api/intipopedido/5
        [HttpDelete("{conexionName}/{id}")]
        public async Task<IActionResult> Deleteintipopedido(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intipopedido == null)
                    {
                        return Problem("Entidad intipopedido es null.");
                    }
                    var intipopedido = await _context.intipopedido.FindAsync(id);
                    if (intipopedido == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.intipopedido.Remove(intipopedido);
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

        private bool intipopedidoExists(string id)
        {
            return (_context.intipopedido?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
