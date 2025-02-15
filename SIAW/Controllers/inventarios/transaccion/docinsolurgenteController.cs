using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_funciones;

namespace SIAW.Controllers.inventarios.transaccion
{
    [Route("api/[controller]")]
    [ApiController]
    public class docinsolurgenteController : ControllerBase
    {
        private readonly string _controllerName = "docinsolurgenteController";

        private readonly Configuracion configuracion = new Configuracion();
        private readonly Nombres nombres = new Nombres();
        private readonly Seguridad seguridad = new Seguridad();
        private readonly Items items = new Items();
        private readonly Cliente cliente = new Cliente();
        private readonly Almacen almacen = new Almacen();


        private readonly UserConnectionManager _userConnectionManager;
        public docinsolurgenteController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        [HttpGet]
        [Route("getParamsIniSolUrg/{userConn}/{usuario}/{codempresa}")]
        public async Task<ActionResult<object>> getParamsIniSolUrg(string userConn, string usuario, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    int codalmacen = await configuracion.usr_codalmacen(_context, usuario);
                    string codalmacendescripcion = await nombres.nombrealmacen(_context, codalmacen);
                    string codmoneda_total = await Empresa.monedabase(_context, codempresa);
                    
                    return Ok(new
                    {
                        codalmacen,
                        codalmacendescripcion,
                        codmoneda_total
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener parametros iniciales Sol. Urgente: " + ex.Message);
                throw;
            }
        }

        [HttpPost]
        [Route("grabarDocumento/{userConn}/{codempresa}/{traspaso}")]
        public async Task<ActionResult<object>> grabarDocumento(string userConn, string codempresa, bool traspaso, requestGabrarPedido dataGrabar)
        {
            // hacer un trim a todas las casillas
            return Ok();
        }







        private async Task<(bool valido, string msg)> validardatos(DBContext _context, insolurgente cabecera, List<insolurgente1> detalle)
        {
            if (cabecera.numeroidproforma == null || cabecera.numeroidproforma <= 0)
            {
                return (false, "No puede dejar el tipo de pedido en blanco.");
            }
            if (cabecera.codalmacen == null || cabecera.codalmacen <= 0)
            {
                return (false, "No puede dejar la casilla de Almacen en blanco.");
            }
            if (cabecera.codalmdestino == null || cabecera.codalmdestino <= 0)
            {
                return (false, "No puede dejar la casilla de Almacen Destino en blanco.");
            }
            if (cabecera.codalmacen == cabecera.codalmdestino)
            {
                return (false, "No puede hacer un pedido al mismo almacen en que se encuentra.");
            }
            if (cabecera.codvendedor == null || cabecera.codvendedor <= 0)
            {
                return (false, "No puede dejar la casilla de Vendedor en blanco.");
            }
            if (string.IsNullOrWhiteSpace(cabecera.codcliente))
            {
                return (false, "No puede dejar la casilla de Cliente en blanco.");
            }
            if (cabecera.codtarifa == null || cabecera.codtarifa <= 0)
            {
                return (false, "No puede dejar la casilla de Tarifa en blanco.");
            }
            if (cabecera.totalventa == null || cabecera.totalventa <= 0)
            {
                return (false, "No puede dejar la casilla de total de venta en blanco.");
            }
            if (string.IsNullOrWhiteSpace(cabecera.codmoneda))
            {
                return (false, "No puede dejar la casilla de Moneda en blanco.");
            }
            if (string.IsNullOrWhiteSpace(cabecera.transporte))
            {
                return (false, "No puede dejar la casilla de Transporte en blanco.");
            }
            if (string.IsNullOrWhiteSpace(cabecera.tpollegada))
            {
                return (false, "No puede dejar la casilla de Tiempo de Llegada en blanco.");
            }
            if (string.IsNullOrWhiteSpace(cabecera.obs))
            {
                return (false, "No puede dejar la casilla de Observacion en blanco.");
            }
            if (string.IsNullOrWhiteSpace(cabecera.nomcliente))
            {
                return (false, "No puede dejar la casilla de nombre del cliente en blanco.");
            }
            if (cabecera.nomcliente.Trim().Replace(" ", "")  == "SINNOMBRE")
            {
                return (false, "Debe indicar un nombre del cliente para el cual se esta realizando la solicitud.");
            }
            if (string.IsNullOrWhiteSpace(cabecera.tipocliente))
            {
                return (false, "Debe indicar el tipo de cliente.");
            }

            if (await seguridad.periodo_fechaabierta_context(_context, cabecera.fecha.Date, 2) == false)
            {
                return (false, "No puede crear documentos para ese periodo de fechas.");
            }

            if (detalle.Count() <= 0)
            {
                return (false, "No tiene ningun item en su pedido.");
            }
            //////////////////////////// VER SI SE VALIDARA EN OTRO LADO, DEVUELVE OTRO MENSAJE MAS
            var validoMaxVta = await MaximoDeVenta(_context, cabecera.codalmacen, cabecera.codtarifa, cabecera.codcliente, cabecera.fecha, detalle);
            if (validoMaxVta.valido == false)
            {
                return (false, "El Documento esta vendiendo cantidades mayores a las permitidas para un solo cliente.");
            }
            var validaMontTrans = await ValidarMontoTransporte(_context, cabecera.codalmacen, cabecera.codalmdestino, cabecera.codtarifa, cabecera.codcliente, cabecera.fecha, cabecera.codmoneda, cabecera.total, (double)cabecera.peso_pedido, cabecera.transporte, detalle);
            cabecera.flete = validaMontTrans.flete;
            if (validaMontTrans.valido == false)
            {

            }
            return (true, "");
        }

        private async Task<(bool valido, string msg)> MaximoDeVenta(DBContext _context, int codalmacen, int codtarifa, string codcliente, DateTime fecha, List<insolurgente1> detalle)
        {
            double max = 0;
            int diascontrol = 0;
            bool resultado = true;

            foreach (var reg in detalle)
            {
                max = await items.MaximoDeVenta(_context, reg.coditem, codalmacen.ToString(), codtarifa);
                if (max > 0)  // si controla
                {
                    diascontrol = await items.MaximoDeVenta_PeriodoDeControl(_context, reg.coditem, codalmacen.ToString(), codtarifa);
                    if (diascontrol > 0)
                    {
                        double aux = await cliente.CantidadVendida(_context, reg.coditem, codcliente, fecha.Date.AddDays(-diascontrol), fecha.Date) + (double)(reg.cantidad);
                        if (aux <= max)
                        {
                            // nada
                        }
                        else
                        {
                            return (false, "Se sobrepasara el maximo de venta del item " + reg.coditem + " en " + diascontrol + " dias");
                        }
                    }
                }
            }
            return (true, "");
        }

        private async Task<(bool valido, string flete)> ValidarMontoTransporte(DBContext _context, int codalmacen, int codalmdestino, int codtarifa, string codcliente, DateTime fecha, string codmoneda_total, decimal total, double pesoTotal, string transporte, List<insolurgente1> detalle)
        {
            double MontoMin = 0;
            double PesoMin = 0;
            bool resultado = true;
            string flete = "";

            // verificar si son de la misma area
            if (await almacen.AreaAlmacen(_context,codalmacen) == await almacen.AreaAlmacen(_context, codalmdestino))
            {
                // ###########################
                // #   EN LA MISMA AREA
                // ###########################
                MontoMin = await almacen.MontoMinimoAlmacen(_context, codalmacen, codtarifa, true, codmoneda_total);
                PesoMin = await almacen.PesoMinimoAlmacen(_context, codalmacen, codtarifa, true);
                if (MontoMin > 0) // por monto
                {
                    if ((double)total < MontoMin)
                    {
                        // ####->poner flete paga el cliente
                        flete = "FLETE PAGADO POR EL CLIENTE";
                    }
                    else
                    {
                        // ####->poner flete paga la empresa
                        flete = "FLETE PAGADO POR LA EMPRESA";
                    }
                }
                else // por peso
                {
                    if (pesoTotal < PesoMin)
                    {
                        // ####->poner flete paga el cliente
                        flete = "FLETE PAGADO POR EL CLIENTE";
                    }
                    else
                    {
                        // ####->poner flete paga la empresa
                        flete = "FLETE PAGADO POR LA EMPRESA";
                    }
                }
            }
            else
            {
                // ###########################
                // #   A OTRAS AREAS
                // ###########################
                MontoMin = await almacen.MontoMinimoAlmacen(_context,codalmacen,codtarifa,false,codmoneda_total);
                PesoMin = await almacen.PesoMinimoAlmacen(_context, codalmacen, codtarifa, false);
                if (pesoTotal >= PesoMin || (double)total >= MontoMin)
                {
                    if (codtarifa == 1 || codtarifa == 4 || codtarifa == 7) // precio 1 (mayorista)
                    {
                        if (transporte == "TRANSPORTADORA")
                        {
                            flete = "FLETE PAGADO POR LA EMPRESA";
                        }
                        else
                        {
                            flete = "FLETE PAGADO POR EL CLIENTE";
                        }
                    }
                    else  // otros precios 2 y 3
                    {
                        if (await itemsEspeciales(_context,detalle))
                        {
                            if (transporte == "TRANSPORTADORA")
                            {
                                flete = "FLETE PAGADO POR LA EMPRESA";
                            }
                            else
                            {
                                flete = "FLETE PAGADO POR EL CLIENTE";
                            }
                        }
                        else
                        {
                            flete = "FLETE PAGADO POR LA EMPRESA";
                        }
                    }
                }
                else
                {
                    resultado = false; // por que en areas si hay minimo
                    flete = "SOLICITUD NO VALIDA  ";
                }
            }
            return (resultado, flete);
        }


        private async Task<bool> itemsEspeciales(DBContext _context, List<insolurgente1> detalle)
        {
            bool resultado = false;
            foreach (var reg in detalle)
            {
                if (await EstaRestringido(_context,reg.coditem))
                {
                    resultado = true;
                    break;
                }
            }
            return resultado;
        }
        private async Task<bool> EstaRestringido(DBContext _context, string coditem)
        {
            bool resultado = false;
            int codgrupo = await items.itemgrupo(_context, coditem);
            switch (codgrupo)
            {
                case 11:
                    resultado = true; 
                    break;
                case 13:
                    resultado = true;
                    break;
                case 25:
                    resultado = true;
                    break;
                case 4:
                    resultado = true;
                    break;
                default:
                    resultado = false;
                    break;
            }
            return resultado;
        }

    }
}
