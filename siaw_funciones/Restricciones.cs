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
    public class Restricciones
    {
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
        public async Task<double> empaqueminimo(DBContext _context, string codigo, int codtarifa, int coddescuento)
        {
            // sacar el empaque de tarifa
            var canttarif = await _context.veempaque1
                .Join(_context.intarifa,
                    e => e.codempaque,
                    t => t.codempaque,
                    (e, t) => new { e, t })
                .Where(x => x.e.item == codigo && x.t.codigo == codtarifa)
                .Select(x => x.e.cantidad)
                .FirstOrDefaultAsync() ?? 0;

            // sacar el empaque del descuento
            var cantdesc = await _context.veempaque1
                .Join(_context.vedescuento,
                    e => e.codempaque,
                    d => d.codempaque,
                    (e, d) => new { e, d })
                .Where(x => x.e.item == codigo && x.d.codigo == coddescuento)
                .Select(x => x.e.cantidad)
                .FirstOrDefaultAsync() ?? 0;

            if (cantdesc > canttarif)
            {
                return (double)cantdesc;
            }
            return (double)canttarif;
        }


    }
}
