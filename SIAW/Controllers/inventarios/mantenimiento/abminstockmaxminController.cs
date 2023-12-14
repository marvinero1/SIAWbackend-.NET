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
    public class abminstockmaxminController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public abminstockmaxminController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/inalmacen
        [HttpGet]
        [Route("getStockMaxMin/{userConn}/{codalmacendesde}/{codalmacenhasta}")]
        public async Task<ActionResult<IEnumerable<instockalm>>> getStockMaxMin(string userConn, int codalmacendesde, int codalmacenhasta)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var resultado = await _context.instockalm
                        .Join(
                            _context.initem,
                            p1 => p1.item,
                            p2 => p2.codigo,
                            (p1, p2) => new { p1, p2 }
                        )
                        .Where(x => x.p1.codalmacen == codalmacendesde && x.p1.codalmpedido == codalmacenhasta)
                        .OrderBy(x => x.p1.item)
                        .Select(x => new
                        {
                            x.p1.codalmacen,
                            x.p1.codalmpedido,
                            x.p1.item,
                            x.p2.descripcion,
                            x.p2.medida,
                            x.p1.smax,
                            x.p1.smin,
                            x.p1.ptopedido
                        })
                        .ToListAsync();
                    if (resultado.Count > 0)
                    {
                        return Ok(resultado);
                    }
                    return BadRequest(new { resp = "No se encontraron datos" });
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }



        // POST: api/
        [Authorize]
        [HttpPost]
        [Route("guardardetalle/{userConn}/{codinfisico}")]
        public async Task<ActionResult<string>> guardardetalle(string userConn, int codinfisico)
        {

            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);


            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {

                        dbContexTransaction.Commit();
                        return Ok(new { resp = "Datos guardados con Exito" });
                    }
                    catch (DbUpdateException)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor");
                    }
                }
            }
        }


    }
}
