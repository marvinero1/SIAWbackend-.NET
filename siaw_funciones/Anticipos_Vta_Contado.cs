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
                    moneda_anticipo = await empresa.monedabase(_context, codigoempresa);
                }
            }
            catch (Exception)
            {
                monto_anticipo = 0;
                moneda_anticipo = await empresa.monedabase(_context, codigoempresa);
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
    }
}
