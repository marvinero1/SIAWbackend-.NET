using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Controllers.ventas.busquedas;

namespace SIAW.Controllers.inventarios.busquedas
{
    [Route("api/inventario/busq/[controller]")]
    [ApiController]
    public class prgbusqinnotaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public prgbusqinnotaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        [HttpPost]
        [Route("getNotMovByParam/{userConn}")]
        public async Task<IActionResult> getNotMovByParam(string userConn, RequestBusquedaNM? filtrosBusquedaNM)
        {
            // valida
            string validacion = validaData(filtrosBusquedaNM);
            if (validacion != "ok")
            {
                return BadRequest(new { resp = validacion });
            }
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.inmovimiento.AsQueryable();
                    if (filtrosBusquedaNM != null)
                    {
                        // busqueda por id
                        if (filtrosBusquedaNM.itodos1)
                        {
                            query = query.Where(i => string.Compare(i.id, filtrosBusquedaNM.id1) >= 0 && string.Compare(i.id, filtrosBusquedaNM.id2) <= 0);
                        }
                        // busqueda por fecha
                        if (filtrosBusquedaNM.ftodos1)
                        {
                            query = query.Where(i => i.fecha >= (filtrosBusquedaNM.fechade ?? new DateTime(1900, 1, 1)).Date && i.fecha <= (filtrosBusquedaNM.fechaa ?? new DateTime(1900, 1, 1)).Date);
                        }
                        // busqueda por almacen
                        if (filtrosBusquedaNM.atodos1)
                        {
                            query = query.Where(i => i.codalmacen >= filtrosBusquedaNM.codalmacen1 && i.codalmacen <= filtrosBusquedaNM.codalmacen2);
                        }
                    }

                    var notMovimientoBusq = await query
                        .OrderBy(i => i.fecha)
                        .ThenBy(i => i.id)
                        .ThenBy(i => i.numeroid)
                        .Select(i => new
                        {
                            i.codigo,
                            i.id,
                            i.numeroid,
                            i.codconcepto,
                            i.fecha,
                            i.codalmacen,
                            i.codalmorigen,
                            i.codalmdestino,
                            i.obs,
                            i.anulada
                        }).ToListAsync();


                    return Ok(notMovimientoBusq);
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor en busq Avanzada NM: " + ex.Message);
            }
        }

        private string validaData(RequestBusquedaNM? filtrosBusquedaNM)
        {
            if (filtrosBusquedaNM != null)
            {
                if (filtrosBusquedaNM.itodos1 == true)
                {
                    if (filtrosBusquedaNM.id1 == "")
                    {
                        return "Debe poner el ID desde el cual desea buscar las Notas.";
                    }
                    if (filtrosBusquedaNM.id2 == "")
                    {
                        return "Debe poner el ID hasta el cual desea buscar las Notas.";
                    }
                }
                if (filtrosBusquedaNM.ftodos1 == true)
                {
                    if (filtrosBusquedaNM.fechade > filtrosBusquedaNM.fechaa)
                    {
                        return "La fecha de inicio no puede ser mayor a la fecha de termino del intervalo deseado.";
                    }
                }
                if (filtrosBusquedaNM.atodos1 == true)
                {
                    if (filtrosBusquedaNM.codalmacen1 == 0)
                    {
                        return "Debe poner el Almacen desde el cual desea buscar las Notas.";
                    }
                    if (filtrosBusquedaNM.codalmacen2 == 0)
                    {
                        return "Debe poner el Almacen hasta el cual desea buscar las Notas.";
                    }
                }
            }
            return "ok";
        }
    }

    public class RequestBusquedaNM
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
