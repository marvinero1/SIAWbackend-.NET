using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
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
        private readonly Documento documento = new Documento();
        private readonly Ventas ventas = new Ventas();
        private readonly Inventario inventario = new Inventario();
        private readonly Empresa empresa = new Empresa();
        private readonly Saldos saldos = new Saldos();
        private readonly Restricciones restricciones = new Restricciones();
        private readonly HardCoded hardCoded = new HardCoded();


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
        [Route("grabarDocumento/{userConn}/{valida_aceptaSolUrg}")]
        public async Task<ActionResult<object>> grabarDocumento(string userConn, string codempresa, bool valida_aceptaSolUrg, bool checkEspecial, bool solUrgcubreSaldoValido, bool solUrgVtaMaxValido, requestGabrarSolUrgente dataGrabar)
        {
            insolurgente cabecera = dataGrabar.cabecera;
            List<insolurgente1> detalle = dataGrabar.tablaDetalle;
            // hacer un trim a todas las casillas
            cabecera.id = cabecera.id.Trim();
            cabecera.obs = cabecera.obs.Trim();
            cabecera.codcliente = cabecera.codcliente.Trim();
            cabecera.codmoneda = cabecera.codmoneda.Trim();
            cabecera.tpollegada = cabecera.tpollegada.Trim();
            cabecera.transporte = cabecera.transporte.Trim();
            cabecera.idnm = cabecera.idnm.Trim();
            cabecera.idfc = cabecera.idfc.Trim();
            cabecera.horareg = cabecera.horareg.Trim();
            cabecera.usuarioreg = cabecera.usuarioreg.Trim();
            cabecera.fid = cabecera.fid.Trim();
            cabecera.flete = cabecera.flete.Trim();
            cabecera.id_comple = cabecera.id_comple.Trim();
            cabecera.nomcliente = cabecera.nomcliente.Trim();
            cabecera.tipocliente = cabecera.tipocliente.Trim();
            cabecera.idproforma = cabecera.idproforma.Trim();
            cabecera.niveles_descuento = cabecera.niveles_descuento.Trim();
            if (cabecera.codtarifa == null)
            {
                cabecera.codtarifa = 0;
            }
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // validacion de datos.
                    var valida1 = await validardatos(_context, cabecera, detalle);
                    if (valida1.valido == false)
                    {
                        return BadRequest(new {resp = valida1.msg});
                    }
                    var valida2 = await validardatos_2(_context, cabecera, valida_aceptaSolUrg, detalle);
                    cabecera.flete = valida2.flete;
                    if (valida2.valido == false)
                    {
                        if (valida2.pedirClave != null)
                        {
                            return StatusCode(203, new { 
                                valido = false,
                                resp = "Se requiere habilitacion con Clave.",
                                pedirClave = valida2.pedirClave,
                                alertas = valida2.msgs,
                                flete = cabecera.flete,
                            });
                        }
                        return BadRequest(new
                        {
                            valido = false,
                            resp = "Se encontraron las siguientes validaciones: ",
                            alertas = valida2.msgs,
                            flete = cabecera.flete,
                        });
                    }

                    var valDetalle1 = await validarDetalle(_context, codempresa, checkEspecial, solUrgcubreSaldoValido, cabecera, detalle);
                    if (valDetalle1.valido == false)
                    {
                        if (valDetalle1.clave != null)
                        {
                            return StatusCode(203, new
                            {
                                valido = false,
                                resp = valDetalle1.msg,
                                pedirClave = valDetalle1.clave,
                                alertas = valDetalle1.alertas,
                                flete = cabecera.flete,
                            });
                        }
                        return BadRequest(new
                        {
                            valido = false,
                            resp = valDetalle1.msg,
                            alertas = valDetalle1.alertas,
                            flete = cabecera.flete,
                        });
                    }

                    var valDetalle2 = await validarDetalle_2(_context,codempresa, solUrgVtaMaxValido, cabecera, detalle);
                    if (valDetalle2.valido == false)
                    {
                        if (valDetalle2.clave != null)
                        {
                            return StatusCode(203, new
                            {
                                valido = false,
                                resp = valDetalle2.msg,
                                pedirClave = valDetalle2.clave,
                                alertas = new List<string>(),
                                flete = cabecera.flete,
                                negativos = valDetalle2.negativos,
                                maxVenta = valDetalle2.dtnocumplenMaxVta,
                            });
                        }
                        return BadRequest(new
                        {
                            valido = false,
                            resp = valDetalle2.msg,
                            alertas = new List<string>(),
                            flete = cabecera.flete,
                            negativos = valDetalle2.negativos,
                            maxVenta = valDetalle2.dtnocumplenMaxVta,
                        });
                    }



                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al guardar Sol. Urgente: " + ex.Message);
                throw;
            }

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
                return (false, "No tiene ningun item en la solicitud urgente.");
            }
            
            return (true, "");
        }

        private async Task<(bool valido, List<string> msgs, string flete, object ? pedirClave)> validardatos_2(DBContext _context, insolurgente cabecera, bool valida_aceptaSolUrg, List<insolurgente1> detalle)
        {
            int codProforma = 0;
            if (cabecera.idproforma.Trim() != "")
            {
                codProforma = await ventas.codproforma(_context, cabecera.idproforma, cabecera.numeroidproforma ?? 0);
            }
            List<string> mensajes = new List<string>();
            //////////////////////////// VER SI SE VALIDARA EN OTRO LADO, DEVUELVE OTRO MENSAJE MAS
            var validoMaxVta = await MaximoDeVenta(_context, cabecera.codalmacen, cabecera.codtarifa, cabecera.codcliente, cabecera.fecha, detalle);
            if (validoMaxVta.valido == false)
            {
                mensajes.Add(validoMaxVta.msg);
                mensajes.Add("El Documento esta vendiendo cantidades mayores a las permitidas para un solo cliente.");
                return (false, mensajes, cabecera.flete, null);
            }
            var validaMontTrans = await ValidarMontoTransporte(_context, cabecera.codalmacen, cabecera.codalmdestino, cabecera.codtarifa, cabecera.codcliente, cabecera.fecha, cabecera.codmoneda, cabecera.total, (double)cabecera.peso_pedido, cabecera.transporte, detalle);
            cabecera.flete = validaMontTrans.flete;
            if (validaMontTrans.valido == false)
            {
                mensajes.Add("La solicitud no cumple el minimo requerido, se grabara como solicitud no valida, para validarla añada solicitudes complementarias.");
            }
            if (cabecera.idproforma.Trim() != "")
            {
                if (await documento.existe_factura(_context,cabecera.idproforma, cabecera.numeroidproforma ?? 0))
                {
                    // Desde 19/09/2024 Validar que en la factura, proforma y solicitud urgente tengan el mismo codigo de cliente
                    if (await ventas.Cliente_De_Proforma(_context, codProforma) != cabecera.codcliente)
                    {
                        mensajes.Add("El cliente de la Proforma enlazada no es el mismo codigo de cliente de la solicitud, lo cual no esta permitido, verifique esta situacion.");
                        return (false, mensajes, cabecera.flete, null);
                    }
                }
                else
                {
                    mensajes.Add("La proforma indicada no esta registrada.");
                    return (false, mensajes, cabecera.flete, null);
                }
            }

            // verificar a que precio esta la proforma y comprar con la solicitrud urgente si esta al mismo precio
            int codtarifa_proforma = await ventas.ProformaPrimeraTarifa(_context, codProforma);
            if (cabecera.codtarifa != codtarifa_proforma)
            {
                mensajes.Add("La proforma a la que hace referencia esta a precio: " + codtarifa_proforma + " y el precio al cual realiza la solicitu urgente es precio: " + cabecera.codtarifa + ", ambas deben ser al mismo precio!!!");
                return (false, mensajes, cabecera.flete, null);
            }

            if (cabecera.total < await inventario.MinimoAlmacen(_context, cabecera.codalmacen, cabecera.codmoneda, cabecera.fecha.Date))
            {
                mensajes.Add("La solicitud no cumple el minimo requerido de solicitud urgente.");
                cabecera.flete = "SOLICITUD NO VALIDA POR MINIMO DE SOLICITUD";
                if (valida_aceptaSolUrg == false)
                {
                    string codclientedescripcion = await cliente.Razonsocial(_context, cabecera.codcliente);
                    object clave = new
                    {
                        valido = false,
                        resp = "La agencia puede cubrir el saldo de su solicitud urgente, Para eso necesita una autorizacion especial.",
                        servicio = 23,
                        descServicio = "ACEPTAR SOL. URGENTE AUNQUE LA AGENCIA PUEDA CUBRIR",
                        datosDoc = cabecera.id + "-" + cabecera.numeroid + ": " + cabecera.codcliente + "-" + codclientedescripcion + " Total:" + cabecera.total,
                        datoA = cabecera.id,
                        datoB = cabecera.numeroid
                    };

                    return (false, mensajes, cabecera.flete, clave);
                }
                
            }



            return (true, mensajes, cabecera.flete, null);
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


        private async Task<(bool valido, string msg, object? clave, List<string> alertas)> validarDetalle(DBContext _context, string codempresa, bool checkEspecial, bool solUrgcubreSaldoValido, insolurgente cabecera, List<insolurgente1> tablaDetalle)
        {
            List<string> alertas = new List<string>();
            int indice = 1;
            foreach (var reg in tablaDetalle)
            {
                if (string.IsNullOrWhiteSpace(reg.coditem))
                {
                    return (false, "No eligio El Item en la Linea " + indice + " .", null, alertas);
                }
                if (reg.coditem.Trim().Length < 1)
                {
                    return (false, "No eligio El Item en la Linea " + indice + " .", null, alertas);
                }

                if (string.IsNullOrWhiteSpace(reg.udm))
                {
                    return (false, "No puso la Unidad de Medida en la Linea " + indice + " .", null, alertas);
                }
                if (reg.udm.Trim().Length < 1)
                {
                    return (false, "No puso la Unidad de Medida en la Linea " + indice + " .", null, alertas);
                }

                if (reg.cantidad == null)
                {
                    return (false, "No puso la cantidad en la Linea " + indice + " .", null, alertas);
                }
                if (reg.cantidad <= 0)
                {
                    return (false, "La cantidad en la Linea " + indice + " No puede ser menor o igual a 0.", null, alertas);
                }

                if (reg.cantidad_pedido == null)
                {
                    return (false, "No puso la cantidad de pedido total en la Linea " + indice + " .", null, alertas);
                }
                if (reg.cantidad_pedido <= 0)
                {
                    return (false, "La cantidad de pedido total en la Linea " + indice + " No puede ser menor o igual a 0.", null, alertas);
                }
                indice++;
            }

            // validaciones con consultas a base de datos

            int area = await almacen.AreaAlmacen(_context, cabecera.codalmacen);
            List<int> almacenes_de_area = await almacen.Almacenes_del_Area(_context, area);
            foreach (var reg in tablaDetalle)
            {
                if (await empresa.ControlarStockSeguridad_context(_context,codempresa))
                {
                    if (await ventas.Reservar_Para_Tiendas_En_Sol_Urgentes(_context,cabecera.codtarifa))
                    {
                        reg.saldodest = await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, cabecera.codalmdestino, true, cabecera.usuarioreg);
                    }
                    else
                    {
                        reg.saldodest = await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, cabecera.codalmdestino, false, cabecera.usuarioreg);
                    }
                }
                else
                {
                    reg.saldodest = await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, cabecera.codalmdestino, false, cabecera.usuarioreg);
                }

                // adicionar saldo de area
                double total_area = 0;
                string alerta = "";
                if (await ventas.Reservar_Para_Tiendas_En_Sol_Urgentes(_context, cabecera.codtarifa))
                {
                    foreach (var reg2 in almacenes_de_area)
                    {
                        double saldo_ag_area = 0;
                        if (reg2 == cabecera.codalmacen)
                        {
                            saldo_ag_area = 0;
                        }
                        else
                        {
                            try
                            {
                                saldo_ag_area = total_area + (double)await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, reg2, true, cabecera.usuarioreg);
                            }
                            catch (Exception)
                            {
                                saldo_ag_area = 0;
                                alerta = "NO hay saldo para ag: " + reg2.ToString();
                            }
                        }
                        total_area += saldo_ag_area;
                    }
                    reg.saldoarea = (decimal?)total_area;
                }
                else
                {
                    foreach (var reg2 in almacenes_de_area)
                    {
                        double saldo_ag_area = 0;
                        if (reg2 == cabecera.codalmacen)
                        {
                            saldo_ag_area = 0;
                        }
                        else
                        {
                            try
                            {
                                total_area = total_area + (double)await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, reg2, false, cabecera.usuarioreg);
                            }
                            catch (Exception)
                            {
                                saldo_ag_area = 0;
                                alerta = "NO hay saldo para ag: " + reg2.ToString();
                            }
                        }
                        total_area += saldo_ag_area;
                    }
                    reg.saldoarea = (decimal?)total_area;
                }
            }

            infracciones.Clear();
            int i = 1;
            
            foreach (var reg in tablaDetalle)
            {
                var pedidoValido = await validarpedido(_context, (decimal)reg.saldoag, (decimal)reg.cantidad, i, reg.coditem, (decimal)reg.saldodest, (decimal)reg.saldoarea, checkEspecial, solUrgcubreSaldoValido, cabecera);
                if (pedidoValido.msg != "")
                {
                    alertas.Add(pedidoValido.msg);
                }
                if (pedidoValido.valido == false)
                {
                    return (false, pedidoValido.msg, pedidoValido.clave, alertas);
                }
                i++;
            }

            return (true, "",null,alertas);
        }

        private async Task<(bool valido, string msg, object? clave, List<Dtnegativos>? negativos, List<Dtnocumplen>? dtnocumplenMaxVta)> validarDetalle_2(DBContext _context, string codempresa, bool solUrgVtaMaxValido, insolurgente cabecera, List<insolurgente1> tablaDetalle)
        {
            // validar si el pedido al almacen destino genera negativos
            var validaNegs = await Validar_Saldos_Negativos_Doc(_context, false, cabecera.codalmdestino, codempresa, cabecera.usuarioreg, tablaDetalle);
            if (validaNegs.valido == false)
            {
                return (false, validaNegs.msg, null, validaNegs.negativos, null);
            }

            var validaMaxVta = await Validar_Max_Vta(_context, cabecera.id, cabecera.numeroid, codempresa, cabecera.usuarioreg, cabecera.codtarifa, cabecera.codcliente, cabecera.codalmdestino, cabecera.codalmacen, cabecera.fecha, true, solUrgVtaMaxValido, tablaDetalle);
            if (validaMaxVta.valido == false)
            {
                return (false, validaMaxVta.msg, validaMaxVta.clave, null, validaMaxVta.dtnocumplenMaxVta);
            }
            return (true, "", null, null, null);
        }

        List<string> infracciones = new List<string>();

        private async Task<(bool valido, string msg, object? clave)> validarpedido(DBContext _context, decimal saldoag, decimal cantidad, int linea, string item, decimal saldodest, decimal saldoarea, bool checkEspecial, bool solUrgcubreSaldoValido, insolurgente cabecera)
        {
            string alerta = "";
            if (checkEspecial)
            {
                if (cantidad < (saldoag * (decimal)0.5))
                {
                    // la agencia puede cubrir el pedido
                    infracciones.Add(linea.ToString() + " " + item + " Su agencia puede cubrir el pedido.");
                    alerta = linea.ToString() + " " + item + " Su pedido es menor al 50% de su saldo actual, Su agencia puede cubrir este pedido especial.";
                    if (solUrgcubreSaldoValido == false)
                    {
                        string codclientedescripcion = await cliente.Razonsocial(_context, cabecera.codcliente);
                        object clave = new
                        {
                            valido = false,
                            resp = "La agencia puede cubrir el saldo de su solicitud urgente, Para eso necesita una autorizacion especial.",
                            servicio = 23,
                            descServicio = "ACEPTAR SOL. URGENTE",
                            datosDoc = cabecera.id + "-" + cabecera.numeroid + " " + cabecera.codcliente + "-" + codclientedescripcion,
                            datoA = cabecera.id,
                            datoB = cabecera.numeroid
                        };

                        return (false, alerta, clave);
                    }
                }
            }
            else
            {
                if (cantidad < (saldoag * (decimal)0.9))
                {
                    // la agencia puede cubrir el pedido
                    infracciones.Add(linea.ToString() + " " + item + " Su agencia puede cubrir el pedido.");
                    alerta = linea.ToString() + " " + item + " Su pedido es menor al 90% de su saldo actual, Su agencia puede cubrir este pedido especial.";
                    if (solUrgcubreSaldoValido == false)
                    {
                        string codclientedescripcion = await cliente.Razonsocial(_context, cabecera.codcliente);
                        object clave = new
                        {
                            valido = false,
                            resp = "La agencia puede cubrir el saldo de su solicitud urgente, Para eso necesita una autorizacion especial.",
                            servicio = 23,
                            descServicio = "ACEPTAR SOL. URGENTE",
                            datosDoc = cabecera.id + "-" + cabecera.numeroid + " " + cabecera.codcliente + "-" + codclientedescripcion,
                            datoA = cabecera.id,
                            datoB = cabecera.numeroid
                        };

                        return (false, alerta, clave);
                    }
                }
            }


            if (saldoag < 0)
            {
                infracciones.Add(linea.ToString() + " " + item + " el saldo es negativo, debe ser corregido antes de realizar el pedido.");
            }
            else
            {
                if (saldoag >= cantidad)
                {
                    // la agencia puede cubrir el pedido
                    infracciones.Add(linea.ToString() + " " + item + " Su agencia puede cubrir el pedido.");
                }
                else
                {
                    if (cabecera.codtarifa == 1 || cabecera.codtarifa == 4 || cabecera.codtarifa == 7)
                    {
                        // precio 1 o mayorista
                        if (cantidad > saldodest)
                        {
                            // en la agencia destino no alcanza
                            infracciones.Add(linea.ToString() + " " + item + "La agencia destino no tiene suficiente mercaderia para abastecer el pedido .");
                        }
                        else
                        {
                            if (saldodest > await saldos.stockminimo_item(_context,cabecera.codalmdestino,item))
                            {
                                // nada
                            }
                            else
                            {
                                // en la gencia destino el stock minimo esta al limite 
                                infracciones.Add(linea.ToString() + " " + item + "En la agencia destino el saldo esta revervado para el Stock Minimo Propio.");
                            }
                        }
                    }
                    else
                    {
                        // precio 2 y 3
                        if (cantidad < saldodest)
                        {
                            // nada
                        }
                        else
                        {
                            cantidad = cantidad - saldoag;
                            if (cantidad < saldodest)
                            {
                                // nada
                            }
                            else
                            {
                                cantidad = saldodest;
                                if (cantidad > 0)
                                {
                                    // nada
                                }
                                else
                                {
                                    // esta pidiendo cero
                                    infracciones.Add(linea.ToString() + " " + item + " no se podra abastecer ese item en la agencia destino.");
                                }
                            }
                        }
                    }
                }
            }

            // verificacion especial
            if (await almacen.AreaAlmacen(_context, cabecera.codalmacen) == await almacen.AreaAlmacen(_context,cabecera.codalmdestino))
            {
                // misma area
            }
            else
            {
                // entre areas
                if ((saldoarea + saldoag) >= cantidad)
                {
                    // su area puede abastecer
                    infracciones.Add(linea.ToString() + " " + item + " En su area se puede abastecer el pedido , no es necesario pedirlo a otra area para el item de la linea.");
                }
                else
                {
                    if (cantidad > saldodest)
                    {
                        // en la agencia destino no alcanza
                        infracciones.Add(linea.ToString() + " " + item + " En la agencia destino no alcanza la mercaderia para poder cubrir su pedido en la linea.");
                    }
                    else
                    {
                        if (saldodest < (await saldos.stockminimo_item(_context,cabecera.codalmdestino,item) * (decimal)1.2))
                        {
                            // en la agencia destino elsaldo esta muy cerca al stock minimo
                            infracciones.Add(linea.ToString() + " " + item + " En la agencia destino el saldo esta muy cerca al Stock Minimo.");
                        }
                    }
                }
            }
            return (true, alerta, null);
        }


        private async Task<(string msg, bool valido, List<Dtnegativos>? negativos)> Validar_Saldos_Negativos_Doc(DBContext _context, bool alertar_si_no_hay_negativos, int codalmdestino, string cod_empresa, string usuario, List<insolurgente1> tabladetalle)
        {
            List<string> msgs = new List<string>();
            List<string> negs = new List<string>();

            string alerta = "";
            bool resultado = true;
            if (tabladetalle.Count() > 0)
            {
                List<itemDataMatriz> detalleConvertido = tabladetalle.Select(i => new itemDataMatriz
                {
                    coditem = i.coditem,
                    cantidad = (double)i.cantidad,
                    udm = i.udm,
                    cantidad_pedida = (double)i.cantidad

                }).ToList();
                List<Dtnegativos> dtnegativos = await saldos.ValidarNegativosDocVenta(_context, detalleConvertido, codalmdestino, "", 0, msgs, negs, cod_empresa, usuario);

                foreach (var reg in dtnegativos)
                {
                    if (reg.obs == "Genera Negativo")
                    {
                        if (reg.cantidad_conjunto > 0)
                        {
                            negs.Add(reg.coditem_cjto);
                        }
                        if (reg.cantidad_suelta > 0)
                        {
                            negs.Add(reg.coditem_suelto);
                        }
                    }
                }
                if (negs.Count() == 0)
                {
                    if (alertar_si_no_hay_negativos)
                    {
                        alerta = "Ningun item del documento genera negativos.";
                        resultado = true;
                    }
                    else
                    {
                        resultado = false;
                        alerta = "Los items detallados en la pestaña: 'Saldos Negativos' generaran saldos negativos, verifique esta situacion!!!";
                        return (alerta, resultado, dtnegativos);
                    }
                }
            }
            return (alerta, resultado, null);
        }
        private async Task<(string msg, bool valido, List<Dtnocumplen>? dtnocumplenMaxVta, object? clave)> Validar_Max_Vta(DBContext _context, string id, int numeroid, string codempresa, string usuario, int codtarifa, string codcliente, int codalmdestino, int codalmacen, DateTime fecha, bool pedir_clave, bool solUrgcubreSaldoValido, List<insolurgente1> tabladetalle)
        {
            bool resultado = true;
            string alerta = "";

            List<itemDataMatrizMaxVta> dt_detalle_item = new List<itemDataMatrizMaxVta> ();

            int diascontrol = await configuracion.Dias_Proforma_Vta_Item_Cliente(_context, codempresa);
            string valida_nr_pf = await configuracion.Valida_Maxvta_NR_PF(_context, codempresa);


            foreach (var reg in tabladetalle)
            {
                // obtiene los items y la cantidad en proformas en los ultimos X dias mas lo que se quiere vender a ahora
                double cantidad_ttl_vendida_pf = 0;
                double cantidad_ttl_vendida_pf_actual = (double)reg.cantidad;
                double cantidad_ttl_vendida_pf_total = 0;

                if (diascontrol > 0 && reg.cantidad > 0)
                {
                    if (valida_nr_pf == "PF")
                    {
                        cantidad_ttl_vendida_pf = (double)await cliente.CantidadVendida_PF(_context, reg.coditem, codcliente, fecha.Date.AddDays(-diascontrol), fecha.Date);
                    }
                    else if(valida_nr_pf == "NR")
                    {
                        cantidad_ttl_vendida_pf = (double)await cliente.CantidadVendida_NR(_context, reg.coditem, codcliente, fecha.Date.AddDays(-diascontrol), fecha.Date);
                    }
                    else
                    {
                        cantidad_ttl_vendida_pf = (double)await cliente.CantidadVendida_PF(_context, reg.coditem, codcliente, fecha.Date.AddDays(-diascontrol), fecha.Date);
                    }
                    
                    cantidad_ttl_vendida_pf_total = cantidad_ttl_vendida_pf + cantidad_ttl_vendida_pf_actual;

                    // aqui verificar si la cantidad sumada con las pf q hubieran y la pf actual sobrepasan el porcentaje o no
                    // VALIDAR PORCENTAJE MAXIMO DE VENTA
                    // poner la cantidad del item obtenido de la suma de la cantidad actual de la pf y de las pf q hubiera
                    dt_detalle_item.Add(new itemDataMatrizMaxVta
                    {
                        coditem = reg.coditem,
                        cantidad = (double)reg.cantidad,
                        udm = reg.udm,
                        total = (double)reg.total,
                        codtarifa = codtarifa,
                        cantidad_pf_anterior = cantidad_ttl_vendida_pf,
                        cantidad_pf_total = cantidad_ttl_vendida_pf_total
                    });
                }
                else
                {
                    cantidad_ttl_vendida_pf_total = cantidad_ttl_vendida_pf + cantidad_ttl_vendida_pf_actual;
                    dt_detalle_item.Add(new itemDataMatrizMaxVta
                    {
                        coditem = reg.coditem,
                        cantidad = (double)reg.cantidad,
                        udm = reg.udm,
                        total = (double)reg.total,
                        codtarifa = codtarifa,
                        cantidad_pf_anterior = cantidad_ttl_vendida_pf,
                        cantidad_pf_total = cantidad_ttl_vendida_pf_total
                    });
                }
            }

            List<Dtnocumplen> dtnocumplen = await restricciones.ValidarMaximoPorcentajeDeMercaderiaVenta(_context, codcliente, false, dt_detalle_item, codalmdestino, await hardCoded.MaximoPorcentajeDeVentaPorMercaderia(_context, codalmacen), "", 0, codempresa, usuario);
            string cadena_items = "";
            dt_detalle_item.Clear();

            foreach (var reg in dtnocumplen)
            {
                if (reg.obs != "Cumple")
                {
                    resultado = false;
                    cadena_items = reg.codigo + "|";
                }
            }

            if (resultado == false)
            {
                alerta = "La solicitud urgente tiene cantidades que superan el porcentaje maximo de venta en el almacen destino, verifique esta situacion!!!";
                if (pedir_clave && solUrgcubreSaldoValido == false)
                {
                    string codclientedescripcion = await cliente.Razonsocial(_context, codcliente);
                    object clave = new
                    {
                        valido = false,
                        resp = alerta,
                        servicio = 20,
                        descServicio = "ACEPTAR PROFORMAS QUE PASAN EL MAXIMO % DE VENTA PERMITIDO",
                        datosDoc = id + "-" + numeroid + "|" + codcliente + "-" + codclientedescripcion + "|" + cadena_items,
                        datoA = codcliente,
                        datoB = cadena_items
                    };
                    return (alerta, false, dtnocumplen, clave);
                }
            }


            return (alerta, true, null, null);
        }

    }


    public class requestGabrarSolUrgente
    {
        public insolurgente cabecera { get; set; }
        public List<insolurgente1> tablaDetalle { get; set; }
    }


    public class detalle_ValMaxVta
    {
        public int? codsolurgente { get; set; }
        public string coditem { get; set; }
        public decimal? cantidad { get; set; }
        public decimal? saldoag { get; set; }
        public decimal? stockmax { get; set; }
        public string udm { get; set; }
        public decimal? precio { get; set; }
        public decimal? total { get; set; }
        public decimal? saldodest { get; set; }
        public decimal? pedtotal { get; set; }
        public decimal? saldoarea { get; set; }
        public decimal? cantidad_pedido { get; set; }
        public int codtarifa { get; set; }
        public double cantidad_pf_anterior { get; set; }
        public double cantidad_pf_total {  get; set; }
    }

}
