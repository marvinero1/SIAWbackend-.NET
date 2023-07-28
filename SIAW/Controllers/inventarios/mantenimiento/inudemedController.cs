using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class inudemedController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public inudemedController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/inudemed
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<inudemed>>> Getinudemed(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inudemed == null)
                    {
                        return Problem("Entidad inudemed es null.");
                    }
                    var result = await _context.inudemed.OrderBy(Codigo => Codigo.Codigo).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/inudemed/5
        [HttpGet("{conexionName}/{Codigo}")]
        public async Task<ActionResult<inudemed>> Getinudemed(string conexionName, string Codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inudemed == null)
                    {
                        return Problem("Entidad inudemed es null.");
                    }
                    var inudemed = await _context.inudemed.FindAsync(Codigo);

                    if (inudemed == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(inudemed);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/inudemed/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{Codigo}")]
        public async Task<IActionResult> Putinudemed(string conexionName, string Codigo, inudemed inudemed)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (Codigo != inudemed.Codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(inudemed).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!inudemedExists(Codigo))
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

        // POST: api/inudemed
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<inudemed>> Postinudemed(string conexionName, inudemed inudemed)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.inudemed == null)
                {
                    return Problem("Entidad inudemed es null.");
                }
                _context.inudemed.Add(inudemed);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (inudemedExists(inudemed.Codigo))
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

        // DELETE: api/inudemed/5
        [HttpDelete("{conexionName}/{Codigo}")]
        public async Task<IActionResult> Deleteinudemed(string conexionName, string Codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inudemed == null)
                    {
                        return Problem("Entidad inudemed es null.");
                    }
                    var inudemed = await _context.inudemed.FindAsync(Codigo);
                    if (inudemed == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.inudemed.Remove(inudemed);
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

        private bool inudemedExists(string Codigo)
        {
            return (_context.inudemed?.Any(e => e.Codigo == Codigo)).GetValueOrDefault();

        }
    }
}
