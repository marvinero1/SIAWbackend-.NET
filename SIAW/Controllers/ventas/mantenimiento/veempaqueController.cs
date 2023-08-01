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
    public class veempaqueController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public veempaqueController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/veempaque
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<veempaque>>> Getveempaque(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.veempaque == null)
                    {
                        return Problem("Entidad veempaque es null.");
                    }
                    var result = await _context.veempaque.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/veempaque/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<veempaque>> Getveempaque(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.veempaque == null)
                    {
                        return Problem("Entidad veempaque es null.");
                    }
                    var veempaque = await _context.veempaque.FindAsync(codigo);

                    if (veempaque == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(veempaque);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/veempaque/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putveempaque(string conexionName, int codigo, veempaque veempaque)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != veempaque.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(veempaque).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!veempaqueExists(codigo))
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

        // POST: api/veempaque
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<veempaque>> Postveempaque(string conexionName, veempaque veempaque)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.veempaque == null)
                {
                    return Problem("Entidad veempaque es null.");
                }
                _context.veempaque.Add(veempaque);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (veempaqueExists(veempaque.codigo))
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

        // DELETE: api/veempaque/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteveempaque(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.veempaque == null)
                    {
                        return Problem("Entidad veempaque es null.");
                    }
                    var veempaque = await _context.veempaque.FindAsync(codigo);
                    if (veempaque == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.veempaque.Remove(veempaque);
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

        private bool veempaqueExists(int codigo)
        {
            return (_context.veempaque?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
