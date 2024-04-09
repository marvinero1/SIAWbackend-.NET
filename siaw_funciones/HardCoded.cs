using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class HardCoded
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
        private Almacen almacen = new Almacen();
        public async Task<decimal> MaximoPorcentajeDeVentaPorMercaderia(DBContext _context, int codalmacen)
        {
            decimal resultado = 0;

            //using (_context)
            ////using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            if (await almacen.Es_Tienda(_context,codalmacen))
            {
                resultado = 999;
            }
            else
            {
                resultado = 30;
            }

            //}
            return resultado;
        }
    }
}
