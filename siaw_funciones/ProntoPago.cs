using siaw_DBContext.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using siaw_DBContext.Models;

namespace siaw_funciones
{
    public class ProntoPago
    {
        //Clase necesaria para el uso del DBContext del proyecto siaw_Context
        private readonly Ventas ventas = new Ventas();
        public static class DbContextFactory
        {
            public static DBContext Create(string connectionString)
            {
                var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
                optionsBuilder.UseSqlServer(connectionString);

                return new DBContext(optionsBuilder.Options);
            }
        }

        public async Task<bool> DianoHabil(DBContext _context, int codalmacen, DateTime fecha)
        {
            if (fecha.DayOfWeek == DayOfWeek.Sunday)
            {
                return true;
            }
            var resultado = await _context.penohabil.Where(i => i.codalmacen == codalmacen && i.fecha == fecha).CountAsync();
            if (resultado > 0)
            {
                return true;
            }
            return false;
        }

        public static async Task<int> DiasDescuentoProntoPagoNota(DBContext _context,int codremision)
        {
            Ventas ventas = new Ventas();
            ProntoPago prontoPago = new ProntoPago();
            var lista = await ventas.id_nroid_remision2(_context,codremision);
            return await prontoPago.DiasDescuentoProntoPagoNota(_context, lista.id, lista.numeroId,codremision);
        }
        public async Task<int> DiasDescuentoProntoPagoNota(DBContext _context, string id, int numeroid, int codremision)
        {
            string Cliente_De_Remision = await ventas.Cliente_De_Remision(_context, codremision);
            int coddesextra = await NRCodigoDescuentoPP(_context,id, numeroid);
            int diasPP_cliente = 0;
            if (coddesextra > 0)
            {
                diasPP_cliente = await _context.vecliente_desextra
                    .Where(v => v.coddesextra == coddesextra && v.codcliente == Cliente_De_Remision)
                    .Select(v => v.dias)
                    .FirstOrDefaultAsync() ?? 0;
            }
            return diasPP_cliente;
        }

        public static async Task<int> NRCodigoDescuentoPP(DBContext _context, string id, int numeroid)
        {
            int resultado = 0;
            try
            {
                resultado = await _context.vedesextraremi
                .Join(_context.vedesextra,
                      d => d.coddesextra,
                      e => e.codigo,
                      (d, e) => new { d, e })
                .Where(joined => joined.e.prontopago == true
                    && _context.veremision.Any(v => v.id == id && v.numeroid == numeroid && v.codigo == joined.d.codremision))
                .Select(joined => joined.d.coddesextra)
                .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                resultado = 0;
            }
            return resultado;


        }
    }
}
