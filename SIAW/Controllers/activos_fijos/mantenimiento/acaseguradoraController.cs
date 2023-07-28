using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.activos_fijos.mantenimiento
{
    [Route("api/act_fij/mant/[controller]")]
    [ApiController]
    public class acaseguradoraController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public acaseguradoraController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/acaseguradora
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<acaseguradora>>> Getacaseguradora(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.acaseguradora == null)
                    {
                        return Problem("Entidad acaseguradora es null.");
                    }
                    var result = await _context.acaseguradora.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/acaseguradora/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<acaseguradora>> Getacaseguradora(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.acaseguradora == null)
                    {
                        return Problem("Entidad acaseguradora es null.");
                    }
                    var acaseguradora = await _context.acaseguradora.FindAsync(codigo);

                    if (acaseguradora == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(acaseguradora);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/acaseguradora/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putacaseguradora(string conexionName, int codigo, acaseguradora acaseguradora)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != acaseguradora.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(acaseguradora).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!acaseguradoraExists(codigo))
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

        // POST: api/acaseguradora
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<acaseguradora>> Postacaseguradora(string conexionName, acaseguradora acaseguradora)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.acaseguradora == null)
                {
                    return Problem("Entidad acaseguradora es null.");
                }
                _context.acaseguradora.Add(acaseguradora);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (acaseguradoraExists(acaseguradora.codigo))
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

        // DELETE: api/acaseguradora/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteacaseguradora(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.acaseguradora == null)
                    {
                        return Problem("Entidad acaseguradora es null.");
                    }
                    var acaseguradora = await _context.acaseguradora.FindAsync(codigo);
                    if (acaseguradora == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.acaseguradora.Remove(acaseguradora);
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

        private bool acaseguradoraExists(int codigo)
        {
            return (_context.acaseguradora?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }

    }
}
