using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;

namespace SIAW.Controllers.ventas.transaccion
{
    [Route("api/venta/transac/[controller]")]
    [ApiController]
    public class prgfacturarNR_cufdController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly siaw_funciones.Configuracion configuracion = new siaw_funciones.Configuracion();
        private readonly siaw_funciones.Ventas ventas = new siaw_funciones.Ventas();

        private readonly SIAT siat = new SIAT();

        public prgfacturarNR_cufdController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/adsiat_tipodocidentidad
        [HttpGet]
        [Route("getTipoDocIdent/{userConn}")]
        public async Task<ActionResult<IEnumerable<adsiat_tipodocidentidad>>> Getaadsiat_tipodocidentidad(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adsiat_tipodocidentidad == null)
                    {
                        return BadRequest(new { resp = "Entidad adtipocambio es null." });
                    }
                    var result = await _context.adsiat_tipodocidentidad
                        .OrderBy(t => t.codigoclasificador)
                        .Select(t => new
                        {
                            t.codigoclasificador,
                            t.descripcion
                        })
                        .ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private async Task<(bool resp, string msg)> GENERAR_FACTURA_DE_NR(DBContext _context, bool opcion_automatico, string codEmpresa, int codigoremision)
        {
            //if (validar(opcion_automatico))
            if(true)
            {
                bool distribuir_desc_extra_en_factura = await configuracion.distribuir_descuentos_en_facturacion(_context, codEmpresa);
                var datosNR = await cargar_nr(_context, codigoremision);

                if (datosNR.cabecera == null || datosNR.detalle == null)
                {
                    return (false,"No se pudo obtener los datos de la Nota de Remision, consulte con el administrador");
                }
                veremision cabecera = datosNR.cabecera;
                List < veremision_detalle > detalle = datosNR.detalle;
                
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

                    
                     
                    distribuir_descuentos(codigoremision)
                    // No_Distribuir_Descuentos(codigoremision)
                    // acca devuelve detalle modificado
                    detalle = await distribuir_recargos(_context, codigoremision, codEmpresa, detalle);


                }
                return (false,"");
            }
            else
            {
                return (false, "No se pudo generar la factura!!!");
            }
        }

        private async Task<(veremision ? cabecera, List<veremision_detalle>? detalle)> cargar_nr(DBContext _context, int codigo)
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
                return (cabecera, detalle);
            }
            catch (Exception)
            {
                return (null, null);
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
    

}
