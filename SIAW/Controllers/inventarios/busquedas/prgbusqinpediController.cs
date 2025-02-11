using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SIAW.Controllers.inventarios.busquedas
{
    [Route("api/inventario/busq/[controller]")]
    [ApiController]
    public class prgbusqinpediController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public prgbusqinpediController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        [HttpPost]
        [Route("getPedidosByParam/{userConn}")]
        public async Task<IActionResult> getPedidosByParam(string userConn, RequestBusquedaPedido? RequestBusquedaPedido)
        {
            // valida
            string validacion = validaData(RequestBusquedaPedido);
            if (validacion != "ok")
            {
                return BadRequest(new { resp = validacion });
            }
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.inpedido.AsQueryable();
                    if (RequestBusquedaPedido != null)
                    {
                        // busqueda por fecha
                        if (RequestBusquedaPedido.ftodos1)
                        {
                            query = query.Where(i => i.fecha >= (RequestBusquedaPedido.fechade ?? new DateTime(1900, 1, 1)).Date && i.fecha <= (RequestBusquedaPedido.fechaa ?? new DateTime(1900, 1, 1)).Date);
                        }
                        // busqueda por almacen
                        if (RequestBusquedaPedido.atodos1)
                        {
                            query = query.Where(i => i.codalmacen >= RequestBusquedaPedido.codalmacen1 && i.codalmacen <= RequestBusquedaPedido.codalmacen2);
                        }
                    }

                    var notPedidoBusq = await query
                        .OrderBy(i => i.fecha)
                        .ThenBy(i => i.codigo)
                        .Select(i => new
                        {
                            i.codigo,
                            i.id,
                            i.numeroid,
                            i.codalmacen,
                            i.codalmdestino,
                            i.fecha,
                            i.obs
                        }).ToListAsync();


                    return Ok(notPedidoBusq);
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor en busq Avanzada NM: " + ex.Message);
            }
        }


        private string validaData(RequestBusquedaPedido? RequestBusquedaPedido)
        {
            if (RequestBusquedaPedido != null)
            {
                if (RequestBusquedaPedido.ftodos1 == true)
                {
                    if (RequestBusquedaPedido.fechade > RequestBusquedaPedido.fechaa)
                    {
                        return "La fecha de inicio no puede ser mayor a la fecha de termino del intervalo deseado.";
                    }
                }
                if (RequestBusquedaPedido.atodos1 == true)
                {
                    if (RequestBusquedaPedido.codalmacen1 == 0)
                    {
                        return "Debe poner el Almacen desde el cual desea buscar las Notas.";
                    }
                    if (RequestBusquedaPedido.codalmacen2 == 0)
                    {
                        return "Debe poner el Almacen hasta el cual desea buscar las Notas.";
                    }
                }
            }
            return "ok";
        }

    }

    public class RequestBusquedaPedido
    {
        public bool ftodos1 { get; set; } = false;
        public DateTime? fechade { get; set; } = new DateTime(1900, 1, 1);
        public DateTime? fechaa { get; set; } = new DateTime(1900, 1, 1);
        public bool atodos1 { get; set; } = false;
        public int codalmacen1 { get; set; } = 0;
        public int codalmacen2 { get; set; } = 0;
    }

}
