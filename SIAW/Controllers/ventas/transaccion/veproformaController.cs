using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NuGet.Configuration;
using SIAW.Data;
using SIAW.Models;
using System.Security.Policy;
using System.Text;
using System.Web.Http.Results;

namespace SIAW.Controllers.ventas.transaccion
{
    [Route("api/venta/transac/[controller]")]
    [ApiController]
    public class veproformaController : ControllerBase
    {

        private readonly UserConnectionManager _userConnectionManager;
        private empaquesFunciones empaque_func = new empaquesFunciones();

        public veproformaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        /// <summary>
        /// Obtiene saldos de un item de una agencia por VPN
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="agencia"></param>
        /// <param name="codalmacen"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/ad_conexion_vpn/5
        [HttpGet]
        [Route("getsladoVpn/{userConn}/{agencia}/{codalmacen}/{coditem}")]
        public async Task<ActionResult> Getsaldos_vpn(string userConn, string agencia, int codalmacen, string coditem)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                var ad_conexion_vpnResult = empaque_func.Getad_conexion_vpnFromDatabase(userConnectionString, agencia);
                if (ad_conexion_vpnResult == null)
                {
                    return Problem("No se pudo obtener la cadena de conexión");
                }

                var instoactual = await empaque_func.GetSaldosActual(ad_conexion_vpnResult, codalmacen, coditem);
                if (instoactual == null)
                {
                    return NotFound("No existe un registro con esos datos");
                }
                return Ok(instoactual);
                //return Ok(ad_conexion_vpnResult);
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }



        /// <summary>
        /// Obtiene saldos de un item de una agencia de manera local
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="codalmacen"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/ad_conexion_vpn/5
        [HttpGet]
        [Route("getsladoLocal/{userConn}/{codalmacen}/{coditem}")]
        public async Task<ActionResult<instoactual>> Getsaldos_local(string userConn, int codalmacen, string coditem)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                var instoactual = await empaque_func.GetSaldosActual(userConnectionString, codalmacen, coditem);
                if (instoactual == null)
                {
                    return NotFound("No existe un registro con esos datos");
                }
                return Ok(instoactual);
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }












        /// <summary>
        /// Obtiene saldos de manera completa
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="codalmacen"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/ad_conexion_vpn/5
        [HttpGet]
        [Route("GetsaldosPrueba/{userConn}/{agencia}/{codalmacen}/{coditem}/{codempresa}")]
        public async Task<ActionResult<instoactual>> Getsaldos(string userConn, string agencia, int codalmacen, string coditem, string codempresa)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                var conexion = userConnectionString;
                bool obtener_cantidades_aprobadas_de_proformas = empaque_func.IfGetCantidadAprobadasProformas(userConnectionString, codempresa); // si se obtender las cantidades reservadas de las proformas o no
                bool bandera = false;
                // Falta validacion para saber si traera datos de manera local o por vpn
                if (bandera)
                {
                    conexion = empaque_func.Getad_conexion_vpnFromDatabase(userConnectionString, agencia);
                    if (conexion == null)
                    {
                        return Problem("No se pudo obtener la cadena de conexión");
                    }
                }
                // obtiene saldos de agencia del item seleccionado
                var instoactual = getEmpaquesItemSelect(conexion, coditem, codalmacen);
                // obtiene reservas en proforma
                
                if (obtener_cantidades_aprobadas_de_proformas && bandera== true)
                {
                    var saldosReservProformas = empaque_func.GetSaldosReservaProforma(conexion,codalmacen,coditem).Result;
                    return Ok(saldosReservProformas);
                }
                else
                {
                    var saldosReservProformas = empaque_func.GetSaldosReservaProformaFromInstoactual(conexion,codalmacen, coditem).Result;
                    return Ok(saldosReservProformas);
                }




                //return Ok(instoactual);
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }








        private async Task<instoactual> getEmpaquesItemSelect (string conexion, string coditem, int codalmacen)
        {
            instoactual instoactual = null;
            var eskit = empaque_func.GetEsKit(conexion, coditem);

            if (!eskit)  // como no es kit obtiene los datos de stock directamente
            {
                //verificar si el item tiene saldos para ese almacen
                instoactual = await empaque_func.GetSaldosActual(conexion, codalmacen, coditem);
            }
            else // como es kit se debe buscar sus piezas sin importar la cantidad de estas que tenga
            {
                List<inkit> kitItems = await empaque_func.GetItemsKit(conexion, coditem);  // se tiene la lista de piezas
                foreach (inkit kit in kitItems) // se recorre la lista de piezas para consultar sus saldos disponibles de cada una (SE DEBE BASAR EL STOCK EN BASE AL MENOR NUMERO)
                {
                    var pivot = await empaque_func.GetSaldosActual(conexion, codalmacen, kit.item);
                    var cantDisp = pivot.cantidad / kit.cantidad;
                    pivot.cantidad = cantDisp;
                    if (instoactual == null)
                    {
                        instoactual = pivot;
                    }
                    else
                    {
                        if (instoactual.cantidad > cantDisp)
                        {
                            instoactual = pivot;
                        }
                    }
                }
                instoactual.coditem = coditem;
            }

            return instoactual;
        }






        /// <summary>
        /// Obtiene empaques dependiendo al area y codigo de item (debe recibir)
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("getempaques/{userConn}/{item}")]
        public async Task<ActionResult<IEnumerable<adusparametros>>> Getempaques_item_area(string userConn, string item)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                int codarea_empaque = Getcod_area_empaqueFromadparametros(userConnectionString);
                if (codarea_empaque==-1)
                {
                    return Problem("No se pudo obtener el codigo de área");
                }
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var empaques = await _context.veempaque
                        .Join(_context.veempaque1,
                              c => c.codigo,
                              d => d.codempaque,
                              (c, d) => new { C = c, D = d })
                        .Where(cd => cd.C.codarea_empaque == codarea_empaque && cd.D.item == item)
                        .OrderBy(cd => cd.C.codigo)
                        .Select(cd => new
                        {
                            Codigo = cd.C.codigo,
                            Descripcion = cd.C.corta,
                            Cantidad = cd.D.cantidad
                        })
                        .ToListAsync();

                    if (empaques.Count() == 0)
                    {
                        return Problem("No se encontraron datos.");
                    }
                    return Ok(empaques);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }






        protected int Getcod_area_empaqueFromadparametros(string userConnectionString)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adparametros == null)
                {
                    return -1;
                }
                var codarea_empaque = _context.adparametros
                    .Select(a => new
                    {
                        codarea_empaque = a.codarea_empaque
                    })
                    .FirstOrDefault();
                if (codarea_empaque == null)
                {
                    return -1;
                }
                int codArea = (int)codarea_empaque.codarea_empaque;

                return codArea;
            }
        }



    }
}
