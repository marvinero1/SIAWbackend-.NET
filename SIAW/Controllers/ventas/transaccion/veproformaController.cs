using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SIAW.Models;
using System.Web.Http.Results;

namespace SIAW.Controllers.ventas.transaccion
{
    [Route("api/[controller]")]
    [ApiController]
    public class veproformaController : ControllerBase
    {

        private readonly UserConnectionManager _userConnectionManager;
        private get_ad_conexion_vpn conexion_vpn = new get_ad_conexion_vpn();

        public veproformaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }



        // GET: api/ad_conexion_vpn/5
        [HttpGet("{userConn}/{agencia}")]
        public async Task<ActionResult> Getsaldos_vpn(string userConn, string agencia)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                var ad_conexion_vpnResult = conexion_vpn.Getad_conexion_vpnFromDatabase(userConnectionString, agencia);
                if (ad_conexion_vpnResult == null)
                {
                    return Problem("No se pudo obtener la cadena de conexión");
                }
                return Ok(ad_conexion_vpnResult);
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // GET: api/ad_conexion_vpn/5
        [HttpGet("{userConn}/{codalmacen}/{coditem}")]
        public async Task<ActionResult<instoactual>> Getsaldos_local(string userConn, int codalmacen, string coditem)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.instoactual == null)
                    {
                        return Problem("Entidad instoactual es null.");
                    }
                    var instoactual = await _context.instoactual
                    .Where(a => a.codalmacen == codalmacen && a.coditem == coditem)
                    .Select(a => new instoactual
                    {
                        codalmacen = a.codalmacen,
                        coditem = a.coditem,
                        cantidad = a.cantidad,
                        udm = a.udm,
                        porllegar = a.porllegar,
                        fecha= a.fecha,
                        pedido= a.pedido,
                        proformas= a.proformas

                    })
                    .FirstOrDefaultAsync();
                    if (instoactual == null)
                    {
                        return NotFound("No existe un registro con esos datos");
                    }
                    return Ok(instoactual);
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

    }
}
