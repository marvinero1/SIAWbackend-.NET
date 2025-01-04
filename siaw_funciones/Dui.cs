using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using siaw_DBContext.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class Dui
    {
        public async Task<string> dui_correspondiente(DBContext _context, string coditem, int codalmacen, double cant_actual = 0.0)
        {
            string result = "";
            var dataTable = await _context.inmovimiento.Where(i => i.factor == 1 && i.codalmacen == codalmacen && i.anulada == false)
                .Join(
                    _context.inmovimiento1.Where(d => d.coditem == coditem && !string.IsNullOrWhiteSpace(d.codaduana.Trim())),
                    c => c.codigo,
                    d => d.codmovimiento,
                    (c, d) => new { c.fecha, d.coditem, d.codaduana, d.cantidad }
                    )
                .GroupBy(
                    x => new { x.coditem, x.fecha, x.codaduana },
                    x => x.cantidad,
                    (key, cantidades) => new objectDui
                    {
                        coditem = key.coditem,
                        fecha = key.fecha,
                        codaduana = key.codaduana,
                        total = cantidades.Sum()
                    }

                    )
                .OrderBy(r => r.fecha)
                .ToListAsync();
            checked
            {
                if (dataTable.Count() > 0)
                {
                    double num = 0.0;
                    num = await Salidas(_context, coditem, codalmacen, dataTable[0].fecha.Date);
                    num -= cant_actual;
                    int num2 = dataTable.Count() - 1;
                    int num3 = 0;
                    while (true)
                    {
                        int num4 = num3;
                        int num5 = num2;
                        if (num4 > num5)
                        {
                            break;
                        }
                        if (num > (double)dataTable[num3].total)
                        {
                            num -= (double)dataTable[num3].total;
                            dataTable[num3].total = 0;
                            num3++;
                            continue;
                        }
                        dataTable[num3].total = dataTable[num3].total - (decimal)num;
                        result = dataTable[num3].codaduana.ToString();
                        break;
                    }
                }
            }
            return result;
        }

        public async Task<double> Salidas(DBContext _context, string coditem, int codalmacen, DateTime desde)
        {
            double num = 0.0;
            try
            {
                double num2 = 0.0;
                double num3 = 0.0;
                double num4 = 0.0;
                double num5 = 0.0;
                num2 = await SalidasNotaMovimiento(_context, coditem, codalmacen, desde.Date);
                num3 = await SalidasFactura(_context, coditem, codalmacen, desde.Date);
                num4 = await SalidasFacturaKit(_context, coditem, codalmacen, desde.Date);
                num5 = await EntradasNotaCredito(_context, coditem, codalmacen, desde.Date);
                num = num2 + num3 + num4 - num5;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al calcular salidas: " + ex.Message);
                num = 0.0;
            }
            return num;
        }

        public async Task<double> SalidasNotaMovimiento(DBContext _context, string coditem, int codalmacen, DateTime desde)
        {
            double num = 0.0;
            try
            {
                num = (double)await _context.inmovimiento
                    .Where(c => c.factor == -1
                        && c.codalmacen == codalmacen
                        && c.anulada == false
                        && c.fecha >= desde.Date)
                    .Join(
                        _context.inmovimiento1.Where(d => d.coditem == coditem),
                        c => c.codigo,
                        d => d.codmovimiento,
                        (c,d) => d.cantidad
                    )
                    .SumAsync();
                if (Information.IsDBNull(num))
                {
                    num = 0.0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error salidas notas de movimiento: " + ex.Message);
                num = 0.0;
            }
            return num;
        }

        public async Task<double> SalidasFactura(DBContext _context, string coditem, int codalmacen, DateTime desde)
        {
            double num = 0.0;
            try
            {
                num = (double) await _context.vefactura
                    .Where(c => c.codalmacen == codalmacen
                        && c.anulada == false
                        && c.fecha >= desde.Date)
                    .Join(
                        _context.vefactura1.Where(d => d.coditem == coditem),
                        c => c.codigo,
                        d => d.codfactura,
                        (c, d) => d.cantidad
                    ).SumAsync();
                if (Information.IsDBNull(num))
                {
                    num = 0.0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error salidas facturas: " + ex.Message);
                num = 0.0;
            }
            return num;
        }

        public async Task<double> SalidasFacturaKit(DBContext _context, string coditem, int codalmacen, DateTime desde)
        {
            double num = 0.0;
            try
            {
                num = (double) await _context.vefactura
                    .Where(c => c.codalmacen == codalmacen
                        && c.anulada == false
                        && c.fecha == desde.Date)
                    .Join(
                        _context.vefactura1,
                        c => c.codigo,
                        d => d.codfactura,
                        (c,d) => new {c,d})
                    .Join(
                        _context.inkit,
                        cd => cd.d.coditem,
                        t => t.codigo,
                        (cd, t) => cd.d.cantidad * t.cantidad)
                    .SumAsync();
                if (Information.IsDBNull(num))
                {
                    num = 0.0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error salidas facturas Kit: " + ex.Message);
                num = 0.0;
            }
            return num;
        }

        public async Task<double> EntradasNotaCredito(DBContext _context, string coditem, int codalmacen, DateTime desde)
        {
            double num = 0.0;
            try
            {
                num = (double) await _context.venotacredito
                    .Where(c => c.codalmacen == codalmacen
                        && c.anulada == false
                        && c.fecha >= desde.Date)
                    .Join(
                        _context.venotacredito1,
                        c => c.codigo,
                        d => d.codnotacredito,
                        (c,d) => new {c,d}
                        )
                    .Where(cd => cd.d.coditem == coditem)
                    .Select( cd => cd.d.cantidad )
                    .SumAsync();
                if (Information.IsDBNull(num))
                {
                    num = 0.0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error entrada notas credito: " + ex.Message);
                num = 0.0;
            }
            return num;
        }
    }

    public class objectDui
    {
        public string coditem { get; set; }
        public DateTime fecha { get; set; }
        public string codaduana { get; set; }
        public decimal total { get; set; }
    }
}
