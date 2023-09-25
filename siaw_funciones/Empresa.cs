using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;

namespace siaw_funciones
{
    public class Empresa
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
        public async Task<bool> ControlarStockSeguridad(string userConnectionString, string codigoempresa)
        {
            bool resultado;
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                //precio unitario del item
                var stock_seguridad = await _context.adparametros
                    .Where(i => i.codempresa == codigoempresa)
                    .Select(i => i.stock_seguridad)
                    .FirstOrDefaultAsync();
                resultado = (bool)stock_seguridad;
            }
            return resultado;
        }
    }
}
