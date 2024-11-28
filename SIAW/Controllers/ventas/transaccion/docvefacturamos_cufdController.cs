using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Polly;
using Servp_ObtencionCodigos;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;
using siaw_ws_siat;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing.Printing;
using System.Drawing;
using System.Security.Cryptography;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;
using static siaw_funciones.Log;
using static siaw_funciones.Validar_Vta;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using Polly.Caching;
using System.Linq;
using System.Globalization;

namespace SIAW.Controllers.ventas.transaccion
{
    [Route("api/venta/transac/[controller]")]
    [ApiController]
    public class docvefacturamos_cufdController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly siaw_funciones.Configuracion configuracion = new siaw_funciones.Configuracion();
        private readonly siaw_funciones.Ventas ventas = new siaw_funciones.Ventas();
        private readonly siaw_funciones.Cliente cliente = new siaw_funciones.Cliente();
        private readonly siaw_funciones.Funciones funciones = new siaw_funciones.Funciones();
        private readonly siaw_funciones.Cobranzas cobranzas = new siaw_funciones.Cobranzas();
        private readonly siaw_funciones.TipoCambio tipocambio = new siaw_funciones.TipoCambio();
        private readonly siaw_funciones.datosProforma datos_proforma = new siaw_funciones.datosProforma();
        private readonly siaw_funciones.Saldos saldos = new siaw_funciones.Saldos();
        private readonly siaw_funciones.Items items = new siaw_funciones.Items();
        private readonly siaw_funciones.Validar_Vta validar_Vta = new siaw_funciones.Validar_Vta();

        private readonly Seguridad seguridad = new Seguridad();
        private readonly SIAT siat = new SIAT();
        private readonly Empresa empresa = new Empresa();
        private readonly Documento documento = new Documento();
        private readonly Log log = new Log();
        private readonly Nombres nombres = new Nombres();
        private readonly Contabilidad contabilidad = new Contabilidad();
        private readonly Almacen almacen = new Almacen();

        private readonly ServFacturas serv_Facturas = new ServFacturas();
        private readonly Funciones_SIAT funciones_SIAT = new Funciones_SIAT();
        private readonly Adsiat_Parametros_facturacion adsiat_Parametros_Facturacion = new Adsiat_Parametros_facturacion();
        private readonly GZip gzip = new GZip();

        private readonly impresoraTermica_2 impresoraTermica = new impresoraTermica_2();

        private readonly string _controllerName = "docvefacturamos_cufdController";


        public docvefacturamos_cufdController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        /// //////////////////////////////////////////////////////////////////////////// SAMUEL FUNCIONES
        [HttpGet]
        [Route("getInfoCertificadoDigitalTienda/{userConn}/{codalmacen}/{codempresa}")]
        public async Task<ActionResult<IEnumerable<object>>> getInfoCertificadoDigitalTienda(string userConn, int codalmacen, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var datos_certificado_digital = await Definir_Certificado_A_Utilizar(_context, codalmacen, codempresa);
                    if (datos_certificado_digital.result == false)
                    {
                        datos_certificado_digital.eventos.Add("No se pudo obtener la informacion del certificado digital.");

                        return BadRequest(new
                        {
                            resp = "Ocurrio algun error al definir el certificado digital para la firma del XML!!!",
                            habilitado = datos_certificado_digital.result,
                            datos_certificado_digital.msgAlertas,
                            datos_certificado_digital.eventos
                        });
                    }
                    return Ok(new
                    {
                        resp = "Datos Obtenidos Correctamente",
                        habilitado = datos_certificado_digital.result,
                        datos_certificado_digital.msgAlertas,
                        datos_certificado_digital.eventos
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
        [Route("getDosificacionCajaTienda/{userConn}/{fecha}/{codalmacen}")]
        public async Task<ActionResult<IEnumerable<object>>> getDosificacionCajaTienda(string userConn, DateTime fecha, int codalmacen)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    Datos_Dosificacion_Activa datos_dosificacion_Activa = new Datos_Dosificacion_Activa();
                    datos_dosificacion_Activa = await siat.Obtener_Cufd_Dosificacion_Activa(_context, fecha, codalmacen);
                    if (datos_dosificacion_Activa.resultado == true)
                    {
                        if (await ventas.cufd_tipofactura(_context, datos_dosificacion_Activa.cufd) == 1)
                        {
                            return Ok(new
                            {
                                resp = "Atencion!!! la dosificacion CUFD elegida es manual, esta seguro de continuar??? ",
                                nrocaja = "",
                                cufd = "",
                                codigo_control = "",
                                dtpfecha_limite = new DateTime(1900, 1, 1).Date,
                                nrolugar = "",
                                tipo = "",
                                codtipo_comprobante = "",
                                codtipo_comprobantedescripcion = ""
                            });
                        }
                        // si hay datos de dosificacion
                        string alerta = await ventas.alertadosificacion(_context, datos_dosificacion_Activa.nrocaja);
                        return Ok(new
                        {
                            resp = alerta,
                            nrocaja = datos_dosificacion_Activa.nrocaja,
                            cufd = datos_dosificacion_Activa.cufd,
                            codigo_control = datos_dosificacion_Activa.codcontrol,
                            dtpfecha_limite = datos_dosificacion_Activa.fechainicio.Date,
                            nrolugar = await ventas.caja_numeroLugar(_context, datos_dosificacion_Activa.nrocaja),
                            tipo = datos_dosificacion_Activa.tipo,
                            codtipo_comprobante = "",
                            codtipo_comprobantedescripcion = ""
                        });
                    }
                    else
                    {
                        return BadRequest(new { resp = "No se encontro dosificacion de CUFD activa para el almacen: " });
                    }
                }

            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }
        [HttpGet]
        [Route("leerparametrosTienda/{userConn}/{codempresa}/{usuario}")]
        public async Task<ActionResult<IEnumerable<object>>> leerparametrosTienda(string userConn, string codempresa, string usuario)
        {
            try
            {

                // Validar que los parámetros no sean nulos ni vacíos
                if (string.IsNullOrWhiteSpace(codempresa))
                {
                    return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Empresa'." });
                }

                if (string.IsNullOrWhiteSpace(usuario))
                {
                    return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Usuario'." });
                }

                // Validar si userConn es nulo o vacío, ya que es parte de la ruta
                if (string.IsNullOrWhiteSpace(userConn))
                {
                    return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'UserConn'." });
                }

                string id = "";
                int numeroid = 0;
                string codmoneda = "";
                double tdc = 0;
                int codalmacen = 0;
                int codtarifadefect = 0;
                int coddescuentodefect = 0;
                int codtipopago = 0;
                string codtipopagodescripcion = "";
                string idcuenta = "";
                string idcuentadescripcion = "";
                string codcliente = "";
                string codclientedescripcion = "";

                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    id = await configuracion.usr_idfactura(_context, usuario);
                    if (id == "")
                    {
                        numeroid = 0;
                        return BadRequest(new { resp = "No se encontro un id de factura configurado para el usuario." });
                    }
                    else
                    {
                        numeroid = await documento.ventasnumeroid(_context, id) + 1;
                    }
                    codmoneda = await tipocambio.monedafact(_context, codempresa);
                    tdc = (double)await tipocambio._tipocambio(_context, await Empresa.monedabase(_context, codempresa), codmoneda, await funciones.FechaDelServidor(_context));
                    codalmacen = await configuracion.usr_codalmacen(_context, usuario);
                    //codtarifadefect = await configuracion.usr_codtarifa(_context, usuario) == 0 ? 0 : configuracion.usr_codtarifa(_context, usuario);
                    //coddescuentodefect = await configuracion.usr_coddescuento(_context, usuario) == 0 ? 0 : configuracion.usr_coddescuento(_context, usuario);
                    codtarifadefect = await configuracion.usr_codtarifa(_context, usuario);
                    coddescuentodefect = await configuracion.usr_coddescuento(_context, usuario);
                    codtipopago = await configuracion.parametros_ctasporcobrar_tipopago(_context, usuario);
                    codtipopagodescripcion = await nombres.nombretipopago(_context, codtipopago);
                    idcuenta = await configuracion.usr_idcuenta(_context, usuario);
                    idcuentadescripcion = await nombres.nombrecuenta_fondos(_context, idcuenta);
                    codcliente = await configuracion.usr_codcliente(_context, usuario);
                    codclientedescripcion = await nombres.nombrecliente(_context, codcliente);

                    return Ok(new
                    {
                        codmoneda = codmoneda,
                        tdc = tdc,
                        codalmacen = codalmacen,
                        codtarifadefect = codtarifadefect,
                        coddescuentodefect = coddescuentodefect,
                        codtipopago = codtipopago,
                        codtipopagodescripcion = await nombres.nombretipopago(_context, codtipopago),
                        idcuenta = codtipopagodescripcion,
                        idcuentadescripcion = idcuentadescripcion,
                        codcliente = codcliente,
                        codclientedescripcion = codclientedescripcion
                    });
                }

            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor al obtener los parametros para facturar: {ex.Message}");
                throw;
            }
        }

        private async Task<List<itemDataMatriz>> ConvertirDetalleFactura(List<dataDetalleFactura> tabladetalle)
        {
            return tabladetalle.Select(detalle => new itemDataMatriz
            {
                coditem = detalle.coditem,
                descripcion = detalle.descripcion,
                medida = detalle.medida,
                udm = detalle.udm,
                porceniva = (double)detalle.porceniva,
                cantidad = (double)detalle.cantidad,
                codtarifa = detalle.codtarifa,
                coddescuento = detalle.coddescuento,
                preciolista = (double)detalle.preciolista,
                niveldesc = detalle.niveldesc,
                preciodesc = (double)detalle.preciodesc,
                precioneto = (double)detalle.precioneto,
                total = (double)detalle.total,
                cumple = detalle.cumple,
                // Propiedades adicionales que puedes mapear o inicializar
                cumpleMin = true,
                cumpleEmp = true,
                nroitem = 0,
                porcentaje = 0,
                monto_descto = 0,
                subtotal_descto_extra = 0
            }).ToList();
        }

        private async Task<object?> Llenar_Datos_Del_Documento(DBContext _context, string codempresa, vefactura DVTA, List<itemDataMatriz> tabladetalle, DatosDocVta objDocVta)
        {
            // Llena los datos de la proforma
            objDocVta.coddocumento = 0;
            objDocVta.estado_doc_vta = "NUEVO";
            objDocVta.id = DVTA.id;
            objDocVta.numeroid = DVTA.numeroid.ToString();
            objDocVta.fechadoc = Convert.ToDateTime(DVTA.fecha);
            objDocVta.codcliente = DVTA.codcliente;
            objDocVta.nombcliente = DVTA.nomcliente;
            objDocVta.nitfactura = DVTA.nit;
            objDocVta.codcliente_real = DVTA.codcliente_real;
            objDocVta.nomcliente_real = DVTA.nomcliente;
            objDocVta.codmoneda = DVTA.codmoneda;
            objDocVta.codtarifadefecto = await validar_Vta.Precio_Unico_Del_Documento(_context, tabladetalle, codempresa);
            objDocVta.subtotaldoc = (double)DVTA.subtotal;
            objDocVta.totdesctos_extras = (double)DVTA.descuentos;
            objDocVta.totrecargos = (double)DVTA.recargos;
            objDocVta.totaldoc = (double)DVTA.total;
            //if (DVTA.tipo_vta == ""1)
            //{
            //    objDocVta.tipo_vta = "1";
            //}
            //else
            //{
            //    objDocVta.tipo_vta = "0";
            //}
            // objDocVta.tipo_vta = DVTA.tipo_vta;
            if (DVTA.tipopago == 0)
            {
                objDocVta.tipo_vta = "CONTADO";
            }
            else
            {
                objDocVta.tipo_vta = "CREDITO";
            }


            objDocVta.codalmacen = DVTA.codalmacen.ToString();
            objDocVta.codvendedor = DVTA.codvendedor.ToString();
            objDocVta.preciovta = objDocVta.codtarifadefecto.ToString();
            objDocVta.desctoespecial = "0";
            objDocVta.preparacion = "NORMAL";
            if (DVTA.contra_entrega == true)
            {
                objDocVta.contra_entrega = "SI";
            }
            else
            {
                objDocVta.contra_entrega = "NO";
            }
            objDocVta.estado_contra_entrega = DVTA.estado_contra_entrega;

            objDocVta.desclinea_segun_solicitud = false;
            objDocVta.idsol_nivel = "";
            objDocVta.nroidsol_nivel = "0";

            if (objDocVta.desclinea_segun_solicitud)
            {
                objDocVta.codcliente_real = await ventas.Cliente_Referencia_Solicitud_Descuentos(_context, objDocVta.idsol_nivel, Convert.ToInt32(objDocVta.nroidsol_nivel));
                objDocVta.nomcliente_real = await cliente.Razonsocial(_context, objDocVta.codcliente_real);
            }
            else
            {
                objDocVta.codcliente_real = DVTA.codcliente_real;
                objDocVta.nomcliente_real = await cliente.Razonsocial(_context, objDocVta.codcliente_real);
            }

            objDocVta.niveles_descuento = "ACTUAL";

            // Datos al pie de la proforma
            objDocVta.transporte = DVTA.transporte;
            objDocVta.nombre_transporte = DVTA.transporte;
            objDocVta.fletepor = DVTA.fletepor;
            objDocVta.tipoentrega = "ENTREGAR";
            objDocVta.direccion = DVTA.direccion;

            objDocVta.nroitems = tabladetalle.Count;

            // Datos del complemento mayosita - dimediado
            objDocVta.idpf_complemento = "";
            objDocVta.nroidpf_complemento = "0";
            objDocVta.tipo_cliente = "NORMAL";
            objDocVta.cliente_habilitado = "HABILITADO";

            objDocVta.latitud = "0";
            objDocVta.longitud = "0";
            objDocVta.ubicacion = "LOCAL";

            objDocVta.pago_con_anticipo = false;
            objDocVta.vta_cliente_en_oficina = false;

            // Para facturación mostrador
            objDocVta.idFC_complementaria = "";
            objDocVta.nroidFC_complementaria = "0";
            objDocVta.nrocaja = "";
            objDocVta.nroautorizacion = "";
            objDocVta.tipo_caja = "";
            //sia_DAL.Datos.Instancia.FechaDelServidor.AddDays(10);
            objDocVta.version_codcontrol = "";
            objDocVta.nrofactura = "0";
            objDocVta.nroticket = "";
            objDocVta.idanticipo = "";
            objDocVta.noridanticipo = "0";
            objDocVta.monto_anticipo = 0;
            objDocVta.idpf_solurgente = "";
            objDocVta.noridpf_solurgente = "0";

            return objDocVta;
        }
        //public async Task<int> Precio_Unico_Del_Documento_Factura(DBContext _context, List<dataDetalleFactura> tabladetalle, string codempresa)
        //{
        //    int resultado = -1;
        //    var lista_precios = await Lista_Precios_En_El_Documento_Factura(tabladetalle);
        //    if (lista_precios.Count == 1)
        //    {
        //        resultado = lista_precios[0];
        //    }
        //    else
        //    {
        //        resultado = -1;
        //    }
        //    return resultado;

        //}

        //public async Task<List<int>> Lista_Precios_En_El_Documento_Factura(List<dataDetalleFactura> tabladetalle)
        //{
        //    var elementosUnicos = tabladetalle
        //        .Select(objeto => objeto.codtarifa)
        //        .Distinct()
        //        .ToList();
        //    return elementosUnicos;

        //}
        private async Task<(bool resp, string msg, object? objContra)> Validar_Detalle(DBContext _context, dataCrearGrabarFacturaTienda dataFacturaTienda)
        {
            bool resultado = true;
            vefactura cabecera = dataFacturaTienda.cabecera;
            List<dataDetalleFactura> tabladetalle = dataFacturaTienda.detalle;
            string codcliente = cabecera.codcliente;
            string codempresa = dataFacturaTienda.codempresa;
            string usuario = dataFacturaTienda.usuario;
            // ###VALIDAR QUE EL USUARIO DE TIENDA NO FACTURE DE ALMACEN
            foreach (var detalle in tabladetalle)
            {
                if (Convert.IsDBNull(detalle.coddescuento))
                {
                    detalle.coddescuento = 0;
                }

                if (string.IsNullOrEmpty(detalle.coddescuento.ToString()))
                {
                    detalle.coddescuento = 0;
                }

                if (detalle.coditem == null)
                {
                    resultado = false;
                    return (resultado, "No eligió el item en la línea " + tabladetalle.IndexOf(detalle) + 1 + ", Item: " + detalle.coditem + ".", null);
                }
                else if (detalle.coditem.Length < 1)
                {
                    resultado = false;
                    return (resultado, "No eligió el item en la línea " + tabladetalle.IndexOf(detalle) + 1 + ", Item: " + detalle.coditem + ".", null);
                }
                else if (!await items.itemventa_context(_context, detalle.coditem))
                {
                    resultado = false;
                    return (resultado, "El item en la línea " + tabladetalle.IndexOf(detalle) + 1 + " " + detalle.coditem + "no está a la venta.", null);
                }
                else if (detalle.udm == null)
                {
                    resultado = false;
                    return (resultado, "No puso la unidad de medida en la línea " + tabladetalle.IndexOf(detalle) + 1 + ", Item: " + detalle.coditem + ".", null);
                }
                else if (string.IsNullOrEmpty(detalle.udm))
                {
                    resultado = false;
                    return (resultado, "No puso la unidad de medida en la línea " + tabladetalle.IndexOf(detalle) + 1 + ", Item: " + detalle.coditem + ".", null);
                }
                else if (Convert.IsDBNull(detalle.cantidad) || detalle.cantidad <= 0)
                {
                    resultado = false;
                    return (resultado, "No puso la cantidad en la línea " + tabladetalle.IndexOf(detalle) + 1 + ", Item: " + detalle.coditem + ".", null);
                }
                else if (Convert.IsDBNull(detalle.preciolista) || detalle.preciolista <= 0)
                {
                    resultado = false;
                    return (resultado, "No puso el precio en la línea " + tabladetalle.IndexOf(detalle) + 1 + ", Item: " + detalle.coditem, null);
                }
                else if (!await ventas.ValidarTarifa(_context, codcliente, detalle.coditem, detalle.codtarifa))
                {
                    resultado = false;
                    return (resultado, "El item en la línea " + tabladetalle.IndexOf(detalle) + 1 + ", Item: " + detalle.coditem + " no se puede vender a ese precio para este cliente.", null);
                }
                else if (!await ventas.ValidarTarifa_Descuento(_context, detalle.coddescuento, detalle.codtarifa))
                {
                    resultado = false;
                    return (resultado, "El item en la línea " + tabladetalle.IndexOf(detalle) + 1 + ", Item: " + detalle.coditem + " no se puede vender a ese tipo de precio con ese descuento especial.", null);
                }
                detalle.distdescuento = 0;
                detalle.distrecargo = 0;
                detalle.preciodist = detalle.precioneto;
            }
            //verificar que la unidad de medida sea entero o decimal
            if (resultado)
            {
                foreach (var detalle in tabladetalle)
                {
                    if (await ventas.UnidadSoloEnteros(_context, detalle.udm))
                    { //verificar que la cantidad sea entero
                        if (detalle.cantidad != Math.Floor(detalle.cantidad))
                        {
                            resultado = false;
                            return (resultado, "La cantidad en la línea " + tabladetalle.IndexOf(detalle) + 1 + ", Item: " + detalle.coditem + " no puede tener decimales.", null);
                        }
                    }
                }
            }

            //Validar items sin peso
            if (resultado)
            {
                foreach (var detalle in tabladetalle)
                {
                    if (await items.itempeso(_context, detalle.coditem) <= 0)
                    { //verificar que tenga peso el item
                        resultado = false;
                        return (resultado, "La item en la línea " + tabladetalle.IndexOf(detalle) + 1 + ", Item: " + detalle.coditem + "  no tiene peso registrado, por favor verifique esta situacion.", null);
                    }
                }
            }

            //Validar items repetidos
            if (resultado)
            {
                ResultadoValidacion objres = new ResultadoValidacion();
                validar_Vta.InicializarResultado(objres); // Inicializar_Resultado_Validacion(objres);
                DatosDocVta datosDocVta = new DatosDocVta();
                List<itemDataMatriz> tabladetalle2 = await ConvertirDetalleFactura(tabladetalle);
                await Llenar_Datos_Del_Documento(_context, codempresa, cabecera, tabladetalle2, datosDocVta);

                objres = await validar_Vta.Validar_Items_Repetidos(_context, tabladetalle2, datosDocVta, codempresa);
                if (objres.resultado == false)
                {
                    resultado = false;
                    return (resultado, "Observaciones de Validacion: " + objres.observacion + "-->" + objres.obsdetalle, null);
                }
            }
            //VALIDACION TARIFAS PERMITIDAS POR USUARIO
            if (resultado)
            {
                foreach (var detalle in tabladetalle)
                {
                    if (!await ventas.UsuarioTarifa_Permitido(_context, usuario, detalle.codtarifa))
                    { //verificar que el usuario tenga habilitado el precio del detalle
                        resultado = false;
                        return (resultado, "Este usuario no esta habilitado para ver ese tipo de Precio " + detalle.codtarifa + ".", null);
                    }
                }
            }
            return (resultado, "", null);
        }
        private async Task<(bool resp, string msg, object? objContra)> Validar_Negativos(DBContext _context, bool alertar, dataCrearGrabarFacturaTienda dataFacturaTienda)
        {
            bool resultado = true;
            vefactura cabecera = dataFacturaTienda.cabecera;
            List<dataDetalleFactura> tabladetalle = dataFacturaTienda.detalle;
            string codcliente = cabecera.codcliente;
            string codempresa = dataFacturaTienda.codempresa;
            string usuario = dataFacturaTienda.usuario;
            List<Dtnegativos> dtnegativos = new List<Dtnegativos>();
            ResultadoValidacion objres = new ResultadoValidacion();
            validar_Vta.InicializarResultado(objres); // Inicializar_Resultado_Validacion(objres);
            DatosDocVta datosDocVta = new DatosDocVta();
            List<itemDataMatriz> tabladetalle2 = await ConvertirDetalleFactura(tabladetalle);
            await Llenar_Datos_Del_Documento(_context, codempresa, cabecera, tabladetalle2, datosDocVta);

            (objres, dtnegativos) = await validar_Vta.Validar_Saldos_Negativos_Doc(_context, tabladetalle2, datosDocVta, dtnegativos, codempresa, usuario);
            if (objres.resultado == false)
            {
                resultado = false;
                return (resultado, "Hay items que generan saldos negativos, verifique el detalle!!!", null);
            }
            else
            {
                if (alertar)
                {
                    return (resultado, "El documento no genera saldos negativos!!!", null);
                }
            }
            return (resultado, "", null);
        }
        private async Task<(bool resp, string msg, object? objContra)> Validar_Datos_Cabecera(DBContext _context, dataCrearGrabarFacturaTienda dataFacturaTienda)
        {
            bool resultado = true;
            string msg = "";
            vefactura cabecera = dataFacturaTienda.cabecera;
            List<dataDetalleFactura> tabladetalle = dataFacturaTienda.detalle;
            string codcliente = cabecera.codcliente;
            string codempresa = dataFacturaTienda.codempresa;
            string usuario = dataFacturaTienda.usuario;
            int nro_items_max = await ventas.numitemscaja(_context, dataFacturaTienda.nrocaja);

            cabecera.id = cabecera.id.Trim();
            cabecera.codcliente = cabecera.codcliente.Trim();
            cabecera.nomcliente = cabecera.nomcliente.Trim();
            cabecera.codmoneda = cabecera.codmoneda.Trim();
            cabecera.transporte = cabecera.transporte.Trim();
            cabecera.fletepor = cabecera.fletepor.Trim();
            cabecera.direccion = cabecera.direccion.Trim();
            cabecera.cufd = cabecera.cufd.Trim();
            cabecera.codbanco = cabecera.codbanco.Trim();
            cabecera.idcuenta = cabecera.idcuenta.Trim();
            cabecera.nomcliente = cabecera.nomcliente.Replace("|", "l");
            cabecera.nomcliente = cabecera.nomcliente.Replace("°", "o");
            cabecera.nomcliente = cabecera.nomcliente.Replace("\"", "");

            cabecera.nit = await ventas.LimpiarNit(cabecera.nit);

            if (cabecera.codmoneda == await Empresa.monedabase(_context, codempresa))
            {
                cabecera.tdc = 1;
            }
            else
            {
                cabecera.tdc = await tipocambio._tipocambio(_context, await Empresa.monedabase(_context, codempresa), cabecera.codmoneda, cabecera.fecha);
            }
            if (cabecera.direccion == "")
            {
                cabecera.direccion = "---";
            }
            if (cabecera.idanticipo.Trim() == "")
            {
                cabecera.idanticipo = "";
                cabecera.monto_anticipo = 0;
            }
            if (cabecera.numeroidanticipo == 0)
            {
                cabecera.numeroidanticipo = 0;
                cabecera.monto_anticipo = 0;
            }
            if (cabecera.idfc.Trim() == "")
            {
                cabecera.idfc = "";
                cabecera.numeroidfc = 0;
            }
            //VALIDAR DATOS DE LA CABECERA
            //Desde 25/05/2023 
            //validar que no sea venta al credito
            if (cabecera.tipopago == 1)
            {
                resultado = false;
                return (resultado, "Las ventas al credito no esta permitidas en tiendas, por favor consulte con el administrador del sistema!!!", null);
            }
            if (cabecera.contra_entrega == true)
            {
                resultado = false;
                return (resultado, "Las ventas contra entrega no esta permitidas en tiendas, por favor consulte con el administrador del sistema!!!", null);
            }

            if (string.IsNullOrWhiteSpace(cabecera.nrocaja.ToString()))
            {
                resultado = false;
                return (resultado, "No puede dejar el numero de caja en blanco.", null);
            }
            else if (string.IsNullOrWhiteSpace(cabecera.cufd))
            {
                resultado = false;
                return (resultado, "No puede dejar el CUFD en blanco.", null);
            }
            else if (string.IsNullOrWhiteSpace(cabecera.fechalimite.ToString()))
            {
                resultado = false;
                return (resultado, "No puede dejar la fecha límite de emisión en blanco.", null);
            }
            else if (tabladetalle.Count > nro_items_max)
            {
                resultado = false;
                return (resultado, $"El número de ítems del documento sobrepasa el límite máximo de esta caja, el máximo de ítems permitido es: {nro_items_max}", null);
            }
            else if (await ventas.cufd_fechalimiteDate(_context, cabecera.cufd) < cabecera.fecha.Date)
            {
                resultado = false;
                return (resultado, "La dosificación ha llegado a su fecha límite.", null);
            }
            else if (string.IsNullOrWhiteSpace(cabecera.codalmacen.ToString()))
            {
                resultado = false;
                return (resultado, "No puede dejar la casilla de Almacén en blanco.", null);
            }
            else if (string.IsNullOrWhiteSpace(cabecera.id))
            {
                resultado = false;
                return (resultado, "No puede dejar la casilla de Id de la Factura en blanco.", null);
            }
            else if (!await ventas.Existe_ID_Factura(_context, cabecera.id))
            {
                resultado = false;
                return (resultado, "Ese Id de Facturación no existe, seleccione otro.", null);
            }
            else if (cabecera.idanticipo.Trim().Length > 0 && (cabecera.numeroidanticipo.ToString().Trim() == "" || cabecera.numeroidanticipo == 0))
            {
                resultado = false;
                return (resultado, "Selecciono un ID de Anticipo pero el numeroid del Anticipo es invalido.", null);
            }
            else if (cabecera.idanticipo.Trim().Length > 0 && cabecera.numeroidanticipo > 0 && cabecera.monto_anticipo == 0)
            {
                resultado = false;
                return (resultado, "Selecciono un ID de Anticipo pero el monto del Anticipo no puede ser 0 (cero).", null);
            }
            else if (cabecera.idfc.Trim().Length > 0 && cabecera.numeroidfc.ToString().Trim() == "")
            {
                resultado = false;
                return (resultado, "Selecciono un ID de Factura de Complemento pero no coloco el numeroid del Complemento.", null);
            }
            else if (string.IsNullOrWhiteSpace(cabecera.codvendedor.ToString()))
            {
                resultado = false;
                return (resultado, "No puede dejar la casilla de Vendedor en blanco.", null);
            }
            else if (string.IsNullOrWhiteSpace(cabecera.codcliente))
            {
                resultado = false;
                return (resultado, "No puede dejar la casilla de Código de Cliente en blanco.", null);
            }
            else if (await cliente.EsClienteSinNombre(_context, cabecera.codcliente))
            {
                if (!string.IsNullOrWhiteSpace(cabecera.nit))
                {
                    if (long.TryParse(cabecera.nit, out long nitVal))
                    {
                        if (nitVal > 0 && !await configuracion.emp_permitir_facturas_sin_nombre(_context, codempresa))
                        {
                            resultado = false;
                            return (resultado, "No se puede realizar facturas a códigos SIN NOMBRE y con NIT diferente de cero. Debe crear al cliente.", null);
                        }
                    }
                }
            }
            else if (!await cliente.clientehabilitado(_context, cabecera.codcliente))
            {
                resultado = false;
                return (resultado, "Ese Cliente no está habilitado.", null);
            }
            else if (string.IsNullOrWhiteSpace(cabecera.nomcliente))
            {
                resultado = false;
                return (resultado, "No puede dejar la casilla de Nombre del Cliente en blanco.", null);
            }
            else if (string.IsNullOrWhiteSpace(cabecera.nit) || cabecera.nit.Length == 0)
            {
                resultado = false;
                return (resultado, "No puede dejar la casilla de N.I.T. del cliente en blanco.", null);
            }
            else if (cabecera.tipo_docid < 0)
            {
                resultado = false;
                return (resultado, "Debe identificar el tipo de documento de identificación del cliente.", null);
            }
            else if (cabecera.tipo_docid + 1 != 1)
            {
                if (cabecera.complemento_ci.Trim().Length > 0)
                {
                    resultado = false;
                    return (resultado, "Esta intentando registrar un tipo de documento que no es C.I. con complemento, lo cual no es correcto, verifique esta situacion.", null);
                }
            }
            else if ((cabecera.tipopago == 0 && string.IsNullOrWhiteSpace(cabecera.idcuenta)))
            {
                resultado = false;
                return (resultado, "No puede dejar la casilla de Cuenta de Ingresos en blanco.", null);
            }
            else if ((cabecera.tipopago == 0 && string.IsNullOrWhiteSpace(cabecera.codtipopago.ToString())))
            {
                resultado = false;
                return (resultado, "No puede dejar la casilla de Forma de Pago en blanco.", null);
            }
            else if (cabecera.nroticket == "")
            {
                resultado = false;
                return (resultado, "No puede dejar la casilla de Ticket de Servicio en blanco.", null);
            }

            if (resultado)
            {
                if (await ventas.Validar_NIT_Correcto_Factura(_context, cabecera.nit, cabecera.tipo_docid.ToString()) == false)
                {
                    resultado = false;
                    return (resultado, "Verifique que el NIT tenga el formato correcto!!!", null);
                }
            }
            if (resultado)
            {
                if (dataFacturaTienda.ids_proforma != "" && dataFacturaTienda.nro_id_proforma > 0)
                {//si ha elegido una proforma con la cual enlazar se debe  validar que esta proforma no haya sido enlazada antes
                 // Buscar la factura por el código
                    var dt = await _context.vefactura
                    .Where(factura => factura.anulada == false &&
                                      _context.veproforma.Any(proforma => proforma.codigo == factura.codproforma &&
                                                                          proforma.id == dataFacturaTienda.ids_proforma &&
                                                                          proforma.numeroid == dataFacturaTienda.nro_id_proforma))
                    .Select(factura => factura.codigo)
                    .ToListAsync();
                    if (dt.Count() > 0)
                    {
                        resultado = false;
                        return (resultado, "La proforma: " + dataFacturaTienda.ids_proforma + "-" + dataFacturaTienda.nro_id_proforma + " ya fue facturada, verifique esta situacion!!!", null);
                    }
                    //Desde 19/09/2024 Validar que en la factura, proforma y solicitud urgente tengan el mismo codigo de cliente
                    if (await ventas.Cliente_De_Proforma(_context, await ventas.codproforma(_context, dataFacturaTienda.ids_proforma, dataFacturaTienda.nro_id_proforma)) != cabecera.codcliente)
                    {
                        resultado = false;
                        return (resultado, "El cliente de la Proforma enlazada no es el mismo codigo de cliente de la factura, lo cual no esta permitido, verifique esta situacion.", null);
                    }

                }
            }
            //verificar que haya elegido el tipo de doc de identidad
            if (resultado)
            {
                if (cabecera.tipo_docid < 0)
                {
                    resultado = false;
                    return (resultado, "Debe identificar el tipo de documento de identificación del cliente.", null);
                }
            }
            //validar email
            if (resultado)
            {
                if (cabecera.email.Trim().Length == 0)
                {
                    resultado = false;
                    return (resultado, "Si no especifica una dirección de email valida, no se podra enviar la factura en formato digital.", null);
                }
            }
            //Desde 23/08/2024 que valide si el numero de docuemnto ingresado es un NIT valido para el SIN
            //En caso que el usuario selecciono CI y el numero es un NIT valido para el SIN entonces obligar al usuario a que cambie el tipo de documento a NIT
            //En caso que el usuario selecciono NIT y el numero NO es un NIT valido para el SIN entonces obligar al usuario a que cambie el tipo de documento a CI u otro pero que no sea NIT
            if (resultado)
            {
                var resp = await funciones_SIAT.Validar_NIT_En_El_SIN_Crear_Cliente(_context, codempresa, Convert.ToInt32(cabecera.tipo_docid), cabecera.codalmacen, long.Parse(cabecera.nit), usuario);
                if (resp.resp == false)
                {
                    resultado = false;
                    msg = resp.mensaje;
                }
                else
                {
                    resultado = true;
                    msg = resp.mensaje;
                }
            }

            return (resultado, msg, null);
        }
        private async Task<(bool resp, string msg, object? objContra)> Validar_Documento(DBContext _context, string userConn, string opcion_validar, bool alertar_si_todo_valido, string cadena_controles, dataCrearGrabarFacturaTienda dataFacturaTienda)
        {
            bool resultado = true;
            int NroNoValidos = 0;
            string msg = "";

            string codempresa = dataFacturaTienda.codempresa;
            string usuario = dataFacturaTienda.usuario;
            vefactura cabecera = dataFacturaTienda.cabecera;
            List<dataDetalleFactura> tabladetalle = dataFacturaTienda.detalle;
            DatosDocVta datosDocVta = new DatosDocVta();
            List<itemDataMatriz> tabladetalle2 = await ConvertirDetalleFactura(tabladetalle);
            await Llenar_Datos_Del_Documento(_context, codempresa, cabecera, tabladetalle2, datosDocVta);
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                List<itemDataMatriz> itemDataMatriz = new List<itemDataMatriz>();
                List<vedesextraDatos>? vedesextraDatos = new List<vedesextraDatos>();
                List<vedetalleEtiqueta> vedetalleEtiqueta = new List<vedetalleEtiqueta>();
                List<vedetalleanticipoProforma>? vedetalleanticipoProforma = new List<vedetalleanticipoProforma>();
                List<verecargosDatos>? verecargosDatos = new List<verecargosDatos>();
                List<Controles>? controles_recibidos = new List<Controles>();
                List<Controles>? controles_nuevos = new List<Controles>();

                itemDataMatriz = dataFacturaTienda.detalleItemsProf_fact;
                vedesextraDatos = dataFacturaTienda.detalleDescuentos_fact;
                //vedetalleEtiqueta = dataFacturaTienda.detalleEtiqueta_fact;
                //vedetalleanticipoProforma = dataFacturaTienda.detalleAnticipos_fact;
                verecargosDatos = dataFacturaTienda.detalleRecargos_fact;
                controles_recibidos = dataFacturaTienda.detalleControles_fact;

                var dtvalidar = await validar_Vta.DocumentoValido(userConnectionString, cadena_controles, "factura", opcion_validar, datosDocVta, itemDataMatriz, vedesextraDatos, vedetalleEtiqueta, vedetalleanticipoProforma, verecargosDatos, controles_recibidos, codempresa, usuario);
                dtvalidar = dtvalidar.Select(p => { p.CodServicio = p.CodServicio == "" ? "0" : p.CodServicio; return p; }).ToList();
                if (dtvalidar != null)
                {
                    //contar los no validos; sino hacer en un foreach
                    NroNoValidos = dtvalidar.Select(p => { p.Valido = "NO"; return p; }).Count();

                    if (NroNoValidos > 0)
                    {
                        if (opcion_validar == "grabar" || opcion_validar == "validar")
                        {
                            msg = "El documento no cumple condiciones para ser Grabado, verifique el resultado de la revision!!!";
                        }
                        else
                        {
                            msg = "El documento no cumple condiciones para ser Grabado y Aprobado, verifique el resultado de la revision!!!";
                        }
                    }
                    else
                    {
                        if (alertar_si_todo_valido)
                        {
                            msg = "El documento es valido!!!";
                        }
                        resultado = true;
                    }
                }
                else { resultado = false; }

            }
            catch (Exception ex)
            {
                return (false, "Error en el servidor al validar Factura Tienda" + ex.Message, null);
                throw;
            }

            return (resultado, msg, null);

        }
        private async Task<(bool resultado, string msg)> ValidarDataRecibida(DBContext _context, dataCrearGrabarFacturaTienda dataFactura)
        {
            DateTime fecha_actual = (await funciones.FechaDelServidor(_context)).Date;
            if (dataFactura == null)
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura'.");
            }
            if (dataFactura.cabecera == null)
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Cabecera'.");
            }
            /*
            if (string.IsNullOrWhiteSpace(dataFactura.codbanco))
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - CodBanco'.");
            }
            if (string.IsNullOrWhiteSpace(dataFactura.codcuentab))
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Codcuentab'.");
            }
            */
            if (string.IsNullOrWhiteSpace(dataFactura.codempresa))
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Codempresa'.");
            }
            if (string.IsNullOrWhiteSpace(dataFactura.codigo_control))
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Codigo Control'.");
            }
            if (dataFactura.codtipopago == 0 || string.IsNullOrWhiteSpace(dataFactura.codtipopago.ToString()))
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - CodTipopago'.");
            }
            /*
            if (string.IsNullOrWhiteSpace(dataFactura.codtipo_comprobante))
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Codtipo Comprobante'.");
            }
            */
            if (dataFactura.complemento_ci == null)
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Complemento CI'.");
            }
            if (string.IsNullOrWhiteSpace(dataFactura.condicion))
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Condicion'.");
            }
            if (string.IsNullOrWhiteSpace(dataFactura.cufd))
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - CUFD'.");
            }
            if (dataFactura.detalle == null)
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Detalle'.");
            }
            if (dataFactura.detalleControles_fact == null)
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Detalle Controles'.");
            }
            if (dataFactura.detalleDescuentos_fact == null)
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Detalle Descuentos'.");
            }
            if (dataFactura.detalleItemsProf_fact == null)
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Detalle ItemsProf'.");
            }
            if (dataFactura.detalleRecargos_fact == null)
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Detalle Recargos'.");
            }
            if (dataFactura.dgvfacturas == null)
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Detalle Dgvfacturas'.");
            }
            if (string.IsNullOrWhiteSpace(dataFactura.factnit))
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Nit Factura'.");
            }
            if (string.IsNullOrWhiteSpace(dataFactura.factnomb))
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Nombre Factura'.");
            }
            if (string.IsNullOrWhiteSpace(dataFactura.idcuenta))
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - ID Cuenta'.");
            }
            if (string.IsNullOrWhiteSpace(dataFactura.idfactura))
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - ID Factura'.");
            }
            if (dataFactura.ids_proforma == null)
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - ID Propforma'.");
            }
            if (dataFactura.ids_proforma.Length > 0 && dataFactura.nro_id_proforma == 0)
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Numero ID Propforma'.");
            }
            if (string.IsNullOrWhiteSpace(dataFactura.nrocaja.ToString()))
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Nro Caja'.");
            }
            /*
            if (string.IsNullOrWhiteSpace(dataFactura.nrocheque.ToString()))
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Nro Cheque'.");
            }
            */
            if (dataFactura.nrolugar.ToString() == null)
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Nro Lugar'.");
            }
            if (string.IsNullOrWhiteSpace(dataFactura.tipo.ToString()))
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Tipo'.");
            }
            if (string.IsNullOrWhiteSpace(dataFactura.usuario.ToString()))
            {
                return (false, "No se ha proporcionado el valor del dato 'Data Factura - Usuario'.");
            }
            if (dataFactura.dtpfecha_limite.Date != fecha_actual)
            {
                return (false, "EL CUFD es valido para emision de facturas solo para fecha: " + dataFactura.dtpfecha_limite.ToShortDateString() + " y la fecha actual del servidor es: " + fecha_actual.Date.ToShortDateString() + " verifique esta situacion!!!");
            }
            return (true, "");
        }

        private async Task<(bool resul, bool factura_se_imprime, List<string> msgAlertas, List<string> eventos, string nomArchivoXML)> GENERAR_XML_FACTURA_FIRMAR_ENVIAR(DBContext _context, List<int> CodFacturas_Grabadas, string codempresa, string usuario, int codalmacen, string ruta_certificado, string Clave_Certificado_Digital, string codigocontrol)
        {
            // para devolver lista de registros logs
            List<string> eventos = new List<string>();
            List<string> msgAlertas = new List<string>();
            string msg = "";
            bool factura_se_imprime = false;
            bool resultado = true;
            string id = "";
            int numeroid = 0;
            string mensaje = "";
            string nit = await empresa.NITempresa(_context, codempresa);
            string cuf = "", cufd = "";
            int nrofactura = 0;
            string archivoPDF = "";
            string rutaFacturaXml = "";
            string rutaFacturaXmlSigned = "";
            string ruta_factura_xml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certificado");
            string ruta_factura_xml_signed = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certificado");

            // eso para detectar si hay cambio de CUFD (cuando en el mismo dia se genera otro CUFD)
            string cadena_msj = "";
            //mensaje = DateTime.Now.Year.ToString("0000") +
            //DateTime.Now.Month.ToString("00") +
            //DateTime.Now.Day.ToString("00") + " " +
            //DateTime.Now.Hour.ToString("00") + ":" +
            //DateTime.Now.Minute.ToString("00") + " - " + cadena_msj;

            var Pametros_Facturacion_Ag1 = await siat.Obtener_Parametros_Facturacion(_context, codalmacen);
            ////////////////////////////////////////////////////////////////////////////////////////////
            //CREAR XML - FIRMARLO - COMPRIMIR EN GZIP - CONVERTIR EN BYTES - SACAR HASH - ENVIAR AL SIN
            //////////////////////////////////////////////////////////////////////////////////////////////
            byte[] miComprimidoGZIP = null;
            string nomArchivoXML = "";
            foreach (var codFacturas in CodFacturas_Grabadas)
            {
                if (!string.IsNullOrWhiteSpace(codFacturas.ToString()))
                {
                    try
                    {
                        bool miresultado = true;
                        var docfc = await ventas.id_nroid_factura(_context, Convert.ToInt32(codFacturas));
                        id = docfc.id;
                        numeroid = docfc.numeroId;
                        // Generar XML Serializado
                        int codDocSector = await adsiat_Parametros_Facturacion.TipoDocSector(_context, codalmacen);
                        if (codDocSector == 1)
                        {
                            //1: FACTURA COMPRA VENTA (2 DECIMALES)
                            // miresultado = await siat.Generar_XML_Factura_Serializado(id, numeroid, codempresa, false);
                        }
                        else
                        {
                            //35: FACTURA COMPRA VENTA BONIFICACIONES (2 DECIMALES)
                            miresultado = await funciones_SIAT.Generar_XML_Factura_Compra_Venta_Bonificaciones_Serializado(_context, id, numeroid, codempresa, false, usuario);
                        }
                        if (miresultado)
                        {
                            mensaje = "XML generado exitosamente!!!";
                            eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                            await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "docvefacturamos_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                        }
                        // Firmar XML
                        //definir el nombre del archivo
                        archivoPDF = id + "_" + numeroid + ".pdf";
                        nomArchivoXML = $"{id}_{numeroid}_Dsig.xml";
                        rutaFacturaXml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certificado", $"{id}_{numeroid}.xml");
                        rutaFacturaXmlSigned = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certificado", nomArchivoXML);
                        if (miresultado)
                        {
                            miresultado = await funciones_SIAT.Firmar_XML_Con_SHA256(rutaFacturaXml, ruta_certificado, Clave_Certificado_Digital, rutaFacturaXmlSigned);
                            if (miresultado)
                            {
                                mensaje = "XML Firmado exitosamente";
                                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "docvefacturamos_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                            }
                            else
                            {
                                mensaje = "XML no se pudo firmar";
                                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "docvefacturamos_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                            }
                        }

                        // Comprimir en GZIP
                        string pathDestino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certificado", $"{id}_{numeroid}.gzip");
                        if (miresultado)
                        {
                            miresultado = await gzip.CompactaArchivoAsync(rutaFacturaXmlSigned, pathDestino);
                            if (miresultado)
                            {
                                mensaje = "Archivo comprimido en GZIP exitosamente!!!";
                                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "docvefacturamos_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                            }
                            else
                            {
                                mensaje = "No se pudo comprimir en GZIP!!!";
                                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "docvefacturamos_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                            }
                        }
                        //EL ARCHIVO COMPRESO CONVERTIR EN BYTES()
                        // Convertir a Bytes
                        //byte[] miComprimidoGZIP;
                        if (miresultado)
                        {
                            try
                            {
                                // miComprimidoGZIP = await gzip.CompressGZIP(File.ReadAllBytes(rutaFacturaXmlSigned));
                                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(rutaFacturaXmlSigned);
                                // Comprime el archivo
                                miComprimidoGZIP = gzip.CompressGZIP(fileBytes);
                                eventos.Add("Archivo GZIP convertido en Bytes exitosamente!!!");
                                mensaje = "GZIP convertido a bytes!!!";
                                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "docvefacturamos_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                            }
                            catch (Exception ex)
                            {
                                mensaje = "No se pudo convertir a bytes, " + ex.Message;
                                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "docvefacturamos_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                                miresultado = false;
                            }
                        }

                        // Generar HASH
                        string miHASH = "";
                        if (miresultado)
                        {
                            try
                            {
                                //miHASH = await siat.GenerarSHA256DeArchivoAsync(pathDestino);
                                miHASH = await siat.GenerarSHA256DeArchivoAsync(pathDestino);
                                mensaje = "HASH firma digital generado exitosamente";
                                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "docvefacturamos_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                            }
                            catch (Exception ex)
                            {
                                mensaje = "No se pudo generar el HASH la huella digital. " + ex.Message;
                                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "docvefacturamos_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                                miresultado = false;
                            }
                        }

                        // Enviar al SIN
                        if (miresultado)
                        {
                            DataTable dtFactura = new DataTable();
                            dtFactura.Clear();
                            var datos = await _context.vefactura
                            .Where(v => v.id == id && v.numeroid == numeroid)
                            .Select(i => new
                            {
                                i.codigo,
                                i.id,
                                i.numeroid,
                                i.fecha,
                                i.cuf,
                                i.cufd,
                                i.nit,
                                i.en_linea,
                                i.en_linea_SIN
                            }).ToListAsync();
                            var result = datos.Distinct().ToList();
                            dtFactura = funciones.ToDataTable(result);

                            if (dtFactura.Rows.Count > 0)
                            {
                                cuf = (string)dtFactura.Rows[0]["cuf"];
                                cufd = (string)dtFactura.Rows[0]["cufd"];
                                //se verifica como se genero el CUF, si como fuera de linea o en linea
                                if ((bool)dtFactura.Rows[0]["en_linea"] && (bool)dtFactura.Rows[0]["en_linea_sin"])
                                {
                                    //ESTA EN MODO FACTURACION EN LINEA
                                    var enviar_factura_al_sin = await funciones_SIAT.ENVIAR_FACTURA_AL_SIN(_context, codigocontrol, codempresa, usuario, cufd, long.Parse(nit), cuf, miComprimidoGZIP, miHASH, codalmacen, (int)dtFactura.Rows[0]["codigo"], (string)dtFactura.Rows[0]["id"], (int)dtFactura.Rows[0]["numeroid"]);
                                    if (enviar_factura_al_sin.resul)
                                    {
                                        //se envio al SIN
                                        mensaje = "Recepción de factura de almacén exitosa!!!";
                                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - ---> " + id + "-" + numeroid + " " + mensaje);
                                        factura_se_imprime = true;
                                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "docvefacturamos_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                                    }
                                    else
                                    {
                                        //no se recepciono la factura en el SIN
                                        mensaje = "Recepción de factura de almacén rechazada!!!";
                                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - ---> " + id + "-" + numeroid + " " + mensaje);
                                        factura_se_imprime = false;
                                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "docvefacturamos_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                                    }
                                }
                                else
                                {
                                    // ESTA EN MODO FACTURACION FUERA DE LINEA
                                    //registrar log siat
                                    factura_se_imprime = true;
                                    mensaje = "No se envía al SIN, CUF generado fuera de línea!!!";
                                    eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "docvefacturamos_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                                    //Desde 03-07-2023
                                    //actualizar la cod_recepcion_siat
                                    string cod_recepcion_siat = "";
                                    int cod_estado_siat = 0;
                                    cod_recepcion_siat = "0";
                                    cod_estado_siat = 0;

                                    int codigoFactura = Convert.ToInt32(CodFacturas_Grabadas);

                                    // Buscar la factura por el código
                                    var factura = _context.vefactura.SingleOrDefault(f => f.codigo == codigoFactura);

                                    if (factura != null)
                                    {
                                        // Actualizar los valores
                                        factura.cod_recepcion_siat = cod_recepcion_siat;
                                        factura.cod_estado_siat = cod_estado_siat;

                                        // Guardar cambios en la base de datos
                                        _context.SaveChanges();
                                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "docvefacturamos_cufdController", "Cod_Recepcion:" + cod_recepcion_siat + "|Cod_estado_siat:" + cod_estado_siat, Log.TipoLog_Siat.Envio_Factura);
                                    }
                                }
                            }
                            else
                            {
                                cuf = "";
                                cufd = "";
                                mensaje = ".... " + id + "-" + numeroid + " No se pudo obtener el CUF ni el CUFD de la factura grabada";
                                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                                miresultado = false;
                            }
                            resultado = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        resultado = false;
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - Ocurrió un error al generar el XML de la factura: " + ex.Message);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "docvefacturamos_cufdController", "La factura no fue recepcionada por el SIN", Log.TipoLog_Siat.Envio_Factura);
                    }
                }
            }
            return (resultado, factura_se_imprime, msgAlertas, eventos, nomArchivoXML);
        }
        private async Task<(bool result, List<string> msgAlertas, List<string> eventos, string ruta_certificado, string Clave_Certificado_Digital)> Definir_Certificado_A_Utilizar(DBContext _context, int codalmacen, string codempresa)
        {
            List<string> msgAlertas = new List<string>();
            List<string> eventos = new List<string>();
            string ruta_certificado = "";
            string Clave_Certificado_Digital = "";
            if (codalmacen == 0)
            {
                msgAlertas.Add("No se encontró el almacén, lo cual se necesita para definir el certificado digital a utilizar!!!");
                ruta_certificado = "";
                Clave_Certificado_Digital = "";
                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - No se encontró el almacén, lo cual se necesita para definir el certificado digital a utilizar!!!");
                return (false, msgAlertas, eventos, ruta_certificado, Clave_Certificado_Digital);
            }

            int _codAmbiente = await adsiat_Parametros_Facturacion.Ambiente(_context, codalmacen);

            if (_codAmbiente == 1)
            {
                // Certificado para producción
                ruta_certificado = await configuracion.Dircertif_Produccion(_context, codempresa);
                string cadena_descifrada = seguridad.XorString(await configuracion.Pwd_Certif_Produccion(_context, codempresa), "devstring").Trim();
                Clave_Certificado_Digital = cadena_descifrada;
                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - Facturar NR ha establecido el certificado digital de producción para realizar las firmas digitales!!!");
                return (true, msgAlertas, eventos, ruta_certificado, Clave_Certificado_Digital);
            }
            else
            {
                // Certificado para pruebas
                ruta_certificado = await configuracion.Dircertif_Pruebas(_context, codempresa);
                string cadena_descifrada = seguridad.XorString(await configuracion.Pwd_Certif_Pruebas(_context, codempresa), "devstring").Trim();
                Clave_Certificado_Digital = cadena_descifrada;
                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - Facturar NR ha establecido el certificado digital de pruebas para realizar las firmas digitales!!!");
                return (true, msgAlertas, eventos, ruta_certificado, Clave_Certificado_Digital);
            }
        }
        [HttpGet]
        [Route("getVerifComunicacionSIN/{userConn}/{almacen}")]
        public async Task<object> getVerifComunicacionSIN(string userConn, int almacen)
        {
            try
            {
                // Validar que los parámetros no sean nulos ni vacíos
                if (string.IsNullOrWhiteSpace(almacen.ToString()) || almacen == 0)
                {
                    return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Almacen'." });
                }

                // Validar si userConn es nulo o vacío, ya que es parte de la ruta
                if (string.IsNullOrWhiteSpace(userConn))
                {
                    return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'UserConn'." });
                }

                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    bool resultado = false;
                    string cadena_msj = "";
                    resultado = await serv_Facturas.VerificarComunicacion(_context, almacen);
                    string alerta = "";
                    if (resultado)
                    {
                        cadena_msj = "Verificacion conexion con el SIN exitosa!!!";
                        alerta = "Verificacion conexion con el SIN exitosa";
                    }
                    else
                    {
                        cadena_msj = "Verificacion conexion con el SIN fallida!!!";
                        alerta = "No se pudo establecer la conexion con el SIN, consulte con el administrador del sistema!!! ";
                    }

                    string evento = DateTime.Now.Year.ToString("0000") +
                    DateTime.Now.Month.ToString("00") +
                    DateTime.Now.Day.ToString("00") + " " +
                    DateTime.Now.Hour.ToString("00") + ":" +
                    DateTime.Now.Minute.ToString("00") + " - " + cadena_msj;
                    return Ok(new
                    {
                        resp = alerta,
                        evento = evento
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
        [Route("getDataFacturaTienda/{userConn}/{codFactura}/{codigoempresa}")]
        public async Task<object> getDataFacturaTienda(string userConn, int codFactura, string codigoempresa)
        {
            try
            {
                // Validar que los parámetros no sean nulos ni vacíos
                if (string.IsNullOrWhiteSpace(codFactura.ToString()) || codFactura == 0)
                {
                    return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Codigo de Factura'." });
                }

                if (string.IsNullOrWhiteSpace(codigoempresa))
                {
                    return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Empresa'." });
                }

                // Validar si userConn es nulo o vacío, ya que es parte de la ruta
                if (string.IsNullOrWhiteSpace(userConn))
                {
                    return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'UserConn'." });
                }
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var cabecera = await _context.vefactura.Where(i => i.codigo == codFactura).FirstOrDefaultAsync();
                    var detalle = await _context.vefactura1
                        .Join(_context.initem,
                              v => v.coditem, // Clave en Vefactura1
                              i => i.codigo,  // Clave en Initem
                              (v, i) => new
                              {
                                  v.codfactura,
                                  v.coditem,
                                  v.codproducto_sin,
                                  i.descripcion,
                                  i.medida,
                                  v.cantidad,
                                  v.udm,
                                  v.codtarifa,
                                  v.coddescuento,
                                  v.preciolista,
                                  v.niveldesc,
                                  v.preciodesc,
                                  v.precioneto,
                                  v.total,
                                  v.distdescuento,
                                  v.distrecargo,
                                  v.preciodist,
                                  v.totaldist,
                                  v.codaduana
                              })
                        .Where(v => v.codfactura == codFactura)
                        .OrderBy(v => v.coditem).ToListAsync();

                    if (cabecera == null)
                    {
                        return BadRequest(new { resp = "No se encontró una factura con el codigo proporcionado, consulte al administrador" });
                    }
                    string imp_totalliteral = "SON: " + funciones.ConvertDecimalToWords(cabecera.total).ToUpper() + " " + await nombres.nombremoneda(_context, cabecera.codmoneda);
                    string nitEmpresa = await empresa.NITempresa(_context, codigoempresa);
                    // generar cadena para QR
                    string cadena_QR = await adsiat_Parametros_Facturacion.Generar_Cadena_QR_Link_Factura_SIN(_context, nitEmpresa, cabecera.cuf, cabecera.nrofactura.ToString(), "2", cabecera.codalmacen);

                    string leyendaSIN = ventas.leyenda_para_factura_en_linea(cabecera.en_linea_SIN ?? false);


                    // PARAMETROS DE CABECERA
                    string rsucursal = await contabilidad.sucursalalm(_context, cabecera.codalmacen);
                    string rdireccion_suc = await almacen.direccionalmacen(_context, cabecera.codalmacen);
                    string rtelefono = await almacen.telefonoalmacen(_context, cabecera.codalmacen);
                    string rlugar_emision = await almacen.lugaralmacen(_context, cabecera.codalmacen);
                    string rptovta_ag = "Punto de Vta.: " + await adsiat_Parametros_Facturacion.PuntoDeVta(_context, cabecera.codalmacen);

                    string rfax_ag = await almacen.faxalmacen(_context, cabecera.codalmacen);
                    if (rfax_ag.Trim().Length == 0 || rfax_ag == "0")
                    {
                        rfax_ag = "";
                    }
                    else
                    {
                        rfax_ag = "Fax:" + rfax_ag;
                    }

                    string rlugarFechaHora = await empresa.municipio_empresa(_context, codigoempresa) + " " + cabecera.fecha.ToShortDateString() + "  Hrs." + cabecera.horareg;

                    return Ok(new
                    {
                        paramEmp = new
                        {
                            sucursal = "Sucursal N° " + rsucursal,
                            codptovta = rptovta_ag,
                            direccion = rdireccion_suc,
                            telefono = "Teléfono: " + rtelefono,
                            fax = rfax_ag,
                            lugarEmision = rlugar_emision,
                            lugarFechaHora = rlugarFechaHora
                        },
                        cadena_QR,
                        imp_totalliteral,
                        leyendaSIN,
                        cabecera,
                        detalle
                    });
                }

            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }
        [HttpPost]
        [Route("enviarFacturaEmail/{userConn}/{codempresa}/{usuario}/{codFactura}/{nomArchXML}")]
        public async Task<object> enviarFacturaEmail(string userConn, string codempresa, string usuario, int codFactura, string nomArchXML, [FromForm] IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                return BadRequest(new { resp = "No se ha proporcionado un archivo PDF válido." });
            }
            // Validar que los parámetros no sean nulos ni vacíos
            if (string.IsNullOrWhiteSpace(codFactura.ToString()) || codFactura == 0)
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Codigo de Factura'." });
            }

            if (string.IsNullOrWhiteSpace(codempresa))
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Empresa'." });
            }
            if (string.IsNullOrWhiteSpace(usuario))
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Usuario'." });
            }
            if (string.IsNullOrWhiteSpace(nomArchXML))
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Archivo_XML'." });
            }

            // Validar si userConn es nulo o vacío, ya que es parte de la ruta
            if (string.IsNullOrWhiteSpace(userConn))
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'UserConn'." });
            }

            try
            {
                List<string> eventos = new List<string>();
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    //verificar si se envia el mail
                    if (await configuracion.emp_enviar_factura_por_email(_context, codempresa) == false)
                    {
                        string mi_msg = " Envio de facturas en PDF mas archivo XML por email esta deshabilitado!!!";
                        eventos.Add(mi_msg);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), pdfFile.FileName, pdfFile.FileName, _controllerName, mi_msg, Log.TipoLog_Siat.Creacion);
                        return StatusCode(203, new { eventos = mi_msg, resp = "" });
                    }

                    string email_enviador = await configuracion.Obtener_Email_Origen_Envia_Facturas(_context);
                    string _email_origen_credencial = email_enviador;
                    string _pwd_email_credencial_origen = await configuracion.Obtener_Clave_Email_Origen_Envia_Facturas(_context);

                    if (email_enviador.Trim().Length == 0)
                    {
                        string mi_msg = "No se encontro en la configuracion el email que envia las facturas, consulte con el administrador del sistema.";
                        eventos.Add(mi_msg);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), "", "", _controllerName, mi_msg, Log.TipoLog_Siat.Creacion);
                        return StatusCode(203, new { eventos = mi_msg, resp = "" });
                    }
                    /*
                     If _email_origen_credencial.Trim.Length = 0 Then
                        mi_msg = "No se encontro en la configuracion el email credencial que envia las facturas, consulte con el administrador del sistema."
                        Registrar_Evento(mi_msg)
                        sia_log.Log.Instancia.RegistrarEvento_Siat(sia_compartidos.temporales.Instancia.usuario, sia_log.Entidades.Factura, CodFacturas_Grabadas(0).ToString, "", "", Me.Name, mi_msg, sia_log.TipoLog.Creacion)
                        Return False
                    End If
                     */
                    if (_pwd_email_credencial_origen.Trim().Length == 0)
                    {
                        string mi_msg = "No se encontro la credencial del email que envia las facturas, consulte con el administrador del sistema.";
                        eventos.Add(mi_msg);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), "", "", _controllerName, mi_msg, Log.TipoLog_Siat.Creacion);
                        return StatusCode(203, new { eventos = mi_msg, resp = "" });
                    }

                    var DTFC = await _context.vefactura.Where(i => i.codigo == codFactura).Select(i => new
                    {
                        i.codigo,
                        i.id,
                        i.numeroid,
                        i.fecha,
                        i.codcliente,
                        i.nomcliente,
                        i.nit,
                        i.total,
                        i.codmoneda,
                        i.email,
                        i.nrofactura
                    }).FirstOrDefaultAsync();
                    if (DTFC == null)
                    {
                        return BadRequest(new { resp = "No se encontraron datos con el codigo de proform, consulte con el administrador del sistema." });
                    }
                    string titulo = "Pertec SRL le envia adjunto su factura de compra Nro.: " + DTFC.nrofactura.ToString();


                    string detalle = "Señor:";
                    detalle += Environment.NewLine + DTFC.nomcliente;
                    detalle += Environment.NewLine + "Presente.-";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "Pertec S.R.L. informa que el día de hoy se emitió la factura electrónica adjunta al presente";
                    detalle += Environment.NewLine + "mensaje. Dicho documento puede ser impreso y utilizado como un documento válido para Crédito Fiscal.";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "Si tiene problemas en descargar la factura, también puede obtenerla desde nuestra página web:";
                    detalle += Environment.NewLine + "www.pertec.com.bo o a través de su Whatsapp.";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "En caso de consultas o errores, por favor comunicarse dentro del mes de emisión de la factura";
                    detalle += Environment.NewLine + "con su ejecutivo de ventas y/o Departamento de Servicio al Cliente.";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "Agradeciendo su preferencia, nos es grato saludarlo.";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "Atentamente,";
                    detalle += Environment.NewLine + "PERTEC S.R.L.";

                    string direcc_mail_cliente = DTFC.email;
                    byte[] pdfBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        await pdfFile.CopyToAsync(memoryStream);
                        pdfBytes = memoryStream.ToArray();
                    }

                    string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string pathDirectory = Path.Combine(currentDirectory, "certificado");
                    // rutaFacturaXmlSigned = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certificado", nomArchivoXML);
                    byte[] xmlFile = System.IO.File.ReadAllBytes(Path.Combine(pathDirectory, nomArchXML));

                    // solo por pruebas cambiaremos el email destino del cliente por uno de nosotros, comentar en produccion
                    direcc_mail_cliente = "analista.nal.informatica2@pertec.com.bo";

                    var resultado = await funciones.EnviarEmailFacturas(direcc_mail_cliente, _email_origen_credencial, _pwd_email_credencial_origen, titulo, detalle, pdfBytes, pdfFile.FileName, xmlFile, nomArchXML, true);
                    if (resultado.result == false)
                    {
                        // envio fallido
                        string mi_msg = "No se pudo enviar la factura y el archivo XML al email del cliente!!!";
                        eventos.Add(mi_msg);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), "", "", _controllerName, mi_msg, Log.TipoLog_Siat.Creacion);
                        return BadRequest(new { eventos = mi_msg, resp = resultado.msg });

                    }
                    string mi_msg1 = "La factura y el archivo XML fueron enviados exitosamente al email del cliente!!!";
                    eventos.Add(mi_msg1);
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), "", "", _controllerName, mi_msg1, Log.TipoLog_Siat.Creacion);

                    return Ok(new { eventos = mi_msg1, resp = "" });

                }

            }
            catch (Exception)
            {

                throw;
            }
        }
        [HttpPost]
        [Route("grabarFacturaTienda/{userConn}")]
        public async Task<ActionResult<IEnumerable<object>>> grabarFacturaTienda(string userConn, dataCrearGrabarFacturaTienda dataFactura)
        {
            // Validar que los parámetros no sean nulos ni vacíos
            // Validar si userConn es nulo o vacío, ya que es parte de la ruta
            if (string.IsNullOrWhiteSpace(userConn))
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'UserConn'." });
            }
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                bool opcion_automatico = false;
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var validacion = await ValidarDataRecibida(_context, dataFactura);
                    if (validacion.resultado == false)
                    {
                        return BadRequest(new { resp = validacion.msg });
                    }
                    //VALIDAR EL DETALLE DE LA FACTURA RECIBIDA
                    var validacion_detalle = await Validar_Detalle(_context, dataFactura);
                    if (validacion_detalle.resp == false)  // quiere decir que hay un problema con la Validacion
                    {
                        if (validacion_detalle.objContra == null)
                        {
                            return BadRequest(new { resp = validacion_detalle.msg });
                        }
                        else
                        {
                            // si el objeto no es nulo, significa que requiere de contraseña y los datos se mandan al front
                            return StatusCode(203, new
                            {
                                resp = validacion_detalle.msg,
                                valido = validacion_detalle.resp,
                                objContra = validacion_detalle.objContra
                            });
                        }
                    }
                    //Aqui se separo la funcion Validar_Datos_DSD_JULIO2021 del SIA donde de igual manera hace lo mismo, primero valida los datos de la cabecer y luego los controles del SIA
                    //VALIDAR LA CABECERA DE LA FACTURA RECIBIDA
                    var validacion_cabecera = await Validar_Datos_Cabecera(_context, dataFactura);
                    if (validacion_cabecera.resp == false)  // quiere decir que hay un problema con la Validacion
                    {
                        if (validacion_cabecera.objContra == null)
                        {
                            return BadRequest(new { resp = validacion_cabecera.msg });
                        }
                        else
                        {
                            // si el objeto no es nulo, significa que requiere de contraseña y los datos se mandan al front
                            return StatusCode(203, new
                            {
                                resp = validacion_cabecera.msg,
                                valido = validacion_cabecera.resp,
                                objContra = validacion_cabecera.objContra
                            });
                        }
                    }

                    //VALIDAR TODO EL DOCUMENTO DE LA FACTURA RECIBIDA
                    var validacion_documento = await Validar_Documento(_context, userConn, "grabar", false, "", dataFactura);// Validar_Documento
                    if (validacion_documento.resp == false)  // quiere decir que hay un problema con la Validacion
                    {
                        if (validacion_documento.objContra == null)
                        {
                            return BadRequest(new { resp = validacion_documento.msg });
                        }
                        else
                        {
                            // si el objeto no es nulo, significa que requiere de contraseña y los datos se mandan al front
                            return StatusCode(203, new
                            {
                                resp = validacion_documento.msg,
                                valido = validacion_documento.resp,
                                objContra = validacion_documento.objContra
                            });
                        }
                    }
                    //justo antes de grabar se verifica si se generan saldos negativos
                    //VALIDAR NEGATIVOS
                    var validacion_negativos = await Validar_Negativos(_context, false, dataFactura);
                    if (validacion_negativos.resp == false)  // quiere decir que hay un problema con la Validacion
                    {
                        if (validacion_negativos.objContra == null)
                        {
                            return BadRequest(new { resp = validacion_negativos.msg });
                        }
                        else
                        {
                            // si el objeto no es nulo, significa que requiere de contraseña y los datos se mandan al front
                            return StatusCode(203, new
                            {
                                resp = validacion_negativos.msg,
                                valido = validacion_negativos.resp,
                                objContra = validacion_negativos.objContra
                            });
                        }
                    }

                    // COMO GRABA DESDE ACA, DESDE ACA INICIAMOS EL COMMIT 
                    List<int> codFacturas = new List<int>();
                    List<string> msgAlertas = new List<string>();
                    List<string> eventosLog = new List<string>();
                    bool se_creo_factura = false;
                    using (var dbContexTransaction = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            //var resultados = await Grabar_Documento(_context, dataFactura.idfactura, dataFactura.nrocaja, dataFactura.factnit, dataFactura.condicion,
                            //dataFactura.nrolugar, dataFactura.tipo, dataFactura.codtipo_comprobante, dataFactura.usuario, dataFactura.codempresa, dataFactura.codtipopago,
                            //dataFactura.codbanco, dataFactura.codcuentab, dataFactura.nrocheque, dataFactura.idcuenta, dataFactura.cufd, dataFactura.complemento_ci,
                            //dataFactura.ids_proforma, dataFactura.nro_id_proforma, dataFactura.cabecera, dataFactura.detalle, dataFactura.detalleDescuentos_fact,dataFactura.detalleRecargos_fact, dataFactura.dgvfacturas

                            var resultados = await Grabar_Documento(_context, dataFactura.idfactura, dataFactura.nrocaja, dataFactura.factnit, dataFactura.condicion,
                            dataFactura.nrolugar, dataFactura.tipo, dataFactura.codtipo_comprobante, dataFactura.usuario, dataFactura.codempresa, dataFactura.codtipopago,
                            dataFactura.codbanco, dataFactura.codcuentab, dataFactura.nrocheque, dataFactura.idcuenta, dataFactura.cufd, dataFactura.complemento_ci,
                            dataFactura.ids_proforma, dataFactura.nro_id_proforma, dataFactura.cabecera, dataFactura.detalle, dataFactura.detalleDescuentos_fact, dataFactura.detalleRecargos_fact, dataFactura.dgvfacturas

                            );

                            if (resultados.resul == false)
                            {
                                await dbContexTransaction.RollbackAsync();
                                resultados.eventos.Add("La factura no pude ser grabada por lo cual no se envio al SIN!!!");
                                return BadRequest(new
                                {
                                    resp = "Ocurrio algun error al grabar la factura verifique los resultados de la facturacion!!!",
                                    resultados.resul,
                                    resultados.msgAlertas,
                                    resultados.eventos
                                });
                            }
                            else
                            {
                                await dbContexTransaction.CommitAsync();
                                codFacturas = resultados.CodFacturas_Grabadas;
                                msgAlertas = resultados.msgAlertas;
                                eventosLog = resultados.eventos;
                                se_creo_factura = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Mesaje de error al intentar guardar facturas: " + ex.ToString());
                            await dbContexTransaction.RollbackAsync();
                            return BadRequest(new
                            {
                                resp = "Ocurrio algun error al grabar la factura verifique los resultados de la facturacion!!!"
                            });
                        }
                    }
                    // HASTA ACA EL COMMIT 

                    // variables para devolver de XML
                    string nomArchivoXML = "";
                    bool imprime = false;


                    if (se_creo_factura)
                    {
                        try
                        {
                            int codalmacen = dataFactura.cabecera.codalmacen;
                            var datos_certificado_digital = await Definir_Certificado_A_Utilizar(_context, codalmacen, dataFactura.codempresa);
                            if (datos_certificado_digital.result == false)
                            {
                                datos_certificado_digital.eventos.Add("No se pudo obtener la informacion del certificado digital.");
                                // NO DEBE DE DEVOLVER, DEBE CONTINUAR CON LA LOGICA.
                                /*
                                return BadRequest(new
                                {
                                    resp = "Ocurrio algun error al definir el certificado digital para la firma del XML!!!",
                                    datos_certificado_digital.result,
                                    datos_certificado_digital.msgAlertas,
                                    datos_certificado_digital.eventos
                                });
                                */
                                msgAlertas.Add("Ocurrio algun error al definir el certificado digital para la firma del XML!!!");

                                msgAlertas.AddRange(datos_certificado_digital.msgAlertas);
                                eventosLog.AddRange(datos_certificado_digital.eventos);

                            }
                            else
                            {
                                //comenzar a generar el xml
                                var xml_generado = await GENERAR_XML_FACTURA_FIRMAR_ENVIAR(_context, codFacturas, dataFactura.codempresa, dataFactura.usuario,
                                codalmacen, datos_certificado_digital.ruta_certificado, datos_certificado_digital.Clave_Certificado_Digital, dataFactura.codigo_control);

                                if (xml_generado.resul == false)
                                {
                                    xml_generado.eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - No se pudo generar el archivo XML de la factura, Firmar y enviar al SIN!!!");
                                    xml_generado.eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - El proceso de Firmado de la factura y Envio al SIN termino con errores, verifique esta situacion!!!");
                                    // unimos los logs y mensajes generados a las listas base para que se muestren en secuencia.
                                    msgAlertas.Add("Ocurrio algun error al Generar el XML de la factura verifique los resultados de la facturacion!!!");
                                    // NO DEBE DE DEVOLVER, DEBE CONTINUAR CON LA LOGICA.
                                    /*
                                    return BadRequest(new
                                    {
                                        resp = "Ocurrio algun error al Generar el XML de la factura verifique los resultados de la facturacion!!!",
                                        imprime = xml_generado.resul,
                                        xml_generado.msgAlertas,
                                        xml_generado.eventos
                                    });
                                    */
                                }
                               

                                msgAlertas.AddRange(xml_generado.msgAlertas);
                                eventosLog.AddRange(xml_generado.eventos);
                                nomArchivoXML = xml_generado.nomArchivoXML;
                                imprime = xml_generado.resul;

                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Mesaje de error al intentar generar XML factura: " + ex.ToString());
                            msgAlertas.Add("Mesaje de error al intentar generar XML factura: " + ex.ToString());
                            eventosLog.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - Ocurrio algun error al generar el XML de la factura verifique los resultados de la facturacion!!!");
                            /*
                            return BadRequest(new
                            {
                                resp = "Ocurrio algun error al generar el XML de la factura verifique los resultados de la facturacion!!!"
                            });
                            */
                        }
                    }

                    // CAMBIAR A HABITUAL A LOS CLIENTES POR SU COMPRA
                    if (dataFactura.cabecera.codcliente != "HABITUAL")
                    {
                        await cliente.MarcarClienteHabitual(_context, dataFactura.cabecera.codcliente);

                    }

                    List<string> cadena = new List<string>();
                    cadena.Add("Se han generado los datos de facturacion con exito.");
                    cadena.Add("Factura(s): ");
                    int codigo_factura = 0;
                    foreach (var reg in codFacturas)
                    {
                        cadena.Add("* " + await ventas.Datos_Factura_CUF(_context, reg));
                        codigo_factura = reg;
                    }

                    // devolver la cadena en la respuesta

                    bool todoOk = true;
                    var factura = await _context.vefactura.Where(i => i.codigo == codigo_factura).FirstOrDefaultAsync();
                    if (await ventas.cufd_tipofactura(_context, dataFactura.cufd) == 1)
                    {
                        msgAlertas.Add("Se grabo la Factura " + factura.id + "-" + factura.numeroid + " Nro:" + factura.nrofactura + " con Exito. " + Environment.NewLine + " Como la factura es manual, no se imprimira.");

                    }
                    else
                    {
                        msgAlertas.Add("Se grabo la Factura " + factura.id + "-" + factura.numeroid + " Nro:" + factura.nrofactura + " con Exito.");

                        foreach (var reg in codFacturas)
                        {
                            if (await ventas.Factura_Tiene_CUF(_context, reg) == false)
                            {
                                msgAlertas.Add("Al menos una de las facturas no tiene CUF, por favor verifique esto antes de realizar al impresion.");
                                todoOk = false;
                                break;
                            }
                            if (await ventas.Factura_Tiene_CUFD(_context, reg) == false)
                            {
                                msgAlertas.Add("Al menos una de las facturas no tiene CUFD, por favor verifique esto antes de realizar al impresion.");
                                todoOk = false;
                                break;
                            }
                        }
                        if (todoOk)
                        {
                            //Desde 19-06-2023 Al grabar obtener si la proforma del origen de la tienda es una solicitud urgente
                            // si es asi debe enlazar la nota de movimiento en la solicitud urgente

                            if (dataFactura.ids_proforma.Trim().Length > 0 && dataFactura.nro_id_proforma > 0)
                            {
                                var doc_solurgente = await ventas.Solicitud_Urgente_IdNroid_de_Proforma(_context, dataFactura.ids_proforma, dataFactura.nro_id_proforma);
                                string msgAlert = "";
                                string txtid_solurgente = "";
                                int txtnroid_solurgente = 0;
                                if (doc_solurgente.id != "")
                                {
                                    msgAlertas.Add("La factura es de una proforma que es una solicitud urgente!!!");
                                    txtid_solurgente = doc_solurgente.id;
                                    txtnroid_solurgente = doc_solurgente.nroId;
                                    // actualizar la Codfactura_web
                                    var solurgente = await _context.insolurgente.Where(i => i.id == doc_solurgente.id && i.numeroid == doc_solurgente.nroId).FirstOrDefaultAsync();
                                    solurgente.idfc = factura.id;
                                    solurgente.fnumeroid = factura.numeroid;
                                    await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos
                                    await log.RegistrarEvento(_context, dataFactura.usuario, Log.Entidades.SW_Factura, codigo_factura.ToString(), factura.id, factura.numeroid.ToString(), "docvefacturamos_cufdController", "Grabar enlace FC: " + factura.id + "-" + factura.numeroid + " con SU: " + solurgente.id + "-" + solurgente.numeroid, Log.TipoLog.Creacion);
                                    msgAlertas.Add("La factura fue enlazada con la solicitud urgente: " + solurgente.id + "-" + solurgente.numeroid);
                                }


                                //AQUI IMPRIMIR Y ENVIAR LA FACTURA XML
                                cadena.Add("Se procedera a la impresion y envio de la factura al mail del cliente.");
                                //aqui poner la funcion para imprimir y enviar xml al cliente
                            }


                            if (dataFactura.cabecera.tipopago == 1)
                            {
                                // ##### CONTABILIZAR
                                ///si el documento era a credito debe crear la nota de remision
                                //            If funciones.nrdefactura(codigo.Text, sia_funciones.Configuracion.Instancia.usr_idremision(sia_compartidos.temporales.Instancia.usuario), sia_compartidos.temporales.Instancia.codempresa, sia_funciones.Empresa.Instancia.EsArgentina(sia_compartidos.temporales.Instancia.codempresa)) Then

                                //    sia_funciones.Creditos.Instancia.Actualizar_Credito_2023(codcliente.Text, sia_compartidos.temporales.Instancia.usuario, sia_compartidos.temporales.Instancia.codempresa, True)

                                //    '##### CONTABILIZAR
                                //    If sia_funciones.Seguridad.Instancia.rol_contabiliza(sia_funciones.Seguridad.Instancia.usuario_rol(sia_compartidos.temporales.Instancia.usuario)) Then
                                //        If sia_funciones.Configuracion.Instancia.emp_preg_cont_ventascredito(sia_compartidos.temporales.Instancia.codempresa) Then
                                //            If MessageBox.Show("Desea contabilizar este documento ?", "Confirmacion", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) = Windows.Forms.DialogResult.Yes Then
                                //                Dim frm As New sia_compartidos.prgDatosContabilizar
                                //                frm.ShowDialog()
                                //                If frm.eligio Then
                                //                    If frm.nuevo Then
                                //                        sia_funciones.Contabilidad.Instancia.Contabilizar_Venta_a_Credito(sia_compartidos.temporales.Instancia.codempresa, sia_funciones.Empresa.Instancia.monedabase(sia_compartidos.temporales.Instancia.codempresa), sia_funciones.Ventas.Instancia.CodRemisionDeFactura(sia_funciones.Documento.Instancia.Codigo_Factura(id.Text, CInt(numeroid.Text))), frm.id_elegido, frm.tipo_elegido, 0, True, False) ', True, True, False, True)
                                //                    Else
                                //                        sia_funciones.Contabilidad.Instancia.Contabilizar_Venta_a_Credito(sia_compartidos.temporales.Instancia.codempresa, sia_funciones.Empresa.Instancia.monedabase(sia_compartidos.temporales.Instancia.codempresa), sia_funciones.Ventas.Instancia.CodRemisionDeFactura(sia_funciones.Documento.Instancia.Codigo_Factura(id.Text, CInt(numeroid.Text))), "", "", frm.codigo_elegido, True, False) ', True, True, False, True)
                                //                    End If
                                //                Else
                                //                End If
                                //                frm.Dispose()
                                //            End If
                                //        End If
                                //    End If
                                //    '##### FIN CONTABILIZAR
                                //Else '//si no grbao seguir reintentando
                                //    Dim genero As Boolean = False
                                //    Dim abortar As Boolean = False
                                //    While(Not abortar) And(Not genero)
                                //        If funciones.nrdefactura(codigo.Text, sia_funciones.Configuracion.Instancia.usr_idremision(sia_compartidos.temporales.Instancia.usuario), sia_compartidos.temporales.Instancia.codempresa, sia_funciones.Empresa.Instancia.EsArgentina(sia_compartidos.temporales.Instancia.codempresa)) Then
                                //            genero = True
                                //        Else
                                //            genero = False
                                //            If MessageBox.Show("No se pudo Generar la nota de remision correspondiente, Desea volver a intentarlo ?.", "Generar Nota de Remision", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) = Windows.Forms.DialogResult.Yes Then
                                //                abortar = False
                                //            Else
                                //                abortar = True
                                //            End If
                                //        End If
                                //    End While
                                //    If genero Then
                                //        sia_funciones.Creditos.Instancia.Actualizar_Credito_2023(codcliente.Text, sia_compartidos.temporales.Instancia.usuario, sia_compartidos.temporales.Instancia.codempresa, True)

                                //        '##### CONTABILIZAR
                                //        If sia_funciones.Seguridad.Instancia.rol_contabiliza(sia_funciones.Seguridad.Instancia.usuario_rol(sia_compartidos.temporales.Instancia.usuario)) Then
                                //            If sia_funciones.Configuracion.Instancia.emp_preg_cont_ventascredito(sia_compartidos.temporales.Instancia.codempresa) Then
                                //                If MessageBox.Show("Desea contabilizar este documento ?", "Confirmacion", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) = Windows.Forms.DialogResult.Yes Then
                                //                    Dim frm As New sia_compartidos.prgDatosContabilizar
                                //                    frm.ShowDialog()
                                //                    If frm.eligio Then
                                //                        If frm.nuevo Then
                                //                            sia_funciones.Contabilidad.Instancia.Contabilizar_Venta_a_Credito(sia_compartidos.temporales.Instancia.codempresa, sia_funciones.Empresa.Instancia.monedabase(sia_compartidos.temporales.Instancia.codempresa), sia_funciones.Ventas.Instancia.CodRemisionDeFactura(sia_funciones.Documento.Instancia.Codigo_Factura(id.Text, CInt(numeroid.Text))), frm.id_elegido, frm.tipo_elegido, 0, True, False) ', True, True, False, True)
                                //                        Else
                                //                            sia_funciones.Contabilidad.Instancia.Contabilizar_Venta_a_Credito(sia_compartidos.temporales.Instancia.codempresa, sia_funciones.Empresa.Instancia.monedabase(sia_compartidos.temporales.Instancia.codempresa), sia_funciones.Ventas.Instancia.CodRemisionDeFactura(sia_funciones.Documento.Instancia.Codigo_Factura(id.Text, CInt(numeroid.Text))), "", "", frm.codigo_elegido, True, False) ', True, True, False, True)
                                //                        End If
                                //                    Else
                                //                    End If
                                //                    frm.Dispose()
                                //                End If
                                //            End If
                                //        End If
                                //        '##### FIN CONTABILIZAR
                                //    End If

                                //End If
                                // ##### FIN CONTABILIZAR
                            }
                            else
                            {
                                //'contado
                                //'##### CONTABILIZAR
                                //If sia_funciones.Seguridad.Instancia.rol_contabiliza(sia_funciones.Seguridad.Instancia.usuario_rol(sia_compartidos.temporales.Instancia.usuario)) Then
                                //    If sia_funciones.Configuracion.Instancia.emp_preg_cont_ventascontado(sia_compartidos.temporales.Instancia.codempresa) Then
                                //        If MessageBox.Show("Desea contabilizar este documento ?", "Confirmacion", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) = Windows.Forms.DialogResult.Yes Then
                                //            Dim frm As New sia_compartidos.prgDatosContabilizar
                                //            frm.ShowDialog()
                                //            If frm.eligio Then
                                //                If frm.nuevo Then
                                //                    sia_funciones.Contabilidad.Instancia.Contabilizar_Venta_a_Contado(sia_compartidos.temporales.Instancia.codempresa, sia_funciones.Empresa.Instancia.monedabase(sia_compartidos.temporales.Instancia.codempresa), sia_funciones.Documento.Instancia.Codigo_Factura(id.Text, CInt(numeroid.Text)), frm.id_elegido, frm.tipo_elegido, 0, True)
                                //                Else
                                //                    sia_funciones.Contabilidad.Instancia.Contabilizar_Venta_a_Contado(sia_compartidos.temporales.Instancia.codempresa, sia_funciones.Empresa.Instancia.monedabase(sia_compartidos.temporales.Instancia.codempresa), sia_funciones.Documento.Instancia.Codigo_Factura(id.Text, CInt(numeroid.Text)), "", "", frm.codigo_elegido, True)
                                //                End If
                                //            Else
                                //            End If
                                //            frm.Dispose()
                                //        End If
                                //    End If
                                //End If
                                //'##### FIN CONTABILIZAR
                            }


                        }
                    }


                    // limpia los datos de la dosificacion CUFD activa
                    // limpiar.PerformClick()


                    return Ok(new
                    {
                        resp = "Facturas registras con Exito",
                        imprime,
                        nomArchivoXML,   // se envia nombre del archivo xml para que nos lo devuelva
                        codFactura = codFacturas[0],  // De momento solo enviamnos el primer codigo de factura para que nos lo devuelva
                        cadena,
                        msgAlertas,
                        eventosLog
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }

        [HttpPost]
        //[Route("validarProforma/{userConn}/{cadena_controles}/{entidad}/{opcion_validar}")]
        [Route("validarFacturaTienda/{userConn}/{cadena_controles}/{entidad}/{opcion_validar}/{codempresa}/{usuario}")]
        //Task<ActionResult<itemDataMatriz>>
        //Task<object> ValidarProforma
        //para opcion_validar
        //grabar
        //grabar_aprobar
        //para entidad
        //proforma
        //remision no se usa
        //factura
        //para cadena_controles
        // vacio si no va controlar controles en especifico
        // cadena con el siguiente formato 00001+00002+00003 con los controles en especifico que se quiere controlar
        public async Task<ActionResult<List<Controles>>> validarFacturaTienda(string userConn, string cadena_controles, string entidad, string opcion_validar, string codempresa, string usuario, RequestValidacion RequestValidacion)
        {
            // Validar que los parámetros no sean nulos ni vacíos
            // Validar si userConn es nulo o vacío, ya que es parte de la ruta
            if (string.IsNullOrWhiteSpace(userConn))
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'UserConn'." });
            }
            if (string.IsNullOrWhiteSpace(codempresa))
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Empresa'." });
            }
            if (string.IsNullOrWhiteSpace(usuario))
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Usuario'." });
            }
            if (string.IsNullOrWhiteSpace(cadena_controles))
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Cadena Controles'." });
            }
            if (string.IsNullOrWhiteSpace(entidad))
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Entidad'." });
            }
            if (string.IsNullOrWhiteSpace(opcion_validar))
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Opcion Validar'." });
            }
            if (RequestValidacion == null)
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Request Validacion'." });
            }
            if (RequestValidacion.datosDocVta == null)
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Request Validacion - DatosDocVta'." });
            }
            if (RequestValidacion.detalleEtiqueta == null)
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Request Validacion - Detalle Etiqueta'." });
            }
            if (RequestValidacion.detalleDescuentos == null)
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Request Validacion - Detalle Descuentos'." });
            }
            if (RequestValidacion.detalleAnticipos == null)
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Request Validacion - Detalle Anticipos'." });
            }
            if (RequestValidacion.detalleControles == null)
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Request Validacion - Detalle Controles'." });
            }
            if (RequestValidacion.detalleItemsProf == null)
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Request Validacion - Detalle Items'." });
            }
            if (RequestValidacion.detalleRecargos == null)
            {
                return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Request Validacion - Detalle Recargos'." });
            }

            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                DatosDocVta datosDocVta = new DatosDocVta();
                List<itemDataMatriz> itemDataMatriz = new List<itemDataMatriz>();
                List<vedesextraDatos>? vedesextraDatos = new List<vedesextraDatos>();
                List<vedetalleEtiqueta> vedetalleEtiqueta = new List<vedetalleEtiqueta>();
                List<vedetalleanticipoProforma>? vedetalleanticipoProforma = new List<vedetalleanticipoProforma>();
                List<verecargosDatos>? verecargosDatos = new List<verecargosDatos>();
                List<Controles>? controles_recibidos = new List<Controles>();

                datosDocVta = RequestValidacion.datosDocVta;
                itemDataMatriz = RequestValidacion.detalleItemsProf;
                vedesextraDatos = RequestValidacion.detalleDescuentos;
                vedetalleEtiqueta = RequestValidacion.detalleEtiqueta;
                vedetalleanticipoProforma = RequestValidacion.detalleAnticipos;
                verecargosDatos = RequestValidacion.detalleRecargos;
                controles_recibidos = RequestValidacion.detalleControles;

                var resultado = await validar_Vta.DocumentoValido(userConnectionString, cadena_controles, entidad, opcion_validar, datosDocVta, itemDataMatriz, vedesextraDatos, vedetalleEtiqueta, vedetalleanticipoProforma, verecargosDatos, controles_recibidos, codempresa, usuario);
                resultado = resultado.Select(p => { p.CodServicio = p.CodServicio == "" ? "0" : p.CodServicio; return p; }).ToList();
                if (resultado != null)
                {
                    ///
                    string jsonResult = JsonConvert.SerializeObject(resultado);

                    return Ok(jsonResult);
                }
                else { return BadRequest(new { resp = "No se pudo validar la factura." }); }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al validar Factura: " + ex.Message);
                throw;
            }

        }
        private async Task<List<dataDetalleFactura>> distribuir_recargos(DBContext _context, string codempresa, double subtotal, List<dataDetalleFactura> tabladetalle, List<verecargosDatos> tablarecargos)
        {
            //    Me.verrecargos()
            //Me.versubtotal()
            //Me.verdesextra()
            //Me.vertotal()
            double totalrecargo = 0;
            foreach (var reg in tablarecargos)
            {
                if (reg.porcen > 0)
                {
                    // se cambio en fech 01 - 05 - 2017
                    totalrecargo = totalrecargo + (subtotal / 100) * (double)reg.porcen;
                }
                else//convertir el monto a porcentaje del documento y asi distribuir
                {
                    totalrecargo = totalrecargo + (double)reg.montodoc;
                }
            }

            double montorecar = 0;
            double montosubtotal = 0;
            double montorest = 0;
            int index = 0;
            montorecar = totalrecargo;
            montorest = totalrecargo;
            montosubtotal = subtotal;

            foreach (var reg in tabladetalle)
            {
                if (index < (tabladetalle.Count() - 1))
                {
                    reg.distrecargo = (decimal)await siat.Redondear_SIAT(_context, codempresa, (montorecar * (double)reg.total) / montosubtotal);
                    montorest = montorest - (double)reg.distrecargo;
                    reg.preciodist = reg.precioneto + ((reg.distrecargo - reg.distdescuento) / reg.cantidad);
                    reg.totaldist = (reg.precioneto * reg.cantidad) + (reg.distrecargo - reg.distdescuento);
                }
                else  ///ponerle al el ultimo item todo lo que reste
                {
                    reg.distrecargo = (decimal)montorest;
                    reg.preciodist = reg.precioneto + ((reg.distrecargo - reg.distdescuento) / reg.cantidad);
                    reg.totaldist = (reg.precioneto * reg.cantidad) + (reg.distrecargo - reg.distdescuento);
                }
                reg.distrecargo = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)reg.distrecargo);
                reg.preciodist = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)reg.preciodist);
                reg.totaldist = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)reg.totaldist);
                index++;
            }
            return tabladetalle;

        }

        private async Task<List<dataDetalleFactura>> distribuir_descuentos_prorateo(DBContext _context, string codempresa, double subtotal, List<dataDetalleFactura> tabladetalle, List<vedesextraDatos> tabladescuentos)
        {
            //    Me.verrecargos()
            //Me.versubtotal()
            //Me.verdesextra()
            //Me.vertotal()

            double totaldescuento = 0;
            foreach (var reg in tabladescuentos)
            {
                if (reg.porcen > 0)
                {
                    // se cambio en fech 01 - 05 - 2017
                    totaldescuento += (double)reg.montodoc;
                }
                else
                {
                    totaldescuento = totaldescuento + (double)reg.montodoc;
                }
            }

            double montodesc = 0;
            double montosubtotal = 0;
            double montorest = 0;
            int index = 0;
            montodesc = totaldescuento;
            montorest = totaldescuento;
            montosubtotal = subtotal;

            foreach (var reg in tabladetalle)
            {
                if (index < (tabladetalle.Count() - 1))
                {
                    reg.distdescuento = (decimal)await siat.Redondear_SIAT(_context, codempresa, (montodesc * (double)reg.total) / montosubtotal);
                    montorest = montorest - (double)reg.distdescuento;
                    reg.preciodist = reg.precioneto + ((reg.distrecargo - reg.distdescuento) / reg.cantidad);
                    reg.totaldist = (reg.precioneto * reg.cantidad) + (reg.distrecargo - reg.distdescuento);
                }
                else  ///ponerle al el ultimo item todo lo que reste
                {
                    reg.distdescuento = (decimal)montorest;
                    reg.preciodist = reg.precioneto + ((reg.distrecargo - reg.distdescuento) / reg.cantidad);
                    reg.totaldist = (reg.precioneto * reg.cantidad) + (reg.distrecargo - reg.distdescuento);
                }
                reg.distdescuento = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)reg.distdescuento);
                reg.preciodist = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)reg.preciodist);
                reg.totaldist = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)reg.totaldist);
                index++;
            }
            return tabladetalle;
        }

        private async Task<Total_Detalle_Factura> Totalizar_Detalle_Factura(DBContext _context, List<dataDetalleFactura> detalle)
        {
            Total_Detalle_Factura resultado = new Total_Detalle_Factura();

            foreach (var reg in detalle)
            {
                resultado.Total_factura += (double)reg.total;
                resultado.Total_Dist += (double)reg.totaldist;
                resultado.total_iva += (double)((reg.totaldist / 100) * reg.porceniva);
            }
            resultado.Total_factura = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultado.Total_factura);
            resultado.Total_Dist = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultado.Total_Dist);
            resultado.total_iva = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultado.total_iva);

            resultado.Desctos = resultado.Total_factura - resultado.Total_Dist;
            resultado.Desctos = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultado.Desctos);
            resultado.Recargos = 0;
            resultado.Total_Dist = resultado.Total_Dist + resultado.total_iva;

            return resultado;
        }
        private async Task<double> total_factura(DBContext _context, string codcliente, List<dataDetalleFactura> tabladetalle)
        {
            double resultado = 0;
            double total = 0;
            double total_iva = 0;
            foreach (var reg in tabladetalle)
            {
                total = total + (double)reg.totaldist;
                total_iva = total_iva + (double)((reg.totaldist / 100) * reg.porceniva);
            }
            resultado = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, total) + (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, total_iva);
            return resultado;
        }
        private async Task<(bool resul, List<string> msgAlertas, List<string> eventos, List<int> CodFacturas_Grabadas)> Grabar_Documento(DBContext _context, string idfactura, int nrocaja, string factnit, string condicion, string nrolugar, string tipo, string codtipo_comprobante, string usuario, string codempresa, int codtipopago, string codbanco, string codcuentab, string nrocheque, string idcuenta, string cufd, string complemento_ci, string ids_proforma, int nro_id_proforma, vefactura cabecera, List<dataDetalleFactura> tabladetalle, List<vedesextraDatos> tabladescuentos, List<verecargosDatos> tablarecargos, List<tablaFacturas> dgvfacturas)
        //private async Task<(bool resul, List<string> msgAlertas, List<string> eventos, List<int> CodFacturas_Grabadas)> Grabar_Documento(DBContext _context, string idfactura, int nrocaja, string factnit, string condicion, string nrolugar, string tipo, string codtipo_comprobante, string usuario, string codempresa, int codtipopago, string codbanco, string codcuentab, string nrocheque, string idcuenta, string cufd, string complemento_ci, string ids_proforma, int nro_id_proforma, vefactura cabecera, List<dataDetalleFactura> tabladetalle, List<vedesextraDatos> tabladescuentos, List<verecargosDatos> tablarecargos, List<tablaFacturas> dgvfacturas)
        {
            // para devolver lista de registros logs
            List<string> eventos = new List<string>();
            List<string> msgAlertas = new List<string>();
            List<int> CodFacturas_Grabadas = new List<int>();
            string msgAlert = "";

            try
            {

                bool resultado = true;
                bool descarga = false;

                int cuantas = tabladetalle.Count();
                int idnroactual = 0;
                int nrofactura = 0;
                int numeroid = 0;
                int numero = 0;

                // obtener los valores actualizado segun la dosificacion(por si se cambio el CUFD)
                Datos_Dosificacion_Activa datos_dosificacion_activa = new Datos_Dosificacion_Activa();
                datos_dosificacion_activa = await siat.Obtener_Cufd_Dosificacion_Activa(_context, await funciones.FechaDelServidor(_context), cabecera.codalmacen);
                string msg = "";  // PARA DEVOLVER ESTA COSAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
                string codigo_control = "";
                DateTime dtpfecha_limite = DateTime.Now.Date;
                if (datos_dosificacion_activa.cufd.Trim().Length > 0)
                {
                    // eso para detectar si hay cambio de CUFD (cuando en el mismo dia se genera otro CUFD)
                    if (cufd != datos_dosificacion_activa.cufd.Trim())
                    {
                        // eso para detectar si hay cambio de CUFD (cuando en el mismo dia se genera otro CUFD)
                        msg = "El CUFD activo para fecha: " + (await funciones.FechaDelServidor(_context)).ToShortDateString() + " Ag: " + cabecera.codalmacen + " ha Cambiado!!!, confirme esta situacion.";
                        msgAlertas.Add(msg);
                    }
                    /*
                    cufd = datos_dosificacion_activa.cufd.Trim();
                    nrocaja = (short)datos_dosificacion_activa.nrocaja;
                    codigo_control = datos_dosificacion_activa.codcontrol.Trim();
                    dtpfecha_limite = datos_dosificacion_activa.fechainicio.Date;
                    */
                    /////
                    cufd = await ventas.caja_CUFD(_context, nrocaja);
                    dtpfecha_limite = await ventas.cufd_fechalimiteDate(_context, cufd);
                    nrofactura = await ventas.caja_numerofactura(_context, nrocaja);
                    nrolugar = await ventas.caja_numeroLugar(_context, nrocaja);
                    tipo = await ventas.caja_tipo(_context, nrocaja);
                    codtipo_comprobante = await ventas.caja_tipocomprobante(_context, nrocaja);
                    codigo_control = datos_dosificacion_activa.codcontrol.Trim();
                    // si hay datos de dosificacion
                    string alerta = await ventas.alertadosificacion(_context, nrocaja);
                    if (alerta.Length > 0)
                    {
                        msg = alerta;
                        msgAlertas.Add(msg);
                        return (false, msgAlertas, eventos, CodFacturas_Grabadas);
                    }
                }
                else
                {
                    // si no hay CUFD no se puede grabar la factura
                    msg = "No se econtro una dosificacion de CUFD activa para fecha: " + (await funciones.FechaDelServidor(_context)).ToShortDateString() + " Ag: " + cabecera.codalmacen;
                    msgAlertas.Add(msg);
                    return (false, msgAlertas, eventos, CodFacturas_Grabadas);
                }

                //*****************************************
                tabladetalle = await distribuir_descuentos_prorateo(_context, codempresa, (double)cabecera.subtotal, tabladetalle, tabladescuentos);
                // No_Distribuir_Descuentos(codigoremision)
                // acca devuelve detalle modificado
                tabladetalle = await distribuir_recargos(_context, codempresa, (double)cabecera.subtotal, tabladetalle, tablarecargos);
                ///////////////////////////////////////////////////
                int NROITEMS = tabladetalle.Count();
                var ITEMSPORHOJA = await ventas.numitemscaja(_context, nrocaja);
                List<tablaFacturas> lista = new List<tablaFacturas>();  // DEVOLVER LISTA
                double totfactura = 0;
                //int NumHojas = (NROITEMS % ITEMSPORHOJA == 0) ?
                //   (int)Math.Floor((double)NROITEMS / ITEMSPORHOJA) :
                //   (int)Math.Floor((double)NROITEMS / ITEMSPORHOJA) + 1;
                if (ITEMSPORHOJA >= NROITEMS)
                {
                    //*******************CALCULAR TOTALES Y SUBTOTALES SIN 24-03-2022
                    // la nota se emite en una sola HOJA
                    /*
                    subtotalNR.Text = CDbl(cabecera.Rows(0)("subtotal")).ToString("####,##0.000", new CultureInfo("en-US"))
                    totdesctosNR.Text = CDbl(cabecera.Rows(0)("descuentos")).ToString("####,##0.000", new CultureInfo("en-US"))
                    totrecargosNR.Text = CDbl(cabecera.Rows(0)("recargos")).ToString("####,##0.000", new CultureInfo("en-US"))
                    totremision.Text = CDbl(cabecera.Rows(0)("total")).ToString("####,##0.000", new CultureInfo("en-US"))
                     */

                    // obtener el total final de la factura del detalle (sumatoria de totales de items)

                    Total_Detalle_Factura _TTLFACTURA = new Total_Detalle_Factura();
                    _TTLFACTURA = await Totalizar_Detalle_Factura(_context, tabladetalle);
                    // añadir a la lista
                    tablaFacturas registro = new tablaFacturas();
                    registro.nro = 1;
                    registro.subtotal = _TTLFACTURA.Total_factura;
                    registro.descuentos = _TTLFACTURA.Desctos;
                    registro.recargos = _TTLFACTURA.Recargos;
                    registro.total = registro.subtotal - registro.descuentos;

                    if (await cliente.DiscriminaIVA(_context, cabecera.codcliente))
                    {
                        registro.iva = await siat.Redondear_SIAT(_context, codempresa, (double)(cabecera.iva ?? 0));
                        registro.iva = Math.Round(registro.iva, 2, MidpointRounding.AwayFromZero);

                        registro.monto = await siat.Redondear_SIAT(_context, codempresa, (registro.total - registro.iva));
                        registro.monto = Math.Round(registro.monto, 2, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        registro.iva = 0;
                        registro.monto = await siat.Redondear_SIAT(_context, codempresa, registro.total);
                        registro.monto = Math.Round(registro.monto, 2, MidpointRounding.AwayFromZero);
                    }

                    registro.subtotal = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, registro.subtotal);
                    registro.descuentos = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, registro.descuentos);
                    registro.recargos = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, registro.recargos);
                    registro.total = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, registro.total);

                    lista.Add(registro);
                    totfactura = _TTLFACTURA.Total_Dist;

                }
                else
                {
                    totfactura = await total_factura(_context, cabecera.codcliente, tabladetalle);
                }

                if (resultado)
                {
                    //Validacion dsd 07 - 12 - 2022, en ocasiones existian facturas grabadas sin precios valor 0 en el detalle y en la cabecera de la factura en subtotal y total con valor 0
                    foreach (var detalle in tabladetalle)
                    {

                        if (detalle.precioneto <= 0)
                        {
                            resultado = false;
                            msgAlert = "No puso el PrecioN en la Linea " + tabladetalle.IndexOf(detalle) + 1 + ", Item: " + detalle.coditem + ".";
                            msgAlertas.Add(msgAlert);
                            return (resultado, msgAlertas, eventos, CodFacturas_Grabadas);
                        }
                        else if (detalle.preciolista <= 0)
                        {
                            resultado = false;
                            msgAlert = "No puso el PrecioL en la Linea " + tabladetalle.IndexOf(detalle) + 1 + ", Item: " + detalle.coditem + ".";
                            msgAlertas.Add(msgAlert);
                            return (resultado, msgAlertas, eventos, CodFacturas_Grabadas);
                        }
                        else if (detalle.preciodesc <= 0)
                        {
                            resultado = false;
                            msgAlert = "No puso el PrecioDc en la Linea " + tabladetalle.IndexOf(detalle) + 1 + ", Item: " + detalle.coditem + ".";
                            msgAlertas.Add(msgAlert);
                            return (resultado, msgAlertas, eventos, CodFacturas_Grabadas);
                        }
                        else if (detalle.total <= 0)
                        {
                            resultado = false;
                            msgAlert = "No puso el Total en la Linea " + tabladetalle.IndexOf(detalle) + 1 + ", Item: " + detalle.coditem + ".";
                            msgAlertas.Add(msgAlert);
                            return (resultado, msgAlertas, eventos, CodFacturas_Grabadas);
                        }
                        else if (detalle.preciodist <= 0)
                        {
                            resultado = false;
                            msgAlert = "No puso el PrecioDt en la Linea " + tabladetalle.IndexOf(detalle) + 1 + ", Item: " + detalle.coditem + ".";
                            msgAlertas.Add(msgAlert);
                            return (resultado, msgAlertas, eventos, CodFacturas_Grabadas);
                        }
                        else if (detalle.totaldist <= 0)
                        {
                            resultado = false;
                            msgAlert = "No puso el TotalD en la Linea " + tabladetalle.IndexOf(detalle) + 1 + ", Item: " + detalle.coditem + ".";
                            msgAlertas.Add(msgAlert);
                            return (resultado, msgAlertas, eventos, CodFacturas_Grabadas);
                        }
                    }
                    foreach (var l in lista)
                    {
                        if (l.subtotal == 0 || l.total == 0)
                        {
                            resultado = false;
                            msgAlert = "No se puede grabar una factura con subtotal y/o total 0.";
                            msgAlertas.Add(msgAlert);
                            return (resultado, msgAlertas, eventos, CodFacturas_Grabadas);
                        }
                    }
                }
                //////////////////////////////////////////////
                //obtener id actual
                idnroactual = await documento.ventasnumeroid(_context, idfactura);
                if (await documento.existe_factura(_context, idfactura, idnroactual + 1))
                {
                    resultado = false;
                    msgAlert = "Ese numero de documento, ya existe, por favor consulte con el administrador del sistema.";
                    msgAlertas.Add(msgAlert);
                    return (resultado, msgAlertas, eventos, CodFacturas_Grabadas);
                }
                //fin de obtener id actual
                if (await ventas.iddescarga(_context, idfactura))
                {
                    descarga = false;
                }
                else
                {
                    descarga = true;
                }
                // ver si me queda dosificacion
                if (await ventas.cufd_fechalimiteDate(_context, cufd) >= cabecera.fecha.Date)
                {
                    nrofactura = await ventas.caja_numerofactura(_context, nrocaja);
                }
                else
                {
                    resultado = false;
                    msgAlert = "La dosificacion ha llegado a su fecha limite.";
                    msgAlertas.Add(msgAlert);
                    return (resultado, msgAlertas, eventos, CodFacturas_Grabadas);
                }
                //verificar una vez mas si el ID existe
                if (await ventas.Existe_ID_Factura(_context, idfactura))
                {
                }
                else
                {
                    resultado = false;
                    msgAlert = "Ese ID de facturacion no existe , Por Favor Seleccione Otro.";
                    msgAlertas.Add(msgAlert);
                    return (resultado, msgAlertas, eventos, CodFacturas_Grabadas);
                }
                //otra verificacion de caja y numero de orden y alfanumerico
                if (nrocaja.ToString() == "")
                {
                    resultado = false;
                }
                else if (cufd == "")
                {
                    resultado = false;
                }
                else if (dtpfecha_limite.ToString() == "")
                {
                    resultado = false;
                }
                else if (codigo_control == "")
                {
                    resultado = false;
                }
                if (resultado == false)
                {
                    resultado = false;
                    msgAlert = "Falta algun dato de la dosificacion por favor revise el # de caja, el # de autorizacion y fecha limite.";
                    msgAlertas.Add(msgAlert);
                    return (resultado, msgAlertas, eventos, CodFacturas_Grabadas);
                }

                Datos_Pametros_Facturacion_Ag Parametros_Facturacion_Ag = new Datos_Pametros_Facturacion_Ag();
                Parametros_Facturacion_Ag = await siat.Obtener_Parametros_Facturacion(_context, cabecera.codalmacen);

                int cod_pf = 0;
                if (ids_proforma.Trim().Length > 0 && nro_id_proforma > 0)
                {
                    cod_pf = await ventas.codproforma(_context, ids_proforma, nro_id_proforma);
                }
                else
                {
                    cod_pf = 0;
                }
                if (cabecera.nit == "0" || cabecera.nit == "00" || cabecera.nit == "000")
                {
                    cabecera.nit = Parametros_Facturacion_Ag.nit_cliente;
                }
                //Dsd 08 - 03 - 2023 controlar si el campo del nit esta vacio obtener por defecto de parametros de facturacion nuevamente
                if (cabecera.nit == "")
                {
                    Parametros_Facturacion_Ag = await siat.Obtener_Parametros_Facturacion(_context, cabecera.codalmacen);
                    cabecera.nit = Parametros_Facturacion_Ag.nit_cliente;
                }
                if (cabecera.idfc.Trim().Length == 0)
                {
                    cabecera.numeroidfc = 0;
                }
                //HACER EL INSERT EN VEFACTURA Y VEFACTURA1

                string valor_CUF = "";
                DateTime fechaServ = await funciones.FechaDelServidor(_context);
                string horaServ = datos_proforma.getHoraActual();
                var versionTariAct = await ventas.VersionTarifaActual(_context);
                var factormeDat = await tipocambio._tipocambio(_context, cabecera.codmoneda, await tipocambio.monedatdc(_context, usuario, codempresa), fechaServ);
                // cadena para insertar
                vefactura vefacturaData = new vefactura
                {
                    leyenda = "",
                    tipo_docid = cabecera.tipo_docid,
                    email = cabecera.email,
                    en_linea_SIN = false,
                    en_linea = false,

                    cufd = cufd,
                    cuf = valor_CUF,
                    complemento_ci = complemento_ci,
                    tipo_venta = 0,
                    codproforma = cod_pf,

                    refacturar = false,
                    estado_contra_entrega = cabecera.estado_contra_entrega,
                    contra_entrega = cabecera.contra_entrega,
                    nroticket = cabecera.nroticket,
                    monto_anticipo = cabecera.monto_anticipo,

                    idanticipo = cabecera.idanticipo,
                    numeroidanticipo = cabecera.numeroidanticipo,
                    fecha_cae = "",
                    cae = "",
                    fecha_anulacion = fechaServ,

                    version_tarifa = versionTariAct,
                    notadebito = false,
                    id = idfactura,
                    numeroid = idnroactual + 1,
                    codalmacen = cabecera.codalmacen,

                    codcliente = cabecera.codcliente,
                    nomcliente = cabecera.nomcliente,
                    nit = factnit,
                    condicion = condicion,
                    codvendedor = cabecera.codvendedor,

                    codmoneda = cabecera.codmoneda,
                    fecha = fechaServ,
                    tdc = cabecera.tdc,
                    nrocaja = (short)nrocaja,
                    nroorden = "",

                    alfanumerico = "",
                    nrofactura = nrofactura,
                    nroautorizacion = "",
                    fechalimite = dtpfecha_limite,
                    codigocontrol = codigo_control,

                    nrolugar = nrolugar,
                    tipo = tipo,
                    codtipo_comprobante = codtipo_comprobante,
                    descarga = descarga,
                    transferida = false,

                    codremision = 0,
                    tipopago = cabecera.tipopago,
                    subtotal = (decimal)lista[0].subtotal,
                    descuentos = (decimal)lista[0].descuentos,
                    recargos = (decimal)lista[0].recargos,

                    total = (decimal)lista[0].total,
                    anulada = false,
                    transporte = cabecera.transporte,
                    fletepor = cabecera.fletepor,
                    direccion = cabecera.direccion,

                    contabilizado = false,
                    horareg = horaServ,
                    fechareg = fechaServ,
                    usuarioreg = usuario,
                    factorme = factormeDat,

                    iva = 0,
                    idfc = cabecera.idfc,
                    numeroidfc = cabecera.numeroidfc,
                    codtipopago = codtipopago,
                    codcuentab = codcuentab,

                    codbanco = codbanco,
                    nrocheque = nrocheque,
                    idcuenta = idcuenta,
                    odc = cabecera.odc,
                    peso = 0


                };
                // guardar cabecera
                await _context.vefactura.AddAsync(vefacturaData);
                await _context.SaveChangesAsync();
                int codFactura = vefacturaData.codigo;
                ///ir grabando codigo para impresion
                CodFacturas_Grabadas.Add(codFactura);

                // Calcula el rango de elementos para esta "hoja"
                int start = (1 * ITEMSPORHOJA) - ITEMSPORHOJA;
                // int end = (HOJA * ITEMSPORHOJA) - 1;

                var detalleFactura = tabladetalle.Select((item, index) => new vefactura1
                {
                    codfactura = codFactura,
                    coditem = item.coditem,
                    cantidad = item.cantidad,
                    udm = item.udm,
                    preciolista = item.preciolista,
                    niveldesc = item.niveldesc,
                    preciodesc = item.preciodesc,
                    precioneto = item.precioneto,
                    codtarifa = item.codtarifa,
                    coddescuento = (short)item.coddescuento,
                    total = item.total,
                    distdescuento = (decimal)item.distdescuento,
                    distrecargo = (decimal)item.distrecargo,
                    preciodist = (decimal)item.preciodist,
                    totaldist = (decimal)item.totaldist,
                    porceniva = item.porceniva,
                    codaduana = ""
                }).Skip(start).Take(ITEMSPORHOJA).ToList();  // Limitar al rango de la hoja actual

                await _context.vefactura1.AddRangeAsync(detalleFactura);
                await _context.SaveChangesAsync();

                /// actualiza numero factura y id numeracion
                if (await ventas.asignar_nro_factura(_context, codFactura, cabecera.codalmacen, nrocaja, idfactura))
                {
                    nrofactura = await ventas.factura_nrofactura(_context, codFactura);
                    numeroid = await ventas.factura_nroid(_context, codFactura);
                }
                // ACTUALIZAR CODGRUPOMER
                var dataFactura = await _context.vefactura.Where(i => i.codigo == codFactura).FirstOrDefaultAsync();
                int codFC = dataFactura.codremision ?? 0;
                List<vefactura1> dataVefactura1 = await _context.vefactura1.Where(i => i.codfactura == codFC).ToListAsync();
                dataVefactura1 = await ventas.Factura_Cargar_Grupomer(_context, dataVefactura1);
                await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

                // ACTUALIZAR PESO
                decimal pesoFact = await ventas.Peso_Factura(_context, codFactura);
                dataFactura.peso = pesoFact;
                await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos
                // ACTUALIZAR PESO DETALLE
                await ventas.Actualizar_Peso_Detalle_Factura(_context, codFactura);

                // actualizar el codigo producto del SIN
                var detalleFactura2 = await _context.vefactura1.Where(i => i.codfactura == codFactura).ToListAsync();
                foreach (var reg in detalleFactura2)
                {
                    string codProdSIN = await _context.initem.Where(i => i.codigo == reg.coditem).Select(i => i.codproducto_sin).FirstOrDefaultAsync() ?? "";
                    reg.codproducto_sin = codProdSIN;
                    if (reg.codproducto_sin == null) // arreglar nulos
                    {
                        reg.codproducto_sin = "";
                    }
                }
                await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

                // actualizar leyenda
                dataFactura.leyenda = await siat.generar_leyenda_aleatoria(_context, dataFactura.codalmacen);
                await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

                // Desde 20-12-2022
                // actualizar la Codfactura_web
                string valor_Codigo_factura_web = await siat.Generar_Codigo_Factura_Web(_context, codFactura, dataFactura.codalmacen);
                dataFactura.codfactura_web = valor_Codigo_factura_web;
                await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

                ///////////////////////////////////////////////////////////////////////////////////////////////////
                //              generar el cuf y actualizar el CUF generado en la factura
                ///////////////////////////////////////////////////////////////////////////////////////////////////
                string val_NIT = await empresa.NITempresa(_context, codempresa);
                Parametros_Facturacion_Ag = await siat.Obtener_Parametros_Facturacion(_context, dataFactura.codalmacen);

                if (Parametros_Facturacion_Ag.resultado == true)
                {
                    valor_CUF = "";
                    // obtener el ID-Numeroid de la factura
                    var id_nroid_fact = await Ventas.id_nroid_factura_cuf(_context, codFactura);
                    if (id_nroid_fact.id != "" && id_nroid_fact.numeroId > 0)
                    {
                        string TIPO_EMISION = "";
                        ////////////////////
                        // preguntar si hay conexion con el SIN para generar el CUF en tipo emision en linea(1) o fuera de linea (0)
                        var serviOnline = await _context.adsiat_parametros_facturacion.Where(i => i.codalmacen == dataFactura.codalmacen).Select(i => new
                        {
                            i.servicio_internet_activo,
                            i.servicio_sin_activo
                        }).FirstOrDefaultAsync();

                        bool adsiat_internet_activo = false;
                        bool adsiat_sin_activo = false;
                        if (serviOnline != null)
                        {
                            adsiat_internet_activo = serviOnline.servicio_internet_activo ?? false;
                            adsiat_sin_activo = serviOnline.servicio_sin_activo ?? false;
                        }

                        if (adsiat_internet_activo && await funciones.Verificar_Conexion_Internet() == true)
                        {
                            // actualizar en_linea true porq SI hay conexion a internet
                            dataFactura.en_linea = true;
                            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

                            // If sia_ws_siat.serv_facturas.Instancia.VerificarComunicacion() = True And sia_DAL.adsiat_parametros_facturacion.Instancia.servicios_sin_activo(codalmacen.Text) = True Then
                            if (adsiat_sin_activo && await serv_Facturas.VerificarComunicacion(_context, cabecera.codalmacen))   // ACA FALTA VALIDAR CON EL SIN OJOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
                            {
                                // emision en linea
                                TIPO_EMISION = "1";
                                dataFactura.en_linea_SIN = true;
                                await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos
                            }
                            else
                            {
                                // emision fuera de linea es 2 ////// emision masiva es 3
                                TIPO_EMISION = "2";
                                dataFactura.en_linea_SIN = false;
                                await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos
                            }

                        }
                        else
                        {
                            TIPO_EMISION = "2";
                            // actualizar en_linea false porq NO hay conexion a internet
                            dataFactura.en_linea = false;
                            // YA NO preguntar si hay conexion con el SIN porque si no hay internet no hay como comunicarse con el SIN
                            dataFactura.en_linea_SIN = false;
                            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos
                        }

                        // generar el CUF enviando el parametro correcto si es EN LINEA o FUERA DE LINEA
                        valor_CUF = await siat.Generar_CUF(_context, id_nroid_fact.id, id_nroid_fact.numeroId, dataFactura.codalmacen, val_NIT, Parametros_Facturacion_Ag.codsucursal, Parametros_Facturacion_Ag.modalidad, TIPO_EMISION, Parametros_Facturacion_Ag.tipofactura, Parametros_Facturacion_Ag.tiposector, nrofactura.ToString(), Parametros_Facturacion_Ag.ptovta, dataFactura.codigocontrol);
                        // actualizar el CUF
                        if (valor_CUF.Trim().Length > 0)
                        {
                            dataFactura.cuf = valor_CUF;
                            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos
                            string cadena_msj = "CUF generado exitosamente " + id_nroid_fact.id + "-" + id_nroid_fact.numeroId;
                            string mensaje = DateTime.Now.Year.ToString("0000") +
                            DateTime.Now.Month.ToString("00") +
                            DateTime.Now.Day.ToString("00") + " " +
                            DateTime.Now.Hour.ToString("00") + ":" +
                            DateTime.Now.Minute.ToString("00") + " - " + cadena_msj;
                            eventos.Add(mensaje);

                            cadena_msj = "El CUF de la factura fue generado exitosamente por: " + valor_CUF;
                            await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), id_nroid_fact.id, id_nroid_fact.numeroId.ToString(), _controllerName, cadena_msj, Log.TipoLog_Siat.Creacion);
                            resultado = true;
                        }
                        else
                        {
                            string cadena_msj = "No se pudo generar el CUF de la factura " + id_nroid_fact.id + "-" + id_nroid_fact.numeroId + " consulte con el administrador del sistema!!!";
                            string mensaje = DateTime.Now.Year.ToString("0000") +
                            DateTime.Now.Month.ToString("00") +
                            DateTime.Now.Day.ToString("00") + " " +
                            DateTime.Now.Hour.ToString("00") + ":" +
                            DateTime.Now.Minute.ToString("00") + " - " + cadena_msj;
                            eventos.Add(mensaje);

                            // DEVOLVER cadena_msj
                            msgAlertas.Add(cadena_msj);
                            resultado = false;
                        }

                    }
                    else
                    {
                        string cadena_msj = "No se pudo generar el CUF de la factura " + id_nroid_fact.id + "-" + id_nroid_fact.numeroId + " consulte con el administrador del sistema!!!";
                        string mensaje = DateTime.Now.Year.ToString("0000") +
                        DateTime.Now.Month.ToString("00") +
                        DateTime.Now.Day.ToString("00") + " " +
                        DateTime.Now.Hour.ToString("00") + ":" +
                        DateTime.Now.Minute.ToString("00") + " - " + cadena_msj;
                        eventos.Add(mensaje);

                        // DEVOLVER cadena_msj
                        msgAlertas.Add(cadena_msj);
                        resultado = false;
                    }
                }
                else
                {
                    dataFactura.cuf = "";
                    await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

                    string cadena_msj = "No se pudo generar el CUF de la factura debido a que no se encontro los parametros de facturacion necesarios de la agencia!!!";
                    string mensaje = DateTime.Now.Year.ToString("0000") +
                    DateTime.Now.Month.ToString("00") +
                    DateTime.Now.Day.ToString("00") + " " +
                    DateTime.Now.Hour.ToString("00") + ":" +
                    DateTime.Now.Minute.ToString("00") + " - " + cadena_msj;
                    eventos.Add(mensaje);

                    // DEVOLVER cadena_msj
                    msgAlertas.Add(cadena_msj);
                    resultado = false;
                }
                if (resultado)
                {
                    if (descarga == true)  // si la nota de remision no descarga entonces aqui descargarla
                    {
                        if (await saldos.Vefactura_ActualizarSaldo(_context, codFactura, Saldos.ModoActualizacion.Crear) == false)
                        {
                            // Desde 23/11/2023 registrar en el log si por alguna razon no actualiza en instoactual correctamente al disminuir el saldo de cantidad y la reserva en proforma
                            await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), dataFactura.id, dataFactura.numeroid.ToString(), _controllerName, "No actualizo stock al restar cantidad en Facturar Tienda.", Log.TipoLog.Creacion);
                            msgAlert = "No se pudo actualizar todos los stocks actuales del almacen, Por favor haga correr una actualizacion de stocks cuando vea conveniente."; // devolver
                            msgAlertas.Add(msgAlert);
                        }
                    }
                }
                // creo q aqui debe ir el log de registro de una factura grabada
                // Desde 10-11-2022 se añadio para guardar en el log el grabado de una factura de NR

                foreach (var factuCod in CodFacturas_Grabadas)
                {
                    var id_nroid_fact = await Ventas.id_nroid_factura_cuf(_context, factuCod);
                    if (id_nroid_fact.id != "" && id_nroid_fact.numeroId != 0)
                    {
                        await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Factura, factuCod.ToString(), id_nroid_fact.id, id_nroid_fact.numeroId.ToString(), _controllerName, "Grabar", Log.TipoLog.Creacion);
                    }
                }
                //Desde 23/08/2023 si la factura es pagada con un anticipo actualizar el monto_rest del anticipo segun el monto aplicado
                if (dataFactura.idanticipo.Length > 0 && dataFactura.numeroidanticipo > 0 && dataFactura.monto_anticipo > 0)
                {
                    // actualizar la Codfactura_web
                    var dataanticipo = await _context.coanticipo.Where(i => i.id == dataFactura.idanticipo && i.numeroid == dataFactura.numeroidanticipo).FirstOrDefaultAsync();
                    decimal monto_anticipo = (decimal)(dataanticipo.montorest - dataFactura.monto_anticipo);
                    dataanticipo.montorest = monto_anticipo;
                    await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos
                }
                return (resultado, msgAlertas, eventos, CodFacturas_Grabadas);
            }
            catch (Exception ex)
            {
                msgAlertas.Add(ex.Message);
                return (false, msgAlertas, eventos, CodFacturas_Grabadas);
            }

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
                    int codvendedor = await ventas.VendedorClave(_context, txtclave);
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
        [Route("getParametrosIniciales/{userConn}/{usuario}/{codempresa}")]
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
                    int codtarifadefect = await configuracion.usr_codtarifa(_context, usuario);
                    int coddescuentodefect = await configuracion.usr_coddescuento(_context, usuario);

                    int codtipopago = await configuracion.parametros_ctasporcobrar_tipopago(_context, codempresa);
                    string codtipopagodescripcion = await nombres.nombretipopago(_context, codtipopago);

                    string idcuenta = await configuracion.usr_idcuenta(_context, usuario);
                    string idcuentadescripcion = await nombres.nombrecuenta_fondos(_context, idcuenta);

                    string codcliente = await configuracion.usr_codcliente(_context, usuario);
                    string codclientedescripcion = await nombres.nombrecliente(_context, codcliente);

                    return Ok(new
                    {
                        id,
                        numeroid,
                        fecha,
                        codmoneda,
                        tdc,
                        codalmacen,  
                        codtarifadefect,
                        coddescuentodefect,
                        codtipopago,
                        codtipopagodescripcion,
                        idcuenta,
                        idcuentadescripcion,
                        codcliente,
                        codclientedescripcion
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
        [Route("transfProforma/{userConn}/{idProf}/{nroIdPof}")]
        public async Task<object> transfProforma(string userConn, string idProf, int nroIdPof)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var cabecera = await _context.veproforma.Where(i => i.id == idProf && i.numeroid == nroIdPof).Select(i => new
                    {
                        codigo = i.codigo,
                        ids_proforma = i.id,
                        nro_id_proforma = i.numeroid,
                        codalmacen = i.codalmacen,
                        codcliente = i.codcliente,
                        nomcliente = i.nomcliente,
                        nit = i.nit,
                        complemento_ci = i.complemento_ci,
                        email = i.email,
                        codmoneda = i.codmoneda,
                        tipopago = 0,
                        tdc = i.tdc,
                        total = i.total,
                        subtotal = i.subtotal,
                        transporte = i.transporte,
                        fletepor = i.fletepor,
                        direccion = i.direccion,
                        iva = i.iva,
                        descuentos = i.descuentos,
                        recargos = i.recargos,

                    }).FirstOrDefaultAsync();
                    if (cabecera == null)
                    {
                        return BadRequest(new { resp = "La proforma " + idProf + "-" + nroIdPof + " no fue encontrada." });
                    }
                    // muestra todos los datos del registro actual en las casilla
                    string codclientedescripcion = await nombres.nombrecliente(_context, cabecera.codcliente);
                    string condicion = await cliente.CondicionFrenteAlIva(_context, cabecera.codcliente);

                    // cargar recargos
                    var recargos = await cargarrecargoproforma(_context, cabecera.codigo);
                    // cargar descuentos
                    var descuentos = await cargardesextraproforma(_context, cabecera.codigo);
                    // cargar IVA
                    var iva = await cargarivaproforma(_context, cabecera.codigo);

                    // mostrar detalle del documento actual
                    var detalle = await transferirdetalleproforma(_context, cabecera.codigo);

                    return Ok(new
                    {
                        codclientedescripcion,
                        condicion,

                        cabecera,
                        detalle,
                        recargos,
                        descuentos,
                        iva
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }

        private async Task<List<dataDetalleFactura>> transferirdetalleproforma(DBContext _context, int codigo)
        {
            var detalle = await _context.veproforma1.Where(i => i.codproforma == codigo)
                .Join(_context.initem,
                        p => p.coditem,
                        i => i.codigo,
                        (p, i) => new { p, i })
                .Select(i => new dataDetalleFactura
                {
                    coditem = i.p.coditem,
                    descripcion = i.i.descripcion,
                    medida = i.i.medida,
                    udm = i.p.udm,
                    porceniva = i.p.porceniva ?? 0,

                    niveldesc = i.p.niveldesc,
                    cantidad = i.p.cantidad,
                    codtarifa = i.p.codtarifa,
                    coddescuento = i.p.coddescuento,
                    precioneto = i.p.precioneto,

                    preciodesc = i.p.preciodesc ?? 0,
                    preciolista = i.p.preciolista,
                    total = i.p.total,
                    // cumple = i.p.c
                }).ToListAsync();
            return detalle;
        }

        private async Task<List<recargosData>> cargarrecargoproforma(DBContext _context, int codigo)
        {
            var registros = await _context.verecargoprof.Where(i => i.codproforma == codigo).Select(i => new recargosData
            {
                codrecargo = i.codrecargo,
                descripcion = "",
                porcen = i.porcen,
                monto = i.monto,
                moneda = i.moneda,
                montodoc = i.montodoc,
                codcobranza = i.codcobranza ?? 0
            }).ToListAsync();
            return registros;
        }
        private async Task<List<descuentosData>> cargardesextraproforma(DBContext _context, int codigo)
        {
            var descuentosExtra = await _context.vedesextraprof
                        .Join(_context.vedesextra,
                        p => p.coddesextra,
                        e => e.codigo,
                        (p, e) => new { p, e })
                        .Where(i => i.p.codproforma == codigo)
                        .Select(i => new descuentosData
                        {
                            codproforma = i.p.codproforma,
                            coddesextra = i.p.coddesextra,
                            descripcion = i.e.descripcion,
                            porcen = i.p.porcen,
                            montodoc = i.p.montodoc,
                            codcobranza = i.p.codcobranza ?? 0,
                            codcobranza_contado = i.p.codcobranza_contado ?? 0,
                            codanticipo = i.p.codanticipo ?? 0,
                        })
                        .ToListAsync();


            return descuentosExtra;
        }
        private async Task<List<ivaData>> cargarivaproforma(DBContext _context, int codigo)
        {
            var registros = await _context.veproforma_iva.Where(i => i.codproforma == codigo).Select(i => new ivaData
            {
                codfactura = 0,
                porceniva = i.porceniva,
                total = i.total ?? 0,
                porcenbr = i.porcenbr ?? 0,
                br = i.br ?? 0,
                iva = i.iva ?? 0
            }).ToListAsync();
            return registros;
        }


        [HttpGet]
        [Route("transfFactura/{userConn}/{idFact}/{nroIdFact}")]
        public async Task<object> transfFactura(string userConn, string idFact, int nroIdFact)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var cabecera = await _context.vefactura.Where(i => i.id == idFact && i.numeroid == nroIdFact).Select(i => new
                    {
                        codigo = i.codigo,
                        ids_proforma = "",
                        nro_id_proforma = 0,
                        codalmacen = i.codalmacen,
                        codcliente = i.codcliente,
                        nomcliente = i.nomcliente,
                        nit = i.nit,
                        complemento_ci = i.complemento_ci,
                        email = i.email,
                        codmoneda = i.codmoneda,
                        tipopago = i.tipopago,
                        tdc = i.tdc,
                        total = i.total,
                        subtotal = i.subtotal,
                        transporte = i.transporte,
                        fletepor = i.fletepor,
                        direccion = i.direccion,
                        iva = i.iva,
                        descuentos = i.descuentos,
                        recargos = i.recargos,

                    }).FirstOrDefaultAsync();
                    if (cabecera == null)
                    {
                        return BadRequest(new { resp = "La Factura " + idFact + "-" + nroIdFact + " no fue encontrada." });
                    }
                    // muestra todos los datos del registro actual en las casilla
                    string codclientedescripcion = await nombres.nombrecliente(_context, cabecera.codcliente);
                    string condicion = await cliente.CondicionFrenteAlIva(_context, cabecera.codcliente);

                    // cargar recargos
                    var recargos = await cargarrecargo(_context, cabecera.codigo);
                    // cargar descuentos
                    var descuentos = await cargardesextra(_context, cabecera.codigo);
                    // cargar IVA
                    var iva = await cargariva(_context, cabecera.codigo);

                    // mostrar detalle del documento actual
                    var detalle = await transferirdetallefactura(_context, cabecera.codigo);

                    return Ok(new
                    {
                        codclientedescripcion,
                        condicion,

                        cabecera,
                        detalle,
                        recargos,
                        descuentos,
                        iva
                    });
                }

            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }

        private async Task<List<dataDetalleFactura>> transferirdetallefactura(DBContext _context, int codigo)
        {
            var detalle = await _context.vefactura1.Where(i => i.codfactura == codigo)
                .Join(_context.initem,
                        p => p.coditem,
                        i => i.codigo,
                        (p, i) => new { p, i })
                .Select(i => new dataDetalleFactura
                {
                    coditem = i.p.coditem,
                    descripcion = i.i.descripcion,
                    medida = i.i.medida,
                    udm = i.p.udm,
                    porceniva = i.p.porceniva ?? 0,

                    niveldesc = i.p.niveldesc,
                    cantidad = i.p.cantidad,
                    codtarifa = i.p.codtarifa,
                    coddescuento = i.p.coddescuento,
                    precioneto = i.p.precioneto,

                    preciodesc = i.p.preciodesc ?? 0,
                    preciolista = i.p.preciolista,
                    total = i.p.total,
                    // cumple = i.p.c
                }).ToListAsync();
            return detalle;
        }


        private async Task<List<recargosData>> cargarrecargo(DBContext _context, int codigo)
        {
            var registros = await _context.verecargofact.Where(i => i.codfactura == codigo).Select(i => new recargosData
            {
                codrecargo = i.codrecargo,
                descripcion = "",
                porcen = i.porcen,
                monto = i.monto ?? 0,
                moneda = i.moneda,
                montodoc = i.montodoc,
                codcobranza = i.codcobranza ?? 0
            }).ToListAsync();
            return registros;
        }
        private async Task<List<descuentosData>> cargardesextra(DBContext _context, int codigo)
        {
            var descuentosExtra = await _context.vedesextrafact
                        .Join(_context.vedesextra,
                        p => p.coddesextra,
                        e => e.codigo,
                        (p, e) => new { p, e })
                        .Where(i => i.p.codfactura == codigo)
                        .Select(i => new descuentosData
                        {
                            codproforma = 0,
                            coddesextra = i.p.coddesextra,
                            descripcion = i.e.descripcion,
                            porcen = i.p.porcen,
                            montodoc = i.p.montodoc,
                            codcobranza = 0, //i.p.codcobranza,
                            codcobranza_contado = 0, //i.p.codcobranza_contado,
                            codanticipo = 0 //i.p.codanticipo,
                        })
                        .ToListAsync();
            return descuentosExtra;
        }
        private async Task<List<ivaData>> cargariva(DBContext _context, int codigo)
        {
            var registros = await _context.vefactura_iva.Where(i => i.codfactura == codigo).OrderBy(i => i.porceniva).Select(i => new ivaData
            {
                codfactura = i.codfactura ?? 0,
                porceniva = i.porceniva ?? 0,
                total = i.total ?? 0,
                porcenbr = i.porcenbr ?? 0,
                br = i.br ?? 0,
                iva = i.iva ?? 0
            }).ToListAsync();
            return registros;
        }






        //////////////////////////////////////////////////////////////////////////
        //// TOTALES Y SUBTOTALES
        //////////////////////////////////////////////////////////////////////////

        // RUTAS CONTROLADORES

        [HttpPost]
        [Route("sumas_Subtotal/{userConn}/{codempresa}/{usuario}")]
        public async Task<object> sumas_Subtotal(string userConn, string codempresa, string usuario, List<dataDetalleFactura> tabla_detalle)
        {
            if (tabla_detalle.Count() < 1)
            {
                return BadRequest(new { resp = "No se esta recibiendo ningun dato, verifique esta situación." });
            }
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                double tot = (double)tabla_detalle.Sum(i => (i.cantidad * i.preciolista));
                double totlinea = (double)tabla_detalle.Sum(i => (i.cantidad * i.preciodesc));
                double subtotal = (double)tabla_detalle.Sum(i => i.total);

                // para el desgloce
                // sacar precios
                var descuentos = tabla_detalle
                    .GroupBy(obj => obj.coddescuento)
                    .Select(grp => grp.First())
                    .Select(i => i.coddescuento)
                    .ToList();

                // sacartotales
                List<double> totales = new List<double>();

                for (int i = 0; i < descuentos.Count(); i++)
                {
                    double total = (double)tabla_detalle
                        .Where(row => row.coddescuento == descuentos[i])
                    .Sum(row => row.cantidad * (row.preciodesc - row.precioneto));

                    totales.Add(total);
                }
                var desglose = totales.Select((i, index) => new
                {
                    total = i,
                    descuento = descuentos[index]
                }).ToList();

                return Ok(new
                {
                    resul = tabla_detalle,
                    a = tot,
                    b = tot - totlinea,
                    c = totlinea,
                    d = totlinea - subtotal,
                    e = subtotal,
                    desgloce = desglose
                });
            }

        }




        [HttpPost]
        [Route("recarcularRecargosFact/{userConn}/{descuentos}/{codcliente}/{codmoneda}")]
        public async Task<object> recarcularRecargosFact(string userConn, double descuentos, string codcliente, string codmoneda, RequestTotalizar RequestRecargos)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                List<dataDetalleFactura> tabla_detalle = RequestRecargos.detalleItems;
                var tablarecargos = RequestRecargos.recargosTabla;

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await versubtotal(_context, tabla_detalle);
                    double subtotal = result.st;
                    double peso = result.peso;
                    var respRecargo = await verrecargos(_context, subtotal, codmoneda, tablarecargos);
                    double recargo = respRecargo.total;
                    tablarecargos = respRecargo.tablarecargos;

                    var total = await vertotal(_context, subtotal, recargo, descuentos, codcliente, tabla_detalle);
                    return new
                    {
                        subtotal = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, subtotal),
                        peso = peso,
                        recargo = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, recargo),
                        total = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, total.TotalGen),
                        tablaRecargos = tablarecargos
                    };
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al recalcular recargos: " + ex.Message);
                throw;
            }
        }

        //[Authorize]
        [HttpPost]
        [Route("recarcularDescuentosFact/{userConn}/{codempresa}/{recargos}/{codmoneda}/{codcliente}/{nit}")]
        public async Task<object> recarcularDescuentosFact(string userConn, string codempresa, double recargos, string codmoneda, string codcliente, string nit, RequestTotalizar RequestDescuentos)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                List<dataDetalleFactura> tabla_detalle = RequestDescuentos.detalleItems;
                List<descuentosData>? tabladescuentos = RequestDescuentos.descuentosTabla;


                List<itemDataMatriz> data = tabla_detalle.Select(x => new itemDataMatriz
                {
                    coditem = x.coditem,
                    descripcion = x.descripcion,
                    medida = x.medida,
                    udm = x.udm,
                    porceniva = (double)x.porceniva,
                    niveldesc = x.niveldesc,
                    cantidad = (double)x.cantidad,
                    codtarifa = x.codtarifa,
                    coddescuento = x.coddescuento,
                    precioneto = (double)x.precioneto,
                    preciodesc = (double)x.preciodesc,
                    preciolista = (double)x.preciolista,
                    total = (double)x.total,
                    cumple = x.cumple,
                    monto_descto = 0,
                    subtotal_descto_extra = 0,

                }).ToList();

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (tabladescuentos != null)
                    {
                        /////////////////////////////////////////////////////////////////
                        // verificar que no haya mas de un tipo de precio en el detalle
                        DatosDocVta DVTA = new DatosDocVta(); // dentro de la funcion Validar_Precios_Del_Documento, no se hace nada con DVTA por lo que mandamos asi vacio.
                        ResultadoValidacion misrep = await validar_Vta.Validar_Precios_Del_Documento(_context, DVTA, data, codempresa);
                        if (misrep.resultado == false)
                        {
                            return BadRequest(new { resp = misrep.observacion + " - " + misrep.obsdetalle });
                        }

                        // obtiene el precio unico del documento
                        int _CODTARIFA = await validar_Vta.Precio_Unico_Del_Documento(_context, data, codempresa);

                        if (_CODTARIFA == -1)
                        {
                            return BadRequest(new { resp = "Verifique que en detalle del pedido solo exista un tipo de precio!!!" });
                        }
                        /////////////////////////////////////////////////////////////////



                        var result = await versubtotal(_context, tabla_detalle);
                        double subtotal = result.st;
                        double peso = result.peso;
                        // var respDescuento = await verdesextra(_context, codempresa, veproforma.nit, veproforma.codmoneda, cmbtipo_complementopf, veproforma.idpf_complemento, veproforma.nroidpf_complemento ?? 0, subtotal, veproforma.fecha, tabladescuentos, tabla_detalle);
                        var respDescuento = await verdesextra(_context, codempresa, subtotal, codmoneda, codcliente, nit, data, tabladescuentos);

                        double descuento = respDescuento.respdescuentos;
                        tabladescuentos = respDescuento.tabladescuentos;

                        var total = await vertotal(_context, subtotal, recargos, descuento, codcliente, tabla_detalle);
                        return new
                        {
                            subtotal = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, subtotal),
                            peso = peso,
                            descuento = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, descuento),
                            total = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, total.TotalGen),
                            tablaDescuentos = tabladescuentos
                        };
                    }
                    return BadRequest(new { resp = "No se seleccionaron descuentos extras" });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al recalcular descuentos: " + ex.Message);
                throw;
            }
        }



        [HttpPost]
        [QueueFilter(1)] // Limitar a 1 solicitud concurrente
        [Route("totabilizarFact/{userConn}/{codempresa}/{usuario}/{codmoneda}/{codcliente}/{codalmacen}/{nit}")]
        public async Task<object> totabilizarFact(string userConn, string codempresa, string usuario, string codmoneda, string codcliente, int codalmacen, string nit, RequestTotalizar requestTotalizar)
        {
            List<dataDetalleFactura> tabla_detalle = requestTotalizar.detalleItems;
            List<descuentosData>? tabladescuentos = requestTotalizar.descuentosTabla;
            List<recargosData>? tablarecargos = requestTotalizar.recargosTabla;
            if (tabla_detalle.Count() <= 0)
            {
                return BadRequest(new { resp = "No hay ningun item en su documento!!!" });
            }
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    DateTime fecha = await funciones.FechaDelServidor(_context);
                    fecha = fecha.Date;
                    List <itemDataMatriz> data = tabla_detalle.Select(x => new itemDataMatriz
                    {
                        coditem = x.coditem,
                        descripcion = x.descripcion,
                        medida = x.medida,
                        udm = x.udm,
                        porceniva = (double)x.porceniva,
                        niveldesc = x.niveldesc,
                        cantidad = (double)x.cantidad,
                        codtarifa = x.codtarifa,
                        coddescuento = x.coddescuento,
                        precioneto = (double)x.precioneto,
                        preciodesc = (double)x.preciodesc,
                        preciolista = (double)x.preciolista,
                        total = (double)x.total,
                        cumple = x.cumple,
                        monto_descto = 0,
                        subtotal_descto_extra = 0,

                    }).ToList();

                    ////////////////////////////////////////////////////////////
                    // verificar los precios permitidos al usuario
                    string cadena_precios_no_autorizados_al_us = await validar_Vta.Validar_Precios_Permitidos_Usuario(_context, usuario, data);
                    if (cadena_precios_no_autorizados_al_us.Trim().Length > 0)
                    {
                        return BadRequest(new { resp = "El documento tiene items a precio(s): " + cadena_precios_no_autorizados_al_us + " los cuales no estan asignados al usuario " + usuario + ", verifique esta situacion!!!" });
                    }
                    ////////////////////////////////////////////////////////////
                    ///
                    /*
                    foreach (var reg in tabla_detalle)
                    {
                        reg.cantidad = Math.Round(reg.cantidad, 2, MidpointRounding.AwayFromZero);

                        // MODIFICADO EN FECHA:  09-01-2021
                        reg.niveldesc = await cliente.niveldesccliente(_context, codcliente, reg.coditem, reg.codtarifa, "ACTUAL");
                        if (reg.codtarifa > 0)
                        {
                            string monBaseTarif = await ventas.monedabasetarifa(_context, reg.codtarifa);
                            reg.preciolista = await tipocambio._conversion(_context, codmoneda, monBaseTarif, fecha, (decimal)(await ventas.preciodelistaitem(_context, reg.codtarifa, reg.coditem)));

                            // si hay descuento
                            reg.preciodesc = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (await tipocambio._conversion(_context, codmoneda, monBaseTarif, fecha, (decimal)(await ventas.preciocliente(_context, codcliente, codalmacen, reg.codtarifa, reg.coditem, "NO", reg.niveldesc, "ACTUAL")))));
                            reg.precioneto = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (await tipocambio._conversion(_context, codmoneda, monBaseTarif, fecha, (decimal)(await ventas.preciocondescitem(_context, codcliente, codalmacen, reg.codtarifa, reg.coditem, reg.coddescuento, "NO", reg.niveldesc, "ACTUAL")))));

                            if (reg.cantidad > 0)
                            {
                                reg.total = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (reg.cantidad * reg.precioneto));
                            }
                            else
                            {
                                reg.total = 0;
                            }
                        }
                        else
                        {
                            reg.preciolista = 0;
                            reg.preciodesc = 0;
                            reg.precioneto = 0;
                            reg.total = 0;
                        }
                    }
                    */
                    tabla_detalle = await calcularDetalle(_context, codcliente, codmoneda, fecha, codalmacen, tabla_detalle);

                    var resultSubTotal = await versubtotal(_context, tabla_detalle);
                    var resultDesc = await verdesextra(_context, codempresa, resultSubTotal.st, codmoneda, codcliente, nit, data, tabladescuentos);
                    var resultRec = await verrecargos(_context, resultSubTotal.st, codmoneda, tablarecargos);
                    var resultTotal = await vertotal(_context, resultSubTotal.st, resultRec.total, resultDesc.respdescuentos, codcliente, tabla_detalle);
                    return Ok(new
                    {
                        tabla_detalle = tabla_detalle,
                        
                        tabla_descuentos = resultDesc.tabladescuentos,
                        tabla_recargos = resultRec.tablarecargos,
                        tabla_iva = resultTotal.tablaiva,
                        totales = new
                        {
                            peso = resultSubTotal.peso,
                            subtotal = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultSubTotal.st),
                            descuentos = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultDesc.respdescuentos),
                            recargos = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultRec.total),
                            total = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultTotal.TotalGen),
                            iva = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultTotal.totalIva),
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al recalcular descuentos: " + ex.Message);
                throw;
            }
        }


        // FUNCIONES PRIVATE PARA LAS OPERACIONES
        private async Task<List<dataDetalleFactura>> calcularDetalle (DBContext _context, string codcliente, string codmoneda, DateTime fecha, int codalmacen, List<dataDetalleFactura> tabla_detalle)
        {
            foreach (var reg in tabla_detalle)
            {
                reg.cantidad = Math.Round(reg.cantidad, 2, MidpointRounding.AwayFromZero);

                // MODIFICADO EN FECHA:  09-01-2021
                reg.niveldesc = await cliente.niveldesccliente(_context, codcliente, reg.coditem, reg.codtarifa, "ACTUAL");
                if (reg.codtarifa > 0)
                {
                    string monBaseTarif = await ventas.monedabasetarifa(_context, reg.codtarifa);
                    reg.preciolista = await tipocambio._conversion(_context, codmoneda, monBaseTarif, fecha, (decimal)(await ventas.preciodelistaitem(_context, reg.codtarifa, reg.coditem)));

                    // si hay descuento
                    reg.preciodesc = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (await tipocambio._conversion(_context, codmoneda, monBaseTarif, fecha, (decimal)(await ventas.preciocliente(_context, codcliente, codalmacen, reg.codtarifa, reg.coditem, "NO", reg.niveldesc, "ACTUAL")))));
                    reg.precioneto = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (await tipocambio._conversion(_context, codmoneda, monBaseTarif, fecha, (decimal)(await ventas.preciocondescitem(_context, codcliente, codalmacen, reg.codtarifa, reg.coditem, reg.coddescuento, "NO", reg.niveldesc, "ACTUAL")))));

                    if (reg.cantidad > 0)
                    {
                        reg.total = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (reg.cantidad * reg.precioneto));
                    }
                    else
                    {
                        reg.total = 0;
                    }
                }
                else
                {
                    reg.preciolista = 0;
                    reg.preciodesc = 0;
                    reg.precioneto = 0;
                    reg.total = 0;
                }
            }
            return tabla_detalle;
        }

        private async Task<(double st, double peso)> versubtotal(DBContext _context, List<dataDetalleFactura> tabla_detalle)
        {
            // filtro de codigos de items
            tabla_detalle = tabla_detalle.Where(item => item.coditem != null && item.coditem.Length >= 8).ToList();
            // calculo subtotal
            double peso = 0;
            double st = 0;

            foreach (var reg in tabla_detalle)
            {
                st = st + (double)reg.total;
                peso = peso + (await items.itempeso(_context, reg.coditem)) * (double)reg.cantidad;
            }

            // desde 08/01/2023 redondear el resultado a dos decimales con el SQLServer
            // REVISAR SI HAY OTRO MODO NO DA CON LINQ.
            st = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)st);
            return (st, peso);
        }
        private async Task<(double total, List<recargosData>? tablarecargos)> verrecargos(DBContext _context, double subtotal, string codmoneda, List<recargosData>? tablarecargos)
        {
            DateTime fecha = await funciones.FechaDelServidor(_context);
            double total = 0;
            if (tablarecargos == null)
            {
                return (0, tablarecargos);
            }
            foreach (var reg in tablarecargos)
            {
                if (reg.porcen > 0)
                {
                    reg.montodoc = (decimal)(subtotal / 100) * reg.porcen;
                }
                else
                {
                    reg.montodoc = await tipocambio._conversion(_context, codmoneda, reg.moneda, fecha.Date, reg.monto);
                }
                reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)reg.montodoc);
                total = total + (double)reg.montodoc;
            }
            total = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, total);
            return (total, tablarecargos);
        }
        private async Task<(double respdescuentos, List<descuentosData> tabladescuentos)> verdesextra(DBContext _context, string codempresa, double subtotal, string codmoneda, string codcliente, string nit, List<itemDataMatriz> dataDetalle, List<descuentosData> tabladescuentos)
        {
            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);

            List<tabladescuentos> descuentos_aux = tabladescuentos.Select(i => new tabladescuentos
            {
                codproforma = 0,
                coddesextra = i.coddesextra,
                porcen = i.porcen,
                montodoc = i.montodoc,
                codcobranza = i.codcobranza,
                codcobranza_contado = i.codcobranza_contado,
                codanticipo = i.codanticipo,
                aplicacion = i.aplicacion,
                codmoneda = i.codmoneda,
                descrip = i.descripcion
            }).ToList();

            descuentos_aux = await ventas.Ordenar_Descuentos_Extra(_context, descuentos_aux);

            tabladescuentos = descuentos_aux.Select(i => new descuentosData
            {
                codproforma = i.codproforma,
                coddesextra = i.coddesextra,
                descripcion = i.descrip ?? "",
                porcen = i.porcen,
                montodoc = i.montodoc,
                codcobranza = i.codcobranza ?? 0,
                codcobranza_contado = i.codcobranza_contado ?? 0,
                codanticipo = i.codanticipo ?? 0,
                aplicacion = i.aplicacion ?? "",
                codmoneda = i.codmoneda ?? "",
            }).ToList();

            DateTime fecha = await funciones.FechaDelServidor(_context);



            //calcular el monto  de descuento segun el porcentaje
            ////////////////////////////////////////////////////////////////////////////////
            //primero calcular los montos de los que se aplican en el detalle o son
            //diferenciados por item
            ////////////////////////////////////////////////////////////////////////////////
            
            foreach (var reg in tabladescuentos)
            {
                // verifica si el descuento es diferenciado por item
                if (await ventas.DescuentoExtra_Diferenciado_x_item(_context, reg.coddesextra))
                {
                    var resp = await ventas.DescuentoExtra_CalcularMonto(_context, reg.coddesextra, dataDetalle, codcliente, nit);
                    double monto_desc = resp.resultado;
                    reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, monto_desc);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////
            // los que se aplican en el SUBTOTAL
            ////////////////////////////////////////////////////////////////////////////////
            foreach (var reg in tabladescuentos)
            {
                if (!await ventas.DescuentoExtra_Diferenciado_x_item(_context, reg.coddesextra))
                {
                    if (reg.aplicacion == "SUBTOTAL")
                    {
                        if (coddesextra_depositos == reg.coddesextra)
                        {
                            // el monto por descuento de deposito ya esta calculado
                            // pero se debe verificar si este monto de este descuento esta en la misma moneda que la proforma
                            if (reg.codmoneda != codmoneda)
                            {
                                double monto_cambio = (double)await tipocambio._conversion(_context, codmoneda, reg.codmoneda, fecha, reg.montodoc);
                                reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, monto_cambio);
                                reg.codmoneda = codmoneda;
                            }
                        }
                        else
                        {
                            // este descuento se aplica sobre el subtotal de la venta
                            reg.montodoc = (decimal)(subtotal / 100) * reg.porcen;
                            reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)reg.montodoc);
                        }
                    }
                }
            }
            // totalizar los descuentos que se aplicar al subtotal
            double total_desctos1 = 0;
            foreach (var reg in tabladescuentos)
            {
                if (reg.aplicacion == "SUBTOTAL")
                {
                    total_desctos1 += (double)reg.montodoc;
                }
            }
            total_desctos1 = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)total_desctos1);

            ////////////////////////////////////////////////////////////////////////////////
            // los que se aplican en el TOTAL
            ////////////////////////////////////////////////////////////////////////////////
            double total_preliminar = subtotal - total_desctos1;
            foreach (var reg in tabladescuentos)
            {
                if (!await ventas.DescuentoExtra_Diferenciado_x_item(_context, reg.coddesextra))
                {
                    if (reg.aplicacion == "TOTAL")
                    {
                        if (coddesextra_depositos == reg.coddesextra)
                        {
                            //el descuento se aplica sobre el monto del deposito
                            //ya esta calculado
                        }
                        else
                        {
                            //este descuento se aplica sobre el subtotal de la venta
                            reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)((total_preliminar / 100) * (double)reg.porcen));
                        }
                    }
                }
            }
            double total_desctos2 = 0;
            foreach (var reg in tabladescuentos)
            {
                if (reg.aplicacion == "TOTAL")
                {
                    total_desctos2 += (double)reg.montodoc;
                }
            }
            double totalDescuentos = (total_desctos1 + total_desctos2);
            return (totalDescuentos, tabladescuentos);
            //return totalDescuentos;
        }
        private async Task<(double totalIva, double TotalGen, List<veproforma_iva> tablaiva)> vertotal(DBContext _context, double subtotal, double recargos, double descuentos, string codcliente, List<dataDetalleFactura> tabladetalle)
        {
            double suma = subtotal + recargos - descuentos;
            double totalIva = 0;
            if (suma < 0)
            {
                suma = 0;
            }
            List<veproforma_iva> tablaiva = new List<veproforma_iva>();
            if (await cliente.DiscriminaIVA(_context, codcliente))
            {
                // Calculo de ivas
                tablaiva = await CalcularTablaIVA(subtotal, recargos, descuentos, tabladetalle);
                //fin calculo ivas
                totalIva = await veriva(tablaiva);
                suma = suma + totalIva;
            }
            

            return (totalIva, suma, tablaiva);
        }

        private async Task<List<veproforma_iva>> CalcularTablaIVA(double subtotal, double recargos, double descuentos, List<dataDetalleFactura> tabladetalle)
        {
            List<clsDobleDoble> lista = new List<clsDobleDoble>();

            foreach (var reg in tabladetalle)
            {
                bool encontro = false;
                foreach (var item in lista)
                {
                    if (item.dobleA == (double)reg.porceniva)
                    {
                        encontro = true;
                        item.dobleB = item.dobleB + (double)reg.total;
                        break;
                    }
                }
                if (!encontro)
                {
                    clsDobleDoble newReg = new clsDobleDoble();
                    newReg.dobleA = (double)reg.porceniva;
                    newReg.dobleB = (double)reg.total;
                    lista.Add(newReg);
                }
            }
            // pasar a tabla
            var tablaiva = lista.Select(i => new veproforma_iva
            {
                codproforma = 0,
                porceniva = (decimal)i.dobleA,
                total = (decimal)i.dobleB,
                porcenbr = 0,
                br = 0,
                iva = 0
            }).ToList();

            //calcular porcentaje de br
            double porcenbr = 0;
            try
            {
                if (subtotal > 0)
                {
                    porcenbr = ((recargos - descuentos) * 100) / subtotal;
                }
            }
            catch (Exception)
            {
                porcenbr = 0;
            }
            //calcular en la tabla
            foreach (var reg in tablaiva)
            {
                reg.porcenbr = (decimal)porcenbr;
                reg.br = (reg.total / 100) * (decimal)porcenbr;
                reg.iva = ((reg.total + reg.br) / 100) * reg.porceniva;
            }
            return tablaiva;
        }
        private async Task<double> veriva(List<veproforma_iva> tablaiva)
        {
            var total = tablaiva.Sum(i => i.iva) ?? 0;
            return (double)total;
        }















        [HttpGet]
        [Route("imprimirFactura/{userConn}/{codFactura}/{codempresa}")]
        public async Task<object> imprimirFactura(string userConn, int codFactura, string codempresa)
        {
            try
            {

                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var cabecera = await _context.vefactura.Where(i => i.codigo == codFactura).FirstOrDefaultAsync();
                    if (cabecera == null)
                    {
                        return BadRequest(new { resp = "No se encontraron datos con el codigo de facturo proporcionado." });
                    }
                    // string nombreImpresora = "EPSON TM-T88V Receipt5";

                    var nombreImpresora = await _context.inalmacen.Where(i => i.codigo == cabecera.codalmacen).Select(i => i.impresora_nr).FirstOrDefaultAsync();
                    if (nombreImpresora == null)
                    {
                        return BadRequest(new { resp = "No se encontró una impresora registrada en la base de datos." });
                    }
                    Font fuente = new Font("Consolas", 10);
                    await impresoraTermica.ImprimirTexto(_context, codempresa, nombreImpresora, fuente, cabecera);
                    return Ok("Imprimiendo Factura.");

                }
                    
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al imprimir: {ex.Message}");
            }
        }



        [HttpGet]
        [Route("imprimirReciboAnticipo/{userConn}/{codFactura}/{codempresa}")]
        public async Task<object> imprimirReciboAnticipo(string userConn, int codFactura, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var codAlmacen = await _context.vefactura.Where(i => i.codigo == codFactura).Select(i => i.codalmacen).FirstOrDefaultAsync();
                    var idAnticipo = await _context.vefactura.Where(i => i.codigo == codFactura).Select(i => i.idanticipo).FirstOrDefaultAsync();
                    if (idAnticipo.Trim().Length > 0)
                    {
                        // string nombreImpresora = "EPSON TM-T88V Receipt5";

                        var nombreImpresora = await _context.inalmacen.Where(i => i.codigo == codAlmacen).Select(i => i.impresora_nr).FirstOrDefaultAsync();
                        if (nombreImpresora == null)
                        {
                            return BadRequest(new { resp = "No se encontró una impresora registrada en la base de datos." });
                        }
                        string tipo_impresion_anticipo = "(Original)";
                        Font fuente = new Font("Consolas", 10);
                        await impresoraTermica.ImprimirAnticipo(_context, codempresa, nombreImpresora, fuente, tipo_impresion_anticipo, codFactura);
                        return Ok("Imprimiendo Recibo.");
                    }
                    return StatusCode(203, new { msg = "Esta factura no tiene anticipo" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al imprimir: {ex.Message}");
            }
        }
    }





    public class impresoraTermica_2
    {
        private readonly Funciones funciones = new Funciones();
        private readonly Contabilidad contabilidad = new Contabilidad();
        private readonly Nombres nombres = new Nombres();
        private readonly Adsiat_Parametros_facturacion adsiat_Parametros_Facturacion = new Adsiat_Parametros_facturacion();
        private readonly Almacen almacen = new Almacen();
        private readonly Empresa empresa = new Empresa();
        private readonly Cobranzas cobranzas = new Cobranzas();
        private readonly SIAT siat = new SIAT();
        private readonly Ventas ventas = new Ventas();

        public async Task ImprimirTexto(DBContext _context, string codempresa, string nombreImpresora, Font fuente, vefactura cabecera)
        {
            string nomEmpresa = await nombres.nombreempresa(_context, codempresa);
            int puntoVta = await adsiat_Parametros_Facturacion.PuntoDeVta(_context, cabecera.codalmacen);
            string dirAlm = await almacen.direccionalmacen(_context, cabecera.codalmacen);
            string telefonoalm = await almacen.telefonoalmacen(_context, cabecera.codalmacen);
            string faxAg = await almacen.faxalmacen(_context, cabecera.codalmacen);
            string sucAlm = await contabilidad.sucursalalm(_context, cabecera.codalmacen);
            string nitEmpresa = await empresa.NITempresa(_context, codempresa);
            string municipioEmpresa = await empresa.municipio_empresa(_context,codempresa);
            string moneda = await nombres.nombremoneda(_context, cabecera.codmoneda);
            int _codDocSector = await adsiat_Parametros_Facturacion.TipoDocSector(_context, cabecera.codalmacen);
            double nroAnticipo = await cobranzas.Recibo_De_Anticipo(_context, cabecera.idanticipo, cabecera.numeroidanticipo ?? 0);
            string nomVendedor = await nombres.nombrevendedor(_context, cabecera.codvendedor);
            string cadena_QR = await adsiat_Parametros_Facturacion.Generar_Cadena_QR_Link_Factura_SIN(_context, nitEmpresa, cabecera.cuf, cabecera.nrofactura.ToString(), "1", cabecera.codalmacen);

            var detalle = await _context.vefactura1.Where(i => i.codfactura == cabecera.codigo)
                .Join(
                    _context.initem,
                    f => f.coditem,
                    i => i.codigo,
                    (f,i) => new {f,i})
                .Select(join => new impDetalleFacturaRollo
                {
                    descripcorta = join.i.descripcorta,
                    medida = join.i.medida,
                    coditem = join.f.coditem,
                    cantidad = join.f.cantidad,
                    total = join.f.total,
                    preciolista = join.f.preciolista,
                    precioneto = join.f.precioneto,
                })
                .OrderBy(i => i.coditem)
                .ToListAsync();
            foreach (var reg  in detalle)
            {
                var oper1 = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context,(reg.preciolista * reg.cantidad));
                var oper2 = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (reg.precioneto * reg.cantidad));
                reg.descuento = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (oper1 - oper2));
            }

            PrintDocument pd = new PrintDocument
            {
                PrinterSettings = { PrinterName = nombreImpresora },
                DocumentName = "Facturacion" + cabecera.id + "-" + cabecera.numeroid
            };

            if (!pd.PrinterSettings.IsValid)
            {
                throw new Exception("La impresora no está disponible o no es válida.");
            }

            pd.PrintPage += async (sender, e) =>
            {
                try
                {
                    // Definir las coordenadas para imprimir
                    float x = 10;
                    float y = 4;
                    int NC = 39;  // Ancho del área de impresión
                    float lineOffset;
                    string cadena = "";
                    // Imprimir el texto pasado como parámetro
                    // Configuración de fuentes
                    Font printFont = new Font("Arial", 10, FontStyle.Bold, GraphicsUnit.Point);
                    Font barcodeFont = new Font("Courier New", 16);   // Substituted to Barcode1 Font
                    
                    //////////////////////////////////////////////////////////////////////////////////////////////
                    //TITULO FACTURA
                    //////////////////////////////////////////////////////////////////////////////////////////////
                    NC = 57;
                    printFont = new Font("Arial", 10, FontStyle.Bold, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    cadena = funciones.CentrarCadena("FACTURA", NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    //////////////////////////////////////////////////////////////////////////////////////////////
                    //TITULO CON DERECHO A CREDITO FISCAL
                    //////////////////////////////////////////////////////////////////////////////////////////////
                    NC = 57;
                    printFont = new Font("Arial", 8, FontStyle.Bold, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    cadena = funciones.CentrarCadena("CON DERECHO A CREDITO FISCAL", NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset + 10; // Espacio adicional después del subtítulo



                    // Nombre de la empresa
                    NC = 57;
                    printFont = new Font("Arial", 10, FontStyle.Bold, GraphicsUnit.Point);
                    cadena = funciones.CentrarCadena(nomEmpresa, NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                    // Ajuste para la siguiente línea
                    printFont = new Font("Consolas", 7, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    NC = 49;
                    y += lineOffset + 10;

                    // Casa Matriz
                    cadena = funciones.CentrarCadena("Casa Matriz", NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    // Dirección General Acha N°330
                    cadena = funciones.CentrarCadena("Gral. Acha N°330", NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    // Ciudad
                    cadena = funciones.CentrarCadena("Cochabamba-Bolivia", NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    // Teléfonos
                    cadena = funciones.CentrarCadena("Telfs.: 4259660 Fax:4111282", NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset * 2;

                    // Sucursal
                    cadena = funciones.CentrarCadena("Sucursal Nro.: " + sucAlm, NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    // Punto de Venta
                    cadena = funciones.CentrarCadena("Punto de Vta.: " + puntoVta, NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    // Dirección del Almacén
                    cadena = funciones.CentrarCadena(dirAlm, NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    // Teléfono del Almacén
                    cadena = funciones.CentrarCadena("Telefono: " + telefonoalm, NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    // Fax del Almacén (solo si existe)
                    if (!string.IsNullOrEmpty(faxAg) && faxAg != "0")
                    {
                        cadena = funciones.CentrarCadena("Fax: " + faxAg, NC, " ");
                        e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                        y += lineOffset;
                    }
                    else
                    {
                        y += lineOffset;
                    }


                    /*
                     
                    'dsd 10-03-2022 ya no se debe imprimir el dato de SFC segun normativa segun bryan
                    '//lugar emision
                    'cadena = sia_funciones.Funciones.Instancia.centrarcadena(sia_funciones.Almacen.Instancia.lugaralmacen(codalmacen.Text), NC, " ")
                    'e.Graphics.DrawString(cadena, printFont, System.Drawing.Brushes.Black, x, y)
                    'y += lineOffset

                    '//sfc
                    'dsd 10-03-2022 ya no se debe imprimir el dato de SFC segun normativa
                    'cadena = sia_funciones.Funciones.Instancia.centrarcadena("SFC-" & sia_funciones.Contabilidad.Instancia.numero_sistema_facturacion_computarizada_cufd(codalmacen.Text, cufd.Text), NC, " ")
                    'e.Graphics.DrawString(cadena, printFont, System.Drawing.Brushes.Black, x, y)
                    'y += lineOffset


                    '//actividad economica
                    'dsd 10-03-2022 ya no se debe imprimir el dato de SFC segun normativa segun bryan
                    'printFont = New System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point)
                    'lineOffset = printFont.GetHeight(e.Graphics)
                    'NC = 60
                    'cadena = sia_funciones.Funciones.Instancia.centrarcadena("ACTIVIDAD ECONOMICA", NC, " ")
                    'e.Graphics.DrawString(cadena, printFont, System.Drawing.Brushes.Black, x, y)
                    'y += lineOffset

                    'NC = 60
                    'printFont = New System.Drawing.Font("Arial Narrow", 8, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point)
                    'lineOffset = printFont.GetHeight(e.Graphics)

                    'cadena = sia_funciones.Almacen.Instancia.actividadalmacen(sia_compartidos.temporales.Instancia.almacenactual)
                    'resultado_filas = sia_funciones.Funciones.Instancia.Dividir_cadena_en_filas(cadena, NC)
                    'For i = 0 To resultado_filas.Length - 1
                    '    If Not IsNothing(resultado_filas(i)) Then
                    '        'e.Graphics.DrawString(sia_funciones.Funciones.Instancia.centrarcadena(resultado_filas(i), NC, " "), printFont, System.Drawing.Brushes.Black, x, y)
                    '        e.Graphics.DrawString(sia_funciones.Funciones.Instancia.centrarcadena(resultado_filas(i), NC, " "), printFont, System.Drawing.Brushes.Black, x, y)
                    '        y += lineOffset
                    '    End If
                    'Next
                    'y += lineOffset

                     
                     */
                    //////////////////////////////////////////////////////////////////////////////////////////////
                    //datos del nro de factura y dosificacion
                    //////////////////////////////////////////////////////////////////////////////////////////////
                    NC = 46;
                    printFont = new Font("Consolas", 8, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    // Línea de separación
                    cadena = "----------------------------------------";
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    // NIT de la empresa
                    cadena = "NIT         :  " + nitEmpresa;
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    // Número de factura
                    cadena = "FACTURA No. :  " + cabecera.nrofactura.ToString("0000000000");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    // Código de autorización (CUF), dividido en dos líneas si es necesario
                    int nc = 42;
                    cadena = "COD. AUTORIZACION: " + cabecera.cuf;
                    string[] cadenaCuf = funciones.CortarCadena_CUF(cadena, nc); // Función para cortar la cadena en varias líneas

                    e.Graphics.DrawString(cadenaCuf[0], printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    if (cadenaCuf.Length > 1)
                    {
                        e.Graphics.DrawString(cadenaCuf[1], printFont, Brushes.Black, x, y);
                        y += lineOffset;
                    }
                    /*
                     
                     'resultado_filas = sia_funciones.Funciones.Instancia.Dividir_cadena_en_filas(cadena, NC)
                    'For i = 0 To resultado_filas.Length - 1
                    '    If Not IsNothing(resultado_filas(i)) Then
                    '        e.Graphics.DrawString(resultado_filas(i), printFont, System.Drawing.Brushes.Black, x, y)
                    '        y += lineOffset
                    '    End If
                    'Next
                    'e.Graphics.DrawString(cadena, printFont, System.Drawing.Brushes.Black, x, y)
                    'y += lineOffset

                     */
                    cadena = "----------------------------------------";
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    //////////////////////////////////////////////////////////////////////////////////////////////
                    //////////////////////////////////////////////////////////////////////////////////////////////
                    //datos del cliente
                    //////////////////////////////////////////////////////////////////////////////////////////////

                    NC = 41;
                    printFont = new Font("Arial Narrow", 9, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);

                    /*
                     
                    '//lugar y fecha de emision de la factura hasta 28-06-2022 ya q las horas de registro no entraba
                    'cadena = "LUGAR Y FECHA EMISION: " & sia_funciones.Empresa.Instancia.municipio_empresa(sia_compartidos.temporales.Instancia.codempresa) & " " & fecha.Value.Date.ToShortDateString & " Hrs:" & sia_funciones.Ventas.Instancia.Horareg_De_Factura(id.Text, numeroid.Text)
                    'e.Graphics.DrawString(cadena, printFont, System.Drawing.Brushes.Black, x, y)
                    'y += lineOffset
                     
                     */

                    // lugar y fecha de emision de la factura desde 28-06-2022 ya q las horas de registro no entraba
                    cadena = "LUGAR Y FECHA EMISION: " + municipioEmpresa + " " +
                    cabecera.fecha.ToShortDateString() + " Hrs: " + cabecera.horareg;

                    // Dividir la cadena en filas y dibujar en el gráfico
                    string[] resultadoFilas = funciones.Dividir_cadena_en_filas(cadena, nc);
                    foreach (var fila in resultadoFilas)
                    {
                        if (!string.IsNullOrEmpty(fila))
                        {
                            e.Graphics.DrawString(fila, printFont, Brushes.Black, x, y);
                            y += lineOffset;
                        }
                    }

                    // Nombre del cliente
                    cadena = "NOMBRE / RAZON SOCIAL: " + cabecera.nomcliente;
                    resultadoFilas = funciones.Dividir_cadena_en_filas(cadena, nc);
                    foreach (var fila in resultadoFilas)
                    {
                        if (!string.IsNullOrEmpty(fila))
                        {
                            e.Graphics.DrawString(fila, printFont, Brushes.Black, x, y);
                            y += lineOffset;
                        }
                    }

                    // NIT del cliente
                    cadena = "NIT/CI/CEX                           : " +
                             funciones.Rellenar(cabecera.nit + cabecera.complemento_ci, 15, " ", false);
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    // Código del cliente
                    cadena = "CODIGO CLIENTE               : " +
                             funciones.Rellenar(cabecera.nit + cabecera.complemento_ci, 15, " ", false);
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;


                    //////////////////////////////////////////////////////////////////////////////////////////////
                    //titulo de las columnas del detalle
                    //////////////////////////////////////////////////////////////////////////////////////////////

                    NC = 57;
                    printFont = new Font("Consolas", 8, FontStyle.Bold, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    cadena = "----------------------------------------";
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    /*
                     
                    ''cadena = " PRODUCTO                        MEDIDA " ' antes de NSF
                    ''cadena = " CODIGO PRODUCTO  DESCRIPCION UD MEDIDA " ' penualtimo despues de NSF 
                    'cadena = "# CODIGO PRODUCTO      DESCRIPCION      "
                    'e.Graphics.DrawString(cadena, printFont, System.Drawing.Brushes.Black, x, y)
                    'y += lineOffset

                    ''cadena = " UD     CANT      PRECIO        IMPORTE " ' antes de NSF
                    ''cadena = "    CANTIDAD PRECIO DESCUENTO  SUBTOTAL " ' penualtimo despues de NSF 
                    'cadena = "UNIDAD   CANTIDAD  PRECIO  DESC. SUBTOTAL"
                    'e.Graphics.DrawString(cadena, printFont, System.Drawing.Brushes.Black, x, y)
                    'y += lineOffset
                    ''cadena = "MEDIDA  CANTIDAD PRECIO DESC.  SUBTOTAL "
                    'cadena = "DE MEDIDA                               "
                    'e.Graphics.DrawString(cadena, printFont, System.Drawing.Brushes.Black, x, y)
                    'y += lineOffset

                     */

                    NC = 80; // o 57 si deseas cambiar el valor
                    printFont = new Font("Arial Narrow", 9, FontStyle.Bold, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);

                    // Centrar el texto "DETALLE"
                    cadena = funciones.CentrarCadena("DETALLE", NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    // Cambiar el tipo de fuente
                    printFont = new Font("Consolas", 8, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);

                    // Dibujar la línea de separación
                    cadena = "----------------------------------------";
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    //////////////////////////////////////////////////////////////////////////////////////////////
                    ///CUERPO DE LA FACTURA
                    //////////////////////////////////////////////////////////////////////////////////////////////
                    printFont = new Font("Arial Narrow", 9, FontStyle.Bold, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    double descuento = 0;
                    string descripcorta = "";
                    foreach (var reg in detalle)
                    {
                        printFont = new Font("Arial Narrow", 9, FontStyle.Bold, GraphicsUnit.Point);
                        lineOffset = printFont.GetHeight(e.Graphics);

                        cadena = "" + funciones.Rellenar(reg.coditem,8," ", false) + " - " + funciones.Rellenar(reg.descripcorta,19, " ", false) + funciones.Rellenar(reg.medida,15, " ", false);
                        e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                        y += lineOffset;
                        /*
                         
                        '.DrawString("UUU 99,999.99  9,999.999   9999,999.99")
                        '//en fecha 04-12-2014 se añadio un decimal se consensuo con norka y mariela
                        'cadena = sia_funciones.Funciones.Instancia.rellenar(CStr(tabladetalle.Rows(i)("udm")), 3, " ") & " " & sia_funciones.Funciones.Instancia.rellenar(CDbl(tabladetalle.Rows(i)("cantidad")).ToString("####,##0.000", new CultureInfo("en-US")), 11, " ") & "  " & sia_funciones.Funciones.Instancia.rellenar(CDbl(tabladetalle.Rows(i)("preciodist")).ToString("#,##0.000#"), 9, " ") & "  " & sia_funciones.Funciones.Instancia.rellenar(CDbl(tabladetalle.Rows(i)("totaldist")).ToString("####,##0.000", new CultureInfo("en-US")), 11, " ")
                        'ya  no en 2 decimales
                        'cadena = sia_funciones.Funciones.Instancia.rellenar(CStr(tabladetalle.Rows(i)("udm")), 3, " ") & " " & sia_funciones.Funciones.Instancia.rellenar(CDbl(tabladetalle.Rows(i)("cantidad")).ToString("####,##0.000", new CultureInfo("en-US")), 9, " ") & " " & sia_funciones.Funciones.Instancia.rellenar(CDbl(Math.Round(tabladetalle.Rows(i)("precioneto"), 2, MidpointRounding.AwayFromZero)).ToString("#,##0.00#"), 7, " ") & " " & sia_funciones.Funciones.Instancia.rellenar(CDbl(0.0).ToString("####,##0.000", new CultureInfo("en-US")), 8, " ") & "  " & sia_funciones.Funciones.Instancia.rellenar(CDbl(Math.Round(tabladetalle.Rows(i)("total"), 2, MidpointRounding.AwayFromZero)).ToString("####,##0.000", new CultureInfo("en-US")), 9, " ")
                        'e.Graphics.DrawString(cadena, printFont, System.Drawing.Brushes.Black, x, y)
                        'y += lineOffset

                         */
                        printFont = new Font("Arial Narrow", 9, FontStyle.Regular, GraphicsUnit.Point);
                        lineOffset = printFont.GetHeight(e.Graphics);

                        // si incluir monto descuento
                        if (_codDocSector == 1)
                        {
                            cadena = "" 
                            + funciones.Rellenar(reg.cantidad.ToString("####,##0.000", new CultureInfo("en-US")), 12, " ", false) 
                            + " X " 
                            + funciones.Rellenar(reg.preciolista.ToString("#,##0.00#", new CultureInfo("en-US")), 13, " ", false) 
                            + " - " 
                            + funciones.Rellenar(reg.descuento.ToString("####,##0.000", new CultureInfo("en-US")), 5, " ") 
                            + " " 
                            + funciones.Rellenar(reg.total.ToString("####,##0.000", new CultureInfo("en-US")), 35, " ");
                            
                            e.Graphics.DrawString(cadena, printFont, System.Drawing.Brushes.Black, x, y);
                            y += lineOffset;
                        }
                        else
                        {
                            cadena = ""
                            + funciones.Rellenar(reg.cantidad.ToString("####,##0.000", new CultureInfo("en-US")), 12, " ", false)
                            + " X "
                            + funciones.Rellenar(reg.preciolista.ToString("#,##0.00000", new CultureInfo("en-US")), 9, " ", false)
                            + " - "
                            + funciones.Rellenar(reg.descuento.ToString("####,##0.00000", new CultureInfo("en-US")), 10, " ", false)
                            + " "
                            + funciones.Rellenar(reg.total.ToString("####,##0.00000", new CultureInfo("en-US")), 30, " ");

                            e.Graphics.DrawString(cadena, printFont, System.Drawing.Brushes.Black, x, y);
                            y += lineOffset;
                        }
                    }

                    printFont = new Font("Consolas", 8, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    cadena = "----------------------------------------";
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;


                    //////////////////////////////////////////////////////////////////////////////////////////////
                    ///total de la factura
                    //////////////////////////////////////////////////////////////////////////////////////////////
                    printFont = new Font("Consolas", 8, FontStyle.Bold, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);

                    // Subtotal
                    cadena = "SubTotal(BS):" + funciones.Rellenar(Convert.ToDouble(cabecera.subtotal).ToString("####,##0.000", new CultureInfo("en-US")), 26, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    // Descuentos
                    double descuentos1 = 0; // Si se utiliza en otra parte, asegúrate de asignarle un valor apropiado
                    cadena = "Descuentos(BS):" + funciones.Rellenar(Convert.ToDouble(cabecera.descuentos).ToString("####,##0.000", new CultureInfo("en-US")), 24, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    // Total
                    cadena = "Total(BS):" + funciones.Rellenar(Convert.ToDouble(cabecera.total).ToString("####,##0.000", new CultureInfo("en-US")), 29, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    // Importe Base Crédito Fiscal
                    cadena = "Importe Base Credito Fiscal(BS):";
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;

                    // Línea con total
                    cadena = "_________________________" + funciones.Rellenar(Convert.ToDouble(cabecera.total).ToString("####,##0.000", new CultureInfo("en-US")), 14, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;
                    y += lineOffset;

                    // Total en literal
                    string literal = "Son: " + funciones.ConvertDecimalToWords(cabecera.total).ToUpper() + " " + moneda;
                    cadena = literal.Trim();
                    NC = 42;
                    string[] resultado_filas = funciones.Dividir_cadena_en_filas(cadena, NC);
                    for (int i = 0; i < resultado_filas.Length; i++)
                    {
                        if (resultado_filas[i] != null)
                        {
                            e.Graphics.DrawString(resultado_filas[i], printFont, Brushes.Black, x, y);
                            y += lineOffset;
                        }
                    }

                    /*
                     
                    'dsd 10-03-2022 ya no se debe imprimir el dato de SFC segun normativa segun bryan
                    '//////////////////////////////////////////////////////////////////////////////////////////////
                    '//equivalencias en moneda extranjera
                    '//////////////////////////////////////////////////////////////////////////////////////////////
                    'cadena = Trim("Equivalentes a: " & CDbl(sia_funciones.TipoCambio.Instancia.conversion(sia_funciones.TipoCambio.Instancia.monedatdc(sia_compartidos.temporales.Instancia.usuario, sia_compartidos.temporales.Instancia.codempresa), codmoneda.Text, fecha.Value.Date, CDbl(total.Text))).ToString("###,##0.00") & " " & sia_funciones.Nombres.Instancia.nombremoneda(sia_funciones.TipoCambio.Instancia.monedatdc(sia_compartidos.temporales.Instancia.usuario, sia_compartidos.temporales.Instancia.codempresa)))
                    'NC = 42
                    'resultado_filas = sia_funciones.Funciones.Instancia.Dividir_cadena_en_filas(cadena, NC)
                    'For i = 0 To resultado_filas.Length - 1
                    '    If Not IsNothing(resultado_filas(i)) Then
                    '        e.Graphics.DrawString(resultado_filas(i), printFont, System.Drawing.Brushes.Black, x, y)
                    '        y += lineOffset
                    '    End If
                    'Next
                     
                     */
                    //////////////////////////////////////////////////////////////////////////////////////////////
                    //recibo del anticipo
                    //////////////////////////////////////////////////////////////////////////////////////////////
                    if (string.IsNullOrEmpty(cabecera.idanticipo))
                    {
                        // Si idanticipo está vacío, no se hace nada.
                    }
                    else
                    {
                        try
                        {
                            cadena = "ANT: " + cabecera.idanticipo + "-" + cabecera.numeroidanticipo + " Rec." + nroAnticipo;
                            e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                            y += lineOffset;
                        }
                        catch (Exception ex)
                        {
                            // NADA
                        }
                    }
                    

                    ////////////////////////////////////////////////////////////////////////////////////////////////
                    // Código de control
                    ////////////////////////////////////////////////////////////////////////////////////////////////
                    // Se quitó desde 10-03-2022
                    // cadena = "Codigo De Control: " + sia_funciones.Ventas.Instancia.CodigoControlDeFactura(id.Text, numeroid.Text);
                    // e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    // y += lineOffset;

                    ////////////////////////////////////////////////////////////////////////////////////////////////
                    // Fecha límite de emisión
                    ////////////////////////////////////////////////////////////////////////////////////////////////
                    // Se quitó desde 10-03-2022
                    // cadena = "Fecha Limite De Emision: " + sia_funciones.Ventas.Instancia.nroautorizacion_fechalimiteDate(cufd.Text).ToShortDateString();
                    // e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    // y += lineOffset;

                    cadena = "----------------------------------------";
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                    //////////////////////////////////////////////////////////////////////////////////////////////
                    //leyenda advertencia
                    //////////////////////////////////////////////////////////////////////////////////////////////
                    printFont = new System.Drawing.Font("Arial Narrow", 8, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);

                    NC = 47; // 42
                    resultado_filas = funciones.Dividir_cadena_en_filas("ESTA FACTURA CONTRIBUYE AL DESARROLLO DEL PAÍS, EL USO ILÍCITO DE ESTA SERÁ SANCIONADO PENALMENTE DE ACUERDO A LEY.", NC);
                    for (int i = 0; i < resultado_filas.Length; i++)
                    {
                        if (resultado_filas[i] != null) // En C#, se usa 'null' en lugar de 'IsNothing'
                        {
                            e.Graphics.DrawString(funciones.CentrarCadena(resultado_filas[i], NC, " "), printFont, System.Drawing.Brushes.Black, x, y);
                            y += lineOffset;
                        }
                    }
                    y += lineOffset;


                    //////////////////////////////////////////////////////////////////////////////////////////////
                    //impresion de la leyenda
                    //////////////////////////////////////////////////////////////////////////////////////////////
                    printFont = new System.Drawing.Font("Arial Narrow", 8, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);

                    e.Graphics.DrawString("", printFont, Brushes.Black, x, y);

                    NC = 60;
                    // cadena = sia_funciones.Ventas.Instancia.leyenda_dosificacion(cufd.Text);
                    // cadena = sia_funciones.Ventas.Instancia.leyenda_CUFD(cufd.Text);
                    cadena = cabecera.leyenda;
                    if (cadena.Trim().Length > 0)
                    {
                        resultado_filas = funciones.Dividir_cadena_en_filas(cadena, NC);
                        for (int i = 0; i < resultado_filas.Length; i++)
                        {
                            if (resultado_filas[i] != null) // En C#, se usa 'null' en lugar de 'IsNothing'
                            {
                                e.Graphics.DrawString(funciones.CentrarCadena(resultado_filas[i], NC, " "), printFont, System.Drawing.Brushes.Black, x, y);
                                y += lineOffset;
                            }
                        }
                    }
                    y += lineOffset;

                    ///////////////////////////////
                    //nueva leyenda o frase de SIN
                    //esta leyenda es la que indica si es: EN LINEA o NO EN LINEA

                    
                    printFont = new System.Drawing.Font("Arial Narrow", 8, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    NC = 60;
                    resultado_filas = funciones.Dividir_cadena_en_filas(ventas.leyenda_para_factura_en_linea(cabecera.en_linea ?? false), NC);
                    for (int i = 0; i < resultado_filas.Length; i++)
                    {
                        if (resultado_filas[i] != null) // En C#, se usa 'null' en lugar de 'IsNothing'
                        {
                            e.Graphics.DrawString(funciones.CentrarCadena(resultado_filas[i], NC, " "), printFont, System.Drawing.Brushes.Black, x, y);
                            y += lineOffset;
                        }
                    }

                    //////////////////////////////////////////////////////////////////////////////////////////////
                    //codigo QR
                    //////////////////////////////////////////////////////////////////////////////////////////////
                    // Dim cadena_QR As String = generar_cadena_qr()
                    e.Graphics.DrawString(cadena_QR, barcodeFont, Brushes.Black, x + 60, y + 10);

                    y += lineOffset + 120; // 90

                    // Leyenda q la factura se encuentra en la pagina web de pertec
                    NC = 60;
                    cadena = "Esta factura se encuentra tambien disponible en el siguiente enlace: https://www.pertec.com.bo :";
                    resultado_filas = funciones.Dividir_cadena_en_filas(cadena,NC);
                    for (int i = 0; i < resultado_filas.Length; i++)
                    {
                        if (resultado_filas[i] != null) // En C#, se usa 'null' en lugar de 'IsNothing'
                        {
                            e.Graphics.DrawString(resultado_filas[i], printFont, System.Drawing.Brushes.Black, x, y);
                            y += lineOffset;
                        }
                    }
                    y += lineOffset;

                    // Codigo Factura Web
                    NC = 84;
                    cadena = "Código Web: " + cabecera.codfactura_web;
                    cadena = funciones.CentrarCadena(cadena, NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;
                    y += lineOffset;

                    /*
                     
                    'NC = 60
                    'cadena = "Codigo Web: " & sia_funciones.Ventas.Instancia.Codigo_de_Factura_Web(CInt(codigo.Text))
                    'resultado_filas = sia_funciones.Funciones.Instancia.Dividir_cadena_en_filas(cadena, NC)
                    'For i = 0 To resultado_filas.Length - 1
                    '    If Not IsNothing(resultado_filas(i)) Then
                    '        'e.Graphics.DrawString(resultado_filas(i), printFont, System.Drawing.Brushes.Black, x, y)
                    '        e.Graphics.DrawString(sia_funciones.Funciones.Instancia.centrarcadena(resultado_filas(i), NC, " "), printFont, System.Drawing.Brushes.Black, x, y)
                    '        y += lineOffset
                    '    End If
                    'Next
                    'y += lineOffset

                    'y += lineOffset

                     */

                    // ID-numeroid factua y vendedor
                    cadena = cabecera.id + "-" + cabecera.numeroid + " Vendedor: " + cabecera.codvendedor + " " + nomVendedor;
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                    // publicidad pernos-tuercas-tornillos
                    NC = 46;
                    printFont = new Font("Consolas", 8, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    y += lineOffset;
                    cadena = funciones.CentrarCadena("***PERNOS-TUERCAS-TORNILLOS***", NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                    // Indicate that no more data to print, and the Print Document can now send the print data to the spooler.
                    e.HasMorePages = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            };

            pd.Print();
        }


        public async Task ImprimirAnticipo(DBContext _context, string codempresa, string nombreImpresora, Font fuente, string tipo_impresion_anticipo, int codFactura)
        {
            string nomEmpresa = await nombres.nombreempresa(_context, codempresa);
            string nitEmpresa = await empresa.NITempresa(_context, codempresa);

            var datosFact = await _context.vefactura.Where(i => i.codigo == codFactura).Select(i => new
            {
                i.id,
                i.numeroid,
                i.nrofactura,
                i.idanticipo,
                i.numeroidanticipo,
                i.monto_anticipo,
                i.codmoneda,
                i.fecha,
                i.nomcliente,
                i.nit,


            }).FirstOrDefaultAsync();

            double nroRecibo = await cobranzas.Recibo_De_Anticipo(_context, datosFact.idanticipo, datosFact.numeroidanticipo ?? 0);
            DateTime fechaAnti = await cobranzas.Fecha_De_Anticipo(_context, datosFact.idanticipo, datosFact.numeroidanticipo ?? 0);

            PrintDocument pd = new PrintDocument
            {
                PrinterSettings = { PrinterName = nombreImpresora },
                DocumentName = "Anticipo"
            };

            if (!pd.PrinterSettings.IsValid)
            {
                throw new Exception("La impresora no está disponible o no es válida.");
            }

            pd.PrintPage += async (sender, e) =>
            {
                try
                {
                    // Definir las coordenadas para imprimir
                    float x = 10;
                    float y = 4;
                    int NC = 47;  // Ancho del área de impresión
                    float lineOffset;
                    string cadena = "";
                    // Imprimir el texto pasado como parámetro
                    // Configuración de fuentes
                    Font printFont = new Font("Consolas", 7, FontStyle.Regular, GraphicsUnit.Point);
                    Font barcodeFont = new Font("Courier New", 16);   // Substituted to Barcode1 Font

                    e.Graphics.PageUnit = GraphicsUnit.Point;
                    printFont = new Font("Arial", 10, FontStyle.Bold, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);

                    // --------------------------------------------------------------------------------

                    // imprimir el ancticipo si tiene

                    if (datosFact.idanticipo.Trim() != "")
                    {
                        //nombre empresa
                        cadena = nomEmpresa;
                        e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                        // nit
                        y += lineOffset + 2; // Espacio adicional después del subtítulo
                        printFont = new Font("Consolas", 7, FontStyle.Regular, GraphicsUnit.Point);
                        lineOffset = printFont.GetHeight(e.Graphics);
                        cadena = "NIT:" + nitEmpresa;
                        e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);


                        // titulo
                        y += lineOffset;
                        printFont = new Font("Consolas", 7, FontStyle.Bold, GraphicsUnit.Point);
                        lineOffset = printFont.GetHeight(e.Graphics);
                        cadena = funciones.CentrarCadena("REVERSION DE ANTICIPO", NC, " ");
                        e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                        // imprime la etiqueta si es ORIGINAL o COPIA
                        y += lineOffset;
                        printFont = new Font("Consolas", 7, FontStyle.Regular, GraphicsUnit.Point);
                        lineOffset = printFont.GetHeight(e.Graphics);
                        cadena = funciones.CentrarCadena(tipo_impresion_anticipo, NC, " ");
                        e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                        // fecha
                        y += lineOffset;
                        printFont = new Font("Consolas", 7, FontStyle.Regular, GraphicsUnit.Point);
                        lineOffset = printFont.GetHeight(e.Graphics);
                        cadena = funciones.CentrarCadena("Fecha: " + datosFact.fecha.Date.ToShortDateString() , NC, " ");
                        e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                        // reversion del anticipo
                        y += lineOffset;
                        y += lineOffset;
                        NC = 46;
                        cadena = "Reversión del anticipo " + datosFact.idanticipo + "-" + datosFact.numeroidanticipo + " con Nro de Recibo " + nroRecibo.ToString();

                        cadena += " De fecha " + fechaAnti.ToShortDateString();

                        cadena += " A la factura " + datosFact.id + "-" + datosFact.numeroid +
                                  " Nro." + datosFact.nrofactura +
                                  " Por un monto de: " + datosFact.monto_anticipo +
                                  " " + datosFact.codmoneda;
                        var resultado_filas = funciones.Dividir_cadena_en_filas(cadena, NC);

                        for (int i = 0; i < resultado_filas.Length; i++)
                        {
                            if (resultado_filas[i] != null)
                            {
                                e.Graphics.DrawString(funciones.CentrarCadena(resultado_filas[i], NC, " "), printFont, Brushes.Black, x, y);
                                y += lineOffset;
                            }
                        }

                        // lineas para la firma
                        y += lineOffset;
                        y += lineOffset;
                        y += lineOffset;
                        y += lineOffset;
                        cadena = "-------------------------------------";
                        e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                        // nombre del cliente
                        NC = 41;
                        printFont = new Font("Arial Narrow", 9, FontStyle.Regular, GraphicsUnit.Point);
                        lineOffset = printFont.GetHeight(e.Graphics);
                        y += lineOffset;
                        cadena = datosFact.nomcliente;
                        e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                        // nit del cliente
                        printFont = new Font("Consolas", 7, FontStyle.Regular, GraphicsUnit.Point);
                        lineOffset = printFont.GetHeight(e.Graphics);
                        y += lineOffset + 5;
                        cadena = datosFact.nit;
                        e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                        // adverencia
                        y += lineOffset;
                        y += lineOffset;
                        cadena = funciones.CentrarCadena("***NO VALIDO PARA CREDITO FISCAL***", NC, " ");
                        e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                        y += lineOffset;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            };
            pd.Print();
        }

    }


    public class impDetalleFacturaRollo
    {
        public string descripcorta { get; set; }
        public string medida { get; set; }
        public string coditem { get; set; }
        public decimal cantidad { get; set; }
        public decimal total { get; set; }
        public decimal preciolista { get; set; }
        public decimal precioneto { get; set; }
        public decimal descuento { get; set; } = 0;
    }



    public class dataDetalleFactura
    {
        public int codfactura { get; set; } = 0;
        public string coditem { get; set; } = "";
        public string descripcion { get; set; } = "";
        public string medida { get; set; } = "";
        public string udm { get; set; } = "";
        public decimal porceniva { get; set; } = 0;

        public string niveldesc { get; set; } = "";
        public decimal cantidad { get; set; } = 0;
        public int codtarifa { get; set; } = 0;
        public int coddescuento { get; set; } = 0;
        public decimal precioneto { get; set; } = 0;

        public decimal preciodesc { get; set; } = 0;
        public decimal preciolista { get; set; } = 0;
        public decimal total { get; set; } = 0;
        public bool cumple { get; set; } = true;

        public decimal distdescuento { get; set; } = 0;
        public decimal distrecargo { get; set; } = 0;
        public decimal preciodist { get; set; } = 0;
        public decimal totaldist { get; set; } = 0;
        public string codaduana { get; set; } = "";
        public int codgrupomer { get; set; } = 0;
        public decimal peso { get; set; } = 0;
        public string codproducto_sin { get; set; } = "";
    }
    public class recargosData
    {
        public int codrecargo { get; set; } = 0;
        public string descripcion { get; set; } = "";
        public decimal porcen { get; set; } = 0;
        public decimal monto { get; set; } = 0;
        public string moneda { get; set; } = "";
        public decimal montodoc { get; set; } = 0;
        public int codcobranza { get; set; } = 0;
    }

    public class descuentosData
    {
        public int codproforma { get; set; } = 0;
        public int coddesextra { get; set; } = 0;
        public string descripcion { get; set; } = "";
        public decimal porcen { get; set; } = 0;
        public decimal montodoc { get; set; } = 0;
        public int codcobranza { get; set; } = 0;
        public int codcobranza_contado { get; set; } = 0;
        public int codanticipo { get; set; } = 0;

        public string aplicacion { get; set; } = "";
        public string codmoneda { get; set; } = "";
    }

    public class ivaData
    {
        public int codfactura { get; set; } = 0;
        public decimal porceniva { get; set; } = 0;
        public decimal total { get; set; } = 0;
        public decimal porcenbr { get; set; } = 0;
        public decimal br { get; set; } = 0;
        public decimal iva { get; set; } = 0;
    }

    public class RequestTotalizar
    {
        public List<dataDetalleFactura> detalleItems { get; set; }
        public List<recargosData>? recargosTabla { get; set; }
        public List<descuentosData>? descuentosTabla { get; set; }
    }






    // samuel
    // CLASES AUXILIARES

    
    public class Total_Detalle_FacturaTienda
    {
        public double total_iva { get; set; } = 0;
        public double Desctos { get; set; } = 0;
        public double Recargos { get; set; } = 0;
        public double Total_factura { get; set; } = 0;
        public double Total_Dist { get; set; } = 0;
    }

    public class dataDosificaCajaTienda
    {
        public string nrocaja { get; set; }
        public string cufd { get; set; }
        public string codigo_control { get; set; }
        public DateTime dtpfecha_limite { get; set; }
        public string nrolugar { get; set; }
        public string tipo { get; set; }
        public string codtipo_comprobante { get; set; }
        public string codtipo_comprobantedescripcion { get; set; }

    }

    public class dataCrearGrabarFacturaTienda
    {
        public string idfactura { get; set; }
        public int nrocaja { get; set; }
        public string factnit { get; set; }
        public string condicion { get; set; }
        public string nrolugar { get; set; }
        public string tipo { get; set; }
        public string codtipo_comprobante { get; set; }
        public string usuario { get; set; }
        public string codempresa { get; set; }
        public int codtipopago { get; set; }
        public string codbanco { get; set; }
        public string codcuentab { get; set; }
        public string nrocheque { get; set; }
        public string idcuenta { get; set; }
        public string cufd { get; set; }
        public string complemento_ci { get; set; }
        public DateTime dtpfecha_limite { get; set; }
        public string codigo_control { get; set; }
        public string factnomb { get; set; }
        public string ids_proforma { get; set; }
        public int nro_id_proforma { get; set; }

        public vefactura cabecera { get; set; }
        public List<dataDetalleFactura> detalle { get; set; }
        public List<tablaFacturas> dgvfacturas { get; set; }

        //public List<vedetalleanticipoProforma>? detalleAnticipos_fact { get; set; }
        public List<vedesextraDatos>? detalleDescuentos_fact { get; set; }
        //public List<vedetalleEtiqueta> detalleEtiqueta_fact { get; set; }
        public List<itemDataMatriz> detalleItemsProf_fact { get; set; }
        public List<verecargosDatos>? detalleRecargos_fact { get; set; }
        public List<Controles>? detalleControles_fact { get; set; }
    }
    //public class recargosData
    //{
    //    public int codrecargo { get; set; } = 0;
    //    public string descripcion { get; set; } = "";
    //    public decimal porcen { get; set; } = 0;
    //    public decimal monto { get; set; } = 0;
    //    public string moneda { get; set; } = "";
    //    public decimal montodoc { get; set; } = 0;
    //    public int codcobranza { get; set; } = 0;
    //}

    //public class descuentosData
    //{
    //    public int codfactura { get; set; } = 0;
    //    public int coddesextra { get; set; } = 0;
    //    public string descripcion { get; set; } = "";
    //    public decimal porcen { get; set; } = 0;
    //    public decimal montodoc { get; set; } = 0;
    //    public int codcobranza { get; set; } = 0;
    //    public string aplicacion { get; set; } = "";
    //    public string codmoneda { get; set; } = "";
    //}


}
