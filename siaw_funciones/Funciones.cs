using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;

namespace siaw_funciones
{
    public class Funciones
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
        // Encriptaciones
        public async Task<string> EncriptarMD5(string clearString)
        {
            System.Text.UnicodeEncoding uEncode = new System.Text.UnicodeEncoding();
            byte[] bytClearString = uEncode.GetBytes(clearString);
            System.Security.Cryptography.MD5CryptoServiceProvider sha = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] hash = sha.ComputeHash(bytClearString);
            return Convert.ToBase64String(hash);
        }




        public async Task<string> SP(DateTime fecha, int hora, int codalmacen, string dato_a, string dato_b, string servicio)
        {
            string resultado = "";
            string cadena = "";
            cadena = dato_a + hora.ToString() + dato_b + fecha.Year.ToString("0000") + servicio + fecha.Month.ToString("00") + codalmacen.ToString() + fecha.Day.ToString("00");
            resultado = await encriptarMD5llave_yea(cadena);
            if (resultado.Length >= 8)
            {
                resultado = resultado.Substring(resultado.Length - 7, 6);
            }
            return resultado.ToUpper();
        }


        public async Task<string> encriptarMD5llave_yea(string ClearString)
        {
            ClearString = "slip" + ClearString + "knot";
            System.Text.UnicodeEncoding uEncode = new System.Text.UnicodeEncoding();
            byte[] bytClearString = uEncode.GetBytes(ClearString);
            System.Security.Cryptography.MD5CryptoServiceProvider sha = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] hash = sha.ComputeHash(bytClearString);
            return Convert.ToBase64String(hash).Substring(1, 8);
        }

        public async Task<DateTime> FechaDelServidor(DBContext _context)
        {
            DateTime resultado = DateTime.Now;


            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                //resultado = _context.FechaServidor.FromSqlRaw("SELECT GETDATE() AS fecha").FirstOrDefault()?.Fecha ?? DateTime.Now;
                var query = await _context.Database.ExecuteSqlRawAsync("SELECT GETDATE() AS fecha");
                resultado = query > 0 ? DateTime.Now : resultado;
                //}
            }
            catch (Exception ex)
            {
                // Manejar la excepción según tus necesidades
                Console.WriteLine($"Error: {ex.Message}");
            }

            return resultado;
        }
        public string Rellenar(string cadena, int ancho, string relleno, bool ElRellenoALaIzquierda = true)
        {
            string cadena2 = "";

            if (ElRellenoALaIzquierda)
            {
                if (cadena.Length > ancho)
                {
                    cadena2 = cadena.Substring(cadena.Length - ancho, ancho);
                }
                else if (cadena.Length == ancho)
                {
                    cadena2 = cadena;
                }
                else
                {
                    for (int i = 1; i <= (ancho - cadena.Length); i++)
                    {
                        cadena2 += relleno;
                    }
                    cadena2 += cadena;
                }
            }
            else
            {
                if (cadena.Length > ancho)
                {
                    cadena2 = cadena.Substring(0, ancho);
                }
                else if (cadena.Length == ancho)
                {
                    cadena2 = cadena;
                }
                else
                {
                    for (int i = 1; i <= (ancho - cadena.Length); i++)
                    {
                        cadena2 = relleno + cadena2;
                    }
                    cadena2 = cadena + cadena2;
                }
            }

            return cadena2;
        }
        public double LimpiarDoble(object valor)
        {
            if (valor == DBNull.Value || valor == null)
            {
                return 0;
            }
            else
            {
                return Convert.ToDouble(valor);
            }
        }
        public DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);

            // Get all the properties
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in Props)
            {
                // Setting column names as Property names
                dataTable.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            foreach (T item in items)
            {
                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    // Setting values for the columns
                    values[i] = Props[i].GetValue(item, null);
                }

                // Adding values to DataTable
                dataTable.Rows.Add(values);
            }

            // Return the datatable
            return dataTable;
        }

        public bool EsNumero(string cadena)
        {
            bool resultado = true;
            cadena = cadena.Trim();
            if (cadena == "")
            {
                resultado = false;
            }
            else
            {
                double valor;
                if (!double.TryParse(cadena, out valor))
                {
                    resultado = false;
                }
            }
            return resultado;
        }

    }
}
