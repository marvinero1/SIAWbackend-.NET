using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Route("api/venta/mant/[controller]")]
    [ApiController]
    public class venumeracionController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public venumeracionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/venumeracion
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<venumeracion>>> Getvenumeracion(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.venumeracion == null)
                    {
                        return Problem("Entidad venumeracion es null.");
                    }
                    var result = await _context.venumeracion.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/venumeracion/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<venumeracion>> Getvenumeracion(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }


        /// <summary>
        /// Obtiene algunos datos de la tabla venumeracion para catalogo por tipodoc y si esta habilitado
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="tipodoc"></param>
        /// <returns></returns>
        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}/{tipodoc}")]
        public async Task<ActionResult<IEnumerable<venumeracion>>> Getvenumeracion_catalogo(string userConn, int tipodoc)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }



        // PUT: api/venumeracion/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putvenumeracion(string userConn, string id, venumeracion venumeracion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
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
                    if (!venumeracionExists(id, _context))
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
            


        }

        // POST: api/venumeracion
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<venumeracion>> Postvenumeracion(string userConn, venumeracion venumeracion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
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
                    if (venumeracionExists(venumeracion.id, _context))
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
            
        }

        // DELETE: api/venumeracion/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletevenumeracion(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool venumeracionExists(string id, DBContext _context)
        {
            return (_context.venumeracion?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
