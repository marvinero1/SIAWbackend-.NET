using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;
using LibSIAVB;
using siaw_ws_siat;
using SIAW.Controllers.ventas.transaccion;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Polly;
using SIAW.Controllers.ventas.modificacion;

namespace SIAW.Controllers.z_pruebas
{
    [Route("api/pruebas/[controller]")]
    [ApiController]
    public class generadorProfsController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly siaw_funciones.empaquesFunciones empaque_func = new siaw_funciones.empaquesFunciones();
        private readonly siaw_funciones.datosProforma datos_proforma = new siaw_funciones.datosProforma();
        private readonly siaw_funciones.ClienteCasual clienteCasual = new siaw_funciones.ClienteCasual();
        private readonly siaw_funciones.Cliente cliente = new siaw_funciones.Cliente();
        private readonly siaw_funciones.Empresa empresa = new siaw_funciones.Empresa();
        private readonly siaw_funciones.Saldos saldos = new siaw_funciones.Saldos();
        private readonly siaw_funciones.TipoCambio tipocambio = new siaw_funciones.TipoCambio();
        private readonly siaw_funciones.Ventas ventas = new siaw_funciones.Ventas();
        //private readonly siaw_funciones.IVentas ventas;
        private readonly siaw_funciones.Items items = new siaw_funciones.Items();
        private readonly siaw_funciones.Validar_Vta validar_Vta = new siaw_funciones.Validar_Vta();
        private readonly siaw_funciones.Almacen almacen = new siaw_funciones.Almacen();
        private readonly siaw_funciones.SIAT siat = new siaw_funciones.SIAT();
        private readonly siaw_funciones.Configuracion configuracion = new siaw_funciones.Configuracion();
        private readonly siaw_funciones.Creditos creditos = new siaw_funciones.Creditos();
        private readonly siaw_funciones.Cobranzas cobranzas = new siaw_funciones.Cobranzas();
        private readonly siaw_funciones.Nombres nombres = new siaw_funciones.Nombres();
        private readonly siaw_funciones.Seguridad seguridad = new siaw_funciones.Seguridad();

        private readonly siaw_funciones.Funciones funciones = new Funciones();
        private readonly siaw_funciones.ziputil ziputil = new ziputil();
        private readonly func_encriptado encripVB = new func_encriptado();
        private readonly Anticipos_Vta_Contado anticipos_vta_contado = new Anticipos_Vta_Contado();
        private readonly Log log = new Log();
        private readonly Depositos_Cliente depositos_cliente = new Depositos_Cliente();
        private readonly string _controllerName = "veproformaController";


        private readonly Funciones_SIAT funciones_SIAT = new Funciones_SIAT();
        private readonly ServFacturas serv_Facturas = new ServFacturas();


        public generadorProfsController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        [HttpPost]
        [Route("generarProformas/{userConn}/{fechaInicio}/{fechaFin}")]
        public async Task<ActionResult<List<sldosItemCompleto>>> generarProformas(string userConn, DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var dtProformas = await _context.veproforma.Where(i => i.fecha >= fechaInicio && i.fecha <= fechaFin && i.anulada == false && i.codcliente.StartsWith("SN"))
                        .Select(i => new
                        {
                            i.codigo,
                            i.id,
                            i.numeroid,
                            i.fecha,
                            i.codalmacen,
                            i.nit
                        }).ToListAsync();
                    foreach (var reg in dtProformas) {

                    }
                    return Ok();
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener empaques: " + ex.Message);
                throw;
            }
        }




        private async Task<object> transferirdoc(DBContext _context, string idProforma, int nroidProforma, string usuario)
        {
            var cabecera = await _context.veproforma
                        .Where(i => i.id == idProforma && i.numeroid == nroidProforma)
                        .FirstOrDefaultAsync();

            if (cabecera == null)
            {
                return BadRequest(new { resp = "No se encontró una proforma con los datos proporcionados, revise los datos" });
            }

            if (cabecera.tipo_complementopf == 0)
            {
                cabecera.tipo_complementopf = 3;
            }
            if (cabecera.tipo_complementopf == 1 || cabecera.tipo_complementopf == 2)
            {
                cabecera.tipo_complementopf = cabecera.tipo_complementopf - 1;
            }


            int codvendedorClienteProf = await ventas.Vendedor_de_Cliente_De_Proforma(_context, idProforma, nroidProforma);
            if (!await seguridad.autorizado_vendedores(_context, usuario, codvendedorClienteProf, codvendedorClienteProf))
            {
                return BadRequest(new { resp = "No esta autorizado para ver esta información." });
            }


            // obtener razon social de cliente
            var codclientedescripcion = await cliente.Razonsocial(_context, cabecera.codcliente);
            // obtener tipo cliente
            var tipo_cliente = await cliente.Tipo_Cliente(_context, cabecera.codcliente);
            // establecer ubicacion
            if (cabecera.ubicacion == null || cabecera.ubicacion == "")
            {
                cabecera.ubicacion = "NSE";
            }
            // texto Confirmada 
            string descConfirmada = "";
            if ((cabecera.confirmada ?? false) == true)
            {
                descConfirmada = "CONFIRMADA " + cabecera.hora_confirmada + " " + (cabecera.fecha_confirmada ?? new DateTime(1900, 1, 1)).ToShortDateString();
            }

            string estadodoc = "";
            if (cabecera.anulada)
            {
                estadodoc = "ANULADA";
            }
            else
            {
                if (cabecera.transferida)
                {
                    estadodoc = "TRANSFERIDA";
                }
                else
                {
                    if (cabecera.aprobada)
                    {
                        estadodoc = "APROBADA";
                    }
                    else
                    {
                        estadodoc = "NO APROBADA";
                    }
                }
            }

            bool cliHabilitado = await cliente.clientehabilitado(_context, cabecera.codcliente);

            // obtener detalles.
            var codProforma = cabecera.codigo;
            var detalle = await _context.veproforma1
                .Where(i => i.codproforma == codProforma)
                .Join(_context.initem,
                p => p.coditem,
                i => i.codigo,
                (p, i) => new { p, i })
                .Select(i => new itemDataMatriz
                {
                    //codproforma = i.p.codproforma,
                    coditem = i.p.coditem,
                    descripcion = i.i.descripcion,
                    medida = i.i.medida,
                    cantidad = (double)i.p.cantidad,
                    udm = i.p.udm,
                    precioneto = (double)i.p.precioneto,
                    preciodesc = (double)(i.p.preciodesc ?? 0),
                    niveldesc = i.p.niveldesc,
                    preciolista = (double)i.p.preciolista,
                    codtarifa = i.p.codtarifa,
                    coddescuento = i.p.coddescuento,
                    total = (double)i.p.total,
                    // cantaut = i.p.cantaut,
                    // totalaut = i.p.totalaut,
                    // obs = i.p.obs,
                    porceniva = (double)(i.p.porceniva ?? 0),
                    cantidad_pedida = (double)(i.p.cantidad_pedida ?? 0),
                    // peso = i.p.peso,
                    nroitem = i.p.nroitem ?? 0,
                    // id = i.p.id,
                    porcen_mercaderia = 0,
                    porcendesc = 0
                })
                .ToListAsync();



            // obtener recargos
            var recargos = await _context.verecargoprof.Where(i => i.codproforma == codProforma).ToListAsync();

            // obtener descuentos
            var descuentosExtra = await _context.vedesextraprof
                .Join(_context.vedesextra,
                p => p.coddesextra,
                e => e.codigo,
                (p, e) => new { p, e })
                .Where(i => i.p.codproforma == codProforma)
                .Select(i => new
                {
                    i.p.codproforma,
                    i.p.coddesextra,
                    descripcion = i.e.descripcion,
                    i.p.porcen,
                    i.p.montodoc,
                    i.p.codcobranza,
                    i.p.codcobranza_contado,
                    i.p.codanticipo,
                    i.p.id
                })
                .ToListAsync();

            // obtener iva
            var iva = await _context.veproforma_iva.Where(i => i.codproforma == codProforma).ToListAsync();
            // veproforma etiqueta
            var profEtiqueta = await _context.veproforma_etiqueta.Where(i => i.id_proforma == idProforma && i.nroid_proforma == nroidProforma).ToListAsync();
            // veetiqueta proforma
            var etiquetaProf = await _context.veetiqueta_proforma.Where(i => i.id == idProforma && i.numeroid == nroidProforma).ToListAsync();
            // etiqueta de proforma si es venta casual
            var veprofEtiqueta = await _context.veproforma_etiqueta.Where(i => i.id_proforma == idProforma && i.nroid_proforma == nroidProforma).FirstOrDefaultAsync();

            // calcular el porcendesc
            string codclienteReal = cabecera.codcliente;
            if (veprofEtiqueta != null)
            {
                codclienteReal = veprofEtiqueta.codcliente_real;
            }
            foreach (var reg in detalle)
            {
                reg.porcendesc = (double)await cliente.porcendesccliente(_context, codclienteReal, reg.coditem, reg.codtarifa, cabecera.niveles_descuento);
            }

            // OBTENER ANTICIPOS
            var dt_anticipo_pf = await anticipos_vta_contado.Anticipos_Aplicados_a_Proforma(_context, idProforma, nroidProforma);
            foreach (var reg in dt_anticipo_pf)
            {
                reg.docanticipo = reg.id_anticipo + "-" + reg.nroid_anticipo;
            }
            //totalizar_anticipos_asignados()
            string anticiposTot = await totalizar_anticipos_asignados(_context, cabecera.codmoneda, dt_anticipo_pf);

            // obtener validaciones
            var dtvalidar = await Recuperar_Validacion(_context, codProforma);

            return Ok(new
            {
                codclientedescripcion,
                tipo_cliente,
                descConfirmada,
                habilitado = cliHabilitado,
                anticiposTot,
                estadodoc,

                cabecera = cabecera,
                detalle = detalle,
                descuentos = descuentosExtra,
                recargos = recargos,
                iva = iva,
                profEtiqueta = profEtiqueta,
                veprofEtiqueta = veprofEtiqueta,
                etiquetaProf = etiquetaProf,
                anticipos = dt_anticipo_pf,
                detalleValida = dtvalidar
            });
        }
        private async Task<string> totalizar_anticipos_asignados(DBContext _context, string codmoneda, List<vedetalleanticipoProforma> dt_anticipo_pf)
        {
            double ttl_aplicado = 0;
            foreach (var reg in dt_anticipo_pf)
            {
                // Desde 14/12/2023 realizar la conversion del monto asignado segun la moneda del anticipo y proforma
                if (reg.codmoneda == codmoneda)
                {
                    ttl_aplicado += reg.monto;
                }
                else
                {
                    ttl_aplicado += (double)(await tipocambio._conversion(_context, codmoneda, reg.codmoneda, DateTime.Now.Date, (decimal)reg.monto));
                    ttl_aplicado = Math.Round(ttl_aplicado, 2);
                }
            }
            return ttl_aplicado.ToString("#,0.00", new System.Globalization.CultureInfo("en-US"));
        }

        private async Task<List<DataValidar>> Recuperar_Validacion(DBContext _context, int codigodoc)
        {
            // recuperar el detalle de la validacion
            var dtvalidar = await _context.veproforma_valida
                .Join(_context.vetipo_control_vtas,
                    p1 => p1.codcontrol,
                    p2 => p2.codcontrol,
                    (p1, p2) => new
                    {
                        p1,
                        p2
                    })
                .Where(joined => joined.p1.codproforma == codigodoc)
                .OrderBy(joined => joined.p2.orden)
                .Select(joined => new DataValidar
                {
                    codproforma = joined.p1.codproforma,
                    orden = joined.p2.orden,
                    codcontrol = joined.p1.codcontrol,
                    desc_grabar = "", // as desc_grabar
                    grabar = joined.p2.grabar,
                    grabar_aprobar = joined.p2.grabar_aprobar,
                    nit = joined.p1.nit,
                    nroitems = joined.p1.nroitems,
                    subtotal = joined.p1.subtotal,
                    recargos = joined.p1.recargos,
                    descuentos = joined.p1.descuentos,

                    total = joined.p1.total,
                    valido = joined.p1.valido,
                    observacion = joined.p1.observacion,
                    obsdetalle = joined.p1.obsdetalle,
                    codservicio = joined.p1.codservicio,
                    descservicio = "", // as descservicio
                    descripcion = joined.p2.descripcion,
                    datoa = joined.p1.datoa,
                    datob = joined.p1.datob,
                    clave_servicio = joined.p1.clave_servicio,
                    accion = "" // as accion
                })
                .ToListAsync();

            foreach (var reg in dtvalidar)
            {
                if (reg.codservicio != null && reg.codservicio > 0)
                {
                    reg.descservicio = await nombres.nombre_servicio(_context, reg.codservicio ?? 0);
                }
            }
            return dtvalidar;
        }

        private async Task<object> TotalizarProf(DBContext _context, string codempresa, string usuario, string userConnectionString, bool desclinea_segun_solicitud, string opcion_nivel, string codcliente_real, int cmbtipo_complementopf, TotabilizarProformaCompleta datosProforma)
        {
            veproforma veproforma = datosProforma.veproforma;
            List<veproforma1_2> veproforma1_2 = datosProforma.veproforma1_2;
            List<veproforma_valida> veproforma_valida = datosProforma.veproforma_valida;
            var veproforma_anticipo = datosProforma.veproforma_anticipo;
            var vedesextraprof = datosProforma.vedesextraprof;
            var verecargoprof = datosProforma.verecargoprof;
            var veproforma_iva = datosProforma.veproforma_iva;
            var tabla_anticipos_asignados = datosProforma.detalleAnticipos;

            var data = veproforma1_2.Select(i => new cargadofromMatriz
            {
                coditem = i.coditem,
                tarifa = i.codtarifa,
                descuento = i.coddescuento,
                empaque = i.empaque,
                cantidad_pedida = i.cantidad_pedida ?? 0,
                cantidad = i.cantidad,
                // codcliente = veproforma.codcliente
                codcliente = codcliente_real,
                opcion_nivel = opcion_nivel,
                codalmacen = veproforma.codalmacen,
                desc_linea_seg_solicitud = desclinea_segun_solicitud ? "SI" : "NO",  //(SI o NO)
                codmoneda = veproforma.codmoneda,
                fecha = veproforma.fecha,
                nroitem = i.nroitem,
                porcen_mercaderia = i.porcen_mercaderia
            }).ToList();


            var tabla_detalle = veproforma1_2.Select(i => new itemDataMatriz
            {
                coditem = i.coditem,
                descripcion = "",
                medida = "",
                udm = i.udm,
                porceniva = (double)i.porceniva,
                empaque = i.empaque,
                cantidad_pedida = (double)i.cantidad_pedida,
                cantidad = (double)i.cantidad,
                porcen_mercaderia = Convert.ToDouble(i.porcen_mercaderia),
                codtarifa = i.codtarifa,
                coddescuento = i.coddescuento,
                preciolista = (double)i.preciolista,
                niveldesc = i.niveldesc,
                porcendesc = 0,
                preciodesc = (double)i.preciodesc,
                precioneto = (double)i.precioneto,
                total = (double)i.total
            }).ToList();


            //aplicar desctos primavera 2022
            //Aplicar_Descto_Primavera2022(False)

            ////////////////////////////////////////////////////////////
            //verificar los precios permitidos al usuario
            /*
            string cadena_precios_no_autorizados_al_us = await validar_Vta.Validar_Precios_Permitidos_Usuario(_context, usuario, tabla_detalle);
            if (cadena_precios_no_autorizados_al_us.Trim().Length > 0)
            {
                return BadRequest(new { resp = "El documento tiene items a precio(s): " + cadena_precios_no_autorizados_al_us + " los cuales no estan asignados al usuario " + veproforma.usuarioreg + " verifique esta situacion!!!" });
            }
            */
            ////////////////////////////////////////////////////////////


            //verificar si la solicitud de descuentos de linea existe
            /*
            if (desclinea_segun_solicitud)
            {
                if (!await ventas.Existe_Solicitud_Descuento_Nivel(_context, veproforma.idsoldesctos, veproforma.nroidsoldesctos ?? 0))
                {
                    return BadRequest(new { resp = "Ha elegido utilizar la solicitud de descuentos de nivel: " + veproforma.idsoldesctos + "-" + veproforma.nroidsoldesctos + " para aplicar descuentos de linea, pero la solicitud indicada no existe!!!" });
                }
                if (codcliente_real != await ventas.Cliente_Solicitud_Descuento_Nivel(_context, veproforma.idsoldesctos, veproforma.nroidsoldesctos ?? 0))
                {
                    return BadRequest(new { resp = "La solicitud de descuentos de nivel: " + veproforma.idsoldesctos + "-" + veproforma.nroidsoldesctos + " a la que hace referencia no pertenece al mismo cliente de esta proforma!!!" });
                }
            }
            */
            /*
             codmoneda.Text = Trim(codmoneda.Text)

            If Trim(codmoneda.Text) = "" Then
                codmoneda.Text = sia_funciones.Empresa.Instancia.monedabase(sia_compartidos.temporales.Instancia.codempresa)
                tdc.Text = "1"
            Else
                tdc.Text = sia_funciones.TipoCambio.Instancia.tipocambio(sia_funciones.Empresa.Instancia.monedabase(sia_compartidos.temporales.Instancia.codempresa), codmoneda.Text, fecha.Value.Date)
            End If
             */




            var resultado = await calculoPreciosMatriz(_context, codempresa, usuario, userConnectionString, data, false);


            // var totales = await RECALCULARPRECIOS(_context, false, codempresa, cmbtipo_complementopf, codcliente_real, resultado, verecargoprof, veproforma, vedesextraprof);
            var totales = await RECALCULARPRECIOS(_context, false, codempresa, cmbtipo_complementopf, codcliente_real, resultado, verecargoprof, veproforma, vedesextraprof, tabla_anticipos_asignados);

            return (new
            {
                totales = totales,
                detalleProf = resultado
            });

        }

        private async Task<List<itemDataMatriz>> calculoPreciosMatriz(DBContext _context, string codEmpresa, string usuario, string userConnectionString, List<cargadofromMatriz> data, bool calcular_porcentaje)
        {
            List<itemDataMatriz> resultado = new List<itemDataMatriz>();
            string monedabase = "";
            int _descuento_precio = 0;
            //porcentaje de mercaderia
            decimal porcen_merca = 0;
            var controla_stok_seguridad = await empresa.ControlarStockSeguridad(userConnectionString, codEmpresa);
            foreach (var reg in data)
            {
                //precio unitario del item
                var precioItem = await _context.intarifa1
                    .Where(i => i.codtarifa == reg.tarifa && i.item == reg.coditem)
                    .Select(i => i.precio)
                    .FirstOrDefaultAsync() ?? 0;
                //convertir a la moneda el precio item
                monedabase = await ventas.monedabasetarifa(_context, reg.tarifa);
                precioItem = await tipocambio._conversion(_context, reg.codmoneda, monedabase, reg.fecha, (decimal)precioItem);
                precioItem = await cliente.Redondear_5_Decimales(_context, (decimal)precioItem);
                porcen_merca = reg.porcen_mercaderia;
                if (calcular_porcentaje == true)
                {
                    if (reg.codalmacen > 0)
                    {
                        if (controla_stok_seguridad == true)
                        {
                            //List<sldosItemCompleto> sld_ctrlstock_para_vtas = await saldos.SaldoItem_CrtlStock_Para_Ventas(userConnectionString, "311", codalmacen, coditem, "PE", "dpd3");
                            var sld_ctrlstock_para_vtas = await saldos.SaldoItem_CrtlStock_Para_Ventas(userConnectionString, "", reg.codalmacen, reg.coditem, codEmpresa, usuario);
                            if (sld_ctrlstock_para_vtas > 0)
                            {
                                porcen_merca = reg.cantidad * 100 / sld_ctrlstock_para_vtas;
                            }
                            else { porcen_merca = 0; }
                        }
                        else { porcen_merca = 0; }
                    }
                    else
                    {
                        porcen_merca = 0;
                    }
                }


                // descuento asignar asutomaticamente dependiendo de cantidad
                _descuento_precio = await ventas.Codigo_Descuento_Especial_Precio(_context, reg.tarifa);
                // pregunta si la cantidad ingresada cumple o no el empaque para descuento
                if (await ventas.Cumple_Empaque_De_DesctoEspecial(_context, reg.coditem, reg.tarifa, _descuento_precio, reg.cantidad, reg.codcliente))
                {
                    // si cumple
                    reg.descuento = _descuento_precio;
                }
                else
                {
                    reg.descuento = 0;
                }

                //descuento de nivel del cliente
                var niveldesc = await cliente.niveldesccliente(_context, reg.codcliente, reg.coditem, reg.tarifa, reg.opcion_nivel, false);

                //porcentaje de descuento de nivel del cliente
                var porcentajedesc = await cliente.porcendesccliente(_context, reg.codcliente, reg.coditem, reg.tarifa, reg.opcion_nivel, false);

                //preciodesc 
                var preciodesc = await cliente.Preciodesc(_context, reg.codcliente, reg.codalmacen, reg.tarifa, reg.coditem, reg.desc_linea_seg_solicitud, niveldesc, reg.opcion_nivel);
                preciodesc = await tipocambio.conversion(userConnectionString, reg.codmoneda, monedabase, reg.fecha, (decimal)preciodesc);

                preciodesc = await cliente.Redondear_5_Decimales(_context, preciodesc);
                //precioneto 
                var precioneto = await cliente.Preciocondescitem(_context, reg.codcliente, reg.codalmacen, reg.tarifa, reg.coditem, reg.descuento, reg.desc_linea_seg_solicitud, niveldesc, reg.opcion_nivel);
                precioneto = await tipocambio.conversion(userConnectionString, reg.codmoneda, monedabase, reg.fecha, (decimal)precioneto);
                precioneto = await cliente.Redondear_5_Decimales(_context, precioneto);
                //total
                var total = reg.cantidad * precioneto;
                total = await cliente.Redondear_5_Decimales(_context, total);

                var item = await _context.initem
                    .Where(i => i.codigo == reg.coditem)
                    .Select(i => new itemDataMatriz
                    {
                        coditem = i.codigo,
                        descripcion = i.descripcion,
                        medida = i.medida,
                        udm = i.unidad,
                        porceniva = (double)i.iva,
                        empaque = reg.empaque,
                        cantidad_pedida = (double)reg.cantidad_pedida,
                        cantidad = (double)reg.cantidad,
                        porcen_mercaderia = (double)Math.Round(porcen_merca, 2),
                        codtarifa = reg.tarifa,
                        coddescuento = reg.descuento,
                        preciolista = (double)precioItem,
                        niveldesc = niveldesc,
                        porcendesc = (double)porcentajedesc,
                        preciodesc = (double)preciodesc,
                        precioneto = (double)precioneto,
                        total = (double)total,
                        cumpleMin = reg.cumpleMin,
                        nroitem = reg.nroitem ?? 0
                    })
                    .FirstOrDefaultAsync();

                if (item != null)
                {
                    resultado.Add(item);
                }
            }
            if (resultado.Count() < 1)
            {
                return null;
            }
            resultado = resultado.OrderBy(i => i.nroitem).ThenByDescending(i => i.coddescuento).ToList();
            return resultado;
        }

        private async Task<object> RECALCULARPRECIOS(DBContext _context, bool reaplicar_desc_deposito, string codempresa, int cmbtipo_complementopf, string codcliente_real, List<itemDataMatriz> tabla_detalle, List<tablarecargos> tablarecargos, veproforma veproforma, List<tabladescuentos> vedesextraprof, List<vedetalleanticipoProforma> tabla_anticipos_asignados)
        {
            var tabladescuentos = vedesextraprof.Select(i => new tabladescuentos
            {
                codproforma = i.codproforma,
                coddesextra = i.coddesextra,
                porcen = i.porcen,
                montodoc = i.montodoc,
                codcobranza = i.codcobranza,
                codcobranza_contado = i.codcobranza_contado,
                codanticipo = i.codanticipo,
                id = i.id,
                codmoneda = veproforma.codmoneda
            }).ToList();

            var result = await versubtotal(_context, tabla_detalle);
            double subtotal = result.st;
            double peso = result.peso;
            if (reaplicar_desc_deposito)
            {
                // Revisar_Aplicar_Descto_Deposito(preguntar_si_aplicare_desc_deposito);
            }

            var respRecargo = await verrecargos(_context, codempresa, veproforma.codmoneda, veproforma.fecha, subtotal, tablarecargos);
            double recargo = respRecargo.total;

            //var respDescuento = await verdesextra(_context, codempresa, veproforma.nit, veproforma.codmoneda, cmbtipo_complementopf, veproforma.idpf_complemento, veproforma.nroidpf_complemento ?? 0, subtotal, veproforma.fecha, tabladescuentos, tabla_detalle);
            var respDescuento = await verdesextra(_context, codempresa, veproforma.nit, veproforma.codmoneda, cmbtipo_complementopf, veproforma.idpf_complemento, veproforma.nroidpf_complemento ?? 0, subtotal, veproforma.fecha, tabladescuentos, tabla_detalle, veproforma.tipopago, (bool)veproforma.contra_entrega, codcliente_real, tabla_anticipos_asignados);
            double descuento = respDescuento.respdescuentos;

            var resultados = await vertotal(_context, subtotal, recargo, descuento, codcliente_real, veproforma.codmoneda, codempresa, veproforma.fecha, tabla_detalle, tablarecargos);
            //QUITAR
            return new
            {
                subtotal = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, subtotal),
                peso = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, peso),
                recargo = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, recargo),
                descuento = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, descuento),
                iva = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultados.totalIva),
                total = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultados.TotalGen),
                tablaIva = resultados.tablaiva,

                tablaRecargos = respRecargo.tablarecargos,
                tablaDescuentos = respDescuento.tabladescuentos,
                mensaje = respDescuento.mensaje
            };

        }


        private async Task<(double st, double peso)> versubtotal(DBContext _context, List<itemDataMatriz> tabla_detalle)
        {
            // filtro de codigos de items
            tabla_detalle = tabla_detalle.Where(item => item.coditem != null && item.coditem.Length >= 8).ToList();
            // calculo subtotal
            double peso = 0;
            double st = 0;

            foreach (var reg in tabla_detalle)
            {
                st = st + reg.total;
                peso = (double)(peso + (await items.itempeso(_context, reg.coditem)) * reg.cantidad);
            }

            // desde 08/01/2023 redondear el resultado a dos decimales con el SQLServer
            // REVISAR SI HAY OTRO MODO NO DA CON LINQ.
            st = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, st);
            return (st, peso);
        }

        private async Task<(double total, List<tablarecargos> tablarecargos)> verrecargos(DBContext _context, string codempresa, string codmoneda, DateTime fecha, double subtotal, List<tablarecargos> tablarecargos)
        {
            int codrecargo_pedido_urg_provincia = await configuracion.emp_codrecargo_pedido_urgente_provincia(_context, codempresa);
            //TOTALIZAR LOS RECARGOS QUE NO SON POR PEDIDO URG PROVINCIAS (los que se aplican al total final)
            double total = 0;
            foreach (var reg in tablarecargos)
            {
                string tipo = await ventas.Tipo_Recargo(_context, reg.codrecargo);
                if (reg.codrecargo != codrecargo_pedido_urg_provincia)
                {
                    if (tipo == "MONTO")
                    {
                        //si el recargo se aplica directo en MONTO
                        reg.montodoc = await tipocambio._conversion(_context, codmoneda, reg.moneda, fecha, reg.monto);
                    }
                    else
                    {
                        //si el recargo se aplica directo en %
                        reg.montodoc = (decimal)subtotal / 100 * reg.porcen;
                    }
                    reg.montodoc = Math.Round(reg.montodoc, 2);
                    total += (double)reg.montodoc;
                }
            }
            return (total, tablarecargos);

        }

        private async Task<(double respdescuentos, string mensaje, List<tabladescuentos> tabladescuentos)> verdesextra(DBContext _context, string codempresa, string nit, string codmoneda, int cmbtipo_complementopf, string idpf_complemento, int nroidpf_complemento, double subtotal, DateTime fecha, List<tabladescuentos> tabladescuentos, List<itemDataMatriz> detalleProf, int tipopago, bool? contraEntrega, string codcliente_real, List<vedetalleanticipoProforma> dt_anticipo_pf)
        {
            string mensaje = "";
            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            tabladescuentos = await ventas.Ordenar_Descuentos_Extra(_context, tabladescuentos);
            double monto_desc_pf_complementaria = 0;
            //calcular el monto  de descuento segun el porcentaje
            ////////////////////////////////////////////////////////////////////////////////
            //primero calcular los montos de los que se aplican en el detalle o son
            //diferenciados por item
            ////////////////////////////////////////////////////////////////////////////////
            foreach (var reg in tabladescuentos)
            {
                //verifica si el descuento es diferenciado por item
                if (await ventas.DescuentoExtra_Diferenciado_x_item(_context, reg.coddesextra))
                {
                    var resp = await ventas.DescuentoExtra_CalcularMonto(_context, reg.coddesextra, detalleProf, "", nit);
                    double monto_desc = resp.resultado;
                    detalleProf = resp.dt;

                    //si hay complemento, verificar cual es el complemento
                    if (cmbtipo_complementopf == 1 && idpf_complemento.Trim().Length > 0 && nroidpf_complemento > 0)
                    {
                        int codproforma_complementaria = await ventas.codproforma(_context, idpf_complemento, nroidpf_complemento);
                        //verificar si la proforma ya tiene el mismo descto extra, solo SI NO TIENE, se debe calcular de esa cuanto seria el descto
                        //implemantado en fecha:31-08-2022
                        if (!await ventas.Proforma_Tiene_DescuentoExtra(_context, codproforma_complementaria, reg.coddesextra))
                        {
                            List<itemDataMatriz> dtproforma1 = await _context.veproforma1
                                .Where(i => i.codproforma == codproforma_complementaria)
                                .OrderBy(i => i.coditem)
                                .Select(i => new itemDataMatriz
                                {
                                    coditem = i.coditem,
                                    //descripcion = i.descripcion,

                                    //medida = i.medida,
                                    udm = i.udm,
                                    porceniva = (double)i.porceniva,
                                    cantidad_pedida = (double)i.cantidad_pedida,
                                    cantidad = (double)i.cantidad,
                                    //porcen_mercaderia = i.porcen_mercaderia,
                                    codtarifa = i.codtarifa,
                                    coddescuento = i.coddescuento,
                                    preciolista = (double)i.preciolista,
                                    niveldesc = i.niveldesc,
                                    //porcendesc = i.porcendesc,
                                    //preciodesc = i.preciodesc,
                                    precioneto = (double)i.precioneto,
                                    total = (double)i.total,
                                    //cumple = i.cumple,
                                    nroitem = i.nroitem ?? 0,
                                })
                                .ToListAsync();
                            var resul = await ventas.DescuentoExtra_CalcularMonto(_context, reg.coddesextra, dtproforma1, "", nit);
                            monto_desc_pf_complementaria = resul.resultado;
                        }
                        else
                        {
                            monto_desc_pf_complementaria = 0;
                        }

                    }
                    //sumar el monto de la proforma complementaria
                    reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)(monto_desc + monto_desc_pf_complementaria));
                }
            }

            ////////////////////////////////////////////////////////////////////////////////
            //los que se aplican en el SUBTOTAL
            ////////////////////////////////////////////////////////////////////////////////
            foreach (var reg in tabladescuentos)
            {
                if (!await ventas.DescuentoExtra_Diferenciado_x_item(_context, reg.coddesextra))
                {
                    if (reg.aplicacion == "SUBTOTAL")
                    {
                        if (coddesextra_depositos == reg.coddesextra)
                        {
                            //el monto por descuento de deposito ya esta calculado
                            //pero se debe verificar si este monto de este descuento esta en la misma moneda que la proforma

                            if (reg.codmoneda != codmoneda)
                            {
                                double monto_cambio = (double)await tipocambio._conversion(_context, codmoneda, reg.codmoneda, fecha, reg.montodoc);
                                reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)monto_cambio);
                                reg.codmoneda = codmoneda;
                            }
                        }
                        else
                        {
                            //este descuento se aplica sobre el subtotal de la venta
                            reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)((subtotal / 100) * (double)reg.porcen));
                        }
                    }
                }
            }

            //totalizar los descuentos que se aplicar al subtotal
            double total_desctos1 = 0;
            foreach (var reg in tabladescuentos)
            {
                if (reg.aplicacion == "SUBTOTAL")
                {
                    total_desctos1 += (double)reg.montodoc;
                }
            }
            //desde 08 / 01 / 2023 redondear el resultado a dos decimales con el SQLServer
            total_desctos1 = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)total_desctos1);
            // retornar total_desctos1

            ////////////////////////////////////////////////////////////////////////////////
            //los que se aplican en el TOTAL
            ////////////////////////////////////////////////////////////////////////////////
            double total_preliminar = subtotal - total_desctos1;
            //'##########################################################
            //'//Desde 24/09/2024 en el total_preliminar verificar si el cliente tiene anticipos anteriores con saldos pendientes y si estos no tienen enlace con un deposito,
            //'sino lo tuvieran el enlace entonces el monto del anticipo debe restar al total_preliminar para que con ese nuevo subtotal se saque el 3 % del descuento por deposito que le corresponde
            //'si los anticipos tienen un enlace con deposito entonces no deben restar al total_preliminar

            if (await configuracion.Calculo_Desc_Deposito_Contado(_context, codempresa) == "SUBTOTAL2" && tipopago == 0 && contraEntrega == false)
            {
                var tablaAnticiposSinDeposito = await anticipos_vta_contado.Anticipos_MontoRestante_Sin_Deposito(_context, codcliente_real);
                decimal totalAnticiposSinDeposito = 0;
                decimal montoCambio = 0;
                string cadenaAnticipos = string.Empty;

                if (tablaAnticiposSinDeposito.Count > 0)
                {
                    foreach (var row in tablaAnticiposSinDeposito)
                    {
                        if (row.codmoneda == codmoneda)
                        {
                            totalAnticiposSinDeposito += await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, row.montorest);
                        }
                        else
                        {
                            montoCambio = await tipocambio._conversion(_context, codmoneda, row.codmoneda, row.fecha, (decimal)row.montorest);
                            montoCambio = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)montoCambio);
                            totalAnticiposSinDeposito += montoCambio;
                        }

                        if (string.IsNullOrEmpty(cadenaAnticipos))
                        {
                            cadenaAnticipos = row.id + "-" + row.numeroid;
                        }
                        else
                        {
                            cadenaAnticipos += ", " + row.id + "-" + row.numeroid;
                        }
                    }

                    total_preliminar -= (double)totalAnticiposSinDeposito;

                    if (!string.IsNullOrEmpty(cadenaAnticipos))
                    {
                        mensaje = "El cliente tiene anticipos que no tienen enlace con un deposito (" + cadenaAnticipos + "), con montos restantes mayores a 0 (cero); los cuales suman:  " + totalAnticiposSinDeposito + " " + codmoneda + ". Por lo tanto ese monto se reducira al monto del subtotal preliminar despues de los demas descuentos: (" + total_preliminar.ToString("#,0.00", new System.Globalization.CultureInfo("en-US")) + " " + codmoneda + ".), de este monto se realizara el calculo del descuento por deposito al contado.";
                    }
                }
                else if (dt_anticipo_pf.Count > 0)
                {
                    foreach (var row in dt_anticipo_pf)
                    {
                        if (!await cobranzas.Anticipo_Esta_Enlazado_a_Deposito(_context, row.id_anticipo, row.nroid_anticipo))
                        {
                            if (row.codmoneda == codmoneda)
                            {
                                totalAnticiposSinDeposito += await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, row.monto);
                            }
                            else
                            {
                                montoCambio = await tipocambio._conversion(_context, codmoneda, row.codmoneda, row.fechareg, (decimal)row.monto);
                                montoCambio = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)montoCambio);
                                totalAnticiposSinDeposito += montoCambio;
                            }

                            if (string.IsNullOrEmpty(cadenaAnticipos))
                            {
                                cadenaAnticipos = row.id_anticipo + "-" + row.nroid_anticipo;
                            }
                            else
                            {
                                cadenaAnticipos += ", " + row.id_anticipo + "-" + row.nroid_anticipo;
                            }
                        }
                    }

                    total_preliminar -= (double)totalAnticiposSinDeposito;

                    if (!string.IsNullOrEmpty(cadenaAnticipos))
                    {
                        mensaje = "El cliente tiene anticipos asignados que no tienen enlace con un deposito (" + cadenaAnticipos + "), con montos restantes mayores a 0 (cero); los cuales suman: " + totalAnticiposSinDeposito + " " + codmoneda + ". Por lo tanto ese monto se reducira al monto del subtotal preliminar despues de los demas descuentos: (" + total_preliminar + " " + codmoneda + "), de este monto se realizara el calculo del descuento por deposito al contado.";
                    }
                }
            }

            //'##########################################################
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
            //desde 08 / 01 / 2023 redondear el resultado a dos decimales con el SQLServer
            total_desctos2 = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)total_desctos2);

            double respdescuentos = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)(total_desctos1 + total_desctos2));

            return (respdescuentos, mensaje, tabladescuentos);

        }



        private async Task<(double totalIva, double TotalGen, List<veproforma_iva> tablaiva)> vertotal(DBContext _context, double subtotal, double recargos, double descuentos, string codcliente_real, string codmoneda, string codempresa, DateTime fecha, List<itemDataMatriz> tabladetalle, List<tablarecargos> tablarecargos)
        {
            double suma = subtotal + recargos - descuentos;
            double totalIva = 0;
            if (suma < 0)
            {
                suma = 0;
            }
            List<veproforma_iva> tablaiva = new List<veproforma_iva>();
            if (await cliente.DiscriminaIVA(_context, codcliente_real))
            {
                // Calculo de ivas
                tablaiva = await CalcularTablaIVA(subtotal, recargos, descuentos, tabladetalle);
                //fin calculo ivas
                totalIva = await veriva(tablaiva);
                suma = suma + totalIva;
            }
            //obtener los recargos que se aplican al final
            var respues = await ventas.Recargos_Sobre_Total_Final(_context, suma, codmoneda, fecha, codempresa, tablarecargos);
            double ttl_recargos_finales = respues.ttl_recargos_sobre_total_final;

            suma = suma + ttl_recargos_finales;
            return (totalIva, suma, tablaiva);
        }

        private async Task<List<veproforma_iva>> CalcularTablaIVA(double subtotal, double recargos, double descuentos, List<itemDataMatriz> tabladetalle)
        {
            List<clsDobleDoble> lista = new List<clsDobleDoble>();

            foreach (var reg in tabladetalle)
            {
                bool encontro = false;
                foreach (var item in lista)
                {
                    if (item.dobleA == reg.porceniva)
                    {
                        encontro = true;
                        item.dobleB = item.dobleB + reg.total;
                        break;
                    }
                }
                if (!encontro)
                {
                    clsDobleDoble newReg = new clsDobleDoble();
                    newReg.dobleA = reg.porceniva;
                    newReg.dobleB = reg.total;
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





    }
}
