using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.fondos.mantenimiento
{
    [Route("api/fondos/mant/fncuenta/[controller]")]
    [ApiController]
    public class fncuentaController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public fncuentaController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/fncuenta
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<fncuenta>>> Getfncuenta(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.fncuenta == null)
                    {
                        return Problem("Entidad fncuenta es null.");
                    }
                    var result = await _context.fncuenta.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/fncuenta/5
        [HttpGet("{conexionName}/{id}")]
        public async Task<ActionResult<fncuenta>> Getfncuenta(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.fncuenta == null)
                    {
                        return Problem("Entidad fncuenta es null.");
                    }
                    var fncuenta = await _context.fncuenta.FindAsync(id);

                    if (fncuenta == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(fncuenta);
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
        public async Task<ActionResult<IEnumerable<fncuenta>>> Getfncuenta_catalogo(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    var query = _context.fncuenta
                    .OrderBy(i => i.id)
                    .Select(i => new
                    {
                        i.id,
                        i.descripcion
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return Problem("Entidad fncuenta es null.");
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

        // PUT: api/fncuenta/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{id}")]
        public async Task<IActionResult> Putfncuenta(string conexionName, string id, fncuenta fncuenta)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (id != fncuenta.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(fncuenta).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!fncuentaExists(id))
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

        // POST: api/fncuenta
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<fncuenta>> Postfncuenta(string conexionName, fncuenta fncuenta)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.fncuenta == null)
                {
                    return Problem("Entidad fncuenta es null.");
                }
                _context.fncuenta.Add(fncuenta);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (fncuentaExists(fncuenta.id))
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

        // DELETE: api/fncuenta/5
        [HttpDelete("{conexionName}/{id}")]
        public async Task<IActionResult> Deletefncuenta(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.fncuenta == null)
                    {
                        return Problem("Entidad fncuenta es null.");
                    }
                    var fncuenta = await _context.fncuenta.FindAsync(id);
                    if (fncuenta == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.fncuenta.Remove(fncuenta);
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

        private bool fncuentaExists(string id)
        {
            return (_context.fncuenta?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
