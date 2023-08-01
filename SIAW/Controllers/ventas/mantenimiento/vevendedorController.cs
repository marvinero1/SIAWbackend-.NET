using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Authorize]
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class vevendedorController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public vevendedorController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/vevendedor
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<vevendedor>>> Getvevendedor(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.vevendedor == null)
                    {
                        return Problem("Entidad vevendedor es null.");
                    }
                    var result = await _context.vevendedor.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/vevendedor/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<vevendedor>> Getvevendedor(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.vevendedor == null)
                    {
                        return Problem("Entidad vevendedor es null.");
                    }
                    var vevendedor = await _context.vevendedor.FindAsync(codigo);

                    if (vevendedor == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(vevendedor);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/vevendedor/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putvevendedor(string conexionName, int codigo, vevendedor vevendedor)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != vevendedor.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(vevendedor).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!vevendedorExists(codigo))
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

        // POST: api/vevendedor
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<vevendedor>> Postvevendedor(string conexionName, vevendedor vevendedor)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.vevendedor == null)
                {
                    return Problem("Entidad vevendedor es null.");
                }
                _context.vevendedor.Add(vevendedor);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (vevendedorExists(vevendedor.codigo))
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

        // DELETE: api/vevendedor/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deletevevendedor(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.vevendedor == null)
                    {
                        return Problem("Entidad vevendedor es null.");
                    }
                    var vevendedor = await _context.vevendedor.FindAsync(codigo);
                    if (vevendedor == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.vevendedor.Remove(vevendedor);
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

        private bool vevendedorExists(int codigo)
        {
            return (_context.vevendedor?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
