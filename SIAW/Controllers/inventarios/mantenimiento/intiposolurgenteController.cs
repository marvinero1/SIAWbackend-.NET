using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/intiposolurgente/[controller]")]
    [ApiController]
    public class intiposolurgenteController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public intiposolurgenteController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/intiposolurgente
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<intiposolurgente>>> Getintiposolurgente(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intiposolurgente == null)
                    {
                        return Problem("Entidad intiposolurgente es null.");
                    }
                    var result = await _context.intiposolurgente.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/intiposolurgente/5
        [HttpGet("{conexionName}/{id}")]
        public async Task<ActionResult<intiposolurgente>> Getintiposolurgente(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intiposolurgente == null)
                    {
                        return Problem("Entidad intiposolurgente es null.");
                    }
                    var intiposolurgente = await _context.intiposolurgente.FindAsync(id);

                    if (intiposolurgente == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(intiposolurgente);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/intiposolurgente/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{id}")]
        public async Task<IActionResult> Putintiposolurgente(string conexionName, string id, intiposolurgente intiposolurgente)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (id != intiposolurgente.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(intiposolurgente).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!intiposolurgenteExists(id))
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

        // POST: api/intiposolurgente
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<intiposolurgente>> Postintiposolurgente(string conexionName, intiposolurgente intiposolurgente)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.intiposolurgente == null)
                {
                    return Problem("Entidad intiposolurgente es null.");
                }
                _context.intiposolurgente.Add(intiposolurgente);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (intiposolurgenteExists(intiposolurgente.id))
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

        // DELETE: api/intiposolurgente/5
        [HttpDelete("{conexionName}/{id}")]
        public async Task<IActionResult> Deleteintiposolurgente(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.intiposolurgente == null)
                    {
                        return Problem("Entidad intiposolurgente es null.");
                    }
                    var intiposolurgente = await _context.intiposolurgente.FindAsync(id);
                    if (intiposolurgente == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.intiposolurgente.Remove(intiposolurgente);
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

        private bool intiposolurgenteExists(string id)
        {
            return (_context.intiposolurgente?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
