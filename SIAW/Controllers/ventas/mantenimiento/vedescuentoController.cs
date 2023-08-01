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
    public class vedescuentoController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public vedescuentoController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/vedescuento
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<vedescuento>>> Getvedescuento(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.vedescuento == null)
                    {
                        return Problem("Entidad vedescuento es null.");
                    }
                    var result = await _context.vedescuento.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/vedescuento/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<vedescuento>> Getvedescuento(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.vedescuento == null)
                    {
                        return Problem("Entidad vedescuento es null.");
                    }
                    var vedescuento = await _context.vedescuento.FindAsync(codigo);

                    if (vedescuento == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(vedescuento);
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
        public async Task<ActionResult<IEnumerable<vedescuento>>> Getvedescuento_catalogo(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    var query = _context.vedescuento
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return Problem("Entidad vedescuento es null.");
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

        // PUT: api/vedescuento/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putvedescuento(string conexionName, int codigo, vedescuento vedescuento)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != vedescuento.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(vedescuento).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!vedescuentoExists(codigo))
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

        // POST: api/vedescuento
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<vedescuento>> Postvedescuento(string conexionName, vedescuento vedescuento)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.vedescuento == null)
                {
                    return Problem("Entidad vedescuento es null.");
                }
                _context.vedescuento.Add(vedescuento);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (vedescuentoExists(vedescuento.codigo))
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

        // DELETE: api/vedescuento/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deletevedescuento(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.vedescuento == null)
                    {
                        return Problem("Entidad vedescuento es null.");
                    }
                    var vedescuento = await _context.vedescuento.FindAsync(codigo);
                    if (vedescuento == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.vedescuento.Remove(vedescuento);
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

        private bool vedescuentoExists(int codigo)
        {
            return (_context.vedescuento?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
