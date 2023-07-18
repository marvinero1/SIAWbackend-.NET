using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using System.Net;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/inkit/[controller]")]
    [ApiController]
    public class inkitController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public inkitController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/inkit
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<inkit>>> Getinkit(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inkit == null)
                    {
                        return Problem("Entidad inkit es null.");
                    }
                    var result = await _context.inkit.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/inkit/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<inkit>> Getinkit(string conexionName, string codigo, string item)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inkit == null)
                    {
                        return Problem("Entidad inkit es null.");
                    }
                    var inkit = _context.inkit.FirstOrDefault(objeto => objeto.codigo == codigo && objeto.item == item);

                    if (inkit == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(inkit);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        /// <summary>
        /// Obtiene todos los registros de la tabla inkit (item) con initem, dependiendo del codigo de item
        /// </summary>
        /// <param name="conexionName"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/initem_inkit
        [HttpGet]
        [Route("initem_inkit/{conexionName}/{coditem}")]
        public async Task<ActionResult<IEnumerable<inkit>>> Getinitem_inkit(string conexionName, string coditem)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    var query = from k in _context.inkit
                                join i in _context.initem on k.item equals i.codigo
                                where k.codigo == coditem
                                orderby k.item
                                select new
                                {
                                    k.codigo,
                                    k.item,
                                    i.descripcion,
                                    i.medida,
                                    k.cantidad,
                                    k.unidad
                                };

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return Problem("No se encontraron registros con esos datos.");
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


        // PUT: api/inkit/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}/{item}")]
        public async Task<IActionResult> Putinkit(string conexionName, string codigo, string item, inkit inkit)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                var kit = _context.inkit.FirstOrDefault(objeto => objeto.codigo == codigo && objeto.item == item);
                if (kit == null)
                {
                    return NotFound("No existe un registro con esa información");
                }

                _context.Entry(inkit).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!inkitExists(codigo, item))
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

        // POST: api/inkit
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<inkit>> Postinkit(string conexionName, inkit inkit)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.inkit == null)
                {
                    return Problem("Entidad inkit es null.");
                }
                _context.inkit.Add(inkit);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (inkitExists(inkit.codigo, inkit.item))
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

        // DELETE: api/inkit/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteinkit(string conexionName, string codigo, string item)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inkit == null)
                    {
                        return Problem("Entidad inkit es null.");
                    }
                    inkit inkit = _context.inkit.FirstOrDefault(objeto => objeto.codigo == codigo && objeto.item == item);
                    if (inkit == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.inkit.Remove(inkit);
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

        private bool inkitExists(string codigo, string item)
        {
            return (_context.inkit?.Any(e => e.codigo == codigo && e.item == item)).GetValueOrDefault();

        }
    }
}
