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
        private readonly Empresa empresa = new Empresa();
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
        public async Task<double> Redondear_SIAT(DBContext _context, string codempresa, double minumero)
        {
            double resultado = 0;
            // El Servicio de Impuestos Nacionales utiliza en la emisión de facturas electrónicas en linea montos 
            // expresados con dos decimales y utiliza el redondeo tradicional o HALF-UP. En este caso, el redondeo 
            // se realiza al número superior cuando el decimal sea igual o superior a 5 y al número inferior 
            // cuando el decimal sea igual o inferior a 5.
            // Ej:

            // 3.14159  será redondeado a 3.14
            // 3.14559  será redondeado a 3.15
            int CODALMACEN = await empresa.AlmacenLocalEmpresa(_context, codempresa);
            int _codDocSector = await _context.adsiat_parametros_facturacion.Where(i => i.codalmacen == CODALMACEN).Select(I => I.tipo_doc_sector).FirstOrDefaultAsync() ?? -1;

            if (_codDocSector == 1)
            {
                // 35 : FACTURA COMPRA VENTA (2 DECIMALES)
                resultado = Math.Round(minumero, 2, MidpointRounding.AwayFromZero);
            }
            else if(_codDocSector == 35)
            {
                // 35 : FACTURA COMPRA VENTA BONIFICACIONES(5 DECIMALES) 
                resultado = Math.Round(minumero, 5, MidpointRounding.AwayFromZero);
            }
            else if (_codDocSector == 24)
            {
                // 35 : notas de credito (2 DECIMALES)
                resultado = Math.Round(minumero, 5, MidpointRounding.AwayFromZero);
            }
            else
            {
                resultado = Math.Round(minumero, 2, MidpointRounding.AwayFromZero);
            }
            return resultado;
        }


    }
}
