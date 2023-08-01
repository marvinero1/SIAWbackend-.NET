using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Authorize]
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class adusparametrosController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public adusparametrosController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/adusparametros
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<adusparametros>>> Getadusparametros(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adusparametros == null)
                    {
                        return Problem("Entidad adusparametros es null.");
                    }
                    var result = await _context.adusparametros.OrderBy(usuario => usuario.usuario).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/adusparametros/5
        [HttpGet("{conexionName}/{usuario}")]
        public async Task<ActionResult<adusparametros>> Getadusparametros(string conexionName, string usuario)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adusparametros == null)
                    {
                        return Problem("Entidad adusparametros es null.");
                    }
                    var adusparametros = await _context.adusparametros.FindAsync(usuario);

                    if (adusparametros == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(adusparametros);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/adusparametros/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{usuario}")]
        public async Task<IActionResult> Putadusparametros(string conexionName, string usuario, adusparametros adusparametros)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (usuario != adusparametros.usuario)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(adusparametros).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adusparametrosExists(usuario))
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

        // POST: api/adusparametros
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<adusparametros>> Postadusparametros(string conexionName, adusparametros adusparametros)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.adusparametros == null)
                {
                    return Problem("Entidad adusparametros es null.");
                }
                _context.adusparametros.Add(adusparametros);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adusparametrosExists(adusparametros.usuario))
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

        // DELETE: api/adusparametros/5
        [HttpDelete("{conexionName}/{usuario}")]
        public async Task<IActionResult> Deleteadusparametros(string conexionName, string usuario)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adusparametros == null)
                    {
                        return Problem("Entidad adusparametros es null.");
                    }
                    var adusparametros = await _context.adusparametros.FindAsync(usuario);
                    if (adusparametros == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.adusparametros.Remove(adusparametros);
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

        private bool adusparametrosExists(string usuario)
        {
            return (_context.adusparametros?.Any(e => e.usuario == usuario)).GetValueOrDefault();

        }
    }
}
