﻿using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using NuGet.Configuration;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;
using System.ComponentModel.DataAnnotations;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.Arm;

namespace SIAW.Controllers.ventas.transaccion
{
    [Route("api/venta/transac/[controller]")]
    [ApiController]
    public class prgfacturarNR_cufdController : ControllerBase
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

        private readonly SIAT siat = new SIAT();
        private readonly Empresa empresa = new Empresa();
        private readonly Documento documento = new Documento();
        private readonly Log log = new Log();

        private readonly string _controllerName = "prgfacturarNR_cufdController";

        public prgfacturarNR_cufdController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        [HttpGet]
        [Route("getDosificacionCaja/{userConn}/{fecha}/{codalmacen}")]
        public async Task<ActionResult<IEnumerable<object>>> getDosificacionCaja(string userConn, DateTime fecha, int codalmacen)
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
                            nrolugar = "",
                            tipo = datos_dosificacion_Activa.tipo,
                            codtipo_comprobante = "",
                            codtipo_comprobantedescripcion = ""
                        });
                    }
                    else
                    {
                        return BadRequest(new {resp = "No se encontro dosificacion de CUFD activa para el almacen: " });
                    }
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }




        // GET: api/adsiat_tipodocidentidad
        [HttpPost]
        [Route("generarFacturasNR/{userConn}")]
        public async Task<ActionResult<IEnumerable<object>>> generarFacturasNR(string userConn, data_get_facturasNR data_get_facturasNR)
        {
            try
            {
                string idNR = data_get_facturasNR.idNR;
                int nroIdNR = data_get_facturasNR.nroIdNR;
                string codEmpresa = data_get_facturasNR.codEmpresa;
                string cufd = data_get_facturasNR.cufd;
                string usuario = data_get_facturasNR.usuario;
                int nrocaja = data_get_facturasNR.nrocaja;
                bool valProfDespachos = data_get_facturasNR.valProfDespachos;
                bool valFechaRemiHoy = data_get_facturasNR.valFechaRemiHoy;
                bool valFactContado = data_get_facturasNR.valFactContado;
                bool valTipoCam = data_get_facturasNR.valTipoCam;



                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {

                    var existeNR = await existenr(_context, idNR, nroIdNR);
                    if (existeNR.existe == false)
                    {
                        return BadRequest(new { resp = "Esa nota de remision no existe o fue anulada." });
                    }
                    int codRemision = existeNR.codRemision;
                    var validaciones = await validar(_context, false, codEmpresa, codRemision, cufd, usuario, valProfDespachos, valFechaRemiHoy, valFactContado, valTipoCam);
                    if (validaciones.resp == false)  // quiere decir que hay un problema con la Validacion
                    {
                        if (validaciones.objContra == null)
                        {
                            return BadRequest(new { resp = validaciones.msg });
                        }
                        else
                        {
                            // si el objeto no es nulo, significa que requiere de contraseña y los datos se mandan al front
                            return StatusCode(203, new { 
                                resp = validaciones.msg, 
                                valido = validaciones.resp,
                                objContra = validaciones.objContra
                            });
                        }
                    }

                    var facturas = await GENERAR_FACTURA_DE_NR(_context, false, codEmpresa, codRemision, nrocaja);
                    if (facturas.resp == false)
                    {
                        return BadRequest(new { resp = facturas.msg });
                    }
                    return Ok(new
                    {
                        valido = facturas.resp,
                        facturas = facturas.dataFacturas
                    });
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // VALIDACIONES PARA LLAMAR EN EL CONTROLADOR.
        private async Task<(bool existe, int codRemision)> existenr(DBContext _context, string id, int numeroid)  // LLAMAR EN EL CONTROLADOR PARA OBTENER EL CODIGO DE NOTA DE REMISION
        {
            var tabla = await _context.veremision.Where(i => i.id == id && i.numeroid == numeroid && i.anulada == false).Select(i => new
            {
                i.codigo
            }).FirstOrDefaultAsync();
            if (tabla == null)
            {
                return (false, 0);   // mostrar: Esa nota de remision no existe o fue anulada.
            }
            return (true, tabla.codigo);
        }


        private async Task<bool> yaestafacturadanr(DBContext _context, int codremision)
        {
            var tabla = await _context.vefactura.Where(i => i.codremision == codremision && i.anulada == false).CountAsync();
            if (tabla > 0)
            {
                return true;
            }
            return false;
        }


        private async Task<(bool resp, string msg, object? objContra)> validar(DBContext _context, bool generacion_automatica, string codEmpresa, int codigoremision, string cufd, string usuario, bool valProfDespachos, bool valFechaRemiHoy, bool valFactContado, bool valTipoCam)
        {
            bool resultado = true;
            var idNroId = await _context.veremision.Where(i => i.codigo == codigoremision).Select(i => new
            {
                i.id,
                i.numeroid
            }).FirstOrDefaultAsync();

            if (await yaestafacturadanr(_context, codigoremision))
            {
                if (generacion_automatica)
                {
                    // anular las facturas
                    var dt =await _context.vefactura.Where( i=> i.anulada == false && i.codremision == codigoremision).Select(i=> new
                    {
                        i.codigo,
                        i.codremision,
                        i.id,
                        i.numeroid,
                        i.fecha,
                        i.codcliente
                    }).ToListAsync();
                    foreach (var reg in dt)
                    {
                        var factura = _context.vefactura.FirstOrDefault(f => f.codigo == reg.codigo);
                        
                        if (factura != null)
                        {
                            factura.anulada = true;
                            await _context.SaveChangesAsync();
                        }
                    }
                    resultado = true;
                }
                else
                {
                    resultado = false;  
                    return (resultado, "Esa nota de remision ya fue facturada.", null);
                }
            }
            /*
             'Desde 31/07/2024 se corrigio esto, ya que al usar un CUFD para 2 dias por fallo de los servicios de impuestos no estaba validando correctamente la fecha limite de dosificacion
             'If sia_funciones.Funciones.Instancia.fecha_del_servidor().Date > sia_funciones.Ventas.Instancia.nroautorizacion_fechalimiteDate(cufd.Text).Date Then
            */
            if ((await funciones.FechaDelServidor(_context)).Date > (await ventas.cufd_fechalimiteDate(_context,cufd)).Date)
            {
                resultado = false;
                return (resultado, "La fecha de la factura a generar excede la fecha limite de la dosificacion.", null);
            }

            // Validar que la proforma este registrada en despachos        //  ACA PEDIR CONTRTASEÑA ES 
            if (resultado == true && generacion_automatica == false)
            {
                if (valProfDespachos) // si esta true no valida, se salta
                {
                    // nada
                }
                else   // valida normal
                {
                    if (await ventas.codproforma_de_remision(_context, codigoremision) > 0)
                    {
                        var dt_despacho = await _context.vedespacho
                            .Join(_context.veproforma,
                                d => new { Id = d.id, NroId = d.nroid },
                                p => new { Id = p.id, NroId = p.numeroid },
                                (d, p) => new { d, p })
                            .Where(dp => _context.veremision
                                .Where(r => r.codigo == codigoremision)
                                .Select(r => r.codproforma)
                                .Contains(dp.p.codigo))
                            .Select(dp => dp.d.estado)
                            .FirstOrDefaultAsync();
                        var objContra = new
                        {
                            servicio = 33,
                            datos_documento = idNroId.id + "-" + idNroId.numeroid,
                            datos_a = idNroId.id,
                            datos_b = idNroId.numeroid
                        };
                        if (dt_despacho != null)
                        {
                            if (dt_despacho == "PREPARADO" || dt_despacho == "DESPACHAR" || dt_despacho == "DESPACHADO")
                            {
                                // nada
                            }
                            else
                            {
                                resultado = false;
                                return (resultado, "La proforma debe estar registrada por lo menos como Preparada para poder facturar su nota de remision, por favor registre su preparacion primero.", objContra);
                            }
                        }
                        else
                        {
                            resultado = false;
                            return (resultado, "La proforma de esta nota de remision no esta registrada en los despachos, por favor registre su preparacion primero.", objContra);
                        }
                    }
                }
                
            }




            // ###VALIDAR QUE EL USUARIO DE TIENDA NO FACTURE DE ALMACEN
            if (await configuracion.usr_usar_bd_opcional(_context,usuario))
            {
                int codalmacen_remision = await ventas.almacen_de_remision(_context,codigoremision);
                if (codalmacen_remision == await configuracion.usr_codalmacen(_context,usuario))
                {
                    // nada
                }
                else
                {
                    resultado = false;
                    return (resultado, "Esta tratando de facturar una nota de otro almacen al que pertenece su usuario. Verifique la nota y el usuario actual.", null);
                }
            }

            // ##VALIDAR QUE LA NOTA DE REMISION SEADE LA MISMA FECHA QUE HOY QUE SE ESTA FACTURANDO
            // 'SE NECESITA PERMISO ESPECIAL  SI NO CUMPLE
            if (resultado == true && generacion_automatica == false)
            {
                if (valFechaRemiHoy)  // SI ES TRUE NO VALIDA SE SALTA
                {
                    // NADA 
                }
                else
                {
                    DateTime fecha_remision = await ventas.RemisionFecha(_context, codigoremision);
                    DateTime fecha_servidor = await funciones.FechaDelServidor(_context);
                    var objContra = new
                    {
                        servicio = 29,
                        datos_documento = idNroId.id + "-" + idNroId.numeroid,
                        datos_a = idNroId.id,
                        datos_b = idNroId.numeroid
                    };
                    if (fecha_remision.Year < fecha_servidor.Year)
                    {
                        resultado = false;
                        return (resultado, "La nota de remision es de una fecha pasada, por favor verifique esta situacion.", objContra);
                    }
                    else
                    {
                        if (fecha_remision.Month < fecha_servidor.Month)
                        {
                            resultado = false;
                            return (resultado, "La nota de remision es de una fecha pasada, por favor verifique esta situacion.", objContra);
                        }
                        else
                        {
                            if (fecha_remision.Day < fecha_servidor.Day)
                            {
                                resultado = false;
                                return (resultado, "La nota de remision es de una fecha pasada, por favor verifique esta situacion.", objContra);
                            }
                            else
                            {
                                // todo ok !!
                            }
                        }
                    }

                    /*
                     'If fecha_remision.Date < sia_funciones.Funciones.Instancia.fecha_del_servidor() Then
                     '    MessageBox.Show("La nota de remision es de una fecha pasada, por favor verifique esta situacion.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                     '    resultado = False
                     'End If
                     */
                }
            }


            /*
             '*********************************************************************************
             '//validar que si la venta es contado si un id-numeroid de anticipo registrado 
             '*********************************************************************************
            */
            if (resultado)
            {
                // si la venta es al contado y no es pagada con anticipo
                // pedir clave para permitir lafacturacion
                if (await ventas.NR_Tipo_Pago(_context,codigoremision) == 0)
                {
                    if (valFactContado)  // SI ES TRUE, TIENE CLAVE Y NO HACE NADA SINO VALIDA
                    {
                        // NADA
                    }
                    else
                    {
                        var ventContCAnti = await ventas.Es_Venta_Contado_Con_Pago_Anticipo(_context, codigoremision);

                        if (await ventas.Es_Venta_Contado_Contra_Entrega(_context, codigoremision))
                        {
                            // si venta CONTADO - CONTRA ENTREGA no tiene que haber pago
                            resultado = true;
                        }
                        else if (!ventContCAnti.resul)
                        {
                            var objContra = new
                            {
                                servicio = 89,
                                datos_documento = idNroId.id + "-" + idNroId.numeroid,
                                datos_a = idNroId.id,
                                datos_b = idNroId.numeroid
                            };
                            // DEVOLVER MENSAJE QUE LLEGA ventContCAnti.msg
                            //****  SI NO ES CON PAGO ANTICIPO  *********
                            resultado = false;    // Pedir clave
                            return (resultado, "Toda venta al contado que no ha sido previamente cancelada con un anticipo, debe ser cancelada en caja antes de facturar e ingresar una clave para emitir la factura.", objContra);
                        }
                        else
                        {
                            resultado = true;
                        }
                    }
                    
                }
            }


            // ### Verificar QUE LA NOTA DE REMISION TENGA CUOTAS SI ES A CREDITO
            if (resultado)
            {
                if (await ventas.NR_Tipo_Pago(_context,codigoremision) == 0)
                {
                    // CONTADO
                }
                else
                {
                    if (await cobranzas.NR_Con_Cuotas(_context,idNroId.id,idNroId.numeroid))
                    {
                        // nada
                    }
                    else
                    {
                        resultado = false;
                        return (resultado, "La nota de remision no tiene plan de cuotas, por favor Vuelva a grabarla, o informe al administrador del sistema.", null);
                    }
                }
            }

            // ### Verificar que el tipo de cambio no tenga algun valor raro
            if (valTipoCam)  // si es true no valida
            {
                // NADA 
            }
            else
            {
                string monBase = await Empresa.monedabase(_context, codEmpresa);
                string monExt = await empresa.monedaext(_context, codEmpresa);
                double tdc_ant = (double)await tipocambio._tipocambio(_context, monBase, monExt, (await funciones.FechaDelServidor(_context)).Date.AddDays(-1));
                double tdc_act = (double)await tipocambio._tipocambio(_context, monBase, monExt, (await funciones.FechaDelServidor(_context)).Date);
                if (Math.Abs(tdc_act - tdc_ant) > 1)
                {
                    var objContra = new
                    {
                        servicio = -1,
                        datos_documento = "",
                        datos_a = "",
                        datos_b = ""
                    };
                    resultado = false;
                    return (resultado, "El tipo de cambio tiene una gran variacion, respecto a la fecha anterior. Por favor consulte con el administrador del sistema antes de continuar. Esta seguro que desea continuar?", objContra);
                }
            }
            return (resultado, "", null);
            
        }


        private async Task<(bool resp, string msg, object? dataFacturas)> GENERAR_FACTURA_DE_NR(DBContext _context, bool opcion_automatico, string codEmpresa, int codigoremision, int nrocaja)
        {
            bool distribuir_desc_extra_en_factura = await configuracion.distribuir_descuentos_en_facturacion(_context, codEmpresa);
            var datosNR = await cargar_nr(_context, codigoremision);

            if (datosNR.cabecera == null || datosNR.detalle == null)
            {
                return (false, "No se pudo obtener los datos de la Nota de Remision, consulte con el administrador", null);
            }
            veremision cabecera = datosNR.cabecera;  // DEVOLVER ESTO
            List<veremision_detalle> detalle = datosNR.detalle;

            int NROITEMS = datosNR.nroitems;

            if (distribuir_desc_extra_en_factura)
            {
                // NNNNNNNOOOOOOOOOOOOO     PROOOOORAAAAAATEEEEEEOOOOOOOOOOO DESTRIBUYE EL DESCUENTO ENC ADA ITEM COMO TIENE QUE SER
                // es la nueva forma implementada en 28-02-2019, aplica los descuentos por item, NO PRORATEA


                // acca devuelde tabla descuentos, total descuentos y el detalle modificado
                var datosDescExtraItem = await calcular_descuentos_extra_por_item(_context, codigoremision, codEmpresa, cabecera.codmoneda, cabecera.codcliente_real, cabecera.nit, (double)cabecera.subtotal, detalle);
                detalle = datosDescExtraItem.detalle;
                // acca devuelve detalle modificado
                detalle = await distribuir_recargos(_context, codigoremision, codEmpresa, detalle);
            }
            else
            {
                // AQUI HACE PRORATEO
                // hace de la forma como siempre hacia lo que mario implemento



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
                // la nota se emite en una sola HOJA
                /*

                subtotalNR.Text = CDbl(cabecera.Rows(0)("subtotal")).ToString("####,##0.00")
                totdesctosNR.Text = CDbl(cabecera.Rows(0)("descuentos")).ToString("####,##0.00")
                totrecargosNR.Text = CDbl(cabecera.Rows(0)("recargos")).ToString("####,##0.00")
                totremision.Text = CDbl(cabecera.Rows(0)("total")).ToString("####,##0.00")

                 */

                // obtener el total final de la factura del detalle (sumatoria de totales de items)
                Total_Detalle_Factura _TTLFACTURA = new Total_Detalle_Factura();
                _TTLFACTURA = await Totalizar_Detalle_Factura(_context, detalle);

                // añadir a la lista

                tablaFacturas registro = new tablaFacturas();
                registro.nro = 1;

                /*

                'registro("subtotal") = sia_funciones.SIAT.Instancia.Redondeo_Decimales_SIA(sia_funciones.SIAT.Instancia.Redondear_SIAT(_TTLFACTURA.Total_factura))
                'desde 08/01/2023 ya se redondea en la funcion Totalizar_Detalle_Factura con dos decimales con el SQLServer

                 */
                registro.subtotal = _TTLFACTURA.Total_factura;

                /*

                'registro("descuentos") = sia_funciones.SIAT.Instancia.Redondeo_Decimales_SIA(sia_funciones.SIAT.Instancia.Redondear_SIAT(_TTLFACTURA.Desctos))
                'desde 08/01/2023 ya se redondea en la funcion Totalizar_Detalle_Factura con dos decimales con el SQLServer

                 */
                registro.descuentos = _TTLFACTURA.Desctos;

                /*

                'registro("recargos") = Math.Round(sia_funciones.SIAT.Instancia.Redondear_SIAT(_TTLFACTURA.Recargos), 2, MidpointRounding.AwayFromZero)
                'desde 08/01/2023 ya se redondea en la funcion Totalizar_Detalle_Factura con dos decimales con el SQLServer

                 */
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
            var facturasInfo = new
            {
                cabecera,
                lista,
                totfactura,
                detalle
            };

            return (true, "", facturasInfo);
        }

        private async Task<(veremision ? cabecera, List<veremision_detalle>? detalle, int nroitems)> cargar_nr(DBContext _context, int codigo)
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
                if (await ventas.DescuentoExtra_Diferenciado_x_item(_context,reg.coddesextra))
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
                if (! await ventas.DescuentoExtra_Diferenciado_x_item(_context,reg.coddesextra))
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
            }
            // descuentos.Text = (total_desctos1 + total_desctos2).ToString("####,##0.00")
            return (detalle,tabladescuentos,ttl_descuento_aplicados);

        }


        private async Task<List<veremision_detalle>> distribuir_recargos(DBContext _context, int codigoremision, string codempresa, List<veremision_detalle> detalle)
        {
            var tabla = await _context.veremision.Where(i=> i.codigo == codigoremision).Select(i => new
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
            resultado.Total_factura = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context,resultado.Total_factura);
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
            
            int i,j,h = new int();
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
                    if (await cliente.DiscriminaIVA(_context,codcliente))
                    {
                        registro.iva = await siat.Redondear_SIAT(_context, codempresa, total_iva);
                        registro.total = await siat.Redondear_SIAT(_context, codempresa, (await siat.Redondear_SIAT(_context, codempresa, total) + await siat.Redondear_SIAT(_context, codempresa, total_iva)));
                    }
                    else
                    {
                        registro.iva = 0;
                        registro.total = await siat.Redondear_SIAT(_context, codempresa, total);
                    }
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




        [HttpPost]
        [Route("grabarFacturasNR/{userConn}")]
        public async Task<ActionResult<IEnumerable<object>>> grabarFacturasNR(string userConn, data_get_facturasNR data_get_facturasNR)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    return Ok();
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }

        }
        private async Task<bool> GRABAR_IMPRIMIR(DBContext _context, int codremision, bool opcion_automatico)
        {
            // If ValidarCrearImprimir()
            if (true)
            {
                // ####validar nombre a facturar y nit
                /*
                 
                If Trim(factnomb.Text.Replace(" ", "")) = "SINNOMBRE" Then
                    factnit.Text = "0"
                Else
                    If sia_funciones.Ventas.Instancia.Validar_NIT_Correcto(factnit.Text, sia_funciones.Ventas.Instancia.Tipo_Documento_Cliente_segun_Remision(id.Text, numeroid.Text)) Then
                    Else
                        MessageBox.Show("Debe indicar un N.I.T. o Nro de C.I. Valido.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                        Exit Sub
                    End If
                End If

                 */
            }
            return true;
        }

        private async Task<(bool resul, string msg)> CREAR_GRABAR_FACTURAS(DBContext _context, string idfactura, int nrocaja, string factnit,string condicion, string nrolugar, string tipo, string codtipo_comprobante, string usuario, string codempresa, int codtipopago, string codbanco, string codcuentab, string nrocheque, string idcuenta, string cufd, string complemento_ci, veremision cabecera, List<veremision_detalle> detalle, List<tablaFacturas> dgvfacturas)
        {
            bool resultado = true;
            bool descarga = false;

            int cuantas = detalle.Count();
            List<int> CodFacturas_Grabadas = new List<int>();
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
                            return (false, msg);
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
                        if (await saldos.Veremision_ActualizarSaldo(_context,usuario,cabecera.codigo,Saldos.ModoActualizacion.Crear) == false)
                        {
                            // Desde 23/11/2023 registrar en el log si por alguna razon no actualiza en instoactual correctamente al disminuir el saldo de cantidad y la reserva en proforma
                            await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Factura, cabecera.codigo.ToString(), cabecera.id, cabecera.numeroid.ToString(), _controllerName, "No actualizo stock al restar cantidad en Facturar NR.", Log.TipoLog.Creacion);
                            string msgAlert = "No se pudo actualizar todos los stocks actuales del almacen, Por favor haga correr una actualizacion de stocks cuando vea conveniente."; // devolver
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
                    await ventas.Convertir_Moneda_Factura_NSF_SIAT(_context,factuCod,await tipocambio.monedafact(_context,codempresa),await funciones.FechaDelServidor(_context),codempresa);

                    // ACTUALIZAR CODGRUPOMER
                    var dataFactura = await _context.vefactura.Where(i => i.codigo == factuCod).FirstOrDefaultAsync();

                    int codNR = dataFactura.codremision ?? 0;
                    List<veremision1> dataVeremision1 = await _context.veremision1.Where(i => i.codremision == codNR).ToListAsync();
                    dataVeremision1 = await ventas.Remision_Cargar_Grupomer(_context, dataVeremision1);
                    await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

                    // ACTUALIZAR PESO
                    decimal pesoFact = await ventas.Peso_Factura(_context,factuCod);
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

                    // verificar si hay alguno que no tenga codigo
                    /*
                     
                    Dim cadena_items_sin_codigo As String = ""
                    Dim dtsincodigo_sin As New DataTable
                    dtsincodigo_sin.Clear()
                    dtsincodigo_sin = sia_DAL.Datos.Instancia.ObtenerDataTable("select coditem from vefactura1 where codproducto_sin='' and codfactura='" & CStr(CInt(CodFacturas_Grabadas(i))) & "'")

                    For u As Integer = 0 To dtsincodigo_sin.Rows.Count - 1
                        If cadena_items_sin_codigo.Trim.Length = 0 Then
                            cadena_items_sin_codigo = dtsincodigo_sin.Rows(u)("coditem")
                        Else
                            cadena_items_sin_codigo &= " , " & dtsincodigo_sin.Rows(u)("coditem")
                        End If
                    Next
                     
                     */

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
                            ////////////////////
                            // preguntar si hay conexion con el SIN para generar el CUF en tipo emision en linea(1) o fuera de linea (0)
                            var serviOnline = await _context.adsiat_parametros_facturacion.Where(i=> i.codalmacen == dataFactura.codalmacen).Select(i=> new
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
                                /*
                                if (adsiat_sin_activo && await funciones.)   // ACA FALTA VALIDAR CON EL SIN OJOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
                                {

                                }
                                */
                            }
                            else
                            {
                                // actualizar en_linea false porq NO hay conexion a internet
                                dataFactura.en_linea = false;
                                // YA NO preguntar si hay conexion con el SIN porque si no hay internet no hay como comunicarse con el SIN
                                dataFactura.en_linea_SIN = false;
                                await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos
                            }


                        }
                        else
                        {

                        }
                    }
                    else
                    {

                    }


                }
            }

            return (resultado,"");
        }


    }

    // CLASES AUXILIARES
    public class tabladescuentosnNR
    {
        public int codremision { get; set; }
        public int coddesextra { get; set; }
        public decimal porcen { get; set; }
        public decimal montodoc { get; set; }
        public int? codcobranza { get; set; }
        public int? codcobranza_contado { get; set; }
        public int? codanticipo { get; set; }
        public string codmoneda { get; set; }
    }

    public class Total_Detalle_Factura
    {
        public double total_iva { get; set; } = 0;
        public double Desctos { get; set; } = 0;
        public double Recargos { get; set; } = 0;
        public double Total_factura { get; set; } = 0;
        public double Total_Dist { get; set; } = 0;
    }

    public class tablaFacturas
    {
        public int nro { get; set; }
        public double subtotal { get; set; }
        public double descuentos { get; set; }
        public double recargos { get; set; }
        public double total { get; set; }
        public double iva { get; set; }
        public double monto { get; set; }
    }




    public class data_get_facturasNR
    {
        public string idNR { get; set; } 
        public int nroIdNR { get; set; } 
        public string codEmpresa { get; set; } 
        public string cufd { get; set; } 
        public string usuario { get; set; }

        public int nrocaja { get; set; }
        public bool valProfDespachos { get; set; }
        public bool valFechaRemiHoy { get; set; }
        public bool valFactContado { get; set; }
        public bool valTipoCam { get; set; }
    }

}