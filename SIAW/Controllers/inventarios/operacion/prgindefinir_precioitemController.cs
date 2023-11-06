using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Models;
using siaw_funciones;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.inventarios.operacion
{
    [Route("api/inventario/oper/[controller]")]
    [ApiController]
    public class prgindefinir_precioitemController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Ventas ventas = new Ventas();
        public prgindefinir_precioitemController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/catalogo
        [HttpGet]
        [Route("getTarifaItem/{userConn}/{item}/{codtarifa}")]
        public async Task<ActionResult<IEnumerable<intarifa1>>> getTarifaItem(string userConn, string item, int codtarifa)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                string monedaBase = await ventas.monedabasetarifa(userConnectionString, codtarifa);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.intarifa1
                    .Where(i => i.item == item && i.codtarifa == codtarifa)
                    .Select(i => new
                    {
                        codtarifa = i.codtarifa,
                        item = i.item,
                        precio = i.precio,
                        monedaBase = monedaBase
                    })
                    .FirstOrDefaultAsync();

                    if (result == null)
                    {
                        return NotFound("No se encontraron registros con los datos proporcionados.");
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



        // PUT: api/intarifa1/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut]
        [Route("updateTarifa1/{userConn}")]
        public async Task<IActionResult> updateTarifa1(string userConn, intarifa1 intarifa1)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var newTarifa1 = await _context.intarifa1.Where(i => i.codtarifa == intarifa1.codtarifa && i.item == intarifa1.item).FirstOrDefaultAsync();
                if(newTarifa1 == null)
                {
                    return NotFound("No se Encontraron registros con los datos proporcionados.");
                }
                
                newTarifa1.precio = intarifa1.precio;
                _context.Entry(newTarifa1).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Problem("Error en el Servidor.");
                }

                return Ok("206");   // actualizado con exito
            }
        }



    }
}
