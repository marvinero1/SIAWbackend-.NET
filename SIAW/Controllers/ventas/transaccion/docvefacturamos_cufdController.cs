using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using siaw_funciones;

namespace SIAW.Controllers.ventas.transaccion
{
    [Route("api/venta/transac/[controller]")]
    [ApiController]
    public class docvefacturamos_cufdController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;

        private readonly siaw_funciones.Ventas ventas = new siaw_funciones.Ventas();
        private readonly siaw_funciones.Configuracion configuracion = new siaw_funciones.Configuracion();
        private readonly siaw_funciones.Documento documento = new siaw_funciones.Documento();
        private readonly siaw_funciones.TipoCambio tipocambio = new siaw_funciones.TipoCambio();
        private readonly siaw_funciones.Funciones funciones = new siaw_funciones.Funciones();

        private readonly Empresa empresa = new Empresa();
        private readonly Nombres nombres = new Nombres();

        private readonly string _controllerName = "docvefacturamos_cufdController";


        public docvefacturamos_cufdController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        [HttpGet]
        [Route("getCodVendedorbyPass/{userConn}/{txtclave}")]
        public async Task<ActionResult<IEnumerable<object>>> getCodVendedorbyPass(string userConn, string txtclave)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    int codvendedor = await ventas.VendedorClave(_context,txtclave);
                    if (codvendedor == -1)
                    {
                        return BadRequest(new
                        {
                            resp = "No se encontró un codigo de vendedor asociada a la clave ingresada."
                        });
                    }
                    return Ok(new
                    {
                        codvendedor
                    });
                }

            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }


        [HttpGet]
        [Route("getParametrosIniciales/{userConn}/{usuario}")]
        public async Task<ActionResult<IEnumerable<object>>> getParametrosIniciales(string userConn, string usuario, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    string id = await configuracion.usr_idfactura(_context, usuario);
                    int numeroid = 0;
                    if (id != "")
                    {
                        numeroid = (await documento.ventasnumeroid(_context, id)) + 1;
                    }
                    DateTime fecha = await funciones.FechaDelServidor(_context);
                    // codvendedor.Text =  sia_compartidos.temporales.instancia.ucodvendedor
                    string codmoneda = await tipocambio.monedafact(_context, codempresa);
                    decimal tdc = await tipocambio._tipocambio(_context, await Empresa.monedabase(_context, codempresa), codmoneda, fecha);
                    int codalmacen = await configuracion.usr_codalmacen(_context, usuario);
                    int codtarifadefect = await configuracion.usr_codtarifa(_context,usuario);
                    int coddescuentodefect = await configuracion.usr_coddescuento(_context, usuario);

                    int codtipopago = await configuracion.parametros_ctasporcobrar_tipopago(_context,codempresa);
                    string codtipopagodescripcion = await nombres.nombretipopago(_context, codtipopago);

                    string idcuenta = await configuracion.usr_idcuenta(_context, usuario);
                    string idcuentadescripcion = await nombres.nombrecuenta_fondos(_context, idcuenta);

                    string codcliente = await configuracion.usr_codcliente(_context, usuario);
                    string codclientedescripcion = await nombres.nombrecliente(_context,codcliente);

                }
                return Ok();

            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }


    }
}
