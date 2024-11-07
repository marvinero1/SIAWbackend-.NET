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
    public class prgbusqanticipoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public prgbusqanticipoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        [HttpPost]
        [Route("getAnticiposByParam/{userConn}")]
        public async Task<IActionResult> getAnticiposByParam(string userConn, RequestBusquedaAnticip? filtrosBusquedaAnti)
        {
            // valida
            string validacion = validaData(filtrosBusquedaAnti);
            if (validacion != "ok")
            {
                return BadRequest(new {resp = validacion});
            }
            
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.coanticipo.AsQueryable();

                    if (filtrosBusquedaAnti != null)
                    {
                        // busqueda por id
                        if (filtrosBusquedaAnti.itodos1)
                        {
                            query = query.Where(i => string.Compare(i.id, filtrosBusquedaAnti.id1) >= 0 && string.Compare(i.id, filtrosBusquedaAnti.id2) <= 0);
                        }
                        // busqueda por fecha
                        if (filtrosBusquedaAnti.ftodos1)
                        {
                            query = query.Where(i => i.fecha >= (filtrosBusquedaAnti.fechade ?? new DateTime(1900, 1, 1)).Date && i.fecha <= (filtrosBusquedaAnti.fechaa ?? new DateTime(1900, 1, 1)).Date);
                        }
                        // busqueda por almacen
                        if (filtrosBusquedaAnti.atodos1)
                        {
                            query = query.Where(i => i.codalmacen >= filtrosBusquedaAnti.codalmacen1 && i.codalmacen <= filtrosBusquedaAnti.codalmacen2);
                        }
                    }
                    var AnticipBusq = await query
                        .Where(i => i.anulado == false)
                        .OrderBy(i => i.fecha)
                        .ThenBy(i => i.id)
                        .ThenBy(i => i.numeroid)
                        //.Take(100)
                        .Select(i => new
                        {
                            i.codigo,
                            i.id,
                            i.numeroid,
                            i.fecha,
                            i.codcliente,
                            i.codvendedor,
                            i.codalmacen,
                            i.codmoneda,
                            i.descripcion,
                            i.anulado,
                            i.monto,
                            i.montorest,
                            para_venta_contado_desc = i.para_venta_contado == true ? "SI":"NO",
                            i.para_venta_contado,

                        }).ToListAsync();
                    return Ok(AnticipBusq);
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor: " + ex.Message);
            }
        }
        private string validaData(RequestBusquedaAnticip? filtrosBusquedaAnti)
        {
            if (filtrosBusquedaAnti != null)
            {
                if (filtrosBusquedaAnti.itodos1 == true)
                {
                    if (filtrosBusquedaAnti.id1 == "")
                    {
                        return "Debe poner el ID desde el cual desea buscar los Anticipos.";
                    }
                    if (filtrosBusquedaAnti.id2 == "")
                    {
                        return "Debe poner el ID hasta el cual desea buscar los Anticipos.";
                    }
                }
                if (filtrosBusquedaAnti.ftodos1 == true)
                {
                    if (filtrosBusquedaAnti.fechade > filtrosBusquedaAnti.fechaa)
                    {
                        return "La fecha de inicio no puede ser mayor a la fecha de termino del intervalo deseado.";
                    }
                }
                if (filtrosBusquedaAnti.atodos1 == true)
                {
                    if (filtrosBusquedaAnti.codalmacen1 == 0)
                    {
                        return "Debe poner el Almacen desde el cual desea buscar los Anticipos.";
                    }
                    if (filtrosBusquedaAnti.codalmacen2 == 0)
                    {
                        return "Debe poner el Almacen hasta el cual desea buscar los Anticipos.";
                    }
                }
            }
            return "ok";
        }
    }


    public class RequestBusquedaAnticip
    {
        public bool itodos1 { get; set; } = false;
        public string id1 { get; set; } = string.Empty;
        public string id2 { get; set; } = string.Empty;
        public bool ftodos1 { get; set; } = false;
        public DateTime? fechade { get; set; } = new DateTime(1900, 1, 1);
        public DateTime? fechaa { get; set; } = new DateTime(1900, 1, 1);
        public bool atodos1 { get; set; } = false;
        public int codalmacen1 { get; set; } = 0;
        public int codalmacen2 { get; set; } = 0;
    }

}
