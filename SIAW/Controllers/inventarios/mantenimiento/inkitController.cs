using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class inkitController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public inkitController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/inkit
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<inkit>>> Getinkit(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inkit == null)
                    {
                        return Problem("Entidad inkit es null.");
                    }
                    var result = await _context.inkit.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/inkit/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<inkit>> Getinkit(string userConn, string codigo, string item)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        /// <summary>
        /// Obtiene todos los registros de la tabla inkit (item) con initem, dependiendo del codigo de item
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/initem_inkit
        [HttpGet]
        [Route("initem_inkit/{userConn}/{coditem}")]
        public async Task<ActionResult<IEnumerable<inkit>>> Getinitem_inkit(string userConn, string coditem)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }


        // PUT: api/inkit/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}/{item}")]
        public async Task<IActionResult> Putinkit(string userConn, string codigo, string item, inkit inkit)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
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
                    if (!inkitExists(codigo, item, _context))
                    {
                        return NotFound("No existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("206");   // actualizado con exito
            }
            


        }

        // POST: api/inkit
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<inkit>> Postinkit(string userConn, inkit inkit)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
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
                    if (inkitExists(inkit.codigo, inkit.item, _context))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("204");   // creado con exito

            }
            
        }

        // DELETE: api/inkit/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}/{item}")]
        public async Task<IActionResult> Deleteinkit(string userConn, string codigo, string item)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inkit == null)
                    {
                        return Problem("Entidad inkit es null.");
                    }
                    var inkit = _context.inkit.FirstOrDefault(objeto => objeto.codigo == codigo && objeto.item == item);
                    if (inkit == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.inkit.Remove(inkit);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool inkitExists(string codigo, string item, DBContext _context)
        {
            return (_context.inkit?.Any(e => e.codigo == codigo && e.item == item)).GetValueOrDefault();

        }
    }
}
