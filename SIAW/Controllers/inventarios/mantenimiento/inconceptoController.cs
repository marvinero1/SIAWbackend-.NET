using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Authorize]
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class inconceptoController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public inconceptoController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/inconcepto
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<inconcepto>>> Getinconcepto(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inconcepto == null)
                    {
                        return Problem("Entidad inconcepto es null.");
                    }
                    var result = await _context.inconcepto.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/inconcepto/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<inconcepto>> Getinconcepto(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inconcepto == null)
                    {
                        return Problem("Entidad inconcepto es null.");
                    }
                    var inconcepto = await _context.inconcepto.FindAsync(codigo);

                    if (inconcepto == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(inconcepto);
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
        public async Task<ActionResult<IEnumerable<inconcepto>>> Getinconcepto_catalogo(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    var query = _context.inconcepto
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return Problem("Entidad inconcepto es null.");
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

        // PUT: api/inconcepto/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putinconcepto(string conexionName, int codigo, inconcepto inconcepto)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != inconcepto.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(inconcepto).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!inconceptoExists(codigo))
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

        // POST: api/inconcepto
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<inconcepto>> Postinconcepto(string conexionName, inconcepto inconcepto)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.inconcepto == null)
                {
                    return Problem("Entidad inconcepto es null.");
                }
                _context.inconcepto.Add(inconcepto);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (inconceptoExists(inconcepto.codigo))
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

        // DELETE: api/inconcepto/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteinconcepto(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inconcepto == null)
                    {
                        return Problem("Entidad inconcepto es null.");
                    }
                    var inconcepto = await _context.inconcepto.FindAsync(codigo);
                    if (inconcepto == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.inconcepto.Remove(inconcepto);
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

        private bool inconceptoExists(int codigo)
        {
            return (_context.inconcepto?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
