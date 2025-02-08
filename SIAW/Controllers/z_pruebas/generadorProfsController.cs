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
using NuGet.Configuration;
using MessagePack;
using Polly.Caching;
using static siaw_funciones.Validar_Vta;

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
        [Route("generarProformas/{userConn}/{usuario}/{codempresa}/{fechaInicio}/{fechaFin}")]
        public async Task<ActionResult<List<sldosItemCompleto>>> generarProformas(string userConn, string usuario, string codempresa, DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    List<string> confirmaciones = new List<string>();
                    var dtProformas = await _context.veproforma.Where(i => i.fecha >= fechaInicio && i.fecha <= fechaFin && i.anulada == false && !i.codcliente.StartsWith("SN") && (i.id.StartsWith("PF") || i.id.StartsWith("XF") || i.id.StartsWith("WF")))
                        .Select(i => new
                        {
                            i.codigo,
                            i.id,
                            i.numeroid,
                            i.fecha,
                            i.codalmacen,
                            i.nit,
                            i.subtotal,
                            i.descuentos,
                            i.total
                        }).ToListAsync();
                    foreach (var reg in dtProformas)
                    {
                        dataProformas datosProf = await transferirdoc(_context, reg.id, reg.numeroid, usuario);

                        veproforma cabecera = datosProf.cabecera;
                        List<itemDataMatriz> detalle = datosProf.detalle;
                        List<tabladescuentos> descuentos = datosProf.descuentos;
                        List<verecargoprof> recargos = datosProf.recargos;
                        List<veproforma_iva> iva = datosProf.iva;
                        List<veproforma_etiqueta> profEtiqueta = datosProf.profEtiqueta;
                        veproforma_etiqueta? veprofEtiqueta = datosProf.veprofEtiqueta;
                        List<veetiqueta_proforma> etiquetaProf = datosProf.etiquetaProf;
                        List<vedetalleanticipoProforma> anticipos = datosProf.anticipos;
                        List<DataValidar> detalleValida = datosProf.detalleValida;

                        cabecera.fecha = await funciones.FechaDelServidor(_context);
                        cabecera.usuarioreg = usuario;
                        cabecera.codigo = 0;
                        // etiquetaProf[0].codigo = 0;

                        // mapear para totalizar
                        List<veproforma1_2> detalle_2 = detalle.Select(i => new veproforma1_2
                        {
                            codproforma = 0,
                            coditem = i.coditem,
                            empaque = 0,
                            cantidad = (decimal)i.cantidad,
                            udm = i.udm,
                            precioneto = (decimal)i.precioneto,
                            preciodesc = (decimal?)i.preciodesc,
                            niveldesc = i.niveldesc,
                            preciolista = (decimal)i.preciolista,
                            codtarifa = i.codtarifa,
                            coddescuento = (short)i.coddescuento,
                            total = (decimal)i.total,
                            cantaut = (decimal?)i.cantidad,
                            totalaut = (decimal?)i.total,
                            obs = "",
                            porceniva = (decimal?)i.porceniva,
                            cantidad_pedida = (decimal?)i.cantidad_pedida,
                            peso = 0,
                            nroitem = i.nroitem,
                            id = 0,
                            porcen_mercaderia = (decimal)i.porcen_mercaderia
                        }).ToList();

                        List<veproforma_valida> valida_2 = detalleValida.Select(i => new veproforma_valida
                        {
                            codproforma = 0,
                            codcontrol = i.codcontrol,
                            nroitems = i.nroitems,
                            nit = i.nit,
                            subtotal = i.subtotal,
                            descuentos = i.descuentos,
                            recargos = i.recargos,
                            total = i.total,
                            valido = i.valido,
                            observacion = i.observacion,
                            obsdetalle = i.obsdetalle,
                            codservicio = i.codservicio,
                            datoa = i.datoa,
                            datob = i.datob,
                            clave_servicio = i.clave_servicio
                        }).ToList();

                        List<veproforma_anticipo> anticipos_2 = anticipos.Select(i => new veproforma_anticipo
                        {
                            codigo = 0,
                            codproforma = 0,
                            codanticipo = i.codanticipo,
                            monto = (decimal?)i.monto,
                            tdc = (decimal?)i.tdc,
                            fechareg = i.fechareg,
                            usuarioreg = i.usuarioreg,
                            horareg = i.horareg
                        }).ToList();

                        List<tablarecargos> recargos_2 = recargos.Select(i => new tablarecargos
                        {
                            codproforma = 0,
                            codrecargo = i.codrecargo,
                            porcen = i.porcen,
                            monto = i.monto,
                            moneda = i.moneda,
                            montodoc = i.montodoc,
                            codcobranza = i.codcobranza,
                            descripcion = ""
                        }).ToList();

                        string codclienteReal = cabecera.codcliente;
                        if (veprofEtiqueta != null)
                        {
                            codclienteReal = veprofEtiqueta.codcliente_real;
                        }


                        /*
                         
                        // datos a enviar

                        public class TotabilizarProformaCompleta
                        {
                            public List<vedetalleanticipoProforma>? detalleAnticipos { get; set; }
                        }
                         
                         */

                        TotabilizarProformaCompleta objTotabiliza = new TotabilizarProformaCompleta
                        {
                            veproforma = cabecera,
                            veproforma1_2 = detalle_2,
                            veproforma_valida = valida_2,
                            veproforma_anticipo = anticipos_2,
                            vedesextraprof = descuentos,
                            verecargoprof = recargos_2,
                            veproforma_iva = iva,
                            detalleAnticipos = anticipos
                        };

                        dataTotales totabilizado = await TotalizarProf(_context, codempresa, usuario, userConnectionString, false, cabecera.niveles_descuento, codclienteReal, cabecera.tipo_complementopf ?? 0, objTotabiliza);

                        
                        // dataTotales totales = totabilizado.totales;
                        List<itemDataMatriz> detalleProf = totabilizado.tablaDetalle;
                        

                        cabecera.subtotal = totabilizado.subtotal;
                        cabecera.peso = totabilizado.peso;
                        cabecera.recargos = totabilizado.recargo;
                        cabecera.descuentos = totabilizado.descuento;
                        cabecera.iva = totabilizado.iva;
                        cabecera.total = totabilizado.total;

                        // una vez ya totalizado, mapear para guardar directamente D: (se debe ajustar lo recibido al totabilizar para que se guarde)

                        List<veproforma1> veproforma1_2 = totabilizado.tablaDetalle.Select(i => new veproforma1
                        {
                            codproforma = 0,
                            coditem = i.coditem,
                            cantidad = (decimal)i.cantidad,
                            udm = i.udm,
                            precioneto = (decimal)i.precioneto,
                            preciodesc = (decimal?)i.preciodesc,
                            niveldesc = i.niveldesc,
                            preciolista = (decimal)i.preciolista,
                            codtarifa = i.codtarifa,
                            coddescuento = (short)i.coddescuento,
                            total = (decimal)i.total,
                            cantaut = (decimal?)i.cantidad,
                            totalaut = (decimal?)i.total,
                            obs = "",
                            porceniva = (decimal?)i.porceniva,
                            cantidad_pedida = (decimal?)i.cantidad_pedida,
                            peso = 0,
                            nroitem = i.nroitem,
                            id = 0
                        }).ToList();

                        List<tabla_veproformaAnticipo> anticipos_3 = anticipos.Select(i=> new tabla_veproformaAnticipo
                        {
                            codproforma = 0,
                            codanticipo = i.codanticipo,
                            docanticipo = i.docanticipo,
                            id_anticipo = i.id_anticipo,
                            nroid_anticipo = i.nroid_anticipo,
                            monto = i.monto,
                            tdc = i.tdc,
                            codmoneda = i.codmoneda,
                            fechareg = i.fechareg,
                            usuarioreg = i.usuarioreg,
                            horareg = i.horareg,
                            codvendedor = i.codvendedor
                        }).ToList();

                        List<verecargoprof> recargos_3 = totabilizado.tablaRecargos.Select(i => new verecargoprof
                        {
                            codproforma = 0,
                            codrecargo = i.codrecargo,
                            porcen = i.porcen,
                            monto = i.monto,
                            moneda = i.moneda,
                            montodoc = i.montodoc,
                            codcobranza = i.codcobranza
                        }).ToList();

                        List<vedesextraprof>? descuentos_3 = totabilizado.tablaDescuentos.Select(i => new vedesextraprof
                        {
                            codproforma = 0,
                            coddesextra = i.coddesextra,
                            porcen = i.porcen,
                            montodoc = i.montodoc,
                            codcobranza = i.codcobranza,
                            codcobranza_contado = i.codcobranza_contado,
                            codanticipo = i.codanticipo,
                            id = 0,
                        }).ToList();

                        SaveProformaCompleta objParaGuardar = new SaveProformaCompleta
                        {
                            veproforma = cabecera,
                            veproforma1 = veproforma1_2,
                            veproforma_valida = valida_2,
                            dt_anticipo_pf = anticipos_3, 
                            vedesextraprof = descuentos_3,
                            verecargoprof = recargos_3,
                            veproforma_iva = totabilizado.tablaIva,
                            veetiqueta_proforma = etiquetaProf[0],

                        };

                        var resultadoGuardado = await guardarProforma(_context, cabecera.id, codempresa, false, codclienteReal, objParaGuardar);
                        
                        confirmaciones.Add(resultadoGuardado.mensaje);

                        pruebas_Prof newReg = new pruebas_Prof
                        {
                            idpf_original = reg.id,
                            nroidpd_original = reg.numeroid,
                            subtotal_original = reg.subtotal,
                            descuentos_original = reg.descuentos,
                            total_original = reg.total,

                            idpf_nueva = cabecera.id,
                            nroidpf_nueva = resultadoGuardado.numeroID,
                            subtotal_nueva = totabilizado.subtotal,
                            descuentos_nueva = totabilizado.descuento,
                            total_nueva = totabilizado.total,

                            fechareg = cabecera.fecha,
                        };
                        _context.pruebas_Prof.Add(newReg);
                        await _context.SaveChangesAsync();

                    }
                    return Ok(new
                    {
                        confirmaciones
                    });
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener empaques: " + ex.Message);
                throw;
            }
        }



        /// ///////////////////////////////////////////////////////////
        // FUNCIONES PARA TRANSFERIR PROFORMAS
        /// ///////////////////////////////////////////////////////////
        private async Task<dataProformas> transferirdoc(DBContext _context, string idProforma, int nroidProforma, string usuario)
        {
            var cabecera = await _context.veproforma
                        .Where(i => i.id == idProforma && i.numeroid == nroidProforma)
                        .FirstOrDefaultAsync();
            /*
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
            */

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
                .Select(i => new tabladescuentos
                {
                    codproforma = i.p.codproforma,
                    coddesextra = i.p.coddesextra,
                    // descripcion = i.e.descripcion,
                    porcen = i.p.porcen,
                    montodoc = i.p.montodoc,
                    codcobranza = i.p.codcobranza,
                    codcobranza_contado = i.p.codcobranza_contado,
                    codanticipo = i.p.codanticipo,
                    id = 0 //i.p.id
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

            return (new dataProformas
            {
                /*
                codclientedescripcion,
                tipo_cliente,
                descConfirmada,
                habilitado = cliHabilitado,
                anticiposTot,
                estadodoc,
                */
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

        /// ///////////////////////////////////////////////////////////
        // FUNCIONES PARA TOTABILIZAR LAS PROFORMAS CALCULOS
        /// ///////////////////////////////////////////////////////////

        private async Task<dataTotales> TotalizarProf(DBContext _context, string codempresa, string usuario, string userConnectionString, bool desclinea_segun_solicitud, string opcion_nivel, string codcliente_real, int cmbtipo_complementopf, TotabilizarProformaCompleta datosProforma)
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
                cantidad = i.cantidad ?? 0,
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



            var resultado = await calculoPreciosMatriz(_context, codempresa, usuario, userConnectionString, data, false, codempresa);


            // var totales = await RECALCULARPRECIOS(_context, false, codempresa, cmbtipo_complementopf, codcliente_real, resultado, verecargoprof, veproforma, vedesextraprof);
            dataTotales totales = await RECALCULARPRECIOS(_context, false, codempresa, cmbtipo_complementopf, codcliente_real, resultado, verecargoprof, veproforma, vedesextraprof, tabla_anticipos_asignados);
            /*
            return (new dataTotal_Tabla
            {
                totales = totales,
                detalleProf = resultado
            });
            */
            return totales;
        }


        private async Task<List<itemDataMatriz>> calculoPreciosMatriz(DBContext _context, string codEmpresa, string usuario, string userConnectionString, List<cargadofromMatriz> data, bool calcular_porcentaje, string codempresa)
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
                if (await ventas.Cumple_Empaque_De_DesctoEspecial(_context, reg.coditem, reg.tarifa, _descuento_precio, reg.cantidad, reg.codcliente, codempresa))
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
                var (preciodesc, exito1) = await cliente.Preciodesc(_context, reg.codcliente, reg.codalmacen, reg.tarifa, reg.coditem, reg.desc_linea_seg_solicitud, niveldesc, reg.opcion_nivel);

                if (exito1)
                {
                    preciodesc = await tipocambio.conversion(userConnectionString, reg.codmoneda, monedabase, reg.fecha, (decimal)preciodesc);
                    preciodesc = await cliente.Redondear_5_Decimales(_context, preciodesc);
                }
                else
                {
                    return null;
                }

                //precioneto 
                var (precioneto, exito2) = await cliente.Preciocondescitem(_context, reg.codcliente, reg.codalmacen, reg.tarifa, reg.coditem, reg.descuento, reg.desc_linea_seg_solicitud, niveldesc, reg.opcion_nivel);
                if (exito2)
                {
                    precioneto = await tipocambio.conversion(userConnectionString, reg.codmoneda, monedabase, reg.fecha, (decimal)precioneto);
                    precioneto = await cliente.Redondear_5_Decimales(_context, precioneto);
                }
                else
                {
                    return null;
                }

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

        private async Task<dataTotales> RECALCULARPRECIOS(DBContext _context, bool reaplicar_desc_deposito, string codempresa, int cmbtipo_complementopf, string codcliente_real, List<itemDataMatriz> tabla_detalle, List<tablarecargos> tablarecargos, veproforma veproforma, List<tabladescuentos> vedesextraprof, List<vedetalleanticipoProforma> tabla_anticipos_asignados)
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
            tabla_detalle = result.tabla_detalle;
            if (reaplicar_desc_deposito)
            {
                // Revisar_Aplicar_Descto_Deposito(preguntar_si_aplicare_desc_deposito);
            }

            var respRecargo = await verrecargos(_context, codempresa, veproforma.codmoneda, veproforma.fecha, subtotal, tablarecargos);
            double recargo = respRecargo.total;

            //var respDescuento = await verdesextra(_context, codempresa, veproforma.nit, veproforma.codmoneda, cmbtipo_complementopf, veproforma.idpf_complemento, veproforma.nroidpf_complemento ?? 0, subtotal, veproforma.fecha, tabladescuentos, tabla_detalle);
            var respDescuento = await verdesextra(_context, codempresa, veproforma.nit, veproforma.codmoneda, cmbtipo_complementopf, veproforma.idpf_complemento, veproforma.nroidpf_complemento ?? 0, subtotal, veproforma.fecha, tabladescuentos, tabla_detalle, veproforma.tipopago, (bool)veproforma.contra_entrega, codcliente_real, veproforma.codvendedor, tabla_anticipos_asignados);
            double descuento = respDescuento.respdescuentos;
            tabla_detalle = respDescuento.detalleProf;

            var resultados = await vertotal(_context, subtotal, recargo, descuento, codcliente_real, veproforma.codmoneda, codempresa, veproforma.fecha, tabla_detalle, tablarecargos);
            //QUITAR
            return new dataTotales
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
                tablaDetalle = tabla_detalle
                //mensaje = respDescuento.mensaje
            };

        }

        private async Task<(double st, double peso, List<itemDataMatriz> tabla_detalle)> versubtotal(DBContext _context, List<itemDataMatriz> tabla_detalle)
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
            return (st, peso, tabla_detalle);
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

        private async Task<(double respdescuentos, string mensaje, List<tabladescuentos> tabladescuentos, List<itemDataMatriz> detalleProf)> verdesextra(DBContext _context, string codempresa, string nit, string codmoneda, int cmbtipo_complementopf, string idpf_complemento, int nroidpf_complemento, double subtotal, DateTime fecha, List<tabladescuentos> tabladescuentos, List<itemDataMatriz> detalleProf, int tipopago, bool? contraEntrega, string codcliente_real, int codvendedor, List<vedetalleanticipoProforma> dt_anticipo_pf)
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
                var tablaAnticiposSinDeposito = await anticipos_vta_contado.Anticipos_MontoRestante_Sin_Deposito(_context, codcliente_real, codvendedor);
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

            return (respdescuentos, mensaje, tabladescuentos, detalleProf);

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

        /// ///////////////////////////////////////////////////////////
        // FUNCIONES PARA GUARDAR LA PROFORMA (SOLO GUARDAR)
        /// ///////////////////////////////////////////////////////////
        private async Task<(string mensaje, bool guardado, int numeroID)> guardarProforma(DBContext _context, string idProf, string codempresa, bool paraAprobar, string codcliente_real, SaveProformaCompleta datosProforma)
        {
            veproforma veproforma = datosProforma.veproforma;
            List<veproforma1> veproforma1 = datosProforma.veproforma1;
            bool check_desclinea_segun_solicitud = false;  // de momento no se utiliza, si se llegara a utilizar, se debe pedir por ruta

            // ###############################
            // ACTUALIZAR DATOS DE CODIGO PRINCIPAL SI ES APLICABLE
            await cliente.ActualizarParametrosDePrincipal(_context, veproforma.codcliente);
            // ###############################
            datosProforma.veproforma.paraaprobar = paraAprobar;

            if (veproforma1.Count() <= 0)
            {
                return ("No hay ningun item en su documento!!! en la proforma anterior: " + veproforma.id + "-" + veproforma.numeroid,false,0);
            }



            // ###############################  SE PUEDE LLAMAR DESDE FRONT END PARA LUEGO IR DIRECTO AL GRABADO ???????

            // RECALCULARPRECIOS(True, True);


            using (var dbContexTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (datosProforma.veproforma.idsoldesctos == null)
                    {
                        datosProforma.veproforma.idsoldesctos = "";
                    }
                    if (datosProforma.veproforma.estado_contra_entrega == null)
                    {
                        datosProforma.veproforma.estado_contra_entrega = "";
                    }
                    if (datosProforma.veproforma.contra_entrega == null)
                    {
                        datosProforma.veproforma.contra_entrega = false;
                    }
                    /*
                    if (datosProforma.veproforma.tipo_complementopf >= 0 && datosProforma.veproforma.tipo_complementopf <= 1)
                    {
                        datosProforma.veproforma.tipo_complementopf = datosProforma.veproforma.tipo_complementopf + 1;
                    }
                    */
                    if (datosProforma.veproforma.tipo_complementopf == null)
                    {
                        datosProforma.veproforma.tipo_complementopf = 0;
                    }
                    if (datosProforma.veproforma.tipo_complementopf >= 3)
                    {
                        datosProforma.veproforma.tipo_complementopf = 0;
                    }
                    if (datosProforma.veproforma.pago_contado_anticipado == null)
                    {
                        datosProforma.veproforma.pago_contado_anticipado = false;
                    }
                    if (datosProforma.veproforma.obs == null)
                    {
                        datosProforma.veproforma.obs = "---";
                    }
                    if (datosProforma.veproforma.obs2 == null)
                    {
                        datosProforma.veproforma.obs2 = "";
                    }
                    if (datosProforma.veproforma.odc == null)
                    {
                        datosProforma.veproforma.odc = "";
                    }
                    if (datosProforma.veproforma.porceniva == null)
                    {
                        datosProforma.veproforma.porceniva = 0;
                    }

                    datosProforma.veproforma.fechareg = DateTime.Today.Date;
                    datosProforma.veproforma.fechaaut = new DateTime(1900, 1, 1);     // PUEDE VARIAR SI ES PARA APROBAR

                    datosProforma.veproforma.horareg = DateTime.Now.ToString("HH:mm");
                    datosProforma.veproforma.horaaut = "00:00";                       // PUEDE VARIAR SI ES PARA APROBAR

                    if (veproforma.confirmada == true)
                    {
                        datosProforma.veproforma.fecha_confirmada = DateTime.Today.Date;
                        datosProforma.veproforma.hora_confirmada = DateTime.Now.ToString("HH:mm");
                    }
                    else
                    {
                        datosProforma.veproforma.fecha_confirmada = new DateTime(1900, 1, 1);
                        datosProforma.veproforma.hora_confirmada = "00:00";
                    }
                    /*
                    if (paraAprobar)
                    {
                        datosProforma.veproforma.fechaaut = DateTime.Today.Date;
                        datosProforma.veproforma.horaaut = DateTime.Now.ToString("HH:mm");
                    }*/

                    // ESTA VALIDACION ES MOMENTANEA, DESPUES SE DEBE COLOCAR SU PROPIA RUTA PARA VALIDAR, YA QUE PEDIRA CLAVE.
                    /*
                    var validacion_inicial = await Validar_Datos_Cabecera(_context, codempresa, codcliente_real, veproforma);

                    if (!validacion_inicial.bandera)
                    {
                        return BadRequest(new { resp = validacion_inicial.msg });
                    }
                    */

                    var result = await Grabar_Documento(_context, idProf, codempresa, datosProforma);
                    if (result.resp != "ok")
                    {
                        dbContexTransaction.Rollback();
                        return (result.resp + " En la proforma anterior: "+ veproforma.id + "-" + veproforma.numeroid, false, 0);
                    }
                    await log.RegistrarEvento(_context, veproforma.usuarioreg, Log.Entidades.SW_Proforma, result.codprof.ToString(), idProf, result.numeroId.ToString(), this._controllerName, "Grabar", Log.TipoLog.Creacion);

                    //Grabar Etiqueta
                    if (datosProforma.veetiqueta_proforma != null)
                    {
                        veetiqueta_proforma dt_etiqueta = datosProforma.veetiqueta_proforma;

                        if (dt_etiqueta.celular == null)
                        {
                            dt_etiqueta.celular = "---";
                        }
                        if (dt_etiqueta.codigo != 0)
                        {
                            dt_etiqueta.codigo = 0;
                        }
                        dt_etiqueta.numeroid = result.numeroId;
                        var etiqueta = await _context.veetiqueta_proforma.Where(i => i.id == dt_etiqueta.id && i.numeroid == dt_etiqueta.numeroid).FirstOrDefaultAsync();
                        if (etiqueta != null)
                        {
                            _context.veetiqueta_proforma.Remove(etiqueta);
                            await _context.SaveChangesAsync();
                        }
                        _context.veetiqueta_proforma.Add(dt_etiqueta);
                        await _context.SaveChangesAsync();
                    }



                    List<string> msgAlerts = new List<string>();
                    // devolver mensajes pero como alerta Extra
                    string msgAler1 = "";
                    // enlazar sol desctos con proforma
                    if (check_desclinea_segun_solicitud == true && veproforma.idsoldesctos.Trim().Length > 0 && veproforma.nroidsoldesctos > 0)
                    {
                        if (!await ventas.Enlazar_Proforma_Nueva_Con_SolDesctos_Nivel(_context, result.codprof, veproforma.idsoldesctos, veproforma.nroidsoldesctos ?? 0))
                        {
                            msgAler1 = "Se grabo la Proforma, pero No se pudo realizar el enlace de esta proforma con la solicitud de descuentos de nivel, verifique el enlace en la solicitu de descuentos!!!";
                            msgAlerts.Add(msgAler1);
                        }
                    }

                    // grabar la etiqueta dsd 16-05-2022        
                    // solo si es cliente casual, y el cliente referencia o real es un no casual
                    //If sia_funciones.Cliente.Instancia.Es_Cliente_Casual(codcliente.Text) = True And sia_funciones.Cliente.Instancia.Es_Cliente_Casual(codcliente_real) = False Then

                    // Desde 10-10-2022 se definira si una venta es casual o no si el codigo de cliente y el codigo de cliente real son diferentes entonces es una venta casual
                    string msgAlert2 = "";
                    if (veproforma.codcliente != codcliente_real)
                    {
                        if (!await Grabar_Proforma_Etiqueta(_context, idProf, result.numeroId, check_desclinea_segun_solicitud, codcliente_real, veproforma))
                        {
                            msgAlert2 = "Se grabo la Proforma, pero No se pudo grabar la etiqueta Cliente Casual/Referencia de la proforma!!!";
                            msgAlerts.Add(msgAlert2);
                        }
                    }

                    if (paraAprobar)
                    {


                        // *****************O J O *************************************************************************************************************
                        // IMPLEMENTADO EN FECHA 26-04-2018 LLAMA A LA FUNNCION QUE VALIDA LO QUE SE VALIDA DESDE LA VENTANA DE APROBACION DE PROFORMAS
                        // *****************O J O *************************************************************************************************************
                        /*

                        string mensajeAprobacion = "";
                        var resultValApro = await Validar_Aprobar_Proforma(_context, veproforma.id, result.numeroId, result.codprof, codempresa, datosProforma.tabladescuentos, datosProforma.DVTA, datosProforma.tablarecargos);

                        msgAlerts.AddRange(resultValApro.msgsAlert);


                        if (resultValApro.resp)
                        {
                            // verifica antes si la proforma esta grabar para aprobar
                            if (await ventas.proforma_para_aprobar(_context, result.codprof))
                            {
                                // **aprobar la proforma
                                var profforAprobar = await _context.veproforma.Where(i => i.codigo == result.codprof).FirstOrDefaultAsync();
                                profforAprobar.aprobada = true;
                                profforAprobar.fechaaut = DateTime.Today.Date;
                                profforAprobar.horaaut = datos_proforma.getHoraActual();
                                profforAprobar.usuarioaut = veproforma.usuarioreg;
                                _context.Entry(profforAprobar).State = EntityState.Modified;
                                await _context.SaveChangesAsync();

                                // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
                                // realizar la reserva de la mercaderia
                                // Desde 15/11/2023 registrar en el log si por alguna razon no actualiza en instoactual correctamente al disminuir el saldo de cantidad y la reserva en proforma
                                if (await ventas.aplicarstocksproforma(_context, result.codprof, codempresa) == false)
                                {
                                    await log.RegistrarEvento(_context, veproforma.usuarioreg, Log.Entidades.SW_Proforma, result.codprof.ToString(), veproforma.id, result.numeroId.ToString(), this._controllerName, "No actualizo stock al sumar cantidad de reserva en PF.", Log.TipoLog.Creacion);
                                }
                                // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

                                mensajeAprobacion = "La proforma fue grabada para aprobar y tambien aprobada.";
                                // Desde 23/11/2023 guardar el log de grabado aqui
                                await log.RegistrarEvento(_context, veproforma.usuarioreg, Log.Entidades.SW_Proforma, result.codprof.ToString(), veproforma.id, result.numeroId.ToString(), this._controllerName, "Grabar Para Aprobar", Log.TipoLog.Creacion);

                            }
                            else
                            {
                                mensajeAprobacion = "La proforma no se grabo para aprobar por lo cual no se puede aprobar.";
                            }
                        }
                        else
                        {
                            mensajeAprobacion = "La proforma solo se grabo para aprobar, pero no se pudo aprobar porque no cumple con las condiciones de aprobacion!!! Revise la proforma en la ventana de modificacion de proformas.";
                            var desaprobarProforma = await _context.veproforma.Where(i => i.codigo == result.codprof).FirstOrDefaultAsync();
                            desaprobarProforma.aprobada = false;
                            desaprobarProforma.fechaaut = new DateTime(1900, 1, 1);
                            desaprobarProforma.horaaut = "00:00";
                            desaprobarProforma.usuarioaut = "";
                            _context.Entry(desaprobarProforma).State = EntityState.Modified;
                            await _context.SaveChangesAsync();
                        }
                        msgAlerts.Add(mensajeAprobacion);
                        */
                    }


                    dbContexTransaction.Commit();
                    return ("Se Grabo la Proforma de manera Exitosa " + idProf + "-" + result.numeroId, true, result.numeroId);
                }
                catch (Exception ex)
                {
                    dbContexTransaction.Rollback();
                    return ($"Error en el servidor al guardar Proforma: {ex.Message} " + veproforma.id + "-" + veproforma.numeroid, false, 0);
                    throw;
                }
            }
        }



        private async Task<(bool resp, List<string> msgsAlert)> Validar_Aprobar_Proforma(DBContext _context, string id_pf, int nroid_pf, int cod_proforma, string codempresa, List<vedesextraDatos> tabladescuentos, DatosDocVta DVTA, List<verecargosDatos> tablarecargos)
        {
            bool resultado = true;
            List<string> msgsAlert = new List<string>();
            var dt_pf = await _context.veproforma.Where(i => i.codigo == cod_proforma).FirstOrDefaultAsync();

            ////////////////////////////////////////////////////////////////////////////////
            // validar el monto de desctos por deposito aplicado
            var respdepCoCred = await depositos_cliente.Validar_Desctos_x_Deposito_Otorgados_De_Cobranzas_Credito(_context, id_pf, nroid_pf, codempresa);
            if (!respdepCoCred.result)
            {
                resultado = false;
                if (respdepCoCred.msgAlert != "")
                {
                    msgsAlert.Add(respdepCoCred.msgAlert);
                }
            }

            var respdepCoCont = await depositos_cliente.Validar_Desctos_x_Deposito_Otorgados_De_Cbzas_Contado_CE(_context, id_pf, nroid_pf, codempresa);
            if (!respdepCoCont.result)
            {
                resultado = false;
                if (respdepCoCont.msgAlert != "")
                {
                    msgsAlert.Add(respdepCoCont.msgAlert);
                }
            }

            var respdepAntProfCont = await depositos_cliente.Validar_Desctos_x_Deposito_Otorgados_De_Anticipos_Que_Pagaron_Proformas_Contado(_context, id_pf, nroid_pf, codempresa);
            if (!respdepAntProfCont.result)
            {
                resultado = false;
                if (respdepAntProfCont.msgAlert != "")
                {
                    msgsAlert.Add(respdepAntProfCont.msgAlert);
                }
            }

            if (resultado == false)
            {
                msgsAlert.Add("No se puede aprobar la proforma, porque tiene descuentos por deposito en montos no validos!!!");
                return (false, msgsAlert);
            }
            ////////////////////////////////////////////////////////////////////////////////


            //======================================================================================
            /////////////////VALIDAR DESCTOS POR DEPOSITO APLICADOS
            //======================================================================================

            var validDescDepApli = await Validar_Descuentos_Por_Deposito_Excedente(_context, codempresa, tabladescuentos, DVTA);
            if (!validDescDepApli.result)
            {
                resultado = false;
                msgsAlert.Add(validDescDepApli.msgAlert);
                msgsAlert.Add("La proforma no puede ser aprobada, porque tiene descuentos por deposito en montos no validos!!!");
                return (false, msgsAlert);
            }
            //======================================================================================
            ///////////////VALIDAR RECARGOS POR DEPOSITO APLICADOS
            //======================================================================================
            var validRecargoDepExcedente = await Validar_Recargos_Por_Deposito_Excedente(_context, codempresa, tablarecargos, DVTA);
            if (!validRecargoDepExcedente.result)
            {
                resultado = false;
                msgsAlert.Add(validDescDepApli.msgAlert);
                msgsAlert.Add("La proforma no puede ser aprobada, porque tiene recargos por descuentos por deposito excedentes en montos no validos!!!");
                return (false, msgsAlert);
            }

            //////////////////////////////////////////////

            // mostrar mensaje de credito disponible
            /*
            
            If IsDBNull(reg("contra_entrega")) Then
                qry = "update veproforma set contra_entrega=0 where id='" & id_pf & "' and numeroid='" & nroid_pf & "'"
                sia_DAL.Datos.Instancia.EjecutarComando(qry)
            End If

             */

            // tipo pago CONTADO
            if (DVTA.tipo_vta == "CONTADO")
            {
                ////////////////////////////////////////////////////////////////////////////////////////////
                // se añadio en fecha 15-3-2016
                // es venta al contado y no necesita validar el credito
                // TODA VENTA AL CONTADO DEBE TENER ASIGNADO ID-NROID DE ANTICIPO SI ES PAGO ANTICIPADO
                // SI SE HABILITO LA OPCION DE PAGO ANTICIPADO
                ////////////////////////////////////////////////////////////////////////////////////////////
                var dt_anticipos = await anticipos_vta_contado.Anticipos_Aplicados_a_Proforma(_context, id_pf, nroid_pf);
                if (dt_anticipos.Count > 0)
                {
                    ResultadoValidacion objres = new ResultadoValidacion();
                    objres = await anticipos_vta_contado.Validar_Anticipo_Asignado_2(_context, true, DVTA, dt_anticipos, codempresa);
                    if (objres.resultado)
                    {
                        // Desde 15/01/2024 se cambio esta funcion porque no estaba validando correctamente la transformacion de moneda de los anticipos a aplicarse ya se en $us o BS
                        // If sia_funciones.Anticipos_Vta_Contado.Instancia.Validar_Anticipo_Asignado(True, dt_anticipos, reg("codcliente"), reg("nomcliente"), reg("total")) = True Then
                        goto finalizar_ok;
                    }
                    else
                    {
                        if (dt_anticipos != null)
                        {
                            return (false, msgsAlert);
                        }
                    }
                }
                goto finalizar_ok;

            }

        finalizar_ok:
            return (true, msgsAlert);

        }


        private async Task<(bool result, string msgAlert)> Validar_Descuentos_Por_Deposito_Excedente(DBContext _context, string codempresa, List<vedesextraDatos> tabladescuentos, DatosDocVta DVTA)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            bool resultado = true;
            string msgAlert = "";

            objres = await validar_Vta.Validar_Descuento_Por_Deposito(_context, DVTA, tabladescuentos, codempresa);

            if (objres.resultado == false)
            {
                msgAlert = objres.observacion + " " + objres.obsdetalle + "Alerta Descuentos Por Deposito!!!";
                resultado = false;
            }
            return (resultado, msgAlert);
        }
        private async Task<(bool result, string msgAlert)> Validar_Recargos_Por_Deposito_Excedente(DBContext _context, string codempresa, List<verecargosDatos> tablarecargos, DatosDocVta DVTA)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            bool resultado = true;
            string msgAlert = "";

            objres = await validar_Vta.Validar_Recargo_Aplicado_Por_Desc_Deposito_Excedente(_context, DVTA, tablarecargos, codempresa);

            if (objres.resultado == false)
            {
                msgAlert = objres.observacion + " " + objres.obsdetalle + "Alerta!!!";
                resultado = false;
            }

            return (resultado, msgAlert);
        }


        private async Task<bool> Grabar_Proforma_Etiqueta(DBContext _context, string idProf, int nroidpf, bool desclinea_segun_solicitud, string codcliente_real, veproforma dtpf)
        {
            try
            {
                veproforma_etiqueta datospfe = new veproforma_etiqueta();
                // obtener datos de la etiqueta
                var dt_etiqueta = await _context.veetiqueta_proforma.Where(i => i.id == idProf && i.numeroid == nroidpf).FirstOrDefaultAsync();
                // obtener datos de proforma
                /*
                var dtpf = await _context.veproforma.Where(i => i.id == idProf && i.numeroid == nroidpf)
                    .Select(i => new
                    {
                        i.codigo,
                        i.id,
                        i.numeroid,
                        i.fecha,
                        i.codcliente,
                        i.direccion,
                        i.latitud_entrega,
                        i.longitud_entrega,
                        i.codalmacen
                    })
                    .FirstOrDefaultAsync();
                */
                datospfe.id_proforma = idProf;
                datospfe.nroid_proforma = nroidpf;
                datospfe.codalmacen = dtpf.codalmacen;
                datospfe.codcliente_casual = dtpf.codcliente;
                if (desclinea_segun_solicitud == true && dtpf.idsoldesctos.Trim().Length > 0 && (dtpf.nroidsoldesctos > 0 || dtpf.nroidsoldesctos != null))
                {
                    datospfe.codcliente_real = await ventas.Cliente_Referencia_Solicitud_Descuentos(_context, dtpf.idsoldesctos, dtpf.nroidsoldesctos ?? 0);
                }
                else
                {
                    datospfe.codcliente_real = codcliente_real;
                }
                datospfe.fecha = dtpf.fecha;
                datospfe.direccion = dtpf.direccion;
                if (dt_etiqueta != null)
                {
                    datospfe.ciudad = dt_etiqueta.ciudad;
                }
                else
                {
                    datospfe.ciudad = "";
                }

                datospfe.latitud_entrega = dtpf.latitud_entrega;
                datospfe.longitud_entrega = dtpf.longitud_entrega;
                datospfe.horareg = dtpf.horareg;
                datospfe.fechareg = dtpf.fechareg;
                datospfe.usuarioreg = dtpf.usuarioreg;

                // insertar proforma_etiqueta (datospfe)
                var profEtiqueta = await _context.veproforma_etiqueta.Where(i => i.id_proforma == datospfe.id_proforma && i.nroid_proforma == datospfe.nroid_proforma).FirstOrDefaultAsync();
                if (profEtiqueta != null)
                {
                    _context.veproforma_etiqueta.Remove(profEtiqueta);
                    await _context.SaveChangesAsync();
                }
                _context.veproforma_etiqueta.Add(datospfe);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<(string resp, int codprof, int numeroId)> Grabar_Documento(DBContext _context, string idProf, string codempresa, SaveProformaCompleta datosProforma)
        {
            veproforma veproforma = datosProforma.veproforma;
            List<veproforma1> veproforma1 = datosProforma.veproforma1;
            var veproforma_valida = datosProforma.veproforma_valida;
            var dt_anticipo_pf = datosProforma.dt_anticipo_pf;
            var vedesextraprof = datosProforma.vedesextraprof;
            var verecargoprof = datosProforma.verecargoprof;
            var veproforma_iva = datosProforma.veproforma_iva;

            ////////////////////   GRABAR DOCUMENTO

            int _ag = await empresa.AlmacenLocalEmpresa(_context, codempresa);
            // verificar si valido el documento, si es tienda no es necesario que valide primero
            if (!await almacen.Es_Tienda(_context, _ag))
            {
                if (veproforma_valida.Count() < 1 || veproforma_valida == null)
                {
                    return ("Antes de grabar el documento debe previamente validar el mismo!!!", 0, 0);
                }
            }


            //************************************************

            //obtenemos numero actual de proforma de nuevo
            int idnroactual = await datos_proforma.getNumActProd(_context, idProf);

            if (idnroactual == 0)
            {
                return ("Error al obtener los datos de numero de proforma", 0, 0);
            }

            // valida si existe ya la proforma
            if (await datos_proforma.existeProforma(_context, idProf, idnroactual))
            {
                return ("Ese numero de documento, ya existe, por favor consulte con el administrador del sistema.", 0, 0);
            }
            veproforma.numeroid = idnroactual;

            //fin de obtener id actual

            // obtener hora y fecha actual si es que la proforma no se importo
            if (veproforma.hora_inicial == "")
            {
                veproforma.fecha_inicial = DateTime.Parse(datos_proforma.getFechaActual());
                veproforma.hora_inicial = datos_proforma.getHoraActual();
            }


            // accion de guardar

            // guarda cabecera (veproforma)
            _context.veproforma.Add(veproforma);
            await _context.SaveChangesAsync();

            var codProforma = veproforma.codigo;

            // actualiza numero id
            var numeracion = _context.venumeracion.FirstOrDefault(n => n.id == idProf);
            numeracion.nroactual += 1;
            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos


            int validaCantProf = await _context.veproforma.Where(i => i.id == veproforma.id && i.numeroid == veproforma.numeroid).CountAsync();
            if (validaCantProf > 1)
            {
                return ("Se detecto más de un número del mismo documento, por favor consulte con el administrador del sistema.", 0, 0);
            }


            // guarda detalle (veproforma1)
            // actualizar codigoproforma para agregar
            veproforma1 = veproforma1.Select(p => { p.codproforma = codProforma; return p; }).ToList();
            // colocar obs como vacio no nulo
            veproforma1 = veproforma1.Select(o => { o.obs = ""; return o; }).ToList();
            // actualizar peso del detalle.
            veproforma1 = await ventas.Actualizar_Peso_Detalle_Proforma(_context, veproforma1);

            _context.veproforma1.AddRange(veproforma1);
            await _context.SaveChangesAsync();





            //======================================================================================
            // grabar detalle de validacion
            //======================================================================================

            veproforma_valida = veproforma_valida.Select(p => { p.codproforma = codProforma; return p; }).ToList();
            _context.veproforma_valida.AddRange(veproforma_valida);
            await _context.SaveChangesAsync();

            //======================================================================================
            //grabar anticipos aplicados
            //======================================================================================
            try
            {
                var anticiposprevios = await _context.veproforma_anticipo.Where(i => i.codproforma == codProforma).ToListAsync();
                if (anticiposprevios.Count() > 0)
                {
                    _context.veproforma_anticipo.RemoveRange(anticiposprevios);
                    await _context.SaveChangesAsync();
                }
                if (dt_anticipo_pf != null)
                {
                    if (dt_anticipo_pf.Count() > 0)
                    {

                        var newData = dt_anticipo_pf
                            .Select(i => new veproforma_anticipo
                            {
                                codproforma = codProforma,
                                codanticipo = i.codanticipo,
                                monto = (decimal?)i.monto,
                                tdc = (decimal?)i.tdc,

                                fechareg = DateTime.Parse(datos_proforma.getFechaActual()),
                                usuarioreg = veproforma.usuarioreg,
                                horareg = datos_proforma.getHoraActual()
                            }).ToList();
                        _context.veproforma_anticipo.AddRange(newData);
                        await _context.SaveChangesAsync();

                    }
                }

            }
            catch (Exception)
            {

                throw;
            }

            //======================================================================================
            //grabar diferencias de anticipos aplicados
            //======================================================================================
            try
            {
                var diferencias_previos = await _context.veproforma_anticipo_diferencias.Where(i => i.codproforma == codProforma).ToListAsync();
                if (diferencias_previos.Count() > 0)
                {
                    _context.veproforma_anticipo_diferencias.RemoveRange(diferencias_previos);
                    await _context.SaveChangesAsync();
                }
                //obtener si hay diferencia enntre el total de aplicado de anticipo contra el total de la proforma
                decimal ttl_anticipos_aplicados = 0;
                decimal ttl_pf = 0;
                decimal diferencia_ant_pf = 0;
                bool anticipo_mayor = true;

                if (dt_anticipo_pf != null)
                {
                    if (dt_anticipo_pf.Count() > 0)
                    {
                        foreach (var ant in dt_anticipo_pf)
                        {
                            ttl_anticipos_aplicados += Math.Round(Convert.ToDecimal(ant.monto), 2);
                        }
                        ttl_pf = Math.Round(veproforma.total, 2);
                        diferencia_ant_pf = Math.Round(ttl_anticipos_aplicados - ttl_pf, 2);
                        if (ttl_anticipos_aplicados != ttl_pf)
                        {
                            anticipo_mayor = ttl_anticipos_aplicados > ttl_pf;

                            var newData = new veproforma_anticipo_diferencias
                            {
                                codproforma = codProforma,
                                monto = diferencia_ant_pf,
                                tdc = 1,
                                fechareg = DateTime.Parse(datos_proforma.getFechaActual()),
                                usuarioreg = veproforma.usuarioreg,
                                horareg = datos_proforma.getHoraActual(),
                                anticipo_aplicado_mayor = anticipo_mayor
                            };
                            await _context.veproforma_anticipo_diferencias.AddAsync(newData);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

            }
            catch (Exception)
            {

                throw;
            }
            // grabar descto por deposito si hay descuentos
            if (vedesextraprof != null)
            {
                if (vedesextraprof.Count() > 0)
                {
                    await grabardesextra(_context, codProforma, vedesextraprof);
                }
            }

            if (verecargoprof != null)
            {
                // grabar recargo si hay recargos
                if (verecargoprof.Count > 0)
                {
                    await grabarrecargo(_context, codProforma, verecargoprof);
                }
            }

            if (veproforma_iva != null)
            {
                // grabar iva
                if (veproforma_iva.Count > 0)
                {
                    await grabariva(_context, codProforma, veproforma_iva);
                }
            }


            bool resultado = new bool();
            // grabar descto por deposito
            if (await ventas.Grabar_Descuento_Por_deposito_Pendiente(_context, codProforma, codempresa, veproforma.usuarioreg, vedesextraprof))
            {
                resultado = true;
            }
            else
            {
                resultado = false;
            }

            // ======================================================================================
            // actualizar saldo restante de anticipos aplicados
            // ======================================================================================
            if (resultado)
            {
                if (dt_anticipo_pf != null)
                {
                    foreach (var reg in dt_anticipo_pf)
                    {
                        if (!await anticipos_vta_contado.ActualizarMontoRestAnticipo(_context, reg.id_anticipo, reg.nroid_anticipo, reg.codproforma ?? 0, reg.codanticipo ?? 0, 0, codempresa))
                        //    if (!await anticipos_vta_contado.ActualizarMontoRestAnticipo(_context, reg.id_anticipo, reg.nroid_anticipo, reg.codproforma ?? 0, reg.codanticipo ?? 0, reg.monto, codempresa))
                        {
                            resultado = false;
                        }
                    }
                }
            }

            return ("ok", codProforma, veproforma.numeroid);


        }


        private async Task grabardesextra(DBContext _context, int codProf, List<vedesextraprof> vedesextraprof)
        {
            var descExtraAnt = await _context.vedesextraprof.Where(i => i.codproforma == codProf).ToListAsync();
            if (descExtraAnt.Count() > 0)
            {
                _context.vedesextraprof.RemoveRange(descExtraAnt);
                await _context.SaveChangesAsync();
            }
            vedesextraprof = vedesextraprof.Select(p => { p.codproforma = codProf; return p; }).ToList();
            _context.vedesextraprof.AddRange(vedesextraprof);
            await _context.SaveChangesAsync();
        }


        private async Task grabarrecargo(DBContext _context, int codProf, List<verecargoprof> verecargoprof)
        {
            var recargosAnt = await _context.verecargoprof.Where(i => i.codproforma == codProf).ToListAsync();
            if (recargosAnt.Count() > 0)
            {
                _context.verecargoprof.RemoveRange(recargosAnt);
                await _context.SaveChangesAsync();
            }
            verecargoprof = verecargoprof.Select(p => { p.codproforma = codProf; return p; }).ToList();
            _context.verecargoprof.AddRange(verecargoprof);
            await _context.SaveChangesAsync();
        }

        private async Task grabariva(DBContext _context, int codProf, List<veproforma_iva> veproforma_iva)
        {
            var ivaAnt = await _context.veproforma_iva.Where(i => i.codproforma == codProf).ToListAsync();
            if (ivaAnt.Count() > 0)
            {
                _context.veproforma_iva.RemoveRange(ivaAnt);
                await _context.SaveChangesAsync();
            }
            veproforma_iva = veproforma_iva.Select(p => { p.codproforma = codProf; return p; }).ToList();
            _context.veproforma_iva.AddRange(veproforma_iva);
            await _context.SaveChangesAsync();
        }


    }


    public class dataProformas
    {
        public veproforma cabecera { get; set; }
        public List<itemDataMatriz> detalle { get; set; }
        public List<tabladescuentos> descuentos { get; set; }
        public List<verecargoprof> recargos { get; set; }
        public List<veproforma_iva> iva { get; set; }
        public List<veproforma_etiqueta> profEtiqueta { get; set; }
        public veproforma_etiqueta? veprofEtiqueta { get; set; }
        public List<veetiqueta_proforma> etiquetaProf { get; set; }
        public List<vedetalleanticipoProforma> anticipos { get; set; }
        public List<DataValidar> detalleValida { get; set; }

    }

    public class dataTotales
    {
        public decimal subtotal { get; set; }
        public decimal peso { get; set; }
        public decimal recargo { get; set; }
        public decimal descuento { get; set; }
        public decimal iva { get; set; }
        public decimal total { get; set; }

        public List<veproforma_iva> tablaIva { get; set; }
        public List<tablarecargos> tablaRecargos { get; set; }
        public List<tabladescuentos> tablaDescuentos { get; set; }
        public List<itemDataMatriz> tablaDetalle { get; set; }

    }
    public class dataTotal_Tabla
    {
        public dataTotales totales { get; set; }
        public List<itemDataMatriz> detalleProf { get; set; }
    }

}