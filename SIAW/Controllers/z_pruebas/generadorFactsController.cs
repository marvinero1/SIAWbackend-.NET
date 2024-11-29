using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SIAW.Controllers.ventas.transaccion;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;
using siaw_ws_siat;
using System.Data;

namespace SIAW.Controllers.z_pruebas
{
    [Route("api/pruebas/[controller]")]
    [ApiController]
    public class generadorFactsController : ControllerBase
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

        private readonly string _controllerName = "prgfacturarNR_cufdController";

        public generadorFactsController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        [HttpPost]
        [Route("generarFacturasNR/{userConn}/{usuario}/{codempresa}/{fechaInicio}/{fechaFin}")]
        public async Task<ActionResult<List<sldosItemCompleto>>> generarFacturasNR(string userConn, string usuario, string codempresa, DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var facturasbyFecha = await _context.vefactura
                        .Select(i => new
                        {
                            i.codigo,
                            i.id,
                            i.numeroid,

                            i.total,
                            i.subtotal,
                            i.recargos,
                            i.descuentos,
                            i.fecha,
                            i.codremision,
                            i.codalmacen,
                            i.idcuenta,
                            i.nrocheque,
                            i.codcuentab,
                            i.codbanco,
                            i.tipo,
                            i.nrolugar,
                            i.condicion,
                            i.codtipo_comprobante,
                            i.nit,
                            i.tipopago,
                            i.complemento_ci,
                            i.nomcliente,
                        })
                        .Where(i => i.fecha >= fechaInicio && i.fecha <= fechaFin && i.codremision!=0).ToListAsync();
                    DateTime fecha = DateTime.Now.Date;
                    if (facturasbyFecha.Count() == 0)
                    {
                        return BadRequest("No se encontraron facturas en las fechas especificar verifique");
                    }
                    Datos_Dosificacion_Activa datos_dosificacion_Activa = await siat.Obtener_Cufd_Dosificacion_Activa(_context, fecha, facturasbyFecha[0].codalmacen);
                    if (datos_dosificacion_Activa.resultado == false)
                    {
                        return BadRequest(new { resp = "No se encontro dosificacion de CUFD activa para el almacen: " });
                    }
                    foreach (var reg in facturasbyFecha)
                    {
                        // comienza la generacion de Facturas de NR.
                        var facturas = await GENERAR_FACTURA_DE_NR(_context, false, codempresa, reg.codremision ?? 0, datos_dosificacion_Activa.nrocaja);
                        
                        
                        // Luego de Generar Facturas se graba directamente.
                        var cabeceraNR = await _context.veremision.Where(i => i.codigo == reg.codremision).FirstOrDefaultAsync();


                        dataCrearGrabarFacturas dataCreGrbFact = new dataCrearGrabarFacturas();
                        dataCreGrbFact.idfactura = reg.id;
                        dataCreGrbFact.nrocaja = datos_dosificacion_Activa.nrocaja;
                        
                        dataCreGrbFact.factnit = reg.nit;
                        dataCreGrbFact.condicion = reg.condicion;
                        dataCreGrbFact.nrolugar = reg.nrolugar;
                        dataCreGrbFact.tipo = reg.tipo;
                        dataCreGrbFact.codtipo_comprobante = reg.codtipo_comprobante;
                        dataCreGrbFact.usuario = usuario;
                        dataCreGrbFact.codempresa = codempresa;
                        dataCreGrbFact.codtipopago = reg.tipopago;
                        dataCreGrbFact.codbanco = reg.codbanco;
                        dataCreGrbFact.codcuentab = reg.codcuentab;
                        dataCreGrbFact.nrocheque = reg.nrocheque;
                        dataCreGrbFact.idcuenta = reg.idcuenta;
                        dataCreGrbFact.cufd = datos_dosificacion_Activa.cufd;
                        dataCreGrbFact.complemento_ci = reg.complemento_ci;
                        dataCreGrbFact.dtpfecha_limite = datos_dosificacion_Activa.fechainicio;
                        dataCreGrbFact.codigo_control = datos_dosificacion_Activa.codcontrol;
                        dataCreGrbFact.factnomb = reg.nomcliente;

                        dataCreGrbFact.detalle = facturas.detalle;
                        dataCreGrbFact.dgvfacturas = facturas.dgvfacturas;









                       
                        //#### validar nombre a facturar y nit
                        if (dataCreGrbFact.factnomb.Replace(" ", "").Trim() == "SINNOMBRE")
                        {
                            dataCreGrbFact.factnit = "0";
                        }


                        List<int> codFacturas = new List<int>();
                        List<string> msgAlertas = new List<string>();
                        List<string> eventosLog = new List<string>();
                        bool se_creo_factura = false;
                        try
                        {
                            var resultados = await CREAR_GRABAR_FACTURAS(_context, dataCreGrbFact.idfactura, dataCreGrbFact.nrocaja, dataCreGrbFact.factnit, dataCreGrbFact.condicion,
                            dataCreGrbFact.nrolugar, dataCreGrbFact.tipo, dataCreGrbFact.codtipo_comprobante, dataCreGrbFact.usuario, dataCreGrbFact.codempresa, dataCreGrbFact.codtipopago,
                            dataCreGrbFact.codbanco, dataCreGrbFact.codcuentab, dataCreGrbFact.nrocheque, dataCreGrbFact.idcuenta, dataCreGrbFact.cufd, dataCreGrbFact.complemento_ci,
                            cabeceraNR, dataCreGrbFact.detalle, dataCreGrbFact.dgvfacturas
                            );

                            if (resultados.resul == false)
                            {
                                resultados.eventos.Add("La factura no pude ser grabada por lo cual no se envio al SIN!!!");
                            }
                            else
                            {
                                codFacturas = resultados.CodFacturas_Grabadas;
                                msgAlertas = resultados.msgAlertas;
                                eventosLog = resultados.eventos;
                                se_creo_factura = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Mesaje de error al intentar guardar facturas: " + ex.ToString());
                        }

                        // variables para devolver de XML
                        string nomArchivoXML = "";
                        bool imprime = false;


                        if (se_creo_factura)
                        {
                            try
                            {
                                int codalmacen = cabeceraNR.codalmacen;
                                var datos_certificado_digital = await Definir_Certificado_A_Utilizar(_context, codalmacen, dataCreGrbFact.codempresa);
                                if (datos_certificado_digital.result == false)
                                {
                                    datos_certificado_digital.eventos.Add("No se pudo obtener la informacion del certificado digital.");
                                    msgAlertas.Add("Ocurrio algun error al definir el certificado digital para la firma del XML!!!");

                                    msgAlertas.AddRange(datos_certificado_digital.msgAlertas);
                                    eventosLog.AddRange(datos_certificado_digital.eventos);

                                }
                                else
                                {
                                    //comenzar a generar el xml
                                    var xml_generado = await GENERAR_XML_FACTURA_FIRMAR_ENVIAR(_context, codFacturas, dataCreGrbFact.codempresa, dataCreGrbFact.usuario,
                                    codalmacen, datos_certificado_digital.ruta_certificado, datos_certificado_digital.Clave_Certificado_Digital, dataCreGrbFact.codigo_control);

                                    if (xml_generado.resul == false)
                                    {
                                        xml_generado.eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - No se pudo generar el archivo XML de la factura, Firmar y enviar al SIN!!!");
                                        xml_generado.eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - El proceso de Firmado de la factura y Envio al SIN termino con errores, verifique esta situacion!!!");

                                        msgAlertas.Add("Ocurrio algun error al Generar el XML de la factura verifique los resultados de la facturacion!!!");

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
                            }
                        }


                        List<string> cadena = new List<string>();
                        cadena.Add("Se han generado los datos de facturacion con exito.");
                        cadena.Add("Factura(s): ");
                        foreach (var reg2 in codFacturas)
                        {
                            cadena.Add("* " + await ventas.Datos_Factura_CUF(_context, reg2));
                        }

                        if (imprime)
                        {
                            cadena.Add("Se procedera a la impresion y envio de la factura al mail del cliente.");
                        }

                        var dataFactNueva = await _context.vefactura.Where(i=> i.codigo == codFacturas[0]).Select(i => new
                        {
                            i.id,
                            i.numeroid,
                            i.total,
                            i.subtotal,
                            i.descuentos,
                        }).FirstOrDefaultAsync();

                        pruebas_Fact newReg = new pruebas_Fact();
                        newReg.idfc_original = reg.id;
                        newReg.nroidfc_original = reg.numeroid;
                        newReg.subtotal_original = reg.subtotal;
                        newReg.descuentos_original = reg.descuentos;
                        newReg.total_original = reg.total;

                        newReg.idfc_nueva = dataFactNueva.id;
                        newReg.nroidfc_nueva = dataFactNueva.numeroid;
                        newReg.subtotal_nueva = dataFactNueva.subtotal;
                        newReg.descuentos_nueva = dataFactNueva.descuentos;
                        newReg.total_nueva = dataFactNueva.total;

                        newReg.fechareg = DateTime.Now.Date;

                        _context.pruebas_Fact.Add(newReg);
                        await _context.SaveChangesAsync();
                    }

                    return Ok("facturas generadas");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }






        private async Task<(bool resp, string msg, List<veremision_detalle>? detalle, List<tablaFacturas>? dgvfacturas)> GENERAR_FACTURA_DE_NR(DBContext _context, bool opcion_automatico, string codEmpresa, int codigoremision, int nrocaja)
        {
            bool distribuir_desc_extra_en_factura = await configuracion.distribuir_descuentos_en_facturacion(_context, codEmpresa);
            var datosNR = await cargar_nr(_context, codigoremision);

            if (datosNR.cabecera == null || datosNR.detalle == null)
            {
                return (false, "No se pudo obtener los datos de la Nota de Remision, consulte con el administrador", null, null);
            }
            veremision cabecera = datosNR.cabecera;  // DEVOLVER ESTO
            List<veremision_detalle> detalle = datosNR.detalle;

            int NROITEMS = datosNR.nroitems;

            if (distribuir_desc_extra_en_factura)
            {
                // acca devuelde tabla descuentos, total descuentos y el detalle modificado
                var datosDescExtraItem = await calcular_descuentos_extra_por_item(_context, codigoremision, codEmpresa, cabecera.codmoneda, cabecera.codcliente_real, cabecera.nit, (double)cabecera.subtotal, detalle);
                detalle = datosDescExtraItem.detalle;
                // acca devuelve detalle modificado
                detalle = await distribuir_recargos(_context, codigoremision, codEmpresa, detalle);
            }
            else
            {
                detalle = await distribuir_descuentos(_context, codigoremision, codEmpresa, detalle);
                // No_Distribuir_Descuentos(codigoremision)
                // acca devuelve detalle modificado
                detalle = await distribuir_recargos(_context, codigoremision, codEmpresa, detalle);

            }

            // ver el numero de items por hoja sea valido
            var ITEMSPORHOJA = await ventas.numitemscaja(_context, nrocaja);
            List<tablaFacturas> lista = new List<tablaFacturas>();  // DEVOLVER LISTA
            double totfactura = 0;
            if (ITEMSPORHOJA >= NROITEMS)
            {
                // obtener el total final de la factura del detalle (sumatoria de totales de items)
                Total_Detalle_Factura _TTLFACTURA = new Total_Detalle_Factura();
                _TTLFACTURA = await Totalizar_Detalle_Factura(_context, detalle);

                // añadir a la lista

                tablaFacturas registro = new tablaFacturas();
                registro.nro = 1;
                registro.subtotal = _TTLFACTURA.Total_factura;
                registro.descuentos = _TTLFACTURA.Desctos;
                registro.recargos = _TTLFACTURA.Recargos;

                registro.total = registro.subtotal - registro.descuentos;
                // 'registro("total") = Math.Round(_TTLFACTURA.Total_Dist, 2, MidpointRounding.AwayFromZero)

                if (await cliente.DiscriminaIVA(_context, cabecera.codcliente))
                {
                    registro.iva = await siat.Redondear_SIAT(_context, codEmpresa, (double)(cabecera.iva ?? 0));
                    registro.iva = Math.Round(registro.iva, 2, MidpointRounding.AwayFromZero);

                    registro.monto = await siat.Redondear_SIAT(_context, codEmpresa, (registro.total - registro.iva));
                    registro.monto = Math.Round(registro.monto, 2, MidpointRounding.AwayFromZero);
                }
                else
                {
                    registro.iva = 0;
                    registro.monto = await siat.Redondear_SIAT(_context, codEmpresa, registro.total);
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
                // la nota sera multifactura
                var facturasDiv = await DIVIDIR_FACTURA(_context, cabecera.codcliente, ITEMSPORHOJA, codEmpresa, detalle);
                lista = facturasDiv.lista;
                totfactura = facturasDiv.totfactura;
            }
            var condicion = await cliente.CondicionFrenteAlIva(_context, cabecera.codcliente);

            return (true, "", detalle, lista);
        }


        private async Task<(veremision? cabecera, List<veremision_detalle>? detalle, int nroitems)> cargar_nr(DBContext _context, int codigo)
        {
            try
            {
                // llenar cabecera
                var cabecera = await _context.veremision.Where(i => i.codigo == codigo).FirstOrDefaultAsync();

                // llenar detalle
                var detalle = await _context.veremision1.Where(i => i.codremision == codigo).Select(i => new veremision_detalle
                {
                    codigo = i.codigo,
                    codremision = i.codremision,
                    coditem = i.coditem,
                    cantidad = i.cantidad,
                    udm = i.udm,
                    precioneto = i.precioneto,
                    preciolista = i.preciolista,
                    niveldesc = i.niveldesc,
                    preciodesc = i.preciodesc ?? 0,
                    codtarifa = i.codtarifa,
                    coddescuento = i.coddescuento,
                    total = i.total,
                    porceniva = i.porceniva ?? 0,
                    codgrupomer = i.codgrupomer ?? 0,
                    peso = i.peso ?? 0,

                    distdescuento = 0,
                    distrecargo = 0,
                    preciodist = (double)i.precioneto
                }).ToListAsync();
                int nroitems = detalle.Count();
                return (cabecera, detalle, nroitems);
            }
            catch (Exception)
            {
                return (null, null, 0);
            }
        }

        private async Task<(List<veremision_detalle> detalle, List<tabladescuentos>? tabladescuentos, double ttl_descuento_aplicados)> calcular_descuentos_extra_por_item(DBContext _context, int codremision, string codempresa, string codmoneda, string codcliente_real, string nit, double subtotal, List<veremision_detalle> detalle)
        {
            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            double _ttl_con_descto = new double();
            //////////////////////////////////////////////////////////////////////////
            //llama a la funcion que  carga los descuentos extra
            var tabladescuentos = await _context.vedesextraremi.Where(i => i.codremision == codremision).Select(i => new tabladescuentos
            {
                // codremision = i.codremision,
                coddesextra = i.coddesextra,
                porcen = i.porcen,
                montodoc = i.montodoc,
                codcobranza = i.codcobranza,
                codcobranza_contado = i.codcobranza_contado,
                codanticipo = i.codanticipo,
                codmoneda = codmoneda
            }).ToListAsync();
            // a los descuentos les pone la moneda de la cabecera de la nota de remision
            // se entiende que estos descuentos estan en la misma moneda

            tabladescuentos = await ventas.Ordenar_Descuentos_Extra(_context, tabladescuentos);
            //////////////////////////////////////////////////////////////////////////

            // añadir columnas para el calculo de los desctos
            // YA ESTA EN SU MODELO RECIEN CREADO

            ////////////////////////////////////////////////////////////////////////////////
            // 1ro  calcular los montos de los que se aplican en el detalle o son
            // DIFERENCIADOS POR ITEM
            ////////////////////////////////////////////////////////////////////////////////

            int i = 0;
            foreach (var reg in tabladescuentos)
            {
                // inicializa variaables
                double _subtotal = 0;
                _ttl_con_descto = 0;

                // calcular los descueuntos aplicados en el precio unitario
                if (await ventas.DescuentoExtra_Diferenciado_x_item(_context, reg.coddesextra))
                {
                    // llama a la funcion que calcula los desctos extras por items
                    detalle = await ventas.DescuentoExtra_CalcularPorItem(_context, reg.coddesextra, detalle, codcliente_real, nit, 0, true);
                    foreach (var reg2 in detalle)
                    {
                        if (i == 0)
                        {
                            _subtotal = (double)reg2.total;
                            _ttl_con_descto = reg2.total_con_descto;
                            reg2.subtotal_descto_extra = reg2.total_con_descto;
                        }
                        else
                        {
                            _subtotal += reg2.subtotal_descto_extra;
                            _ttl_con_descto += reg2.total_con_descto;
                        }
                    }
                    _ttl_con_descto = _subtotal - _ttl_con_descto;
                    reg.montodoc = (decimal)_ttl_con_descto;
                }
                i++;
            }

            double total_desctos1 = 0;
            foreach (var reg in detalle)
            {
                if (reg.total_con_descto > 0)
                {
                    total_desctos1 += (double)reg.total - reg.total_con_descto;
                }
            }

            // poner los preciodist, totaldist,distdescuento
            double _total_dist = 0;
            if (_ttl_con_descto > 0)
            {
                foreach (var reg in detalle)
                {
                    reg.preciodist = reg.precio_con_descto;
                    reg.totaldist = reg.total_con_descto;
                    reg.distdescuento = (double)reg.total - reg.total_con_descto;
                    _total_dist += reg.totaldist;
                }
            }


            ////////////////////////////////////////////////////////////////////////////////
            // 2do    los que se aplican en el :     SUBTOTAL
            // NO DIFERENCIADOS POR ITEM (es decir tienen un descuento general para todos
            // BUSCA SOLO LOS DESCUENTOS POR DEPOSITO
            ////////////////////////////////////////////////////////////////////////////////
            foreach (var reg in tabladescuentos)
            {
                double monto_desc_deposito = 0;
                if (!await ventas.DescuentoExtra_Diferenciado_x_item(_context, reg.coddesextra))
                {
                    if (reg.aplicacion == "SUBTOTAL")
                    {
                        ///////////////////////////////////////////
                        // SI ES DESCTO POR DEPOSITO
                        ///////////////////////////////////////////
                        if (coddesextra_depositos == reg.coddesextra)
                        {
                            // OJO aqui se determina cual sera el porcentaje de descuento a aplicar a cada item
                            // solo para el caso del descto del deposito de realiza de esta forma, el monto total del descto por deposito
                            // ya viene calculados desde la proforma y lego nota de remision
                            // no se debe recuperar el descuento por item en la funciona llamada ya que el descto extra no esta diferenciado por item, sino que es un porcen de descuento gral para todos los items
                            double _porcen_desc_deposito_descto = ((double)reg.montodoc * 100) / _total_dist;
                            detalle = await ventas.DescuentoExtra_CalcularPorItem(_context, reg.coddesextra, detalle, codcliente_real, nit, _porcen_desc_deposito_descto, false);
                        }
                        else
                        {
                            // este descuento se aplica sobre el subtotal de la venta
                            reg.montodoc = ((decimal)subtotal / 100) * reg.porcen;
                        }

                    }
                }
            }

            //////////////////////////////////////////////////////////////////////////////////
            // totalizar los descuentos que se aplicaron
            //////////////////////////////////////////////////////////////////////////////////
            double total_desctos2 = 0;
            foreach (var reg in detalle)
            {
                total_desctos2 += reg.subtotal_descto_extra - reg.total_con_descto;
            }
            //////////////////////////////////////////////////////////////////////////////////



            ////////////////////////////////////////////////////////////////////////////////
            // 3er    Los que se aplican en el:   TOTAL
            // NO DIFERENCIADOS POR ITEM (son los que se aplica un descuento igual o gral para todos los items)
            ////////////////////////////////////////////////////////////////////////////////

            foreach (var reg in tabladescuentos)
            {
                if (!await ventas.DescuentoExtra_Diferenciado_x_item(_context, reg.coddesextra))
                {
                    if (reg.aplicacion == "TOTAL")
                    {
                        if (coddesextra_depositos != reg.coddesextra)
                        {
                            // este descuento se aplica sobre el subtotal de la venta
                            // no se debe recuperar el descuento por item en la funciona llamada ya que el descto extra no esta diferenciado por item, sino que es un porcen de descuento gral para todos los items
                            double _porcen_desc_deposito_descto = (double)reg.porcen;
                            detalle = await ventas.DescuentoExtra_CalcularPorItem(_context, reg.coddesextra, detalle, codcliente_real, nit, _porcen_desc_deposito_descto, false);
                        }
                    }
                }
            }
            // totalizar los descuentos que se aplicaron
            double total_desctos3 = 0;
            foreach (var reg in detalle)
            {
                total_desctos3 += reg.subtotal_descto_extra - reg.total_con_descto;
            }

            double ttl_descuento_aplicados = total_desctos1 + total_desctos2 + total_desctos3;

            // poner los preciodist, totaldist,distdescuento
            foreach (var reg in detalle)
            {
                reg.preciodist = reg.precio_con_descto;
                reg.totaldist = reg.total_con_descto;
                reg.distdescuento = (double)reg.total - reg.total_con_descto;

                reg.distdescuento = (double)await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)reg.distdescuento);
                reg.preciodist = (double)await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)reg.preciodist);
                reg.totaldist = (double)await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)reg.totaldist);

            }
            // descuentos.Text = (total_desctos1 + total_desctos2).ToString("####,##0.000", new CultureInfo("en-US"))
            return (detalle, tabladescuentos, ttl_descuento_aplicados);

        }


        private async Task<List<veremision_detalle>> distribuir_recargos(DBContext _context, int codigoremision, string codempresa, List<veremision_detalle> detalle)
        {
            var tabla = await _context.veremision.Where(i => i.codigo == codigoremision).Select(i => new
            {
                i.subtotal,
                i.recargos
            }).FirstOrDefaultAsync();
            double montorecar = 0;
            double montorest = 0;
            double montosubtotal = 0;
            if (tabla != null)
            {
                montorecar = (double)tabla.recargos;
                montorest = (double)tabla.recargos;
                montosubtotal = (double)tabla.subtotal;
            }
            int index = 0;
            foreach (var reg in detalle)
            {
                if (index < (detalle.Count() - 1))
                {
                    reg.distrecargo = await siat.Redondear_SIAT(_context, codempresa, (montorecar * (double)reg.total) / montosubtotal);
                    montorest = montorest - reg.distrecargo;
                    reg.preciodist = (double)reg.precioneto + ((reg.distrecargo - reg.distdescuento) / (double)reg.cantidad);
                    reg.totaldist = (double)(reg.precioneto * reg.cantidad) + (reg.distrecargo - reg.distdescuento);
                }
                else  ///ponerle al el ultimo item todo lo que reste
                {
                    reg.distrecargo = montorest;
                    reg.preciodist = (double)reg.precioneto + ((reg.distrecargo - reg.distdescuento) / (double)reg.cantidad);
                    reg.totaldist = (double)(reg.precioneto * reg.cantidad) + (reg.distrecargo - reg.distdescuento);
                }
                reg.distrecargo = (double)await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)reg.distrecargo);
                reg.preciodist = (double)await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)reg.preciodist);
                reg.totaldist = (double)await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)reg.totaldist);
                index++;
            }
            return detalle;

        }

        private async Task<List<veremision_detalle>> distribuir_descuentos(DBContext _context, int codigoremision, string codempresa, List<veremision_detalle> detalle)
        {
            var tabla = await _context.veremision.Where(i => i.codigo == codigoremision).Select(i => new
            {
                i.subtotal,
                i.descuentos
            }).FirstOrDefaultAsync();
            double montodesc = 0;
            double montorest = 0;
            double montosubtotal = 0;
            if (tabla != null)
            {
                montodesc = (double)tabla.descuentos;
                montorest = (double)tabla.descuentos;
                montosubtotal = (double)tabla.subtotal;
            }
            int index = 0;
            foreach (var reg in detalle)
            {
                if (index < (detalle.Count() - 1))
                {
                    reg.distdescuento = await siat.Redondear_SIAT(_context, codempresa, (montodesc * (double)reg.total) / montosubtotal);
                    montorest = montorest - reg.distdescuento;
                    reg.preciodist = (double)reg.precioneto + ((reg.distrecargo - reg.distdescuento) / (double)reg.cantidad);
                    reg.totaldist = (double)(reg.precioneto * reg.cantidad) + (reg.distrecargo - reg.distdescuento);
                }
                else  ///ponerle al el ultimo item todo lo que reste
                {
                    reg.distdescuento = montorest;
                    reg.preciodist = (double)reg.precioneto + ((reg.distrecargo - reg.distdescuento) / (double)reg.cantidad);
                    reg.totaldist = (double)(reg.precioneto * reg.cantidad) + (reg.distrecargo - reg.distdescuento);
                }
                reg.distdescuento = (double)await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)reg.distdescuento);
                reg.preciodist = (double)await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)reg.preciodist);
                reg.totaldist = (double)await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)reg.totaldist);
                index++;
            }
            return detalle;
        }



        private async Task<Total_Detalle_Factura> Totalizar_Detalle_Factura(DBContext _context, List<veremision_detalle> detalle)
        {
            Total_Detalle_Factura resultado = new Total_Detalle_Factura();

            foreach (var reg in detalle)
            {
                resultado.Total_factura += (double)reg.total;
                resultado.Total_Dist += reg.totaldist;
                resultado.total_iva += ((reg.totaldist / 100) * (double)reg.porceniva);
            }
            /*
             
            'resultado.Desctos = resultado.Total_factura - resultado.Total_Dist
            'resultado.Desctos = sia_funciones.SIAT.Instancia.Redondear_SIAT(resultado.Desctos)
            'resultado.Recargos = 0
            'resultado.Total_Dist = sia_funciones.SIAT.Instancia.Redondear_SIAT(resultado.Total_Dist) + sia_funciones.SIAT.Instancia.Redondear_SIAT(resultado.total_iva)
            'desde 08/01/2023 redondear el resultado a dos decimales con el SQLServer
             
             */
            resultado.Total_factura = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultado.Total_factura);
            resultado.Total_Dist = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultado.Total_Dist);
            resultado.total_iva = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultado.total_iva);

            resultado.Desctos = resultado.Total_factura - resultado.Total_Dist;
            resultado.Desctos = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultado.Desctos);
            resultado.Recargos = 0;
            resultado.Total_Dist = resultado.Total_Dist + resultado.total_iva;

            return resultado;
        }

        private async Task<(List<tablaFacturas> lista, double totfactura)> DIVIDIR_FACTURA(DBContext _context, string codcliente, int itemsporhoja, string codempresa, List<veremision_detalle> detalle)
        {
            // añadir a la lista
            List<tablaFacturas> lista = new List<tablaFacturas>();

            int i, j, h = new int();
            double total = 0;
            double total_iva = 0;
            double super_total = 0;
            i = 0;
            j = 0;
            h = 1;

            foreach (var reg in detalle)
            {
                j = j + 1;
                total = total + reg.totaldist;

                total_iva = total_iva + ((reg.totaldist / 100) * (double)reg.porceniva);
                if ((j == itemsporhoja) || (i == detalle.Count() - 1))
                {
                    tablaFacturas registro = new tablaFacturas();
                    registro.nro = h;
                    registro.monto = await siat.Redondear_SIAT(_context, codempresa, total);
                    if (await cliente.DiscriminaIVA(_context, codcliente))
                    {
                        registro.iva = await siat.Redondear_SIAT(_context, codempresa, total_iva);
                        registro.total = await siat.Redondear_SIAT(_context, codempresa, (await siat.Redondear_SIAT(_context, codempresa, total) + await siat.Redondear_SIAT(_context, codempresa, total_iva)));
                    }
                    else
                    {
                        registro.iva = 0;
                        registro.total = await siat.Redondear_SIAT(_context, codempresa, total);
                    }

                    registro.subtotal = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, registro.subtotal);
                    registro.descuentos = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, registro.descuentos);
                    registro.recargos = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, registro.recargos);
                    registro.total = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, registro.total);

                    lista.Add(registro);
                    super_total = super_total + registro.total;
                    total = 0;
                    total_iva = 0;
                    j = 0;
                    h += 1;
                    i += 1;
                }
            }
            var totfactura = await siat.Redondear_SIAT(_context, codempresa, super_total);
            return (lista, totfactura);
        }









        private async Task<(bool resul, List<string> msgAlertas, List<string> eventos, List<int> CodFacturas_Grabadas)> CREAR_GRABAR_FACTURAS(DBContext _context, string idfactura, int nrocaja, string factnit, string condicion, string nrolugar, string tipo, string codtipo_comprobante, string usuario, string codempresa, int codtipopago, string codbanco, string codcuentab, string nrocheque, string idcuenta, string cufd, string complemento_ci, veremision cabecera, List<veremision_detalle> detalle, List<tablaFacturas> dgvfacturas)
        {
            // para devolver lista de registros logs
            List<string> eventos = new List<string>();
            List<string> msgAlertas = new List<string>();
            List<int> CodFacturas_Grabadas = new List<int>();

            try
            {

                bool resultado = true;
                bool descarga = false;

                int cuantas = detalle.Count();
                int idnroactual = 0;
                int nrofactura = 0;

                if (cuantas > 0)
                {
                    ///////////////////////////////////////////////////
                    int HOJA = 0;
                    // DETERMINAR CUANTAS HOJAS DE FACTURAS SERAN NECESARIAS PARA TODA LA NOTA DE REMISION
                    // EN FECHA; 17-03-2022 REUNION:
                    // JROMERO, CINTHYA VARGAS, MARIELA MONTAÑO, ALDRIN ALMANZA, ALEX ZAMBRANA, BRYAN DIAZ
                    // SE ACORDO QUE SE IMPRIMIRA UNA SOLA FACTURA (NRO DE FACTURA) AUNQE SE GENEREN VARIAS HOJAS
                    // ESTO SIGNIFICA: UN NOTA DE REMISION GENERA UN SOLO NUMERO DE FACTURA (QUE PUEDE TENER VARIAS HOJAS)
                    int NROITEMS = detalle.Count();
                    var ITEMSPORHOJA = await ventas.numitemscaja(_context, nrocaja);

                    int NumHojas = (NROITEMS % ITEMSPORHOJA == 0) ?
                       (int)Math.Floor((double)NROITEMS / ITEMSPORHOJA) :
                       (int)Math.Floor((double)NROITEMS / ITEMSPORHOJA) + 1;

                    if (resultado)
                    {
                        for (HOJA = 1; HOJA <= NumHojas; HOJA++)
                        {
                            //////////////////////////////////////////////
                            /////CREAR FACTURA
                            //////////////////////////////////////////////
                            if (HOJA == 1)
                            {
                                idnroactual = (await documento.ventasnumeroid(_context, idfactura));
                            }
                            else
                            {
                                idnroactual = idnroactual + 1;
                            }
                            // verificacion para ver si el documento descarga mercaderia
                            // PONER EL OPUESTO SI LA NR DESCARGA TONS NO DESCARGAR
                            // SI LA NOTA DE REMISION DESCARGA ENTONCES NO DESCARGAR
                            if (HOJA == 1)
                            {
                                if (await ventas.iddescarga(_context, cabecera.id))
                                {
                                    descarga = false;
                                }
                                else
                                {
                                    descarga = true;
                                }
                            }
                            // obtiene el numero
                            if (HOJA == 1)
                            {
                                nrofactura = await ventas.caja_numerofactura(_context, nrocaja);
                            }
                            else
                            {
                                nrofactura = nrofactura + 1;
                            }
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
                                cufd = datos_dosificacion_activa.cufd.Trim();
                                nrocaja = (short)datos_dosificacion_activa.nrocaja;
                                codigo_control = datos_dosificacion_activa.codcontrol.Trim();
                                dtpfecha_limite = datos_dosificacion_activa.fechainicio.Date;
                            }
                            else
                            {
                                // si no hay CUFD no se puede grabar la factura
                                msg = "No se econtro una dosificacion de CUFD activa para fecha: " + (await funciones.FechaDelServidor(_context)).ToShortDateString() + " Ag: " + cabecera.codalmacen;
                                msgAlertas.Add(msg);
                                return (false, msgAlertas, eventos, CodFacturas_Grabadas);
                            }

                            string valor_CUF = "";
                            DateTime fechaServ = await funciones.FechaDelServidor(_context);
                            string horaServ = datos_proforma.getHoraActual();
                            var versionTariAct = await ventas.VersionTarifaActual(_context);
                            var factormeDat = await tipocambio._tipocambio(_context, await Empresa.monedabase(_context, codempresa), await tipocambio.monedatdc(_context, usuario, codempresa), fechaServ);
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
                                codproforma = 0,

                                refacturar = false,
                                estado_contra_entrega = cabecera.estado_contra_entrega,
                                contra_entrega = cabecera.contra_entrega,
                                nroticket = "ST",
                                monto_anticipo = 0,

                                idanticipo = "",
                                numeroidanticipo = 0,
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

                                codremision = cabecera.codigo,
                                tipopago = cabecera.tipopago,
                                subtotal = (decimal)dgvfacturas[HOJA - 1].subtotal,
                                descuentos = (decimal)dgvfacturas[HOJA - 1].descuentos,
                                recargos = (decimal)dgvfacturas[HOJA - 1].recargos,

                                total = (decimal)dgvfacturas[HOJA - 1].total,
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
                                idfc = "",
                                numeroidfc = 0,
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
                            int start = (HOJA * ITEMSPORHOJA) - ITEMSPORHOJA;
                            // int end = (HOJA * ITEMSPORHOJA) - 1;

                            var detalleFactura = detalle.Select((item, index) => new vefactura1
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
                                coddescuento = item.coddescuento,
                                total = item.total,
                                distdescuento = (decimal)item.distdescuento,
                                distrecargo = (decimal)item.distrecargo,
                                preciodist = (decimal)item.preciodist,
                                totaldist = (decimal)item.totaldist,
                                porceniva = item.porceniva,
                                codaduana = index.ToString()  // Asignar el índice como código de aduana
                            }).Skip(start).Take(ITEMSPORHOJA).ToList();  // Limitar al rango de la hoja actual

                            await _context.vefactura1.AddRangeAsync(detalleFactura);
                            await _context.SaveChangesAsync();



                            // actualizar el numero de id
                            var numeracionData = await _context.venumeracion.Where(i => i.id == idfactura).FirstOrDefaultAsync();
                            numeracionData.nroactual += 1;
                            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

                            if (HOJA == 1)
                            {
                                // actualizar remision  a transferida
                                var veremisionData = await _context.veremision.Where(i => i.codigo == cabecera.codigo).FirstOrDefaultAsync();
                                veremisionData.transferida = true;
                                await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

                            }

                            if (HOJA == NumHojas)
                            {
                                // solo cuando es la ultima hoja
                                var dosificaData = await _context.vedosificacion.Where(i => i.nrocaja == nrocaja && i.almacen == cabecera.codalmacen && i.activa == true).FirstOrDefaultAsync();
                                dosificaData.nroactual = dosificaData.nroactual + NumHojas;
                                await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos
                            }

                            //////////////////////////////////////////////
                            /////fin CREAR FACTURA
                            //////////////////////////////////////////////
                        }
                    }
                    ///////////////////////////////////////////////////

                    if (resultado)
                    {
                        if (descarga == true)  // si la nota de remision no descarga entonces aqui descargarla
                        {
                            if (await saldos.Veremision_ActualizarSaldo(_context, cabecera.codigo, Saldos.ModoActualizacion.Crear) == false)
                            {
                                // Desde 23/11/2023 registrar en el log si por alguna razon no actualiza en instoactual correctamente al disminuir el saldo de cantidad y la reserva en proforma
                                await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Factura, cabecera.codigo.ToString(), cabecera.id, cabecera.numeroid.ToString(), _controllerName, "No actualizo stock al restar cantidad en Facturar NR.", Log.TipoLog.Creacion);
                                string msgAlert = "No se pudo actualizar todos los stocks actuales del almacen, Por favor haga correr una actualizacion de stocks cuando vea conveniente."; // devolver
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
                }
                else
                {
                    resultado = false;
                }
                // convertir a moneda de factura
                if (resultado)
                {
                    foreach (var factuCod in CodFacturas_Grabadas)
                    {
                        // fecha de nota de remision para el tdc
                        // sia_funciones.Ventas.Instancia.convertirfactura(CInt(CodFacturas_Grabadas(i)), sia_funciones.TipoCambio.Instancia.monedafact(sia_compartidos.temporales.Instancia.codempresa), CDate(cabecera.Rows(0)("fecha")), sia_compartidos.temporales.Instancia.codempresa)
                        // fecha actual para el tdc
                        // Desde 01-05-2023 se palntea que la nota de remision  todo documento debe salir ya en bolivianos, entonces aqui controlar si el codmoneda de la NR es BS
                        // se NO SE DEBE REALIZAR LA CONVERSION, EN CAMBIO SI EN LA NR EL CODMONEDA ES US, SI SE DEBE CONVERTIR A BS
                        await ventas.Convertir_Moneda_Factura_NSF_SIAT(_context, factuCod, await tipocambio.monedafact(_context, codempresa), await funciones.FechaDelServidor(_context), codempresa);

                        // ACTUALIZAR CODGRUPOMER
                        var dataFactura = await _context.vefactura.Where(i => i.codigo == factuCod).FirstOrDefaultAsync();

                        int codNR = dataFactura.codremision ?? 0;
                        List<veremision1> dataVeremision1 = await _context.veremision1.Where(i => i.codremision == codNR).ToListAsync();
                        dataVeremision1 = await ventas.Remision_Cargar_Grupomer(_context, dataVeremision1);
                        await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

                        // ACTUALIZAR PESO
                        decimal pesoFact = await ventas.Peso_Factura(_context, factuCod);
                        dataFactura.peso = pesoFact;
                        await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

                        await ventas.Actualizar_Peso_Detalle_Factura(_context, factuCod);

                        // actualizar el codigo producto del SIN
                        var detalleFactura = await _context.vefactura1.Where(i => i.codfactura == factuCod).ToListAsync();
                        foreach (var reg in detalleFactura)
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
                        string valor_Codigo_factura_web = await siat.Generar_Codigo_Factura_Web(_context, factuCod, dataFactura.codalmacen);
                        dataFactura.codfactura_web = valor_Codigo_factura_web;
                        await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

                        ///////////////////////////////////////////////////////////////////////////////////////////////////
                        //              generar el cuf y actualizar el CUF generado en la factura
                        ///////////////////////////////////////////////////////////////////////////////////////////////////
                        string val_NIT = await empresa.NITempresa(_context, codempresa);
                        Datos_Pametros_Facturacion_Ag Parametros_Facturacion_Ag = new Datos_Pametros_Facturacion_Ag();
                        Parametros_Facturacion_Ag = await siat.Obtener_Parametros_Facturacion(_context, dataFactura.codalmacen);

                        if (Parametros_Facturacion_Ag.resultado == true)
                        {
                            string valor_CUF = "";
                            // obtener el ID-Numeroid de la factura
                            var id_nroid_fact = await Ventas.id_nroid_factura_cuf(_context, factuCod);
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
                                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, factuCod.ToString(), id_nroid_fact.id, id_nroid_fact.numeroId.ToString(), _controllerName, cadena_msj, Log.TipoLog_Siat.Creacion);
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


                    }
                }

                // ####################################################################################
                // igualar suma de items a total del detalle con la cabecera
                // ####################################################################################
                
                if (resultado)
                {
                    await IgualarFacturasANotaRemision_SIAT(_context, cabecera.id, cabecera.numeroid, codempresa);
                }

                // ####################################################################################
                // sia_funciones.Ventas.Instancia.IgualarFacturasANotaRemision_SIAT(id.Text, CInt(numeroid.Text))
                // ####################################################################################



                return (resultado, msgAlertas, eventos, CodFacturas_Grabadas);
            }
            catch (Exception ex)
            {
                msgAlertas.Add(ex.Message);
                return (false, msgAlertas, eventos, CodFacturas_Grabadas);
            }
        }




        private async Task<bool> IgualarFacturasANotaRemision_SIAT(DBContext _context, string id, int numeroid, string codempresa)
        {
            try
            {
                // obtener el total de la nota de remision 'Convertir a BS
                var datosRemision = await _context.veremision.Where(i => i.id == id && i.numeroid == numeroid).Select(i => new
                {
                    i.codigo,
                    i.total,
                    i.tdc
                }).FirstOrDefaultAsync();

                double tot_remision = (double)(datosRemision.total * datosRemision.tdc);
                // Obtener el total de facturas
                double tot_facturas = (double)await _context.vefactura.Where(i => i.anulada == false && i.codremision == datosRemision.codigo).Select(i => i.total).FirstOrDefaultAsync();
                // COmprarar
                double dif = tot_remision - tot_facturas;
                dif = await siat.Redondear_SIAT(_context, codempresa, dif);

                // Si hay diferencia ??
                if (Math.Abs(dif) >= 0.01)
                {
                    // En la ultima factura igualar
                    int codNR = await _context.veremision.Where(i => i.id == id && i.numeroid == numeroid).Select(i => i.codigo).FirstOrDefaultAsync();
                    var dt_factura = await _context.vefactura.Where(i => i.anulada == false && i.codremision == codNR).OrderByDescending(i => i.numeroid).FirstOrDefaultAsync();

                    double descuentos_f = (double)dt_factura.descuentos;

                    double subtotal_f = (double)dt_factura.subtotal;

                    //si la diferencia a mayo a cero se disminuye el descuento sino se aumenta
                    //If dif > 0 Then
                    //    dif *= -1
                    //End If
                    //26/07/2022
                    //si la diferencia es menor a cero se aumenta al descuento 
                    //si la diferencia es mayor a cero se reduce al descuento 
                    if (descuentos_f > 0)
                    {
                        // si el descuento de la factura es mayor a 0 se debe realizar el ajuste en el descuento
                        if (dif < 0)
                        {
                            // dif *= -1
                            descuentos_f = descuentos_f + (dif * -1);
                        }
                        else
                        {
                            descuentos_f = descuentos_f - (dif);
                        }
                        // sia_DAL.Datos.Instancia.EjecutarComando("update vefactura set total=" & total_f.ToString("####0.00") & " where codigo=" & CStr(dt_factura.Rows(0)("codigo")) & " ")
                        dt_factura.descuentos = (decimal)descuentos_f;
                        // arreglar el total final
                        dt_factura.total = (dt_factura.subtotal - (decimal)descuentos_f + dt_factura.recargos);
                        // Guarda los cambios en la base de datos
                        await _context.SaveChangesAsync();

                        //Igualar el detalle
                        var dt_factura1 = await _context.vefactura1.Where(i => i.codfactura == dt_factura.codigo).OrderByDescending(i => i.totaldist).FirstOrDefaultAsync();
                        double total_d = (double)dt_factura1.totaldist;
                        total_d = total_d + dif;
                        // sia_DAL.Datos.Instancia.EjecutarComando("update vefactura1 set totaldist=" & total_d.ToString("####0.00") & " where codfactura=" & CStr(dt_factura1.Rows(0)("codfactura")) & " and coditem='" & CStr(dt_factura1.Rows(0)("coditem")) & "' ")
                        // sia_DAL.Datos.Instancia.EjecutarComando("update vefactura1 set preciodist=totaldist/cantidad where codfactura=" & CStr(dt_factura1.Rows(0)("codfactura")) & " and coditem='" & CStr(dt_factura1.Rows(0)("coditem")) & "' ")

                        // como el ajuste se hace en los descuentos ya no es necesario arreglar el total del detalle PERO SI EL TOTAL DIST
                        // sia_DAL.Datos.Instancia.EjecutarComando("update vefactura1 set total=" & total_d.ToString("####0.00") & " where codfactura=" & CStr(dt_factura1.Rows(0)("codfactura")) & " and coditem='" & CStr(dt_factura1.Rows(0)("coditem")) & "' ")

                        // se añadio estas 2 lineas el 26/07/2022 para igualar el totaldist - distdescuento
                        // se corrigio el calculo de total dist debido a que desde 23/11/2025 ya se pueden realizar ventas con item repetido por tema de division de empaques caja cerrada
                        dt_factura1.totaldist = (decimal)total_d;
                        ////////////////////////////////////////////////
                        dt_factura1.distdescuento = (dt_factura1.total - (decimal)total_d);
                        dt_factura1.preciodist = ((decimal)total_d / dt_factura1.cantidad);
                        // Guarda los cambios en la base de datos
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        // si el descuento de la factura es menor a 0 se debe realizar el ajuste en el subtotal de la factura y en preciolista del detalle de la factura
                        // If dif < 0 Then
                        //     'dif *= -1
                        //     subtotal_f = subtotal_f + (dif * -1)
                        // Else
                        //     subtotal_f = subtotal_f - (dif)
                        // End If
                        subtotal_f = subtotal_f + (dif);

                        // sia_DAL.Datos.Instancia.EjecutarComando("update vefactura set total=" & total_f.ToString("####0.00") & " where codigo=" & CStr(dt_factura.Rows(0)("codigo")) & " ")
                        dt_factura.subtotal = (decimal)subtotal_f;

                        // arreglar el total final
                        dt_factura.total = ((decimal)subtotal_f - dt_factura.descuentos + dt_factura.recargos);
                        // Guarda los cambios en la base de datos
                        await _context.SaveChangesAsync();

                        // Igualar el detalle
                        var dt_factura1 = await _context.vefactura1.Where(i => i.codfactura == dt_factura.codigo).OrderByDescending(i => i.totaldist).FirstOrDefaultAsync();
                        double total = (double)dt_factura1.total;
                        total = total + dif;
                        // sia_DAL.Datos.Instancia.EjecutarComando("update vefactura1 set totaldist=" & total_d.ToString("####0.00") & " where codfactura=" & CStr(dt_factura1.Rows(0)("codfactura")) & " and coditem='" & CStr(dt_factura1.Rows(0)("coditem")) & "' ")
                        // sia_DAL.Datos.Instancia.EjecutarComando("update vefactura1 set preciodist=totaldist/cantidad where codfactura=" & CStr(dt_factura1.Rows(0)("codfactura")) & " and coditem='" & CStr(dt_factura1.Rows(0)("coditem")) & "' ")

                        // como el ajuste se hace en los descuentos ya no es necesario arreglar el total del detalle PERO SI EL TOTAL DIST
                        // sia_DAL.Datos.Instancia.EjecutarComando("update vefactura1 set total=" & total_d.ToString("####0.00") & " where codfactura=" & CStr(dt_factura1.Rows(0)("codfactura")) & " and coditem='" & CStr(dt_factura1.Rows(0)("coditem")) & "' ")
                        if (dt_factura1.coddescuento == 0)
                        {
                            // si el item en cuestion no tiene descuentos de linea realizar de este modo
                            // se añadio estas 2 lineas el 26/07/2022 para igualar el totaldist - distdescuento
                            // se corrigio el calculo de total dist debido a que desde 23/11/2025 ya se pueden realizar ventas con item repetido por tema de division de empaques caja cerrada
                            dt_factura1.total = (decimal)total;
                            dt_factura1.totaldist = (decimal)total;
                            /////////////////////////////////
                            decimal precios = (decimal)total / dt_factura1.cantidad;
                            dt_factura1.preciolista = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, precios);
                            dt_factura1.precioneto = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, precios);
                            dt_factura1.preciodesc = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, precios);
                            dt_factura1.preciodist = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, precios);
                            // Guarda los cambios en la base de datos
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            // si el item en cuestion tiene descuentos de linea realizar de este modo
                            // se añadio estas 2 lineas el 26/07/2022 para igualar el totaldist - distdescuento
                            // se corrigio el calculo de total dist debido a que desde 23/11/2025 ya se pueden realizar ventas con item repetido por tema de division de empaques caja cerrada

                            // DESDE 27-01-2023 AQUI PREGUNTAR SI REALMENTE EXISTE DESCUENTO DE LINEA EN LOS PRECIOS YA QUE A PRECIO 3 O 2 SI PUEDE ESTAR ASIGNADO EL DESCUENTO DE LINEA EN CODDESCUENTO PERO EL PORCENTAJE 
                            // DE DECUENTO DEL ITEM PUEDE SER 0 POR ENDE NO HAY DESCUENTO, EN ESTOS CASOS NO SOLO ACTUALIZAR EL NUEVO PRECIO EN PRECIONETO Y PRECIODIST SINO ACTUALIZAR EN TODOS LOS PRECIOS
                            double precio_neto = (double)dt_factura1.precioneto;
                            double precio_lista = (double)dt_factura1.preciolista;

                            if (precio_neto == precio_lista)
                            {
                                dt_factura1.total = (decimal)total;
                                dt_factura1.totaldist = (decimal)total;
                                /////////////////////////////////
                                decimal precios = (decimal)total / dt_factura1.cantidad;
                                dt_factura1.preciolista = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, precios);
                                dt_factura1.precioneto = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, precios);
                                dt_factura1.preciodesc = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, precios);
                                dt_factura1.preciodist = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, precios);
                                // Guarda los cambios en la base de datos
                                await _context.SaveChangesAsync();
                            }
                            else
                            {
                                dt_factura1.total = (decimal)total;
                                dt_factura1.totaldist = (decimal)total;
                                /////////////////////////////////
                                decimal precios = (decimal)total / dt_factura1.cantidad;
                                // dt_factura1.preciolista = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, precios);
                                dt_factura1.precioneto = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, precios);
                                // dt_factura1.preciodesc = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, precios);
                                dt_factura1.preciodist = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, precios);
                                // Guarda los cambios en la base de datos
                                await _context.SaveChangesAsync();
                            }
                            // Antes del 27-01-2023 era asi
                            // sia_DAL.Datos.Instancia.EjecutarComando("update vefactura1 set total=" & total.ToString("####0.00") & ", totaldist=" & total.ToString("####0.00") & " where codfactura=" & CStr(dt_factura1.Rows(0)("codfactura")) & " and coditem='" & CStr(dt_factura1.Rows(0)("coditem")) & "' and cantidad='" & CStr(dt_factura1.Rows(0)("cantidad")) & "' ")
                            /////////////////////////////////////
                            // 'sia_DAL.Datos.Instancia.EjecutarComando("update vefactura1 set preciolista=round(total/cantidad,5) where codfactura=" & CStr(dt_factura1.Rows(0)("codfactura")) & " and coditem='" & CStr(dt_factura1.Rows(0)("coditem")) & "' ")
                            // sia_DAL.Datos.Instancia.EjecutarComando("update vefactura1 set precioneto=round(total/cantidad,5) where codfactura=" & CStr(dt_factura1.Rows(0)("codfactura")) & " and coditem='" & CStr(dt_factura1.Rows(0)("coditem")) & "' ")
                            // 'sia_DAL.Datos.Instancia.EjecutarComando("update vefactura1 set preciodesc=round(total/cantidad,5) where codfactura=" & CStr(dt_factura1.Rows(0)("codfactura")) & " and coditem='" & CStr(dt_factura1.Rows(0)("coditem")) & "' ")
                            // sia_DAL.Datos.Instancia.EjecutarComando("update vefactura1 set preciodist=round(totaldist/cantidad,5) where codfactura=" & CStr(dt_factura1.Rows(0)("codfactura")) & " and coditem='" & CStr(dt_factura1.Rows(0)("coditem")) & "' ")
                        }
                        // volver a calcular total totaldist y subtotal y total del detalle de la factura distdesceunto
                        dt_factura1.total = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (dt_factura1.cantidad * dt_factura1.precioneto));
                        dt_factura1.totaldist = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (dt_factura1.cantidad * (dt_factura1.preciodist ?? 0)));
                        dt_factura1.distdescuento = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (dt_factura1.total - (dt_factura1.totaldist ?? 0)));
                        // Guarda los cambios en la base de datos
                        await _context.SaveChangesAsync();
                        // arreglar subtotal, descuentos, total de cabecera factura
                        dt_factura.subtotal = await _context.vefactura1.Where(i => i.codfactura == dt_factura.codigo).SumAsync(i => i.total);
                        dt_factura.descuentos = await _context.vefactura1.Where(i => i.codfactura == dt_factura.codigo).SumAsync(i => i.distdescuento) ?? 0;
                        dt_factura.total = (dt_factura.subtotal - dt_factura.descuentos);
                        // Guarda los cambios en la base de datos
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return true;
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
                            await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "prgfacturarNR_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
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
                                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "prgfacturarNR_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                            }
                            else
                            {
                                mensaje = "XML no se pudo firmar";
                                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "prgfacturarNR_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
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
                                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "prgfacturarNR_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                            }
                            else
                            {
                                mensaje = "No se pudo comprimir en GZIP!!!";
                                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "prgfacturarNR_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
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
                                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "prgfacturarNR_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                            }
                            catch (Exception ex)
                            {
                                mensaje = "No se pudo convertir a bytes, " + ex.Message;
                                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "prgfacturarNR_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
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
                                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "prgfacturarNR_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                            }
                            catch (Exception ex)
                            {
                                mensaje = "No se pudo generar el HASH la huella digital. " + ex.Message;
                                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "prgfacturarNR_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
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
                                    var enviar_factura_al_sin = await funciones_SIAT.ENVIAR_FACTURA_AL_SIN(_context, codigocontrol, codempresa, usuario, cufd, long.Parse(nit), cuf, miComprimidoGZIP, miHASH, codalmacen, (int)dtFactura.Rows[0]["codigo"], (string)dtFactura.Rows[0]["id"], (int)dtFactura.Rows[0]["numeroid"],_controllerName);
                                    if (enviar_factura_al_sin.resul)
                                    {
                                        //se envio al SIN
                                        mensaje = "Recepción de factura de almacén exitosa!!!";
                                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - ---> " + id + "-" + numeroid + " " + mensaje);
                                        factura_se_imprime = true;
                                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "prgfacturarNR_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                                    }
                                    else
                                    {
                                        //no se recepciono la factura en el SIN
                                        mensaje = "Recepción de factura de almacén rechazada!!!";
                                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - ---> " + id + "-" + numeroid + " " + mensaje);
                                        factura_se_imprime = false;
                                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "prgfacturarNR_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
                                    }
                                }
                                else
                                {
                                    // ESTA EN MODO FACTURACION FUERA DE LINEA
                                    //registrar log siat
                                    factura_se_imprime = true;
                                    mensaje = "No se envía al SIN, CUF generado fuera de línea!!!";
                                    eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "prgfacturarNR_cufdController", mensaje, Log.TipoLog_Siat.Envio_Factura);
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
                                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "prgfacturarNR_cufdController", "Cod_Recepcion:" + cod_recepcion_siat + "|Cod_estado_siat:" + cod_estado_siat, Log.TipoLog_Siat.Envio_Factura);
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
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFacturas.ToString(), id, numeroid.ToString(), "prgfacturarNR_cufdController", "La factura no fue recepcionada por el SIN", Log.TipoLog_Siat.Envio_Factura);
                    }
                }
            }
            return (resultado, factura_se_imprime, msgAlertas, eventos, nomArchivoXML);
        }













    }
}
