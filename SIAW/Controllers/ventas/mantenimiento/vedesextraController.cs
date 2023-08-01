using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class vedesextraController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public vedesextraController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/vedesextra
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<vedesextra>>> Getvedesextra(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.vedesextra == null)
                    {
                        return Problem("Entidad vedesextra es null.");
                    }
                    var result = await _context.vedesextra.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/vedesextra/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<vedesextra>> Getvedesextra(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.vedesextra == null)
                    {
                        return Problem("Entidad vedesextra es null.");
                    }
                    var vedesextra = await _context.vedesextra.FindAsync(codigo);

                    if (vedesextra == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(vedesextra);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/vedesextra/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putvedesextra(string conexionName, int codigo, vedesextra vedesextra)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != vedesextra.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(vedesextra).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!vedesextraExists(codigo))
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

        // POST: api/vedesextra
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<vedesextra>> Postvedesextra(string conexionName, vedesextra vedesextra)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.vedesextra == null)
                {
                    return Problem("Entidad vedesextra es null.");
                }
                _context.vedesextra.Add(vedesextra);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (vedesextraExists(vedesextra.codigo))
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

        // DELETE: api/vedesextra/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deletevedesextra(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.vedesextra == null)
                    {
                        return Problem("Entidad vedesextra es null.");
                    }
                    var vedesextra = await _context.vedesextra.FindAsync(codigo);
                    if (vedesextra == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.vedesextra.Remove(vedesextra);
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

        private bool vedesextraExists(int codigo)
        {
            return (_context.vedesextra?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
