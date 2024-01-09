using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class abminstockalmController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public abminstockalmController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/stockAlm
        [HttpGet]
        [Route("stockAlm/{userConn}/{codalmacen}")]
        public async Task<ActionResult<IEnumerable<object>>> getstockAlm(string userConn, int codalmacen)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adsiat_actividad == null)
                    {
                        return BadRequest(new { resp = "Entidad adsiat_actividad es null." });
                    }
                    var result = await _context.instockalm
                        .Where(s => s.codalmacen == codalmacen)
                        .Join(_context.initem, s => s.item, i => i.codigo, (s, i) => new
                        {
                            codalmacen = s.codalmacen,
                            item = s.item,
                            smin = s.smin,
                            smax = s.smax,
                            ptopedido = s.ptopedido,
                            codalmpedido = s.codalmpedido,
                            descripcion = i.descripcion,
                            medida = i.medida,
                            unidad = i.unidad
                        })
                        .ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }




        // POST: api/inrosca
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[Authorize]
        [HttpPost]
        [Route("actStockAlm/{userConn}/{codalmacen}")]
        public async Task<ActionResult<inrosca>> actualizardetalle(string userConn, int codalmacen)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var newItemCodes = _context.initem.Select(i => i.codigo);
                var existingItemCodes = _context.instockalm
                    .Where(s => s.codalmacen == 311)
                    .Select(s => s.item);

                var newItems = await newItemCodes.Except(existingItemCodes)
                    .Select(codigo => new instockalm
                    {
                        codalmacen = 311,
                        item = codigo,
                        smin = 0,
                        smax = 0,
                        ptopedido = 0,
                        codalmpedido = 311
                    })
                    .ToListAsync();
                _context.instockalm.AddRange(newItems);
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

    }
}
