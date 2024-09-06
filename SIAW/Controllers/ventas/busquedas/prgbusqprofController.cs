using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;

namespace SIAW.Controllers.ventas.busquedas
{
    [Route("api/venta/busq/[controller]")]
    [ApiController]
    public class prgbusqprofController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public prgbusqprofController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }
        [HttpPost]
        [Route("getProformasByParam/{userConn}")]
        public async Task<IActionResult> getProformasByParam(string userConn, RequestBusquedaProf? filtrosBusquedaProf)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.veproforma.AsQueryable();
                    if (filtrosBusquedaProf != null)
                    {
                        if (filtrosBusquedaProf.itodos1)
                        {
                            query = query.Where(i => string.Compare(i.id, filtrosBusquedaProf.id1) >= 0 && string.Compare(i.id, filtrosBusquedaProf.id2) <= 0);
                        }
                        if (filtrosBusquedaProf.ftodos1)
                        {
                            query = query.Where(i => i.fecha >= (filtrosBusquedaProf.fechade ?? new DateTime(1900,1,1)).Date && i.fecha <= (filtrosBusquedaProf.fechaa ?? new DateTime(1900, 1, 1)).Date);
                        }
                        if (filtrosBusquedaProf.atodos1)
                        {
                            query = query.Where(i => i.codalmacen >= filtrosBusquedaProf.codalmacen1 && i.codalmacen <= filtrosBusquedaProf.codalmacen2);
                        }
                        if (filtrosBusquedaProf.vtodos1)
                        {
                            query = query.Where(i => i.codvendedor >= filtrosBusquedaProf.codvendedor1 && i.codvendedor <= filtrosBusquedaProf.codvendedor2);
                        }
                        if (filtrosBusquedaProf.ctodos1)
                        {
                            query = query.Where(i => string.Compare(i.codcliente, filtrosBusquedaProf.codcliente1) >= 0 && string.Compare(i.codcliente, filtrosBusquedaProf.codcliente2) <= 0);
                        }
                    }
                    var proformasBusq = await query
                        .OrderBy(i => i.fecha)
                        .ThenBy(i => i.id)
                        .ThenBy(i =>i.numeroid)
                        //.Take(100)
                        .Select(i => new
                        {
                            i.codigo,
                            i.id,
                            i.numeroid,
                            i.fecha,
                            i.codcliente,
                            i.nomcliente,
                            i.codvendedor,
                            i.codalmacen,
                            i.total,
                            i.codmoneda
                        }).ToListAsync();
                    return Ok(proformasBusq);
                }

            }
            catch (Exception)
            {

                throw;
            }
        }


        [HttpPost]
        [Route("getNotRemisionByParam/{userConn}")]
        public async Task<IActionResult> getNotRemisionByParam(string userConn, RequestBusquedaProf? filtrosBusquedaProf)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.veremision.AsQueryable();
                    if (filtrosBusquedaProf != null)
                    {
                        if (filtrosBusquedaProf.itodos1)
                        {
                            query = query.Where(i => string.Compare(i.id, filtrosBusquedaProf.id1) >= 0 && string.Compare(i.id, filtrosBusquedaProf.id2) <= 0);
                        }
                        if (filtrosBusquedaProf.ftodos1)
                        {
                            query = query.Where(i => i.fecha >= (filtrosBusquedaProf.fechade ?? new DateTime(1900, 1, 1)).Date && i.fecha <= (filtrosBusquedaProf.fechaa ?? new DateTime(1900, 1, 1)).Date);
                        }
                        if (filtrosBusquedaProf.atodos1)
                        {
                            query = query.Where(i => i.codalmacen >= filtrosBusquedaProf.codalmacen1 && i.codalmacen <= filtrosBusquedaProf.codalmacen2);
                        }
                        if (filtrosBusquedaProf.vtodos1)
                        {
                            query = query.Where(i => i.codvendedor >= filtrosBusquedaProf.codvendedor1 && i.codvendedor <= filtrosBusquedaProf.codvendedor2);
                        }
                        if (filtrosBusquedaProf.ctodos1)
                        {
                            query = query.Where(i => string.Compare(i.codcliente, filtrosBusquedaProf.codcliente1) >= 0 && string.Compare(i.codcliente, filtrosBusquedaProf.codcliente2) <= 0);
                        }
                    }
                    var notaRemiBusq = await query
                        .OrderBy(i => i.fecha)
                        .ThenBy(i => i.id)
                        .ThenBy(i => i.numeroid)
                        .Take(100)
                        .Select(i => new
                        {
                            i.codigo,
                            i.id,
                            i.numeroid,
                            i.fecha,
                            i.codcliente,
                            i.nomcliente,
                            i.codvendedor,
                            i.codalmacen,
                            i.total,
                            i.codmoneda
                        }).ToListAsync();
                    return Ok(notaRemiBusq);
                }

            }
            catch (Exception)
            {

                throw;
            }
        }


    }




    public class RequestBusquedaProf
    {
        public bool itodos1 { get; set; } = false;
        public string id1 { get; set; } = string.Empty;
        public string id2 { get; set; } = string.Empty;
        public bool ftodos1 { get; set; } = false;
        public DateTime? fechade { get; set; } = new DateTime(1900,1,1);
        public DateTime? fechaa { get; set; } = new DateTime(1900, 1, 1);
        public bool atodos1 { get; set; } = false;
        public int codalmacen1 { get; set; } = 0;
        public int codalmacen2 { get; set; } = 0;
        public bool vtodos1 { get; set; } = false;
        public int codvendedor1 { get; set; } = 0;
        public int codvendedor2 { get; set; } = 0;
        public bool ctodos1 { get; set; } = false;
        public string codcliente1 { get; set; } = string.Empty;
        public string codcliente2 { get; set; } = string.Empty;
    }
}
