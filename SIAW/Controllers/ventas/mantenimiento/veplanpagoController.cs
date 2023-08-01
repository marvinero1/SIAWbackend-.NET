using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Authorize]
    [Route("api/venta/mant/[controller]")]
    [ApiController]
    public class veplanpagoController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public veplanpagoController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/veplanpago
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<veplanpago>>> Getveplanpago(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.veplanpago == null)
                    {
                        return Problem("Entidad veplanpago es null.");
                    }
                    var result = await _context.veplanpago.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/veplanpago/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<veplanpago>> Getveplanpago(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.veplanpago == null)
                    {
                        return Problem("Entidad veplanpago es null.");
                    }
                    var veplanpago = await _context.veplanpago.FindAsync(codigo);

                    if (veplanpago == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(veplanpago);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/veplanpago/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putveplanpago(string conexionName, int codigo, veplanpago veplanpago)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != veplanpago.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(veplanpago).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!veplanpagoExists(codigo))
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

        // POST: api/veplanpago
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<veplanpago>> Postveplanpago(string conexionName, veplanpago veplanpago)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.veplanpago == null)
                {
                    return Problem("Entidad veplanpago es null.");
                }
                _context.veplanpago.Add(veplanpago);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (veplanpagoExists(veplanpago.codigo))
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

        // DELETE: api/veplanpago/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteveplanpago(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.veplanpago == null)
                    {
                        return Problem("Entidad veplanpago es null.");
                    }
                    var veplanpago = await _context.veplanpago.FindAsync(codigo);
                    if (veplanpago == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.veplanpago.Remove(veplanpago);
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

        private bool veplanpagoExists(int codigo)
        {
            return (_context.veplanpago?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
