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
    public class venumeracionController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public venumeracionController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/venumeracion
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<venumeracion>>> Getvenumeracion(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.venumeracion == null)
                    {
                        return Problem("Entidad venumeracion es null.");
                    }
                    var result = await _context.venumeracion.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/venumeracion/5
        [HttpGet("{conexionName}/{id}")]
        public async Task<ActionResult<venumeracion>> Getvenumeracion(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.venumeracion == null)
                    {
                        return Problem("Entidad venumeracion es null.");
                    }
                    var venumeracion = await _context.venumeracion.FindAsync(id);

                    if (venumeracion == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(venumeracion);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }


        /// <summary>
        /// Obtiene algunos datos de la tabla venumeracion para catalogo por tipodoc y si esta habilitado
        /// </summary>
        /// <param name="conexionName"></param>
        /// <param name="tipodoc"></param>
        /// <returns></returns>
        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{conexionName}/{tipodoc}")]
        public async Task<ActionResult<IEnumerable<venumeracion>>> Getvenumeracion_catalogo(string conexionName, int tipodoc)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    var query = _context.venumeracion
                    .Where(i => i.tipodoc == tipodoc)
                    .Where(i => i.habilitado == true)
                    .OrderBy(i => i.id)
                    .Select(i => new
                    {
                        i.id,
                        i.descripcion
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return Problem("Entidad venumeracion es null.");
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



        // PUT: api/venumeracion/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{id}")]
        public async Task<IActionResult> Putvenumeracion(string conexionName, string id, venumeracion venumeracion)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (id != venumeracion.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(venumeracion).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!venumeracionExists(id))
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

        // POST: api/venumeracion
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<venumeracion>> Postvenumeracion(string conexionName, venumeracion venumeracion)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.venumeracion == null)
                {
                    return Problem("Entidad venumeracion es null.");
                }
                _context.venumeracion.Add(venumeracion);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (venumeracionExists(venumeracion.id))
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

        // DELETE: api/venumeracion/5
        [HttpDelete("{conexionName}/{id}")]
        public async Task<IActionResult> Deletevenumeracion(string conexionName, string id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.venumeracion == null)
                    {
                        return Problem("Entidad venumeracion es null.");
                    }
                    var venumeracion = await _context.venumeracion.FindAsync(id);
                    if (venumeracion == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.venumeracion.Remove(venumeracion);
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

        private bool venumeracionExists(string id)
        {
            return (_context.venumeracion?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
