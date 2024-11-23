using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SIAW.Controllers.ventas.busquedas
{
    [Route("api/venta/busq/[controller]")]
    [ApiController]
    public class prgbusqfactController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public prgbusqfactController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }
        [HttpPost]
        [Route("getFacturasByParam/{userConn}/{buscar_notasdebito}/{buscar_facturas}")]
        public async Task<IActionResult> getFacturasByParam(string userConn, bool buscar_notasdebito, bool buscar_facturas, RequestBusquedaFact? filtrosBusquedaFact)
        {
            if (filtrosBusquedaFact != null)
            {
                filtrosBusquedaFact.id1 = filtrosBusquedaFact.id1.Trim();
                filtrosBusquedaFact.id2 = filtrosBusquedaFact.id2.Trim();
                filtrosBusquedaFact.codcliente1 = filtrosBusquedaFact.codcliente1.Trim();
                filtrosBusquedaFact.codcliente2 = filtrosBusquedaFact.codcliente2.Trim();
                filtrosBusquedaFact.nomcliente = filtrosBusquedaFact.nomcliente.Trim();
                filtrosBusquedaFact.nit = filtrosBusquedaFact.nit.Trim();

                // validar
                string msgValido = validar(filtrosBusquedaFact);
                if (msgValido != "ok")
                {
                    return BadRequest(new { resp = msgValido });
                }
            }

            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.vefactura.AsQueryable();
                    if (filtrosBusquedaFact != null)
                    {
                        if (filtrosBusquedaFact.itodos1)
                        {
                            query = query.Where(i => string.Compare(i.id, filtrosBusquedaFact.id1) >= 0 && string.Compare(i.id, filtrosBusquedaFact.id2) <= 0);
                        }
                        if (filtrosBusquedaFact.ftodos1)
                        {
                            query = query.Where(i => i.fecha >= (filtrosBusquedaFact.fechade ?? new DateTime(1900, 1, 1)).Date && i.fecha <= (filtrosBusquedaFact.fechaa ?? new DateTime(1900, 1, 1)).Date);
                        }
                        if (filtrosBusquedaFact.atodos1)
                        {
                            query = query.Where(i => i.codalmacen >= filtrosBusquedaFact.codalmacen1 && i.codalmacen <= filtrosBusquedaFact.codalmacen2);
                        }
                        if (filtrosBusquedaFact.vtodos1)
                        {
                            query = query.Where(i => i.codvendedor >= filtrosBusquedaFact.codvendedor1 && i.codvendedor <= filtrosBusquedaFact.codvendedor2);
                        }
                        if (filtrosBusquedaFact.ctodos1)
                        {
                            query = query.Where(i => string.Compare(i.codcliente, filtrosBusquedaFact.codcliente1) >= 0 && string.Compare(i.codcliente, filtrosBusquedaFact.codcliente2) <= 0);
                        }
                        if (filtrosBusquedaFact.facttodos1)
                        {
                            query = query.Where(i => i.nrofactura == filtrosBusquedaFact.nrofactura);
                        }
                        if (filtrosBusquedaFact.ntodos1)
                        {
                            query = query.Where(i => i.nomcliente.Contains(filtrosBusquedaFact.nomcliente));
                        }
                        if (filtrosBusquedaFact.ntodos2)
                        {
                            query = query.Where(i => i.nit.Contains(filtrosBusquedaFact.nit));
                        }
                        if (buscar_notasdebito)
                        {
                            if (buscar_facturas)
                            {
                                // todo
                            }
                            else
                            {
                                query = query.Where(i => i.notadebito == true);
                            }
                        }
                        else
                        {
                            if (buscar_facturas)
                            {
                                query = query.Where(i => i.notadebito == false);
                            }
                            else
                            {
                                query = query.Where(i => i.notadebito == false);
                            }
                        }
                    }
                    var proformasBusq = await query
                        .OrderBy(i => i.fecha)
                        .ThenBy(i => i.id)
                        .ThenBy(i => i.numeroid)
                        .Select(i => new
                        {
                            i.codigo,
                            i.id,
                            i.numeroid,
                            i.fecha,
                            i.codcliente,
                            i.nit,
                            i.nomcliente,
                            i.codvendedor,
                            i.codalmacen,
                            i.total,
                            i.codmoneda,
                            i.alfanumerico,
                            i.nroorden,
                            i.nroautorizacion,
                            i.nrofactura
                        }).ToListAsync();
                    return Ok(proformasBusq);
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        private string validar(RequestBusquedaFact filtrosBusquedaFact)
        {
            if (filtrosBusquedaFact.itodos1)
            {
                if (filtrosBusquedaFact.id1 == "")
                {
                    return "Debe poner el ID desde el cual desea buscar las Facturas.";
                }
                else if(filtrosBusquedaFact.id2 == "")
                {
                    return "Debe poner el ID hasta el cual desea buscar las Facturas.";
                }
            }
            if (filtrosBusquedaFact.ftodos1)
            {
                if ((filtrosBusquedaFact.fechade ?? (new DateTime(1900, 1, 1))).Date > (filtrosBusquedaFact.fechaa ?? (new DateTime(1900, 1, 1))).Date)
                {
                    return "La fecha de inicio no puede ser mayor a la fecha de termino del intervalo deseado.";
                }
            }
            if (filtrosBusquedaFact.atodos1)
            {
                if (filtrosBusquedaFact.codalmacen1 == 0)
                {
                    return "Debe poner el Almacen desde el cual desea buscar las Facturas.";
                }else if(filtrosBusquedaFact.codalmacen2 == 0)
                {
                    return "Debe poner el Almacen hasta el cual desea buscar las Facturas.";
                }
            }
            if (filtrosBusquedaFact.vtodos1)
            {
                if (filtrosBusquedaFact.codvendedor1 == 0)
                {
                    return "Debe poner el Vendedor desde el cual desea buscar las Facturas.";
                }
                else if (filtrosBusquedaFact.codvendedor2 == 0)
                {
                    return "Debe poner el Vendedor hasta el cual desea buscar las Facturas.";
                }
            }
            if (filtrosBusquedaFact.vtodos1)
            {
                if (filtrosBusquedaFact.codcliente1 == "")
                {
                    return "Debe poner el Cliente desde el cual desea buscar las Facturas.";
                }
                else if (filtrosBusquedaFact.codcliente2 == "")
                {
                    return "Debe poner el Cliente hasta el cual desea buscar las Facturas.";
                }
            }
            if (filtrosBusquedaFact.facttodos1)
            {
                if (filtrosBusquedaFact.nrofactura == 0)
                {
                    return "Debe poner el Numero de Factura desde el cual desea buscar las Facturas.";
                }
            }
            if (filtrosBusquedaFact.ntodos1)
            {
                if (filtrosBusquedaFact.nomcliente == "")
                {
                    return "Debe poner el Nombre al cual se facturo.";
                }
            }else if (filtrosBusquedaFact.ntodos2)
            {
                if (filtrosBusquedaFact.nit == "")
                {
                    return "Debe poner el Nombre al cual se facturo.";
                }
            }
            return "ok";
        }

    }


    public class RequestBusquedaFact
    {
        // id
        public bool itodos1 { get; set; } = false;
        public string id1 { get; set; } = string.Empty;
        public string id2 { get; set; } = string.Empty;
        // fecha
        public bool ftodos1 { get; set; } = false;
        public DateTime? fechade { get; set; } = new DateTime(1900, 1, 1);
        public DateTime? fechaa { get; set; } = new DateTime(1900, 1, 1);
        // almacen
        public bool atodos1 { get; set; } = false;
        public int codalmacen1 { get; set; } = 0;
        public int codalmacen2 { get; set; } = 0;
        // vendedor
        public bool vtodos1 { get; set; } = false;
        public int codvendedor1 { get; set; } = 0;
        public int codvendedor2 { get; set; } = 0;
        // cliente
        public bool ctodos1 { get; set; } = false;
        public string codcliente1 { get; set; } = string.Empty;
        public string codcliente2 { get; set; } = string.Empty;
        // factura
        public bool facttodos1 { get; set; } = false;
        public int nrofactura { get; set; } = 0;
        // nombre Fact
        public bool ntodos1 { get; set; } = false;
        public string nomcliente { get; set; } = string.Empty;
        // nit fact
        public bool ntodos2 { get; set; } = false;
        public string nit { get; set; } = string.Empty;
    }
}
