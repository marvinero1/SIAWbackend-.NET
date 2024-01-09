using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Route("api/venta/mant/[controller]")]
    [ApiController]
    public class vedesitemController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public vedesitemController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        /// <summary>
        /// Obtiene todos los datos de la tabla vedesitem ordenado por codalmacen y coditem
        /// </summary>
        /// <param name="userConn"></param>
        /// <returns></returns>
        // GET: api/vedesitem
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<vedesitem>>> Getvedesitem(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedesitem == null)
                    {
                        return BadRequest(new { resp = "Entidad vedesitem es null." });
                    }
                    var result = await _context.vedesitem.OrderBy(codalmacen => codalmacen.codalmacen).ThenBy(coditem => coditem.coditem).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }


        /// <summary>
        /// Obtiene todos los datos de un registro de la tabla vedesitem
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="codalmacen"></param>
        /// <param name="coditem"></param>
        /// <param name="nivel"></param>
        /// <returns></returns>
        // GET: api/vedesitem/5
        [HttpGet("{userConn}/{codalmacen}/{coditem}/{nivel}")]
        public async Task<ActionResult<vedesitem>> Getvedesitem(string userConn, int codalmacen, string coditem, string nivel)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedesitem == null)
                    {
                        return BadRequest(new { resp = "Entidad vedesitem es null." });
                    }
                    var vedesitem = _context.vedesitem.FirstOrDefault(objeto => objeto.codalmacen == codalmacen && objeto.coditem == coditem && objeto.nivel == nivel);

                    if (vedesitem == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(vedesitem);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }



        // PUT: api/vedesitem/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codalmacen}/{coditem}/{nivel}")]
        public async Task<IActionResult> Putvedesitem(string userConn, int codalmacen, string coditem, string nivel, vedesitem vedesitem)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var desitem = _context.vedesitem.FirstOrDefault(objeto => objeto.codalmacen == codalmacen && objeto.coditem == coditem && objeto.nivel == nivel);
                if (desitem == null)
                {
                    return NotFound( new { resp = "No existe un registro con esa información" });
                }

                _context.Entry(desitem).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Problem("Error en el servidor");
                }

                return Ok( new { resp = "206" });   // actualizado con exito
            }
        }

        // POST: api/vedesitem
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<vedesitem>> Postvedesitem(string userConn, vedesitem vedesitem)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.vedesitem == null)
                {
                    return BadRequest(new { resp = "Entidad vedesitem es null." });
                }
                _context.vedesitem.Add(vedesitem);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                }

                return Ok( new { resp = "204" });   // creado con exito

            }
            
        }

        // DELETE: api/vedesitem/5
        [Authorize]
        [HttpDelete("{userConn}/{codalmacen}/{coditem}/{nivel}")]
        public async Task<IActionResult> Deletevedesitem(string userConn, int codalmacen, string coditem, string nivel)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedesitem == null)
                    {
                        return BadRequest(new { resp = "Entidad vedesitem es null." });
                    }
                    var vedesitem = _context.vedesitem.FirstOrDefault(objeto => objeto.codalmacen == codalmacen && objeto.coditem == coditem && objeto.nivel == nivel);
                    if (vedesitem == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.vedesitem.Remove(vedesitem);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

    }
}
