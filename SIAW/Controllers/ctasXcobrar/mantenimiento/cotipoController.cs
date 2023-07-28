using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.ctasXcobrar.mantenimiento
{
    [Route("api/ctasXcobrar/mant/[controller]")]
    [ApiController]
    public class cotipoController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public cotipoController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/cotipo
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<cotipo>>> Getcotipo(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cotipo == null)
                    {
                        return Problem("Entidad cotipo es null.");
                    }
                    var result = await _context.cotipo.OrderByDescending(id => id.id).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/cotipo/5
        [HttpGet("{conexionName}/{id}")]
        public async Task<ActionResult<cotipo>> Getcotipo(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cotipo == null)
                    {
                        return Problem("Entidad cotipo es null.");
                    }
                    var cotipo = await _context.cotipo.FindAsync(id);

                    if (cotipo == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(cotipo);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{conexionName}")]
        public async Task<ActionResult<IEnumerable<cotipo>>> Getcotipo_catalogo(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    var query = _context.cotipo
                    .OrderBy(i => i.id)
                    .Select(i => new
                    {
                        i.id,
                        i.descripcion
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return Problem("No se encontraron registros con esos datos.");
                    }
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }

        // PUT: api/cotipo/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{id}")]
        public async Task<IActionResult> Putcotipo(string conexionName, string id, cotipo cotipo)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (id != cotipo.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(cotipo).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cotipoExists(id))
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

        // POST: api/cotipo
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<cotipo>> Postcotipo(string conexionName, cotipo cotipo)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.cotipo == null)
                {
                    return Problem("Entidad cotipo es null.");
                }
                _context.cotipo.Add(cotipo);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cotipoExists(cotipo.id))
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

        // DELETE: api/cotipo/5
        [HttpDelete("{conexionName}/{id}")]
        public async Task<IActionResult> Deletecotipo(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cotipo == null)
                    {
                        return Problem("Entidad cotipo es null.");
                    }
                    var cotipo = await _context.cotipo.FindAsync(id);
                    if (cotipo == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.cotipo.Remove(cotipo);
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

        private bool cotipoExists(string id)
        {
            return (_context.cotipo?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
