using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class SIAT
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


        public async Task<double> Redondeo_Decimales_SIA_5_decimales_SQL(float minumero)
        {
            double resultado = Math.Round(minumero, 5);
            return resultado;
        }
        public async Task<double> Redondeo_Decimales_SIA_2_decimales_SQL(float minumero)
        {
            double resultado = Math.Round(minumero, 2);
            return resultado;
        }
    }
}
