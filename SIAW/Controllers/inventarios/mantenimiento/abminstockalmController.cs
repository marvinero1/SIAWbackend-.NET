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




        // POST: api/instockalm
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost]
        [Route("actStockAlm/{userConn}/{codalmacen}")]
        public async Task<ActionResult<instockalm>> actualizardetalle(string userConn, int codalmacen)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var newItemCodes = _context.initem.Select(i => i.codigo);
                var existingItemCodes = _context.instockalm
                    .Where(s => s.codalmacen == codalmacen)
                    .Select(s => s.item);

                var newItems = await newItemCodes.Except(existingItemCodes)
                    .Select(codigo => new instockalm
                    {
                        codalmacen = codalmacen,
                        item = codigo,
                        smin = 0,
                        smax = 0,
                        ptopedido = 0,
                        codalmpedido = codalmacen
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

        // PUT: api/instockalm
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut]
        [Route("grabar/{userConn}/{codalmacen}")]
        public async Task<ActionResult<instockalm>> grabardetalle(string userConn, int codalmacen, List<instockalm> instockalm)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var result = await _context.instockalm
                        .Where(s => s.codalmacen == codalmacen)
                        .ToListAsync();
                        // elimina si hay datos
                        if (result.Count() > 0)
                        {
                            _context.instockalm.RemoveRange(result);
                            await _context.SaveChangesAsync();
                        }

                        // crea con la modificaciones
                        _context.instockalm.AddRange(instockalm);
                        await _context.SaveChangesAsync();

                        dbContexTransaction.Commit();
                        return Ok(new { resp = "204" });   // creado con exito
                    }
                    catch (Exception)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor");
                        throw;
                    }
                }
                    
            }
        }

        // DELETE: api/instockalm
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpDelete]
        [Route("eliminar/{userConn}/{codalmacen}")]
        public async Task<ActionResult<instockalm>> eliminardetalle(string userConn, int codalmacen)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    var result = await _context.instockalm
                    .Where(s => s.codalmacen == codalmacen)
                    .ToListAsync();
                    // elimina si hay datos
                    if (result.Count() > 0)
                    {
                        _context.instockalm.RemoveRange(result);
                        await _context.SaveChangesAsync();
                        return Ok(new { resp = "204" });   // creado con exito
                    }
                    return BadRequest(new { resp = "No se encontraron registros con esos datos." });
                    
                }
                catch (Exception)
                {
                    return Problem("Error en el servidor");
                    throw;
                }
            }
        }


    }
}
