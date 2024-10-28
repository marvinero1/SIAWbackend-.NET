using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static siaw_funciones.Validar_Vta;

namespace siaw_funciones
{
    public class Anticipos_Vta_Contado
    {
        public static class DbContextFactory
        {
            public static DBContext Create(string connectionString)
            {
                var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
                optionsBuilder.UseSqlServer(connectionString);

                return new DBContext(optionsBuilder.Options);
            }
        }
        //Clase necesaria para el uso del DBContext del proyecto siaw_Context
        private Empresa empresa = new Empresa();
        private TipoCambio tipoCambio = new TipoCambio();
        private Almacen almacen = new Almacen();
        private Cliente cliente = new Cliente();
        private Cobranzas cobranzas = new Cobranzas();

        public async Task<bool> ActualizarMontoRestAnticipo(DBContext _context, string id_anticipo, int numeroid_anticipo, int codproforma, int codanticipo, double monto_actual_aplicado, string codigoempresa)
        {
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // obtener monto total original del anticipo
            double monto_anticipo = 0;
            string moneda_anticipo = "";
            double monto_proformas = 0;
            double monto_aux = 0;
            double monto_devoluciones = 0;
            double monto_rever_contado_ce = 0;
            double monto_rever_contado_credito = 0;
            double monto_aplicado_en_factura_tienda = 0;
            //obtener el monto original del anticipo
            try
            {
                var dt = await _context.coanticipo.Where(i => i.id == id_anticipo && i.numeroid == numeroid_anticipo)
                    .Select(i => new
                    {
                        i.monto,
                        i.codmoneda
                    }).FirstOrDefaultAsync();
                if (dt != null)
                {
                    monto_anticipo = (double)(dt.monto ?? 0);
                    moneda_anticipo = dt.codmoneda;
                }
                else
                {
                    monto_anticipo = 0;
                    moneda_anticipo = await Empresa.monedabase(_context, codigoempresa);
                }
            }
            catch (Exception)
            {
                monto_anticipo = 0;
                moneda_anticipo = await Empresa.monedabase(_context, codigoempresa);
            }


            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // obtener el monto que se uso el anticipo
            // pero las proformas no debene estar anuladas
            // ademas no se debe tomar en cuenta la proforma en cuestion
            try
            {
                var dt = await _context.veproforma_anticipo
                    .Join(_context.veproforma,
                        p1 => p1.codproforma,
                        p2 => p2.codigo,
                        (p1, p2) => new { p1, p2 })
                    .Where(pair => pair.p2.anulada == false &&
                                   pair.p1.codanticipo == _context.coanticipo
                                                               .Where(c => c.id == id_anticipo && c.numeroid == numeroid_anticipo)
                                                               .Select(c => c.codigo)
                                                               .FirstOrDefault() &&
                                   pair.p1.codproforma != codproforma)
                    .Select(pair => new
                    {
                        pair.p2.fecha,
                        pair.p1.monto,
                        pair.p2.codmoneda
                    }).ToListAsync();
                foreach (var reg in dt)
                {
                    // convertir el monto en la moneda original del anticipo si es necesario
                    monto_aux = (double)await tipoCambio._conversion(_context, moneda_anticipo, reg.codmoneda, reg.fecha, reg.monto??0);
                    monto_aux = Math.Round(monto_aux, 2);
                    monto_proformas += monto_aux;
                }
            }
            catch (Exception)
            {
                monto_proformas = 0;
            }
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++


            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // obtener el monto en devoluciones que se realizo del anticipo
            try
            {
                var dt = await _context.codevanticipo
                .Join(_context.coanticipo,
                    p1 => new { id = p1.idanticipo, numeroid = p1.numeroidanticipo },
                    p2 => new { p2.id, p2.numeroid },
                    (p1, p2) => new { p1, p2 })
                .Where(pair => pair.p1.anulada == false &&
                               pair.p1.idanticipo == id_anticipo &&
                               pair.p1.numeroidanticipo == numeroid_anticipo)
                .Select(pair => new
                {
                    pair.p1.id,
                    pair.p1.numeroid,
                    pair.p1.monto,
                    pair.p2.codmoneda,
                    pair.p1.fecha
                }).ToListAsync();
                foreach (var reg in dt)
                {
                    monto_aux = (double)await tipoCambio._conversion(_context, moneda_anticipo, reg.codmoneda, reg.fecha, reg.monto ?? 0);
                    monto_aux = Math.Round(monto_aux, 2);
                    monto_devoluciones += monto_aux;
                }
            }
            catch (Exception)
            {
                monto_devoluciones = 0;
            }
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++


            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // buscar las reversiones a COBRANZAS-CONTADO 
            try
            {
                var dt = await _context.cocobranza_contado_anticipo
                .Join(_context.coanticipo,
                    p1 => p1.codanticipo,
                    p2 => p2.codigo,
                    (p1, p2) => new { p1, p2 })
                .Join(_context.cocobranza_contado,
                    pair => pair.p1.codcobranza,
                    p3 => p3.codigo,
                    (pair, p3) => new
                    {
                        id = pair.p2.id,
                        numeroid = pair.p2.numeroid,
                        anulado = pair.p2.anulado,
                        p3.reciboanulado,
                        fechacbza = p3.fecha,
                        monto_rever = pair.p1.monto,
                        codmoneda = p3.moneda
                    })
                .Where(result => result.id == id_anticipo &&
                                 result.numeroid == numeroid_anticipo &&
                                 result.anulado == false &&
                                 result.reciboanulado == false)
                .ToListAsync();
                foreach (var reg in dt)
                {
                    monto_aux = (double)await tipoCambio._conversion(_context, moneda_anticipo, reg.codmoneda, reg.fechacbza, reg.monto_rever ?? 0);
                    monto_aux = Math.Round(monto_aux, 2);
                    monto_rever_contado_ce += monto_aux;
                }
            }
            catch (Exception)
            {
                monto_rever_contado_ce = 0;
            }
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++


            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // buscar las reversiones a COBRANZA-CREDITO
            try
            {
                var dt = await _context.cocobranza_anticipo
                    .Join(_context.coanticipo,
                        p1 => p1.codanticipo,
                        p2 => p2.codigo,
                        (p1, p2) => new { p1, p2 })
                    .Join(_context.cocobranza,
                        pair => pair.p1.codcobranza,
                        p3 => p3.codigo,
                        (pair, p3) => new
                        {
                            id = pair.p2.id,
                            numeroid = pair.p2.numeroid,
                            anulado = pair.p2.anulado,
                            p3.reciboanulado,
                            monto_rever = pair.p1.monto,
                            codmoneda = p3.moneda,
                            fechacbza = p3.fecha
                        })
                    .Where(result => result.id == id_anticipo &&
                                     result.numeroid == numeroid_anticipo &&
                                     result.anulado == false &&
                                     result.reciboanulado == false)
                    .ToListAsync();

                foreach (var reg in dt)
                {
                    monto_aux = (double)await tipoCambio._conversion(_context, moneda_anticipo, reg.codmoneda, reg.fechacbza, reg.monto_rever ?? 0);
                    monto_aux = Math.Round(monto_aux, 2);
                    monto_rever_contado_credito += monto_aux;
                }
            }
            catch (Exception)
            {
                monto_rever_contado_credito = 0;
            }
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++


            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // buscar las aplicaciones EN FACTURACION MOSTRADOR
            try
            {
                var dt = await _context.vefactura
                    .Where(factura => factura.idanticipo == id_anticipo &&
                                      factura.numeroidanticipo == numeroid_anticipo &&
                                      factura.anulada == false)
                    .OrderBy(factura => factura.fecha)
                    .Select(factura => new
                    {
                        factura.id,
                        factura.numeroid,
                        factura.fecha,
                        factura.codcliente,
                        factura.codmoneda,
                        factura.monto_anticipo
                    }).ToListAsync();

                foreach (var reg in dt)
                {
                    monto_aux = (double)await tipoCambio._conversion(_context, moneda_anticipo, reg.codmoneda, reg.fecha, reg.monto_anticipo ?? 0);
                    monto_aux = Math.Round(monto_aux, 2);
                    monto_aplicado_en_factura_tienda += monto_aux;
                }
            }
            catch (Exception)
            {
                monto_aplicado_en_factura_tienda = 0;
            }
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++


            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // actualizar el monto restante del anticipo
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            double saldo_anticipo = monto_anticipo - (monto_proformas + monto_devoluciones + monto_actual_aplicado + monto_rever_contado_ce + monto_rever_contado_credito + monto_aplicado_en_factura_tienda);
            saldo_anticipo = Math.Round(saldo_anticipo, 2);

            if (saldo_anticipo < 0)
            {
                saldo_anticipo = 0;
            }

            var anticipoToUpdate = await _context.coanticipo
                .Where(a => a.id == id_anticipo && a.numeroid == numeroid_anticipo)
                .FirstOrDefaultAsync();

            if (anticipoToUpdate != null)
            {
                anticipoToUpdate.montorest = (decimal?)saldo_anticipo;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }



        public async Task<List <vedetalleanticipoProforma>> Anticipos_Aplicados_a_Proforma(DBContext _context, string id, int nroid)
        {
            var dt_anticipos = await _context.veproforma_anticipo
                .Join(_context.veproforma,
                      p1 => p1.codproforma,
                      p2 => p2.codigo,
                      (p1, p2) => new { p1, p2 })
                .Join(_context.coanticipo,
                      combined => combined.p1.codanticipo,
                      p3 => p3.codigo,
                      (combined, p3) => new { combined.p1, combined.p2, p3 })
                .Where(x => x.p2.id == id && x.p2.numeroid == nroid)
                .Select(x => new vedetalleanticipoProforma
                {
                    codproforma = x.p1.codproforma ?? 0,
                    codanticipo = x.p1.codanticipo ?? 0,
                    docanticipo = x.p3.id + "-" + x.p3.numeroid,
                    id_anticipo = x.p3.id,
                    nroid_anticipo = x.p3.numeroid,
                    monto = (double)(x.p1.monto ?? 0),
                    tdc = (double)(x.p1.tdc ?? 0),
                    codmoneda = x.p2.codmoneda,
                    fechareg = x.p1.fechareg ?? new DateTime(1900,1,1),
                    usuarioreg = x.p1.usuarioreg,
                    horareg = x.p1.horareg,
                    codvendedor = x.p3.codvendedor.ToString()

                    /*
                    x.p1,                               // All fields from veproforma_anticipo
                    x.p2.id,                            // id from veproforma
                    x.p2.numeroid,                      // numeroid from veproforma
                    x.p2.nomcliente,                    // nomcliente from veproforma
                    x.p2.codmoneda,                     // codmoneda from veproforma
                    docanticipo = "",                   // Empty string as docanticipo
                    x.p3.anulado,                       // anulado from coanticipo
                    id_anticipo = x.p3.id,              // id from coanticipo as id_anticipo
                    nroid_anticipo = x.p3.numeroid,     // numeroid from coanticipo as nroid_anticipo
                    x.p3.para_venta_contado,            // para_venta_contado from coanticipo
                    x.p3.codvendedor                    // codvendedor from coanticipo
                    */
                }).ToListAsync();
            return dt_anticipos;
        }


        public async Task<ResultadoValidacion> Validar_Anticipo_Asignado_2(DBContext _context, bool para_aprobar, DatosDocVta DVTA, List<vedetalleanticipoProforma> dt_anticipo_pf, string codempresa)
        {
            bool resultado = true;
            string cadena = "";
            string cadena0 = "";
            string cadena1 = "";
            string cadena2 = "";
            string cadena3 = "";
            string cadena4 = "";
            string cadena5 = "";
            string cadena6 = "";
            ResultadoValidacion objres = new ResultadoValidacion();
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;

            //si la venta es al credito no debe validar anticipos asignados
            if (DVTA.tipo_vta == "CREDITO" || DVTA.tipo_vta == "1")
            {
                return objres;
            }
            //si la venta es al CONTADO - CONTRA ENTREGA no debe validar anticipos asignados
            if ((DVTA.tipo_vta == "CONTADO" || DVTA.tipo_vta == "0") && DVTA.contra_entrega == "SI")
            {
                return objres;
            }
            //si la venta es al CONTADO - CONTRA ENTREGA no debe validar anticipos asignados
            if ((DVTA.tipo_vta == "CONTADO" || DVTA.tipo_vta == "0") && DVTA.contra_entrega == "NO")
            {
                if (await almacen.Es_Tienda(_context, Convert.ToInt32(DVTA.codalmacen)))
                {
                    //si la venta es al contado y NO es contra entrega y ES tienda no se debe verificar que haya anticipo
                    return objres;
                }
            }
            ///////////////////////////////////////////////////////////////////
            //VERIFICA QUE LA VENTA AL CONTADO TENGA ASIGNADO AL ANTICIPO
            //DE PAGO RESPECTIVO
            if (resultado == true)
            {
                if (dt_anticipo_pf.Count() == 0)
                {
                    cadena0 = "Toda venta al CONTADO debe tener asignado al menos un anticipo.";
                    cadena0 += Environment.NewLine + "----------------------------------------";
                    resultado = false;
                }
            }
            //VERIFICAR SI EL ANTICIPO EXISTE
            if (resultado == true)
            {
                foreach (var item in dt_anticipo_pf)
                {
                    if (await cobranzas.Existe_Anticipo_Venta_Contado(_context, item.id_anticipo, item.nroid_anticipo) == false)
                    {
                        if (cadena1.Trim().Length == 0)
                        {
                            cadena1 = "No Existen los Anticipos:";
                        }

                        cadena0 += Environment.NewLine + item.id_anticipo + "-" + item.nroid_anticipo + " no existe!!!";
                        resultado = false;
                    }
                }
            }
            if (cadena1.Trim().Length > 0)
            {
                cadena1 += Environment.NewLine + "----------------------------------------";
            }
            ///////////////////////////////////////////////////////////////////
            //VERIFICAR SI EL ANTICIPO ES DEL MISMO CLIENTE segun el codigo de cliente
            string _NitAnticipo = "";
            string _NombreNitAnticipo = "";
            if (resultado == true)
            {
                foreach (var item in dt_anticipo_pf)
                {
                    if ((DVTA.codcliente == await cobranzas.Cliente_De_Anticipo(_context, item.id_anticipo, item.nroid_anticipo)) == false)
                    {
                        //el codcliente de la proforma  NO es el mismo al de ANTICIPO
                        if (cadena2.Trim().Length == 0)
                        {
                            cadena2 = "Anticipos no son del Cliente:" + DVTA.codcliente;
                        }
                        cadena2 += Environment.NewLine + item.id_anticipo + "-" + item.nroid_anticipo;
                        resultado = false;
                    }
                    else
                    {
                        //Desde 1/12/2023 solo validar el NIT si la venta es casual-referencial, y no cuando es una venta casual-casual o referencial-referencial
                        if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente) == true)
                        {
                            _NitAnticipo = await cobranzas.Nit_De_Anticipo(_context, item.id_anticipo, item.nroid_anticipo);
                            _NombreNitAnticipo = await cobranzas.Nombre_Nit_De_Anticipo(_context, item.id_anticipo, item.nroid_anticipo);
                            if ((DVTA.nitfactura == _NitAnticipo) == false)
                            {
                                if (cadena2.Trim().Length == 0)
                                {
                                    cadena2 = "Anticipo no pertenece al Cliente:" + DVTA.codcliente + " NIT: " + _NitAnticipo + "-" + _NombreNitAnticipo;
                                }
                                cadena2 += Environment.NewLine + item.id_anticipo + "-" + item.nroid_anticipo + " es del cliente con NIT: " + _NitAnticipo + " - " + _NombreNitAnticipo;
                                resultado = false;
                            }
                        }

                    }
                }
            }
            if (cadena2.Trim().Length > 0)
            {
                cadena2 += Environment.NewLine + "----------------------------------------";
            }
            //QUE EL ANTICIPO SEA USADO PARA FACTURAR A UN MISNO NOMBRE DE CLIENTE
            //esta politica fue decidia por JRA-MARIELA MONTAÑO A FINES DE FEBRERO-2016
            //validar Nombres_Facturados_Anticipo_Contado
            //esta validacion controla que el anticipo contado se haya utilizado
            //solamente para sacar facturas a un mismo nombre y nit
            //SE IMPLEMENTO EN MARZO2016
            List<string> nombres_facturados;
            foreach (var item in dt_anticipo_pf)
            {
                //obtener los nombres a los cuales se facturo con este anticipo contado
                //que en la practica solo deberia haberse facturado a un solo nombre
                nombres_facturados = await cobranzas.Nombres_Facturados_Anticipo_Contado(_context, item.id_anticipo, item.nroid_anticipo);
                if (nombres_facturados.Count == 0)
                {
                    //si es el mismo nombre se verifica el sgte anticipo
                }
                else if (nombres_facturados.Count > 0)
                {
                    foreach (var nom in nombres_facturados)
                    {
                        if (nom != DVTA.nombcliente)
                        {
                            if (cadena3.Trim().Length == 0)
                            {
                                cadena3 = "Anticipos usado para facturar distintos NIT:" + DVTA.codcliente;
                            }
                            cadena3 += Environment.NewLine + "Anticipo: " + item.id_anticipo + "-" + item.nroid_anticipo + " ya fue utilizado para facturar al nombre: " + nom + " y solo se puede usar para facturar a ese nombre.";
                            resultado = false;
                        }
                    }

                }
            }
            if (cadena3.Trim().Length > 0)
            {
                cadena3 += Environment.NewLine + "----------------------------------------";
            }
            ///////////////////////////////////////////////////////////////////
            //VERIFICAR EL SALDO DISPONIBLE DEL ANTICIPO
            decimal monto_anticipo = 0;
            decimal monto_rest_anticipo_pf = 0;
            decimal monto_rest_anticipo_cb = 0;
            decimal monto_rest_anticipo = 0;
            string moneda_anticipo = "";

            if (resultado == true)
            {
                foreach (var item in dt_anticipo_pf)
                {
                    //obtener el monto del anticipo
                    monto_anticipo = await cobranzas.Monto_De_Anticipo_PF(_context, item.id_anticipo, item.nroid_anticipo);
                    moneda_anticipo = await cobranzas.Moneda_De_Anticipo(_context, item.id_anticipo, item.nroid_anticipo);
                    //convertir el monton del anticipo aplicado en PF a la moneda del anticipo
                    if (item.codmoneda != moneda_anticipo)
                    {
                        //si la moneda del anticipo y proforma no son iguales entonces convertir el monto asignar a la moneda de la proforma
                        item.monto = (double)await tipoCambio._conversion(_context, moneda_anticipo, item.codmoneda, item.fechareg, (decimal)item.monto);
                        item.monto = Math.Round(item.monto, 2);
                    }

                    //actualizar el montorest
                    await ActualizarMontoRestAnticipo(_context, item.id_anticipo, item.nroid_anticipo, item.codproforma, item.codanticipo, item.monto, codempresa);
                    //obtener el monto restante del anticipo sin incluir el monto aplicado para esta proforma
                    monto_rest_anticipo_pf = await Montorest_De_Anticipo_Sin_Proforma(_context, item.id_anticipo, item.nroid_anticipo, item.codproforma, codempresa);
                    //obtener el monto restante del anticipo incluir el monto aplicado a una cobranza
                    monto_rest_anticipo_cb = await Montorest_De_Anticipo_Cobranza_Anticipo(_context, item.id_anticipo, item.nroid_anticipo, codempresa);
                    //obtener el monto restante del anticipo incluir el monto aplicado a una cobranza
                    monto_rest_anticipo = (monto_rest_anticipo_pf + monto_rest_anticipo_cb) - monto_anticipo;
                    monto_rest_anticipo = Math.Round(monto_rest_anticipo, 2);
                    //verificar si el saldos del anticipo es mayor o igual al monto asignado
                    if (item.monto > (double)monto_rest_anticipo)
                    {
                        if (cadena4.Trim().Length == 0)
                        {
                            cadena4 = "Saldo Insuficiente de Anticipo:" + DVTA.codcliente;
                        }
                        cadena4 += Environment.NewLine + "Saldo de: " + item.id_anticipo + "-" + item.nroid_anticipo + " --> es: " + monto_rest_anticipo + " y no alcanza para aplicar a la proforma.";
                        resultado = false;
                    }
                    //convertir el monton del anticipo aplicado en PF a la moneda del anticipo
                    if (item.codmoneda != moneda_anticipo)
                    {
                        //si la moneda del anticipo y proforma no son iguales entonces convertir el monto asignar a la moneda de la proforma
                        item.monto = (double)await tipoCambio._conversion(_context, moneda_anticipo, item.codmoneda, item.fechareg, (decimal)item.monto);
                        item.monto = Math.Round(item.monto, 2);
                    }

                }
            }
            if (cadena4.Trim().Length > 0)
            {
                cadena4 += Environment.NewLine + "----------------------------------------";
            }
            //VERIFICAR SI EL TOTAL ASIGNADO DE ANTICIPOS ES SUFICIENTE
            //Desde 14/12/2023 si la moneda de los anticipos es diferente a la moneda de la proforma convertir a la moneda de la PF
            decimal TTL_asignado = 0;
            decimal DIFERENCIA = 0;
            if (resultado == true)
            {
                foreach (var item in dt_anticipo_pf)
                {
                    TTL_asignado += (decimal)item.monto;
                }

                TTL_asignado = Math.Round(TTL_asignado, 2);
                if (TTL_asignado < (decimal)DVTA.totaldoc)
                {
                    DIFERENCIA = Math.Round(Math.Abs(TTL_asignado - (decimal)DVTA.totaldoc), 2);
                    if (DIFERENCIA > (decimal)0.5)
                    {
                        //'Desde 04/06/2024 debe haber una tolerancia de 0.5 segun solciitado por la supervisora nal de operaciones y servicio al cliente autorizado por gerencia
                        //if (DIFERENCIA >= (decimal)0.01)
                        //Desde 18/12/2024 ya no hay tolerancia de 0.03 segun indicado por mariela el monto debe ser exacto al momento de aplicar anticipos en una proforma
                        //'    cadena5 &= vbCrLf & "El monto total de anticipo asignado no es suficiente para la venta, se asigno:" & TTL_asignado.ToString & "(" & DVTA.codmoneda & ") y el total de la venta es: " & DVTA.totaldoc.ToString & "(" & DVTA.codmoneda & ")"
                        cadena5 += Environment.NewLine + "Existe una diferencia de: " + DIFERENCIA + ". El monto total de anticipo asignado no es suficiente para la venta, se asigno:" + TTL_asignado.ToString() + "(" + DVTA.codmoneda + ") y el total de la venta es: " + DVTA.totaldoc.ToString() + "(" + DVTA.codmoneda + ")";
                        resultado = false;
                    }
                }
                if (TTL_asignado > (decimal)DVTA.totaldoc)
                {
                    //'Desde 04/06/2024 debe haber una tolerancia de 0.5 segun solciitado por la supervisora nal de operaciones y servicio al cliente autorizado por gerencia
                    DIFERENCIA = Math.Round(Math.Abs(TTL_asignado - (decimal)DVTA.totaldoc), 2);
                    if (DIFERENCIA > (decimal)0.5)
                    {
                        cadena5 += Environment.NewLine + "Existe una diferencia de: " + DIFERENCIA + ". El monto total de anticipo asignado es mayor a la venta total. El monto de anticipo asignado es: " + TTL_asignado.ToString() + "(" + DVTA.codmoneda + ") y el total de la venta es: " + DVTA.totaldoc.ToString() + "(" + DVTA.codmoneda + ")";
                        resultado = false;
                    }
                    //    'cadena5 &= vbCrLf & "El monto total de anticipo asignado es mayor a la venta total. El monto de anticipo asignado es: " & TTL_asignado.ToString & "(" & DVTA.codmoneda & ") y el total de la venta es: " & DVTA.totaldoc.ToString & "(" & DVTA.codmoneda & ")"
                    //'resultado = False
                }
            }
            if (cadena5.Trim().Length > 0)
            {
                cadena5 += Environment.NewLine + "----------------------------------------";
            }
            //VERIFICAR QUE EL CODIGO DE VENDEDOR DE LA PROFORMA SEA EL MISMO CODIGO DE VENDEDOR DE LOS ANTICIPOS APLICADOS
            //Desde 23/01/2024
            string codvendedor_pf = "";
            codvendedor_pf = DVTA.codvendedor;
            if (resultado == true)
            {
                foreach (var item in dt_anticipo_pf)
                {
                    if (codvendedor_pf != item.codvendedor)
                    {
                        cadena6 += Environment.NewLine + "El codigo de vendedor del anticipo: " + item.codvendedor + " no es igual que el codigo de vendedor de la proforma: " + codvendedor_pf + " .";
                        resultado = false;
                    }
                }
            }
            if (cadena6.Trim().Length > 0)
            {
                cadena6 += Environment.NewLine + "----------------------------------------";
            }

            if (resultado == false)
            {
                objres.resultado = false;
                cadena = cadena0;
                if (cadena1.Trim().Length > 0)
                {
                    cadena += Environment.NewLine + cadena1;
                }

                if (cadena2.Trim().Length > 0)
                {
                    cadena += Environment.NewLine + cadena2;
                }

                if (cadena3.Trim().Length > 0)
                {
                    cadena += Environment.NewLine + cadena3;
                }

                if (cadena4.Trim().Length > 0)
                {
                    cadena += Environment.NewLine + cadena4;
                }

                if (cadena5.Trim().Length > 0)
                {
                    cadena += Environment.NewLine + cadena5;
                }

                if (cadena6.Trim().Length > 0)
                {
                    cadena += Environment.NewLine + cadena6;
                }
                objres.observacion = "Se econtraron observaciones en los anticipos asignados: ";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }

        public async Task<decimal> Montorest_De_Anticipo_Sin_Proforma(DBContext _context, string id_anticipo, int nroid_anticipo, int codproforma, string codigoempresa)
        {
            decimal resultado = 0;
            //string qry = "";
            decimal monto_anticipo = 0;
            decimal ttl_aplicado = 0;

            try
            {
                var dt_anticipo = await _context.coanticipo.Where(i => i.id == id_anticipo && i.numeroid == nroid_anticipo)
                    .Select(i => new
                    {
                        i.monto,
                        i.codmoneda
                    }).FirstOrDefaultAsync();
                if (dt_anticipo != null)
                {
                    monto_anticipo = dt_anticipo.monto ?? 0;
                }
                else
                {
                    monto_anticipo = 0;
                }
            }
            catch (Exception)
            {
                monto_anticipo = 0;
            }

            try
            {
                var dt = await (from p1 in _context.veproforma_anticipo
                                join p2 in _context.veproforma on p1.codproforma equals p2.codigo
                                join p3 in _context.coanticipo on p1.codanticipo equals p3.codigo
                                where p3.id == id_anticipo && p3.numeroid == nroid_anticipo &&
                                      p2.anulada == false && p1.codproforma != codproforma
                                select new
                                {
                                    p1,
                                    p2.id,
                                    p2.numeroid,
                                    p2.nomcliente,
                                    p2.codmoneda,
                                    codmoneda_anticipo = p3.codmoneda,
                                    p3.fecha,
                                    docanticipo = "",
                                    p2.anulada,
                                    id_anticipo = p3.id,
                                    nroid_anticipo = p3.numeroid,
                                    p3.para_venta_contado,
                                    monto_anticipo = p3.monto
                                }).ToListAsync();

                decimal monto = 0;
                foreach (var reg in dt)
                {
                    if (reg.codmoneda == reg.codmoneda_anticipo)
                    {
                        //No convertir el monto a asignar
                        ttl_aplicado += reg.p1.monto ?? 0;
                    }
                    else
                    {
                        //si la moneda del anticipo y proforma no son iguales entonces convertir el monto asignar a la moneda del anticipo
                        monto = await tipoCambio._conversion(_context, reg.codmoneda_anticipo, reg.codmoneda, reg.fecha, reg.p1.monto ?? 0);
                        monto = Math.Round(monto, 2);
                        ttl_aplicado += monto;
                    }
                }
                resultado = monto_anticipo - ttl_aplicado;
                if (resultado < 0)
                {
                    resultado = 0;
                }
                resultado = Math.Round(resultado, 2);

            }
            catch (Exception)
            {
                resultado = 0;
            }

            return resultado;
        }

        public async Task<decimal> Montorest_De_Anticipo_Cobranza_Anticipo(DBContext _context, string id_anticipo, int nroid_anticipo, string codigoempresa)
        {
            decimal resultado = 0;
            //string qry = "";
            decimal monto_anticipo = 0;
            decimal ttl_aplicado = 0;

            try
            {
                var dt_anticipo = await _context.coanticipo.Where(i => i.id == id_anticipo && i.numeroid == nroid_anticipo)
                    .Select(i => new
                    {
                        i.monto,
                        i.codmoneda
                    }).FirstOrDefaultAsync();
                if (dt_anticipo != null)
                {
                    monto_anticipo = dt_anticipo.monto ?? 0;
                }
                else
                {
                    monto_anticipo = 0;
                }
            }
            catch (Exception)
            {
                monto_anticipo = 0;
            }

            try
            {
                var dt = await (from p1 in _context.cocobranza_anticipo
                                join p2 in _context.cocobranza on p1.codcobranza equals p2.codigo
                                join p3 in _context.coanticipo on p1.codanticipo equals p3.codigo
                                where p3.id == id_anticipo && p3.numeroid == nroid_anticipo &&
                                      p2.reciboanulado == false
                                select new
                                {
                                    p1,
                                    p2.id,
                                    p2.numeroid,
                                    p2.cliente,
                                    p2.moneda,
                                    docanticipo = "",
                                    p2.reciboanulado,
                                    id_anticipo = p3.id,
                                    nroid_anticipo = p3.numeroid,
                                    p3.para_venta_contado,
                                    monto_anticipo = p3.monto
                                }).ToListAsync();

                foreach (var reg in dt)
                {
                    ttl_aplicado += reg.p1.monto ?? 0;
                }
                resultado = monto_anticipo - ttl_aplicado;
                if (resultado < 0)
                {
                    resultado = 0;
                }
                resultado = Math.Round(resultado, 2);

            }
            catch (Exception)
            {
                resultado = 0;
            }

            return resultado;
        }

        public static async Task<double> Total_Anticipos_Aplicados_A_Remision(DBContext _context, int codremision, string moneda)
        {
            TipoCambio tipoCambio = new TipoCambio();
            var dt = await _context.veremision
                .Where(p1 => p1.anulada == false && p1.codigo == codremision)
                .Join(_context.veproforma.Where(p2 => p2.anulada == false),
                      p1 => p1.codproforma,
                      p2 => p2.codigo,
                      (p1, p2) => new { p1, p2 })
                .Join(_context.veproforma_anticipo,
                      p => p.p2.codigo,
                      p3 => p3.codproforma,
                      (p, p3) => new { p.p1, p.p2, p3 })
                .Join(_context.coanticipo.Where(p4 => p4.anulado == false),
                      p => p.p3.codanticipo,
                      p4 => p4.codigo,
                      (p, p4) => new
                      {
                          p4.id,
                          p4.numeroid,
                          p4.monto,
                          p4.codmoneda,
                          monto_dist = p.p3.monto,
                          idpf = p.p2.id,
                          nroidpf = p.p2.numeroid,
                          fpf = p.p2.fecha,
                          p.p2.codcliente,
                          p.p2.idanticipo,
                          p.p2.numeroidanticipo,
                          idnr = p.p1.id,
                          nroidnr = p.p1.numeroid,
                          fnr = p.p1.fecha,
                          ttl_remision = p.p1.total,
                          p.p1.codalmacen,
                          p.p1.fecha
                      })
                .OrderBy(p => p.id)
                .ThenBy(p => p.numeroid)
                .ToListAsync();

            double ttl_dist = 0;
            foreach (var reg in dt)
            {
                if (reg.codmoneda == moneda)
                {
                    ttl_dist += (double)(reg.monto_dist ?? 0);
                }
                else
                {
                    double monto_cambio = (double)await tipoCambio._conversion(_context, moneda, reg.codmoneda, reg.fnr, reg.monto_dist ?? 0);
                    monto_cambio = Math.Round(monto_cambio, 2);
                    ttl_dist += monto_cambio;
                }
            }
            return ttl_dist;
        }
        public async Task<List<vedetalleanticipo>> Anticipos_MontoRestante_Sin_Deposito(DBContext _context, string codcliente)
        {
            var dt_anticipos = await _context.coanticipo
                .Where(x => x.codcliente == codcliente && x.anulado == false && x.montorest > 0 && x.deposito_cliente == false)
                .Select(x => new vedetalleanticipo
                {
                    codigo = x.codigo,
                    id = x.id,
                    numeroid = x.numeroid,
                    codvendedor = x.codvendedor,
                    fecha = x.fecha,
                    codcliente = x.codcliente,
                    monto = (double)(x.monto ?? 0),
                    codmoneda = x.codmoneda,
                    montorest = (double)(x.montorest ?? 0),
                    deposito_cliente = (bool)x.deposito_cliente,
                    iddeposito = x.iddeposito,
                    numeroiddeposito = (int)x.numeroiddeposito
                }).ToListAsync();
            return dt_anticipos;
        }
        public async Task<string> Proformas_Aplicadas_cadena(DBContext _context, string idanticipo, int nroidanticipo)
        {
            var proformas = await _context.veproforma
                .Join(_context.veproforma_anticipo,
                      p1 => p1.codigo,
                      p2 => p2.codproforma,
                      (p1, p2) => new { p1, p2 })
                .Join(_context.coanticipo,
                      combined => combined.p2.codanticipo,
                      p3 => p3.codigo,
                      (combined, p3) => new { combined.p1, combined.p2, p3 })
                .Where(result => result.p3.id == idanticipo
                              && result.p3.numeroid == nroidanticipo
                              && result.p1.anulada == false)
                .OrderBy(result => result.p1.id)
                .ThenBy(result => result.p1.numeroid)
                .Select(result => new
                {
                    result.p1.id,
                    result.p1.numeroid
                })
                .ToListAsync();
            string resultado = "";
            foreach (var reg in proformas)
            {
                resultado = reg.id + "-" + reg.numeroid + ", ";
            }
            return resultado;
        }
        public async Task<string> Cobranzas_Aplicadas_cadena(DBContext _context, string idanticipo, int nroidanticipo)
        {
            var cobranzas = await _context.cocobranza
                .Join(_context.cocobranza_anticipo,
                      p1 => p1.codigo,
                      p2 => p2.codcobranza,
                      (p1, p2) => new { p1, p2 })
                .Join(_context.coanticipo,
                      combined => combined.p2.codanticipo,
                      p3 => p3.codigo,
                      (combined, p3) => new { combined.p1, combined.p2, p3 })
                .Where(result => result.p3.id == idanticipo
                              && result.p3.numeroid == nroidanticipo
                              && result.p1.reciboanulado == false)
                .OrderBy(result => result.p1.id)
                .ThenBy(result => result.p1.numeroid)
                .Select(result => new
                {
                    result.p1.id,
                    result.p1.numeroid
                })
                .ToListAsync();
            string resultado = "";
            foreach (var reg in cobranzas)
            {
                resultado = reg.id + "-" + reg.numeroid + ", ";
            }
            return resultado;
        }

    }
}
