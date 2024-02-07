using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;

namespace siaw_funciones
{
    public class Cobranzas
    {
        Configuracion configuracion = new Configuracion();
        //Clase necesaria para el uso del DBContext del proyecto siaw_Context
        public static class DbContextFactory
        {
            public static DBContext Create(string connectionString)
            {
                var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
                optionsBuilder.UseSqlServer(connectionString);

                return new DBContext(optionsBuilder.Options);
            }
        }


        public async Task<bool> Registrar_Descuento_Por_Deposito_de_Cbza(DBContext _context, string codcobranza, string codcliente, string codcliente_real, string nit, string codproforma, string cod_empresa, string usuarioreg)
        {
            DateTime Depositos_Desde_Fecha = new DateTime(2015, 5, 13);

            // obtener las distribuciones de la cbza, y validar si las distribuciones o paggo estan dentro las fechas permitadas respecto del vencimiento


            return true;
        }

        public async Task<bool> Depositos_Cobranzas_Credito_Cliente_Sin_Aplicar(DBContext _context, string BusquedaPor, string CodCbzas, string codcliente, string nit, string codcliente_real, bool buscar_por_nit, string para_que_es, int codproforma, string opcion, string codempresa, bool incluir_aplicados, DateTime nuevos_depositos_desde, bool para_proforma)
        {
            int coddesextra_deposito = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            // var dtdesc_deposito_negativo 
            return true;
        }



        public async Task<List<consultCocobranza>> Consulta_Deposito_Cobranzas_Credito_Sin_Aplicar (DBContext _context, string busqueda_por, string codcliente, string nit, string codcliente_real, bool buscar_por_nit, string Para_Que_Es, string codcbzas, bool incluir_aplicados, DateTime buscar_depositos_desde)
        {
            if (!buscar_por_nit)
            {
                nit = "";
            }
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //consulta busca los despositos(cbzas) que ya se usaron para aplicar descuento por deposito 
            //pero tienen: saldorest por aplicar de descuento
            //y estan en la tabla: cocobranza_deposito  (estos no son reversiones de anticipos)
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //tipo=0 SON SALDOS PENDIENTES DE DEPOSITOS YA APLICADOS

            // PARTE 1/6

            // Paso 1: Unir cocobranza_deposito y cocobranza
            var query1 = await _context.cocobranza_deposito.Join(
                _context.cocobranza,
                p1 => p1.codcobranza,
                p2 => p2.codigo,
                (p1, p2) => new { p1, p2 }
            )
                .Join(
                _context.fndeposito_cliente,
                join => new { id = join.p2.iddeposito, numeroid = join.p2.numeroiddeposito ?? -1 },
                p3 => new { id = p3.id, numeroid = p3.numeroid },
                (join, p3) => new { join.p1, join.p2, p3 }
            )
               .Where(result => result.p2.reciboanulado == false &&
                                 !_context.cocobranza_anticipo.Any(anticipo => anticipo.codcobranza == result.p1.codcobranza))
                .OrderBy(result => result.p1.codcobranza)
                .Select(result => new consultCocobranza
                {
                    nro = "1",
                    tipo = "0",
                    aplicado = result.p1.aplicado ?? false,
                    codalmacen = result.p2.codalmacen ?? 0,
                    codcobranza = result.p1.codcobranza,

                    idcbza = result.p2.id,
                    nroidcbza = result.p2.numeroid,
                    fecha_cbza = result.p2.fecha,
                    fdeposito = result.p2.fecha,
                    cliente = result.p2.cliente,

                    nit = result.p3.nit,
                    nomcliente_nit = result.p3.nomcliente_nit,
                    monto_cbza = result.p1.montodist,
                    monto_dis = result.p1.montodescto,
                    moncbza = result.p2.moneda,

                    reciboanulado = result.p2.reciboanulado,
                    iddeposito = result.p2.iddeposito,
                    numeroiddeposito = result.p2.numeroiddeposito ?? 0,
                    deposito_cliente = result.p2.deposito_cliente ?? false,
                    contra_entrega = false,

                    codremision = 0,
                    idrem = "",
                    nroidrem = 0,
                    fecha_remi = result.p2.fecha,
                    nrocuota = 0,

                    vencimiento = result.p2.fecha,
                    vencimiento_cliente = result.p2.fecha,
                    monto = 0,
                    montopagado = 0,
                    deposito_en_mora_habilita_descto = true,

                    codcliente = result.p3.codcliente,
                    codigo = result.p2.codigo
                }).ToListAsync(); ;

            
            
            if (!incluir_aplicados)
            {
                query1 = query1.Where(i => i.aplicado == false).ToList();
            }

            if (busqueda_por == "cliente")
            {
                if (Para_Que_Es == "APLICAR_DESCTO")
                {
                    query1 = query1.Where(i => i.codcliente == codcliente_real).ToList();
                }
                else
                {
                    query1 = query1.Where(i => i.codcliente == codcliente_real).ToList();
                }
            }
            else
            {
                query1 = query1.Where(x => codcbzas.Contains((char)x.codigo)).ToList();
            }
            
            return query1;

            // PARTE 2/6
            var query2 = await _context.cocobranza_deposito
                .Join(_context.cocobranza,
                    p1 => p1.codcobranza,
                    p2 => p2.codigo,
                    (p1, p2) => new { p1, p2 })
                .Join(_context.cocobranza_anticipo,
                    p => p.p1.codcobranza,
                    p4 => p4.codcobranza,
                    (p, p4) => new { p.p1, p.p2, p4 })
                .Join(_context.coanticipo,
                    pp => pp.p4.codanticipo,
                    p5 => p5.codigo,
                    (pp, p5) => new { pp.p1, pp.p2, pp.p4, p5 })
                .Join(_context.fndeposito_cliente,
                    ppp => new { id = ppp.p5.iddeposito, numeroid= ppp.p5.numeroiddeposito ?? -1 },
                    p3 => new { p3.id, p3.numeroid },
                    (ppp, p3) => new { ppp.p1, ppp.p2, ppp.p4, ppp.p5, p3 })
                .Where(joined =>
                    joined.p2.reciboanulado == false)
                .OrderBy(joined => joined.p1.codcobranza)
                .Select(joined => new consultCocobranza
                {
                    nro = "2",
                    tipo = "0",
                    aplicado = joined.p1.aplicado ?? false,
                    codalmacen = joined.p2.codalmacen ?? 0,
                    codcobranza = joined.p1.codcobranza,
                    idcbza = joined.p2.id,
                    nroidcbza = joined.p2.numeroid,
                    fecha_cbza = joined.p2.fecha,
                    fdeposito = joined.p2.fecha,
                    cliente = joined.p2.cliente,
                    nit = joined.p3.nit,
                    nomcliente_nit = joined.p3.nomcliente_nit,
                    monto_cbza = joined.p1.montodist,
                    monto_dis = joined.p1.montodescto,
                    moncbza = joined.p2.moneda,
                    reciboanulado = joined.p2.reciboanulado,
                    iddeposito = joined.p5.iddeposito,
                    numeroiddeposito = joined.p5.numeroiddeposito ?? 0,
                    deposito_cliente = joined.p5.deposito_cliente ?? false,
                    contra_entrega = false,
                    codremision = 0,
                    idrem = "",
                    nroidrem = 0,
                    fecha_remi = joined.p2.fecha,
                    nrocuota = 0,
                    vencimiento = joined.p2.fecha,
                    vencimiento_cliente = joined.p2.fecha,
                    monto = 0,
                    montopagado = 0,
                    deposito_en_mora_habilita_descto = true,

                    codcliente = joined.p3.codcliente,
                    codigo = joined.p2.codigo
                }).ToListAsync();

            if (!incluir_aplicados)
            {
                query2 = query2.Where(i => i.aplicado == false).ToList();
            }

            if (busqueda_por == "cliente")
            {
                if (Para_Que_Es == "APLICAR_DESCTO")
                {
                    query2 = query2.Where(i => i.codcliente == codcliente_real).ToList();
                }
                else
                {
                    query2 = query2.Where(i => i.codcliente == codcliente_real).ToList();
                }
            }
            else
            {
                query2 = query2.Where(x => codcbzas.Contains((char)x.codigo)).ToList();
            }


            // parte 3/6
            
            var query3 = await _context.cocobranza
                .Join(_context.copagos, p1 => p1.codigo, p2 => p2.codcobranza, (p1, p2) => new { p1, p2 })
                .Join(_context.coplancuotas, p => p.p2.codcuota, p3 => p3.codigo, (p, p3) => new { p.p1, p.p2, p3 })
                .Join(_context.veremision, p => p.p3.coddocumento, p4 => p4.codigo, (p, p4) => new { p.p1, p.p2, p.p3, p4 })
                .Join(_context.fndeposito_cliente,
                      p => new { Id = p.p1.iddeposito, NumeroId = p.p1.numeroiddeposito ?? -1 },
                      p5 => new { Id = p5.id, NumeroId = p5.numeroid },
                      (p, p5) => new { p.p1, p.p2, p.p3, p.p4, p5 })
                .Join(_context.vecliente, p => p.p1.cliente, p6 => p6.codigo, (p, p6) => new { p.p1, p.p2, p.p3, p.p4, p.p5, p6 })
                .Where(joined => joined.p1.reciboanulado == false
                    && joined.p4.anulada == false
                    && joined.p5.verificado == true
                    && joined.p1.deposito_cliente == true
                    && joined.p1.iddeposito != ""
                    && joined.p1.numeroiddeposito != 0
                    && !_context.cocobranza_anticipo.Select(ca => ca.codcobranza).Distinct().Contains(joined.p1.codigo)
                    && joined.p4.fecha >= new DateTime(2015, 5, 13)
                    && !_context.cocobranza_deposito.Select(cd => cd.codcobranza).Distinct().Contains(joined.p1.codigo))
                .OrderBy(joined => joined.p2.codcobranza)
                .Select(joined => new consultCocobranza
                {
                    nro = "3",
                    tipo = "1",
                    aplicado = false,
                    codalmacen = joined.p1.codalmacen ?? 0,
                    codcobranza = joined.p1.codigo,
                    idcbza = joined.p1.id,
                    nroidcbza = joined.p1.numeroid,
                    fecha_cbza = joined.p1.fecha,
                    fdeposito = joined.p5.fecha ?? new DateTime(1900, 1, 1),
                    cliente = joined.p1.cliente,
                    nit = joined.p5.nit,
                    nomcliente_nit = joined.p5.nomcliente_nit,
                    monto_cbza = joined.p1.monto ?? 0,
                    monto_dis = joined.p2.monto,
                    moncbza = joined.p1.moneda,
                    reciboanulado = joined.p1.reciboanulado,
                    iddeposito = joined.p1.iddeposito,
                    numeroiddeposito = joined.p1.numeroiddeposito ?? 0,
                    deposito_cliente = joined.p1.deposito_cliente ?? false,
                    contra_entrega = joined.p4.contra_entrega ?? false,

                    codremision = joined.p4.codigo,
                    idrem = joined.p4.id,
                    nroidrem = joined.p4.numeroid,
                    fecha_remi = joined.p4.fecha,
                    nrocuota = joined.p3.nrocuota,
                    vencimiento = joined.p3.vencimiento,
                    vencimiento_cliente = joined.p3.vencimiento,
                    monto = joined.p3.monto,
                    montopagado = joined.p3.montopagado ?? 0,
                    deposito_en_mora_habilita_descto = joined.p5.deposito_en_mora_habilita_descto ?? false,

                    codcliente = joined.p5.codcliente,
                    codigo = joined.p1.codigo
                }).ToListAsync();
            if (busqueda_por == "cliente")
            {
                if (Para_Que_Es == "APLICAR_DESCTO")
                {
                    query3 = query3.Where(i => i.codcliente == codcliente_real).ToList();
                    //filtrar solo depositos desde fecha
                    query3 = query3.Where(i => i.fecha_cbza >= buscar_depositos_desde).ToList();
                }
                else
                {
                    query3 = query3.Where(i => i.codcliente == codcliente_real).ToList();
                }
            }
            else
            {
                query3 = query3.Where(x => codcbzas.Contains((char)x.codigo)).ToList();
            }

            // Parte 4/6
            var query4 = await _context.cocobranza
                .Join(_context.copagos, p1 => p1.codigo, p2 => p2.codcobranza, (p1, p2) => new { p1, p2 })
                .Join(_context.veremision, p => p.p2.codremision, p4 => p4.codigo, (p, p4) => new { p.p1, p.p2, p4 })
                .Join(_context.fndeposito_cliente,
                      p => new { Id = p.p1.iddeposito, NumeroId = p.p1.numeroiddeposito ?? -1 },
                      p5 => new { Id = p5.id, NumeroId = p5.numeroid },
                      (p, p5) => new { p.p1, p.p2, p.p4, p5 })
                .Join(_context.vecliente, p => p.p1.cliente, p6 => p6.codigo, (p, p6) => new { p.p1, p.p2, p.p4, p.p5, p6 })
                .Where(joined => joined.p1.reciboanulado == false
                    && joined.p4.anulada == false
                    && joined.p5.verificado == true
                    && joined.p1.deposito_cliente == true
                    && joined.p1.iddeposito != ""
                    && joined.p1.numeroiddeposito != 0
                    && !_context.cocobranza_anticipo.Select(ca => ca.codcobranza).Distinct().Contains(joined.p1.codigo)
                    && joined.p4.fecha >= new DateTime(2015, 5, 13)
                    && !_context.cocobranza_deposito.Select(cd => cd.codcobranza).Distinct().Contains(joined.p1.codigo))
                .OrderBy(joined => joined.p2.codcobranza)
                .Select(joined => new consultCocobranza
                {
                    nro = "4",
                    tipo = "1",
                    aplicado = false,
                    codalmacen = joined.p1.codalmacen ?? 0,
                    codcobranza = joined.p1.codigo,
                    idcbza = joined.p1.id,
                    nroidcbza = joined.p1.numeroid,
                    fecha_cbza = joined.p1.fecha,
                    fdeposito = joined.p5.fecha ?? new DateTime(1900, 1, 1),
                    cliente = joined.p1.cliente,
                    nit = joined.p5.nit,
                    nomcliente_nit = joined.p5.nomcliente_nit,
                    monto_cbza = joined.p1.monto ?? 0,
                    monto_dis = joined.p2.monto,
                    moncbza = joined.p1.moneda,
                    reciboanulado = joined.p1.reciboanulado,
                    iddeposito = joined.p1.iddeposito,
                    numeroiddeposito = joined.p1.numeroiddeposito ?? 0,
                    deposito_cliente = joined.p1.deposito_cliente ?? false,
                    contra_entrega = joined.p4.contra_entrega ?? false,
                    codremision = joined.p4.codigo,
                    idrem = joined.p4.id,
                    nroidrem = joined.p4.numeroid,
                    fecha_remi = joined.p4.fecha,
                    nrocuota = 1,
                    vencimiento = joined.p4.fecha,
                    vencimiento_cliente = joined.p4.fecha,
                    monto = joined.p4.total,
                    montopagado = 0,
                    deposito_en_mora_habilita_descto = joined.p5.deposito_en_mora_habilita_descto ?? false,

                    codcliente = joined.p5.codcliente,
                    codigo = joined.p1.codigo
                }).ToListAsync();

            if (busqueda_por == "cliente")
            {
                if (Para_Que_Es == "APLICAR_DESCTO")
                {
                    query4 = query4.Where(i => i.codcliente == codcliente_real).ToList();
                    //filtrar solo depositos desde fecha
                    query4 = query4.Where(i => i.fecha_cbza >= buscar_depositos_desde).ToList();
                }
                else
                {
                    query4 = query4.Where(i => i.codcliente == codcliente_real).ToList();
                }
            }
            else
            {
                query4 = query4.Where(x => codcbzas.Contains((char)x.codigo)).ToList();
            }


            // Parte 5/6
            var query5 = await _context.cocobranza
                .Join(_context.copagos, p1 => p1.codigo, p2 => p2.codcobranza, (p1, p2) => new { p1, p2 })
                .Join(_context.coplancuotas, p12 => p12.p2.codcuota, p3 => p3.codigo, (p12, p3) => new { p12.p1, p12.p2, p3 })
                .Join(_context.veremision, p123 => p123.p3.coddocumento, p4 => p4.codigo, (p123, p4) => new { p123.p1, p123.p2, p123.p3, p4 })
                .Join(_context.coanticipo, p1234 => p1234.p4.codigo.ToString(), p5 => p5.iddeposito.ToString(), (p1234, p5) => new { p1234.p1, p1234.p2, p1234.p3, p1234.p4, p5 })
                .Join(_context.cocobranza_anticipo, p12345 => p12345.p5.codigo, p6 => p6.codanticipo, (p12345, p6) => new { p12345.p1, p12345.p2, p12345.p3, p12345.p4, p12345.p5, p6 })
                .Join(_context.fndeposito_cliente, p123456 => p123456.p5.id, p7 => p7.id, (p123456, p7) => new { p123456.p1, p123456.p2, p123456.p3, p123456.p4, p123456.p5, p123456.p6, p7 })
                .Join(_context.vecliente, p1234567 => p1234567.p1.cliente, p8 => p8.codigo, (p1234567, p8) => new { p1234567.p1, p1234567.p2, p1234567.p3, p1234567.p4, p1234567.p5, p1234567.p6, p1234567.p7, p8 })

         


                .Where(p => p.p1.reciboanulado == false &&
                            p.p4.anulada == false &&
                            p.p5.anulado == false &&
                            p.p7.verificado == true &&
                            p.p5.deposito_cliente == true &&
                            p.p5.iddeposito != "" &&
                            p.p5.numeroiddeposito != 0 &&
                            p.p4.fecha >= new DateTime(2015, 5, 13) &&
                            !_context.cocobranza_deposito.Select(cd => cd.codcobranza).Distinct().Contains(p.p1.codigo))
                .Select(p => new consultCocobranza
                {
                    nro = "5",
                    tipo = "2",
                    aplicado = false,
                    codalmacen = p.p1.codalmacen ?? 0,
                    codcobranza = p.p1.codigo,
                    idcbza = p.p1.id,
                    nroidcbza = p.p1.numeroid,
                    fecha_cbza = p.p1.fecha,
                    fdeposito = p.p7.fecha ?? new DateTime(1900, 1, 1),
                    cliente = p.p1.cliente,
                    nit = p.p7.nit,
                    nomcliente_nit = p.p7.nomcliente_nit,
                    monto_cbza = p.p1.monto ?? 0,
                    monto_dis = p.p2.monto,
                    moncbza = p.p1.moneda,
                    reciboanulado = p.p1.reciboanulado,
                    iddeposito = p.p5.iddeposito,
                    numeroiddeposito = p.p5.numeroiddeposito ?? 0,
                    deposito_cliente = p.p5.deposito_cliente ?? false,
                    contra_entrega = p.p4.contra_entrega ?? false,
                    codremision = p.p4.codigo,
                    idrem = p.p4.id,
                    nroidrem = p.p4.numeroid,
                    fecha_remi = p.p4.fecha,
                    nrocuota = p.p3.nrocuota,
                    vencimiento = p.p3.vencimiento,
                    vencimiento_cliente = p.p3.vencimiento,
                    monto = p.p3.monto,
                    montopagado = p.p3.montopagado ?? 0,
                    deposito_en_mora_habilita_descto = p.p7.deposito_en_mora_habilita_descto ?? false,

                    codcliente = p.p7.codcliente,
                    codigo = p.p1.codigo
                }).ToListAsync();
            if (busqueda_por == "cliente")
            {
                if (Para_Que_Es == "APLICAR_DESCTO")
                {
                    query5 = query5.Where(i => i.codcliente == codcliente_real).ToList();
                    //filtrar solo depositos desde fecha
                    query5 = query5.Where(i => i.fecha_cbza >= buscar_depositos_desde).ToList();
                }
                else
                {
                    query5 = query5.Where(i => i.codcliente == codcliente_real).ToList();
                }
            }
            else
            {
                query5 = query5.Where(x => codcbzas.Contains((char)x.codigo)).ToList();
            }



            // Parte 6/6

            var query6 = await _context.cocobranza
                .Join(_context.copagos, p1 => p1.codigo, p2 => p2.codcobranza, (p1, p2) => new { p1, p2 })
                .Join(_context.veremision, join1 => join1.p2.codremision, p4 => p4.codigo, (join1, p4) => new { join1, p4 })
                .Join(_context.coanticipo, join2 => new { join2.p4.id, join2.p4.numeroid }, p5 => new { id = p5.iddeposito, numeroid = p5.numeroiddeposito??-1 }, (join2, p5) => new { join2, p5 })
                .Join(_context.cocobranza_anticipo, join3 => new { join3.p5.codigo }, p6 => new { codigo = p6.codanticipo??-1 }, (join3, p6) => new { join3, p6 })
                .Join(_context.fndeposito_cliente, join4 => new { join4.join3.p5.id, join4.join3.p5.numeroid }, p7 => new { p7.id, p7.numeroid }, (join4, p7) => new { join4, p7 })
                .Join(_context.vecliente, join5 => join5.join4.join3.join2.join1.p1.cliente, p8 => p8.codigo, (join5, p8) => new { join5, p8 })
                .Where(joined =>
                    joined.join5.join4.join3.join2.join1.p1.reciboanulado == false &&
                    joined.join5.join4.join3.join2.p4.anulada == false &&
                    joined.join5.join4.join3.p5.anulado == false &&
                    joined.join5.p7.verificado == true &&
                    joined.join5.join4.join3.p5.deposito_cliente == true &&
                    !string.IsNullOrEmpty(joined.join5.join4.join3.p5.iddeposito) &&
                    joined.join5.join4.join3.p5.numeroiddeposito != 0 &&
                    joined.join5.join4.join3.join2.p4.fecha >= new DateTime(2015, 5, 13) &&
                    !_context.cocobranza_deposito.Select(c => c.codcobranza).Distinct().Contains(joined.join5.join4.join3.join2.join1.p1.codigo))
                .Select(joined => new consultCocobranza
                {
                    nro = "6",
                    tipo = "2",
                    aplicado = false,
                    codalmacen = joined.join5.join4.join3.join2.join1.p1.codalmacen ?? 0,
                    codcobranza = joined.join5.join4.join3.join2.join1.p1.codigo,
                    idcbza = joined.join5.join4.join3.join2.join1.p1.id,
                    nroidcbza = joined.join5.join4.join3.join2.join1.p1.numeroid,
                    fecha_cbza = joined.join5.join4.join3.join2.join1.p1.fecha,
                    fdeposito = joined.join5.p7.fecha ?? new DateTime(1900, 1, 1),
                    cliente = joined.join5.join4.join3.join2.join1.p1.cliente,
                    nit = joined.join5.p7.nit,
                    nomcliente_nit = joined.join5.p7.nomcliente_nit,
                    monto_cbza = joined.join5.join4.join3.join2.join1.p1.monto ?? 0,
                    monto_dis = joined.join5.join4.join3.join2.join1.p2.monto,
                    moncbza = joined.join5.join4.join3.join2.join1.p1.moneda,
                    reciboanulado = joined.join5.join4.join3.join2.join1.p1.reciboanulado,
                    iddeposito = joined.join5.join4.join3.p5.iddeposito,
                    numeroiddeposito = joined.join5.join4.join3.p5.numeroiddeposito ?? 0,
                    deposito_cliente = joined.join5.join4.join3.p5.deposito_cliente ?? false,

                    contra_entrega = false,

                    codremision = joined.join5.join4.join3.join2.p4.codigo,
                    idrem = joined.join5.join4.join3.join2.p4.id,
                    nroidrem = joined.join5.join4.join3.join2.p4.numeroid,
                    fecha_remi = joined.join5.join4.join3.join2.p4.fecha,
                    nrocuota = 1,
                    vencimiento = joined.join5.join4.join3.join2.p4.fecha,
                    vencimiento_cliente = joined.join5.join4.join3.join2.p4.fecha,
                    monto = joined.join5.join4.join3.join2.p4.total,
                    montopagado = 0,
                    deposito_en_mora_habilita_descto = joined.join5.p7.deposito_en_mora_habilita_descto ?? false,

                    codcliente = joined.join5.p7.codcliente,
                    codigo = joined.join5.join4.join3.join2.join1.p1.codigo
                }).ToListAsync();

            if (busqueda_por == "cliente")
            {
                if (Para_Que_Es == "APLICAR_DESCTO")
                {
                    query6 = query6.Where(i => i.codcliente == codcliente_real).ToList();
                    //filtrar solo depositos desde fecha
                    query6 = query6.Where(i => i.fecha_cbza >= buscar_depositos_desde).ToList();
                }
                else
                {
                    query6 = query6.Where(i => i.codcliente == codcliente_real).ToList();
                }
            }
            else
            {
                query6 = query6.Where(x => codcbzas.Contains((char)x.codigo)).ToList();
            }


            var resultadosUnion = query1
                .Union(query2)
                .Union(query3)
                .Union(query4)
                .Union(query5)
                .Union(query6)
                .OrderBy(x => x.tipo)
                .ThenBy(x => x.fecha_cbza)
                .ToList();

            return resultadosUnion;

        }
    }
}
