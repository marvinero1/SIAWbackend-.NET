using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.contabilidad.mantenimiento
{
    [Authorize]
    [Route("api/contab/mant/[controller]")]
    [ApiController]
    public class cntipoController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public cntipoController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/cntipo
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<cntipo>>> Getcntipo(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cntipo == null)
                    {
                        return Problem("Entidad cntipo es null.");
                    }
                    var result = await _context.cntipo.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/cntipo/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<cntipo>> Getcntipo(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cntipo == null)
                    {
                        return Problem("Entidad cntipo es null.");
                    }
                    var cntipo = await _context.cntipo.FindAsync(codigo);

                    if (cntipo == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(cntipo);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/cntipo/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putcntipo(string conexionName, string codigo, cntipo cntipo)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != cntipo.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(cntipo).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cntipoExists(codigo))
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

        // POST: api/cntipo
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<cntipo>> Postcntipo(string conexionName, cntipo cntipo)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.cntipo == null)
                {
                    return Problem("Entidad cntipo es null.");
                }
                _context.cntipo.Add(cntipo);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cntipoExists(cntipo.codigo))
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

        // DELETE: api/cntipo/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deletecntipo(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cntipo == null)
                    {
                        return Problem("Entidad cntipo es null.");
                    }
                    var cntipo = await _context.cntipo.FindAsync(codigo);
                    if (cntipo == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.cntipo.Remove(cntipo);
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

        private bool cntipoExists(string codigo)
        {
            return (_context.cntipo?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
