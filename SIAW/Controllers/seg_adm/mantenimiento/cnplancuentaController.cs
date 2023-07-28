using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class cnplancuentaController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public cnplancuentaController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/cnplancuenta
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<cnplancuenta>>> Getcnplancuenta(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cnplancuenta == null)
                    {
                        return Problem("Entidad cnplancuenta es null.");
                    }
                    var result = await _context.cnplancuenta.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/cnplancuenta/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<cnplancuenta>> Getcnplancuenta(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cnplancuenta == null)
                    {
                        return Problem("Entidad cnplancuenta es null.");
                    }
                    var cnplancuenta = await _context.cnplancuenta.FindAsync(codigo);

                    if (cnplancuenta == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(cnplancuenta);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/cnplancuenta/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putcnplancuenta(string conexionName, int codigo, cnplancuenta cnplancuenta)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != cnplancuenta.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(cnplancuenta).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!cnplancuentaExists(codigo))
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

        // POST: api/cnplancuenta
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<cnplancuenta>> Postcnplancuenta(string conexionName, cnplancuenta cnplancuenta)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.cnplancuenta == null)
                {
                    return Problem("Entidad cnplancuenta es null.");
                }
                _context.cnplancuenta.Add(cnplancuenta);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (cnplancuentaExists(cnplancuenta.codigo))
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

        // DELETE: api/cnplancuenta/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deletecnplancuenta(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.cnplancuenta == null)
                    {
                        return Problem("Entidad cnplancuenta es null.");
                    }
                    var cnplancuenta = await _context.cnplancuenta.FindAsync(codigo);
                    if (cnplancuenta == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.cnplancuenta.Remove(cnplancuenta);
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

        private bool cnplancuentaExists(int codigo)
        {
            return (_context.cnplancuenta?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
