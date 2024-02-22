using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
