using LibSIAVB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Polly.Retry;
using SIAW.Controllers.ventas.modificacion;
using SIAW.Controllers.ventas.transaccion;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;
using System.Data;
using System.Web.Http.Results;
using static siaw_funciones.Validar_Vta;
using static System.Net.Mime.MediaTypeNames;


namespace SIAW.Controllers.z_pruebas
{
    [Route("api/pruebas/[controller]")]
    [ApiController]
    public class generadorNRemiController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly siaw_funciones.Ventas ventas = new siaw_funciones.Ventas();
        private readonly siaw_funciones.Cliente cliente = new siaw_funciones.Cliente();
        private readonly Anticipos_Vta_Contado anticipos_vta_contado = new Anticipos_Vta_Contado();
        private readonly siaw_funciones.Nombres nombres = new siaw_funciones.Nombres();
        private readonly siaw_funciones.TipoCambio tipocambio = new siaw_funciones.TipoCambio();
        private readonly siaw_funciones.Configuracion configuracion = new siaw_funciones.Configuracion();
        private readonly siaw_funciones.Saldos saldos = new siaw_funciones.Saldos();
        private readonly siaw_funciones.Creditos creditos = new siaw_funciones.Creditos();
        private readonly siaw_funciones.Empresa empresa = new siaw_funciones.Empresa();
        private readonly siaw_funciones.Almacen almacen = new siaw_funciones.Almacen();
        private readonly siaw_funciones.Funciones funciones = new Funciones();
        private readonly siaw_funciones.Cobranzas cobranzas = new siaw_funciones.Cobranzas();
        private readonly siaw_funciones.datosProforma datos_proforma = new siaw_funciones.datosProforma();
        private readonly siaw_funciones.Items items = new siaw_funciones.Items();
        private readonly siaw_funciones.Validar_Vta validar_Vta = new siaw_funciones.Validar_Vta();
        private readonly siaw_funciones.Seguridad seguridad = new siaw_funciones.Seguridad();
        private readonly siaw_funciones.SIAT siat = new siaw_funciones.SIAT();

        private readonly Documento documento = new Documento();
        private readonly Depositos_Cliente depositos_Cliente = new Depositos_Cliente();
        private readonly Inventario inventario = new Inventario();
        private readonly Restricciones restricciones = new Restricciones();
        private readonly HardCoded hardCoded = new HardCoded();

        private readonly Log log = new Log();
        private readonly string _controllerName = "veremisionController";

        // Definir la política de reintento como una propiedad global
        private readonly AsyncRetryPolicy _retryPolicy;

        public generadorNRemiController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
            // Inicializar el nombre del controlador en el constructor
        }
        [HttpPost]
        [Route("generarNotasRemi/{userConn}/{usuario}/{codempresa}/{fechaInicio}/{fechaFin}")]
        public async Task<ActionResult<List<sldosItemCompleto>>> generarNotasRemi(string userConn, string usuario, string codempresa, DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    List<string> confirmaciones = new List<string>();
                    var drNRemisiones = await _context.veremision.Where(i => i.fecha >= fechaInicio && i.fecha <= fechaFin && i.anulada == false && !i.codcliente.StartsWith("SN") && (i.id.StartsWith("NR")))
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
                            i.total,
                            i.codproforma,
                        }).ToListAsync();

                    foreach (var reg in drNRemisiones)
                    {
                        SaveNRemisionCompleta transferencia = await transferirdoc(_context, reg.id, reg.numeroid);

                        // para totalizar 
                        TotabilizarProformaCompleta datosPtot = new TotabilizarProformaCompleta();
                        datosPtot.veproforma = await _context.veproforma.Where(i => i.codigo == reg.codproforma).FirstOrDefaultAsync();

                        List<veproforma1_2> detalle_2 = new List<veproforma1_2>();
                        detalle_2 = transferencia.veremision1.Select(i => new veproforma1_2
                        {
                            codproforma = 0,
                            coditem = i.coditem,
                            empaque = 0,
                            cantidad = i.cantidad,
                            cantidad_pedida = i.cantidad,
                            udm = i.udm,
                            precioneto = i.precioneto,
                            preciodesc = i.preciodesc,
                            niveldesc = i.niveldesc,
                            preciolista = i.preciolista,
                            codtarifa = i.codtarifa,
                            coddescuento = i.coddescuento,
                            total = i.total,
                            cantaut = i.cantidad,
                            totalaut = i.total,
                            obs = "",
                            porceniva = i.porceniva,
                            peso = i.peso,
                            nroitem = 0,
                            id = 0,
                            porcen_mercaderia = 0
                        }).ToList();

                        datosPtot.veproforma1_2 = detalle_2;
                        datosPtot.veproforma_valida = await _context.veproforma_valida.Where(i => i.codproforma == reg.codproforma).ToListAsync();
                        datosPtot.veproforma_anticipo = await _context.veproforma_anticipo.Where(i => i.codproforma == reg.codproforma).ToListAsync();

                        List<tabladescuentos> descuentos_2 = new List<tabladescuentos>();
                        descuentos_2 = transferencia.vedesextraremi.Select(i => new tabladescuentos
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

                        datosPtot.vedesextraprof = descuentos_2;

                        List<tablarecargos> recargos_2 = new List<tablarecargos>();
                        recargos_2 = transferencia.verecargoremi.Select(i => new tablarecargos
                        {
                            codproforma = 0,
                            codrecargo = i.codrecargo,
                            porcen = i.porcen,
                            monto = i.monto ?? 0,
                            moneda = i.moneda,
                            montodoc = i.montodoc,
                            codcobranza= i.codcobranza,
                            descripcion = ""
                        }).ToList();

                        datosPtot.verecargoprof = recargos_2;

                        List<veproforma_iva> iva_2 = new List<veproforma_iva>();
                        iva_2 = transferencia.veremision_iva.Select(i => new veproforma_iva
                        {
                            codproforma = 0,
                            porceniva = i.porceniva ?? 0,
                            total = i.total,
                            porcenbr = i.porcenbr,
                            br = i.br,
                            iva = i.iva,
                        }).ToList();

                        datosPtot.veproforma_iva = iva_2;

                        List<vedetalleanticipoProforma> detalleAnticipos_2 = await _context.veproforma_anticipo
                        .Where(va => va.codproforma == reg.codproforma)
                        .Join(
                            _context.coanticipo,
                            va => va.codanticipo,    // Llave foránea en veproforma_anticipo
                            co => co.codigo,         // Llave primaria en coanticipo
                            (va, co) => new vedetalleanticipoProforma
                            {
                                codproforma = datosPtot.veproforma.codigo,
                                codanticipo = va.codigo,
                                docanticipo = co.id + "-" + co.numeroid,
                                id_anticipo = co.id,
                                nroid_anticipo = co.numeroid,
                                monto = (double)(va.monto ?? 0),
                                tdc = (double)(va.tdc ?? 0),
                                codmoneda = datosPtot.veproforma.codmoneda,
                                fechareg = (DateTime)va.fechareg,
                                usuarioreg = va.usuarioreg,
                                horareg = va.horareg,
                                codvendedor = datosPtot.veproforma.codvendedor.ToString()

                            }
                        )
                        .ToListAsync();

                        datosPtot.detalleAnticipos = detalleAnticipos_2;
                        string _codcliente_real = "";
                        _codcliente_real = await ventas.Cliente_Referencia_Proforma_Etiqueta(_context, datosPtot.veproforma.id, datosPtot.veproforma.numeroid);
                        if (_codcliente_real == "")
                        {
                            _codcliente_real = datosPtot.veproforma.codcliente_real;
                        }

                        dataTotales resultadosTotabilizar = await TotalizarNR(_context, codempresa, usuario, userConnectionString, false, datosPtot.veproforma.niveles_descuento,_codcliente_real, datosPtot.veproforma.tipo_complementopf ?? 0, datosPtot);


                        // objetos para el grabado
                        transferencia.veremision.subtotal = resultadosTotabilizar.subtotal;
                        transferencia.veremision.peso = resultadosTotabilizar.peso;
                        transferencia.veremision.recargos = resultadosTotabilizar.recargo;
                        transferencia.veremision.descuentos = resultadosTotabilizar.descuento;
                        transferencia.veremision.iva = resultadosTotabilizar.iva;
                        transferencia.veremision.total = resultadosTotabilizar.total;

                        List<veremision1> detalle_3 = new List<veremision1>();
                        detalle_3 = resultadosTotabilizar.tablaDetalle.Select(i => new veremision1
                        {
                            codremision = 0,
                            coditem = i.coditem,
                            cantidad = (decimal)i.cantidad,
                            udm = i.udm,
                            precioneto = (decimal)i.precioneto,
                            preciolista = (decimal)i.preciolista,
                            niveldesc = i.niveldesc,
                            preciodesc = (decimal?)i.preciodesc,
                            codtarifa = i.codtarifa,
                            coddescuento = (short)i.coddescuento,
                            total = (decimal)i.total,
                            porceniva = (decimal?)i.porceniva,
                            codgrupomer = 0,
                            peso = 0,
                            codigo = 0
                        }).ToList();


                        List<vedesextraremi> descuentos_3 = new List<vedesextraremi>();
                        descuentos_3 = resultadosTotabilizar.tablaDescuentos.Select(i => new vedesextraremi
                        {
                            codremision = 0,
                            coddesextra = i.coddesextra,
                            porcen = i.porcen,
                            montodoc = i.montodoc,
                            codcobranza = i.codcobranza,
                            codcobranza_contado = i.codcobranza_contado,
                            codanticipo = i.codanticipo,
                            codigo = 0,
                        }).ToList();

                        List<verecargoremi> recargos_3 = new List<verecargoremi>();
                        recargos_3 = resultadosTotabilizar.tablaRecargos.Select(i => new verecargoremi
                        {
                            codremision = 0,
                            codrecargo = i.codrecargo,
                            porcen = i.porcen,
                            monto = i.monto,
                            moneda = i.moneda,
                            montodoc = i.montodoc,
                            codcobranza = i.codcobranza,
                            codigo = 0,
                        }).ToList();

                        List<veremision_iva> iva_3 = new List<veremision_iva>();
                        iva_3 = resultadosTotabilizar.tablaIva.Select(i => new veremision_iva
                        {
                            codremision = 0,
                            porceniva = i.porceniva,
                            total = i.total,
                            porcenbr = i.porcenbr,
                            br = i.br,
                            iva = i.iva,
                            codigo = 0,
                        }).ToList();


                        // objeto para guardar notas de remision
                        SaveNRemisionCompleta nrSave = new SaveNRemisionCompleta();
                        nrSave.veremision = transferencia.veremision;
                        nrSave.veremision1 = detalle_3;
                        nrSave.vedesextraremi = descuentos_3;
                        nrSave.verecargoremi = recargos_3;
                        nrSave.veremision_iva = iva_3;
                        nrSave.veremision_chequerechazado = null;


                        var resultadoGuardado = await grabarNotaRemision(_context, reg.id, usuario, false, codempresa, nrSave);
                        confirmaciones.Add(resultadoGuardado.mensaje);

                        pruebas_NRemi newReg = new pruebas_NRemi
                        {
                            idnr_original = reg.id,
                            nroidnr_original = reg.numeroid,
                            subtotal_original = reg.subtotal,
                            descuentos_original = reg.descuentos,
                            total_original = reg.total,

                            idnr_nueva = reg.id,
                            nroidnr_nueva = resultadoGuardado.nroid,
                            subtotal_nueva = resultadosTotabilizar.subtotal,
                            descuentos_nueva = resultadosTotabilizar.descuento,
                            total_nueva = resultadosTotabilizar.total,

                            fechareg = DateTime.Now,
                        };
                        _context.pruebas_NRemi.Add(newReg);
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
                Console.WriteLine(ex.Message);
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }
        private async Task<SaveNRemisionCompleta> transferirdoc(DBContext _context, string idNRemi, int nroidNRemi)
        {
            var cabecera = await _context.veremision
                .Where(i => i.id == idNRemi && i.numeroid == nroidNRemi)
                .FirstOrDefaultAsync();

            var detalle = await _context.veremision1.Where(i => i.codremision == cabecera.codigo).ToListAsync();

            var vedesextraremi = await _context.vedesextraremi.Where(i => i.codremision == cabecera.codigo).ToListAsync();

            var verecargoremi = await _context.verecargoremi.Where(i => i.codremision == cabecera.codigo).ToListAsync();

            var veremision_iva = await _context.veremision_iva.Where(i => i.codremision == cabecera.codigo).ToListAsync();

            var veremision_chequerechazado = await _context.veremision_chequerechazado.Where(i => i.codremision == cabecera.codigo).FirstOrDefaultAsync();

            SaveNRemisionCompleta data = new SaveNRemisionCompleta();

            data.veremision = cabecera;
            data.veremision1 = detalle;
            data.vedesextraremi = vedesextraremi;
            data.verecargoremi = verecargoremi;
            data.veremision_iva = veremision_iva;
            data.veremision_chequerechazado = veremision_chequerechazado;

            return data;

        }




        /// ///////////////////////////////////////////////////////////
        // FUNCIONES PARA TOTABILIZAR LAS NOTAS DE REMISION CALCULOS
        /// ///////////////////////////////////////////////////////////

        private async Task<dataTotales> TotalizarNR(DBContext _context, string codempresa, string usuario, string userConnectionString, bool desclinea_segun_solicitud, string opcion_nivel, string codcliente_real, int cmbtipo_complementopf, TotabilizarProformaCompleta datosProforma)
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














        private async Task<(string mensaje,int nroid)> grabarNotaRemision(DBContext _context, string id, string usuario, bool desclinea_segun_solicitud, string codempresa, SaveNRemisionCompleta datosRemision)
        {
           
            // borrar los items con cantidad cero
            datosRemision.veremision1.RemoveAll(i => i.cantidad <= 0);
            if (datosRemision.veremision1.Count() <= 0)
            {
                return ("No hay ningun item en su documento." + id + "-" + datosRemision.veremision.numeroid,0);
            }
            veremision veremision = datosRemision.veremision;
            
            int codNRemision = 0;
            int numeroId = 0;
            bool mostrarModificarPlanCuotas = false;
            List<planPago_object>? plandeCuotas = new List<planPago_object>();
            try
            {
                // El front debe llamar a las funciones de totalizar antes de mandar a grabar
                var doc_grabado = await Grabar_Documento(_context, id, usuario, desclinea_segun_solicitud, codempresa, datosRemision);
                // si doc_grabado.mostrarModificarPlanCuotas  == true    Marvin debe desplegar ventana para modificar plan de cuotas, es ventana nueva aparte
                if (doc_grabado.resp != "ok")
                {
                    return ("No se pudo Grabar la Nota de Remision " + id + "-" + datosRemision.veremision.numeroid + " con Exito.", 0);
                }
                // SI ESTA TODO OK QUE LO GUARDE EN VARIABLES LO QUE RECIBE PARA USAR MAS ADELANTE
                codNRemision = doc_grabado.codNRemision;
                numeroId = doc_grabado.numeroId;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al guardar nota de veremision" + ex.Message);
            }

            try
            {
                await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Nota_Remision, codNRemision.ToString(), datosRemision.veremision.id, numeroId.ToString(), this._controllerName, "Grabar", Log.TipoLog.Creacion);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al guardar Log de veremision" + ex.Message);
            }
            return ("Se grabo con exito la nota de remision: " + id + "-" + numeroId, numeroId);

        }



        private async Task<(string resp, int codNRemision, int numeroId)> Grabar_Documento(DBContext _context, string id, string usuario, bool desclinea_segun_solicitud,  string codempresa, SaveNRemisionCompleta datosRemision)
        {
            veremision veremision = datosRemision.veremision;
            veremision.codigo = 0;
            List<veremision1> veremision1 = datosRemision.veremision1;
            var vedesextraremi = datosRemision.vedesextraremi;
            var verecargoremi = datosRemision.verecargoremi;
            var veremision_iva = datosRemision.veremision_iva;
            var veremision_chequerechazado = datosRemision.veremision_chequerechazado;



            ////////////////////   GRABAR DOCUMENTO
            //obtener id actual

            int idnroactual = await documento.ventasnumeroid(_context, id);
            if (await documento.existe_notaremision(_context, id, idnroactual + 1))
            {
                return ("Ese numero de documento, ya existe, por favor consulte con el administrador del sistema.", 0, 0);
            }
            veremision.fechareg = await funciones.FechaDelServidor(_context);
            veremision.horareg = datos_proforma.getHoraActual();
            veremision.fecha_anulacion = veremision.fecha.Date;
            veremision.version_tarifa = await ventas.VersionTarifaActual(_context);
            veremision.descarga = true;
            veremision.numeroid = idnroactual + 1;
            // fin de obtener id actual

            if (desclinea_segun_solicitud == false)
            {
                veremision.idsoldesctos = "0";
                veremision.nroidsoldesctos = 0;
            }


            // accion de guardar

            // guarda cabecera (veremision)
            _context.veremision.Add(veremision);
            await _context.SaveChangesAsync();

            var codNRemision = veremision.codigo;

            // actualiza numero id
            var numeracion = _context.venumeracion.FirstOrDefault(n => n.id == id);
            numeracion.nroactual += 1;
            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos



            // guarda detalle (veremision1)
            // actualizar codigoproforma para agregar
            veremision1 = veremision1.Select(p => { p.codremision = codNRemision; return p; }).ToList();
            // actualiza grupomer antes de guardado
            veremision1 = await ventas.Remision_Cargar_Grupomer(_context, veremision1);
            // actualizar peso del detalle.
            veremision1 = await ventas.Actualizar_Peso_Detalle_Remision(_context, veremision1);

            _context.veremision1.AddRange(veremision1);
            await _context.SaveChangesAsync();


            try
            {
                if (vedesextraremi != null)
                {
                    // grabar descto si hay descuentos
                    if (vedesextraremi.Count() > 0)
                    {
                        await grabardesextra(_context, codNRemision, vedesextraremi);
                    }
                }
            }
            catch (Exception ex)
            {
                return ("Error al Guardar descuentos extras de Nota de Remision, por favor consulte con el administrador del sistema: " + ex.Message, 0, 0);
            }
            try
            {
                if (verecargoremi != null)
                {
                    // grabar recargo si hay recargos
                    if (verecargoremi.Count > 0)
                    {
                        await grabarrecargo(_context, codNRemision, verecargoremi);
                    }
                }
            }
            catch (Exception ex)
            {
                return ("Error al Guardar recargos de Nota de Remision, por favor consulte con el administrador del sistema: " + ex.Message, 0, 0);
            }
            try
            {
                if (veremision_iva != null)
                {
                    // grabar iva
                    if (veremision_iva.Count > 0)
                    {
                        await grabariva(_context, codNRemision, veremision_iva);
                    }
                }
            }
            catch (Exception ex)
            {
                return ("Error al Guardar iva de Nota de Remision, por favor consulte con el administrador del sistema: " + ex.Message, 0, 0);
            }





            // ####################################
            // generar plan de pagos si es que es a credito
            // #################################

            /*

            List<planPago_object> planCuotasGenerada = new List<planPago_object>();
            bool mostrarModificarPlanCuotas = false;
            if (veremision.tipopago == 1)  // si el tipo pago es a credito == 1
            {
                // SI LA PROFORMA ES DE UNA NOTA ANTERIOR ANULADA SE DEBE HACER EL PLAN DE PAGOS SEGUN LA FECHA
                // DE LA PRIMERA NOTA DE REMISION
                if (await ventas.ProformaTieneNRAnulada(_context, codProforma))
                {
                    // con fecha de ntoa anulada
                    DateTime fecha_antigua = await ventas.ProformaFechaMasAntiguaNR(_context, codProforma);

                    // #si es PP genrarar con su monto y su fecha no importan las complementarias ni influye
                    if (await ventas.remision_es_PP(_context, codNRemision))
                    {

                        if (await ventas.generarcuotaspago(_context, codNRemision, 4, (double)veremision.total, (double)veremision.total, veremision.codmoneda, veremision.codcliente, fecha_antigua, false, codempresa))
                        {
                            // modificarplandepago()
                            // ENVIAR BOOL PARA MOSTRAR PLAN DE CUOTAS
                            mostrarModificarPlanCuotas = true;
                            planCuotasGenerada = await verPlandePago(_context, codNRemision, veremision.usuarioreg);
                        }

                    }
                    else
                    {
                        // #si no es PP perocomo es complementaria hacer todo el el chenko
                        if (await ventas.proforma_es_complementaria(_context, codProforma) == false)
                        {
                            if (await ventas.generarcuotaspago(_context, codNRemision, 4, (double)veremision.total, (double)veremision.total, veremision.codmoneda, veremision.codcliente, fecha_antigua, false, codempresa))
                            {
                                // modificarplandepago()
                                // ENVIAR BOOL PARA MOSTRAR PLAN DE CUOTAS
                                mostrarModificarPlanCuotas = true;
                                planCuotasGenerada = await verPlandePago(_context, codNRemision, veremision.usuarioreg);
                            }
                        }
                        else
                        {
                            var lista = await ventas.lista_PFNR_complementarias_noPP(_context, codProforma);
                            if (await ventas.generarcuotaspago(_context, codNRemision, 4, await ventas.MontoTotalComplementarias(_context, lista), (double)veremision.total, veremision.codmoneda, veremision.codcliente, await ventas.FechaComplementariaDeMayorMonto(_context, lista), false, codempresa))
                            {
                                // modificarplandepago(codigo.Text, codcliente.Text, codmoneda.Text, total.Text)
                                mostrarModificarPlanCuotas = true;
                                planCuotasGenerada = await verPlandePago(_context, codNRemision, veremision.usuarioreg);
                            }
                            // para cada nota complementaria revertir pagos y generar sus cuotas
                            foreach (var reg in lista)
                            {
                                if (reg.codremision == 0)
                                {
                                    // nada
                                }
                                else
                                {
                                    if (reg.codremision == codNRemision)
                                    {
                                        // la actual no hacer pues ya fue hecha
                                    }
                                    else
                                    {
                                        // EN UNA REUNION CON GERENCIA, SE DECIDIO ANULAR ESTA ACCION.
                                        /*
                                        if (await ventas.revertirpagos(_context, reg.codremision,4))
                                        {
                                            await ventas.generarcuotaspago(_context, reg.codremision, 4, await ventas.MontoTotalComplementarias(_context, lista), await ventas.TotalNRconNC_ND(_context, reg.codremision), veremision.codmoneda, veremision.codcliente, await ventas.FechaComplementariaDeMayorMonto(_context, lista), false, codempresa);
                                        }
                                        */
            /*
                                    }
                                }
                            }

                        }
                    }
                }
                else
                {
                    // procedimiento normal
                    // #si es PP genrarar con su monto y su fecha no importan las complementarias ni influye
                    if (await ventas.remision_es_PP(_context, codNRemision))
                    {
                        if (await ventas.generarcuotaspago(_context, codNRemision, 4, (double)veremision.total, (double)veremision.total, veremision.codmoneda, veremision.codcliente, veremision.fecha.Date, false, codempresa))
                        {
                            // modificarplandepago(codigo.Text, codcliente.Text, codmoneda.Text, total.Text)
                            mostrarModificarPlanCuotas = true;
                            planCuotasGenerada = await verPlandePago(_context, codNRemision, veremision.usuarioreg);
                        }
                    }
                    else
                    {
                        // #si no es PP perocomo es complementaria hacer todo el el chenko
                        if (await ventas.proforma_es_complementaria(_context, codProforma) == false)
                        {
                            if (await ventas.generarcuotaspago(_context, codNRemision, 4, (double)veremision.total, (double)veremision.total, veremision.codmoneda, veremision.codcliente, veremision.fecha.Date, false, codempresa))
                            {
                                // modificarplandepago(codigo.Text, codcliente.Text, codmoneda.Text, total.Text)
                                mostrarModificarPlanCuotas = true;
                                planCuotasGenerada = await verPlandePago(_context, codNRemision, veremision.usuarioreg);
                            }

                        }
                        else
                        {
                            var lista = await ventas.lista_PFNR_complementarias_noPP(_context, codProforma);
                            if (await ventas.generarcuotaspago(_context, codNRemision, 4, await ventas.MontoTotalComplementarias(_context, lista), (double)veremision.total, veremision.codmoneda, veremision.codcliente, await ventas.FechaComplementariaDeMayorMonto(_context, lista), false, codempresa))
                            {
                                // modificarplandepago(codigo.Text, codcliente.Text, codmoneda.Text, total.Text)
                                mostrarModificarPlanCuotas = true;
                                planCuotasGenerada = await verPlandePago(_context, codNRemision, veremision.usuarioreg);
                            }
                            // para cada nota complementaria revertir pagos y generar sus cuotas
                            foreach (var reg in lista)
                            {
                                if (reg.codremision == 0)
                                {
                                    // nada
                                }
                                else
                                {
                                    if (reg.codremision == codNRemision)
                                    {
                                        // la actual no hacer pues ya fue hecha
                                    }
                                    else
                                    {
                                        // EN UNA REUNION CON GERENCIA, SE DECIDIO ANULAR ESTA ACCION.
                                        /*
                                        if (await ventas.revertirpagos(_context, reg.codremision, 4))
                                        {
                                            await ventas.generarcuotaspago(_context, reg.codremision, 4, await ventas.MontoTotalComplementarias(_context, lista), await ventas.TotalNRconNC_ND(_context, reg.codremision), veremision.codmoneda, veremision.codcliente, await ventas.FechaComplementariaDeMayorMonto(_context, lista), false, codempresa);
                                        }
                                        */
            /*
                                    }
                                }
                            }
                        }
                    }


                }
            }

            */

            //###################################
            //  FIN
            //###################################


            return ("ok", codNRemision, veremision.numeroid);
        }


        private async Task grabardesextra(DBContext _context, int codRemi, List<vedesextraremi> vedesextraremi)
        {
            var descExtraAnt = await _context.vedesextraremi.Where(i => i.codremision == codRemi).ToListAsync();
            if (descExtraAnt.Count() > 0)
            {
                _context.vedesextraremi.RemoveRange(descExtraAnt);
                await _context.SaveChangesAsync();
            }
            vedesextraremi = vedesextraremi.Select(p => { p.codremision = codRemi; return p; }).ToList();
            _context.vedesextraremi.AddRange(vedesextraremi);
            await _context.SaveChangesAsync();
        }


        private async Task grabarrecargo(DBContext _context, int codRemi, List<verecargoremi> verecargoremi)
        {
            var recargosAnt = await _context.verecargoremi.Where(i => i.codremision == codRemi).ToListAsync();
            if (recargosAnt.Count() > 0)
            {
                _context.verecargoremi.RemoveRange(recargosAnt);
                await _context.SaveChangesAsync();
            }
            verecargoremi = verecargoremi.Select(p => { p.codremision = codRemi; return p; }).ToList();
            _context.verecargoremi.AddRange(verecargoremi);
            await _context.SaveChangesAsync();
        }

        private async Task grabariva(DBContext _context, int codRemi, List<veremision_iva> veremision_iva)
        {
            var ivaAnt = await _context.veremision_iva.Where(i => i.codremision == codRemi).ToListAsync();
            if (ivaAnt.Count() > 0)
            {
                _context.veremision_iva.RemoveRange(ivaAnt);
                await _context.SaveChangesAsync();
            }
            veremision_iva = veremision_iva.Select(p => { p.codremision = codRemi; return p; }).ToList();
            _context.veremision_iva.AddRange(veremision_iva);
            await _context.SaveChangesAsync();
        }
        private async Task<List<planPago_object>> verPlandePago(DBContext _context, int codRemision, string usuario)
        {
            // log de ingreso
            // prgmodifplanpago_Load
            var planPagos = await _context.coplancuotas.Where(i => i.coddocumento == codRemision && i.codtipodoc == 4)
                .Select(i => new planPago_object
                {
                    nrocuota = i.nrocuota,
                    vencimiento = i.vencimiento,
                    monto = i.monto,
                    moneda = i.moneda
                })
                .ToListAsync();

            return planPagos;
        }

    }


    public class planPago_object
    {
        public int nrocuota { get; set; }
        public DateTime vencimiento { get; set; }
        public decimal monto { get; set; }
        public string moneda { get; set; }
    }

}
