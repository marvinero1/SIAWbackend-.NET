using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using System.Net;

namespace SIAW.Controllers.contabilidad.mantenimiento
{
    [Route("api/contab/mant/cncuenta/[controller]")]
    [ApiController]
    public class cncuentaController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public cncuentaController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/cncuenta
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<cncuenta>>> Getcncuenta(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cncuenta == null)
                    {
                        return Problem("Entidad cncuenta es null.");
                    }
                    var result = await _context.cncuenta.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return result;
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/cncuenta/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<cncuenta>> Getcncuenta(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cncuenta == null)
                    {
                        return Problem("Entidad cncuenta es null.");
                    }
                    var cncuenta = await _context.cncuenta.FindAsync(codigo);

                    if (cncuenta == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return cncuenta;
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
        public async Task<ActionResult<IEnumerable<cncuenta>>> Getcncuenta_catalogo(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    var query = _context.cncuenta
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return Problem("Entidad cncuenta es null.");
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


        // PUT: api/cncuenta/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putcncuenta(string conexionName, string codigo, cncuenta cncuenta)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != cncuenta.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(cncuenta).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cncuentaExists(codigo))
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

        // POST: api/cncuenta
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<cncuenta>> Postcncuenta(string conexionName, cncuenta cncuenta)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.cncuenta == null)
                {
                    return Problem("Entidad cncuenta es null.");
                }
                _context.cncuenta.Add(cncuenta);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cncuentaExists(cncuenta.codigo))
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

        // DELETE: api/cncuenta/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deletecncuenta(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cncuenta == null)
                    {
                        return Problem("Entidad cncuenta es null.");
                    }
                    var cncuenta = await _context.cncuenta.FindAsync(codigo);
                    if (cncuenta == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.cncuenta.Remove(cncuenta);
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

        private bool cncuentaExists(string codigo)
        {
            return (_context.cncuenta?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
