using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using System;
using System.Collections.Generic;
using System.Data;
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

        /*
        public async Task<double> Redondeo_Decimales_SIA_5_decimales_SQL(double minumero)
        {
            double resultado = Math.Round(minumero, 5);
            return resultado;
        }
        */
        public async Task<decimal> Redondeo_Decimales_SIA_2_decimales_SQL(DBContext context,double numero)
        {
            try
            {
                decimal resultado = 0;

                if (numero == 0 || numero < 0)
                {
                    resultado = 0;
                }
                else
                {
                    decimal preciofinal1 = 0;
                    var redondeado = new SqlParameter("@resultado", SqlDbType.Decimal)
                    {
                        Direction = ParameterDirection.Output,
                        Precision = 18,
                        Scale = 2
                    };
                    await context.Database.ExecuteSqlRawAsync
                        ("EXEC Redondeo_Decimales_SIA_2_decimales_SQL @minumero, @resultado OUTPUT",
                            new SqlParameter("@minumero", numero),
                            redondeado);
                    preciofinal1 = (decimal)(redondeado.Value);
                    resultado = preciofinal1;

                }
                return resultado;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public async Task<decimal> Redondeo_Decimales_SIA_5_decimales_SQL(DBContext context, decimal numero)
        {
            try
            {
                decimal resultado = 0;

                if (numero == 0 || numero < 0)
                {
                    resultado = 0;
                }
                else
                {
                    decimal preciofinal1 = 0;
                    var redondeado = new SqlParameter("@resultado", SqlDbType.Decimal)
                    {
                        Direction = ParameterDirection.Output,
                        Precision = 18,
                        Scale = 5
                    };
                    await context.Database.ExecuteSqlRawAsync
                        ("EXEC Redondeo_Decimales_SIA_5_decimales_SQL @minumero, @resultado OUTPUT",
                            new SqlParameter("@minumero", numero),
                            redondeado);
                    preciofinal1 = (decimal)(redondeado.Value);
                    resultado = preciofinal1;

                }
                return resultado;
            }
            catch (Exception)
            {
                return -1;
            }
        }
        public async Task<int> Nro_Maximo_Items_Factura_Segun_SIAT(DBContext _context, string codempresa)
        {
            try
            {
                int resultado = 0;
                //using (_context)
                //{
                var result = await _context.adsiat_parametros_facturacion
                    .Where(v => v.codempresa == codempresa)
                    .Select(v => v.nro_max_items_factura_siat)
                    .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = result ?? 0;
                }
                else { resultado = 0; }
                //}
                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 0;
            }
        }


    }
}
