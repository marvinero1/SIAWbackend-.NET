using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using Humanizer;
using System.Globalization;

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

            return resultado.Date;
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
        public string CentrarCadena(string cadena, int ancho, string relleno)
        {
            string resultado = "";
            int restante, adelante, atras;

            if (cadena.Length > ancho)
            {
                resultado = cadena.Substring(0, ancho - 1);
            }
            else if (cadena.Length == ancho)
            {
                resultado = cadena;
            }
            else
            {
                restante = ancho - cadena.Length;
                adelante = (int)Math.Floor((double)restante / 2);
                atras = restante - adelante;
                resultado = Rellenar(cadena, adelante + cadena.Length, relleno);
                resultado = Rellenar(resultado, resultado.Length + atras, relleno, false);
            }

            return resultado;
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


        public DateTime PrincipioDeSemana(DateTime fecha)
        {
            DateTime resultado = DateTime.MinValue;
            switch (fecha.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    resultado = fecha;
                    break;
                case DayOfWeek.Monday:
                    resultado = fecha.AddDays(-1);
                    break;
                case DayOfWeek.Tuesday:
                    resultado = fecha.AddDays(-2);
                    break;
                case DayOfWeek.Wednesday:
                    resultado = fecha.AddDays(-3);
                    break;
                case DayOfWeek.Thursday:
                    resultado = fecha.AddDays(-4);
                    break;
                case DayOfWeek.Friday:
                    resultado = fecha.AddDays(-5);
                    break;
                case DayOfWeek.Saturday:
                    resultado = fecha.AddDays(-6);
                    break;
            }
            return resultado;
        }
        public DateTime FinDeSemana(DateTime fecha)
        {
            DateTime resultado = DateTime.MinValue;
            switch (fecha.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    resultado = fecha.AddDays(6);
                    break;
                case DayOfWeek.Monday:
                    resultado = fecha.AddDays(5);
                    break;
                case DayOfWeek.Tuesday:
                    resultado = fecha.AddDays(4);
                    break;
                case DayOfWeek.Wednesday:
                    resultado = fecha.AddDays(3);
                    break; 
                case DayOfWeek.Thursday:
                    resultado = fecha.AddDays(2);
                    break;
                case DayOfWeek.Friday:
                    resultado = fecha.AddDays(1);
                    break;
                case DayOfWeek.Saturday:
                    resultado = fecha;
                    break;
            }
            return resultado;
        }


        /*

        public string EncryptData(string xmlText, byte[] desKey, byte[] desIV)
        {
            // Convertir la cadena de texto XML en bytes
            byte[] xmlBytes = Encoding.UTF8.GetBytes(xmlText);

            using (MemoryStream inputStream = new MemoryStream(xmlBytes))
            {
                using (MemoryStream outputStream = new MemoryStream())
                {
                    // Create variables to help with read and write.
                    byte[] bin = new byte[4096]; // This is intermediate storage for the encryption.
                    long rdlen = 0; // This is the total number of bytes written.
                    long totlen = inputStream.Length; // Total length of the input stream.
                    int len; // This is the number of bytes to be written at a time.
                    using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                    {
                        using (CryptoStream encStream = new CryptoStream(outputStream, des.CreateEncryptor(desKey, desIV), CryptoStreamMode.Write))
                        {
                            // Read from the input stream, then encrypt and write to the output stream.
                            while (rdlen < totlen - 1)
                            {
                                len = inputStream.Read(bin, 0, 4096);
                                encStream.Write(bin, 0, len);
                                rdlen = Convert.ToInt32(rdlen + len / des.BlockSize * des.BlockSize);
                            }
                        }
                    }

                    // Convertir los datos encriptados a una cadena de texto
                    return outputStream.ToString();
                    byte[] encryptedBytes = outputStream.ToArray();
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }
        */


        public async Task EncryptData(string xmlText, string outName, byte[] desKey, byte[] desIV)
        {
            // Convertir la cadena de texto XML en bytes
            byte[] xmlBytes = Encoding.UTF8.GetBytes(xmlText);

            using (MemoryStream inputStream = new MemoryStream(xmlBytes))
            {
                using (FileStream outputStream = new FileStream(outName, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    outputStream.SetLength(0);

                    // Create variables to help with read and write.
                    byte[] bin = new byte[4096]; // This is intermediate storage for the encryption.
                    long rdlen = 0; // This is the total number of bytes written.
                    long totlen = inputStream.Length; // Total length of the input stream.
                    int len; // This is the number of bytes to be written at a time.
                    using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                    {
                        using (CryptoStream encStream = new CryptoStream(outputStream, des.CreateEncryptor(desKey, desIV), CryptoStreamMode.Write))
                        {
                            // Read from the input stream, then encrypt and write to the output stream.
                            while (rdlen < totlen - 1)
                            {
                                len = await inputStream.ReadAsync(bin, 0, 4096);
                                await  encStream.WriteAsync(bin, 0, len);
                                rdlen = Convert.ToInt32(rdlen + len / des.BlockSize * des.BlockSize);
                            }
                        }
                    }
                }
            }
        }


        public static string DecryptData(string encryptedText, byte[] desKey, byte[] desIV)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

            using (MemoryStream inputStream = new MemoryStream(encryptedBytes))
            {
                using (MemoryStream outputStream = new MemoryStream())
                {
                    // Create variables to help with read and write.
                    byte[] bin = new byte[4096]; // This is intermediate storage for the decryption.
                    long rdlen = 0; // This is the total number of bytes written.
                    long totlen = inputStream.Length; // Total length of the input stream.
                    int len; // This is the number of bytes to be written at a time.
                    using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                    {
                        using (CryptoStream decStream = new CryptoStream(outputStream, des.CreateDecryptor(desKey, desIV), CryptoStreamMode.Write))
                        {
                            // Read from the input stream, then decrypt and write to the output stream.
                            while (rdlen < totlen)
                            {
                                len = inputStream.Read(bin, 0, 4096);
                                decStream.Write(bin, 0, len);
                                rdlen = Convert.ToInt32(rdlen + len / des.BlockSize * des.BlockSize);
                            }
                        }
                    }

                    // Convertir los datos desencriptados a una cadena de texto
                    byte[] decryptedBytes = outputStream.ToArray();
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }

        public async Task DecryptData(string inName, string outName, byte[] desKey, byte[] desIV)
        {
            // Create the file streams to handle the input and output files.
            using (var fin = new System.IO.FileStream(inName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            using (var fout = new System.IO.FileStream(outName, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write))
            {
                fout.SetLength(0);

                // Create variables to help with read and write.
                byte[] bin = new byte[4096]; // This is intermediate storage for the encryption.
                long rdlen = 0; // This is the total number of bytes written.
                long totlen = fin.Length; // Total length of the input file.
                int len; // This is the number of bytes to be written at a time.
                len = 1;
                using (var des = new System.Security.Cryptography.DESCryptoServiceProvider())
                using (var encStream = new System.Security.Cryptography.CryptoStream(fout, des.CreateDecryptor(desKey, desIV), System.Security.Cryptography.CryptoStreamMode.Write))
                {
                    // Read from the input file, then encrypt and write to the output file.
                    while (rdlen < totlen && len <= 0)
                    {
                        len = await fin.ReadAsync(bin, 0, 4096);
                        await encStream.WriteAsync(bin, 0, len);
                        rdlen = Convert.ToInt32(rdlen + len / des.BlockSize * des.BlockSize);
                    }
                }
            }
        }

        public string ConvertDecimalToWords(decimal number)
        {
            int integerPart = (int)Math.Truncate(number);
            int decimalPart = (int)((number - integerPart) * 100);

            string integerPartInWords = integerPart.ToWords(new CultureInfo("es")).ToUpper();
            string decimalPartInWords = $"{decimalPart}/100";

            return $"{integerPartInWords} {decimalPartInWords}";
        }

        ///////////////////////////// ENVIAR EMAILS
        public bool EnviarEmail(string emailOrigen, string emailDestino, List<string>? emailsCC, string emailOrigenCredencial, string pwdEmailCredencialOrigen, string tituloMail, string cuerpoMail, byte[] pdfBytes, string nombreArchivo)
        {
            bool resultado = true;
            try
            {
                SmtpClient smtpServer = new SmtpClient();
                MailMessage email = new MailMessage();

                smtpServer.UseDefaultCredentials = false;
                pwdEmailCredencialOrigen = EncryptionHelper.DecryptString(pwdEmailCredencialOrigen);
                smtpServer.Credentials = new NetworkCredential(emailOrigenCredencial, pwdEmailCredencialOrigen);

                // Configurar el servidor SMTP según el dominio del correo electrónico de origen
                if (emailOrigen.EndsWith("@pertec.com.bo", StringComparison.OrdinalIgnoreCase))  // para que envie por Gmail
                {
                    smtpServer.Host = "smtp.gmail.com";
                    smtpServer.Port = 587;
                    smtpServer.EnableSsl = true;
                }
                else if (emailOrigen.EndsWith("@int.pertec.com.bo", StringComparison.OrdinalIgnoreCase))  // para que envie por titan de hostinger
                {
                    smtpServer.Host = "smtp.titan.email";
                    smtpServer.Port = 587; 
                    smtpServer.EnableSsl = true;
                }
                else
                {
                    throw new Exception("Provedor de correo no soportado.");
                }

                email.From = new MailAddress(emailOrigen);
                //email.To.Add(emailDestino);
                email.Subject = tituloMail;
                email.IsBodyHtml = true;  // Permitir HTML en el cuerpo del correo
                email.Body = cuerpoMail;

                // Agregar destinatarios con copias incluidos
                if (emailsCC != null)
                {
                    foreach (var emailCC in emailsCC)
                    {
                        email.To.Add(emailCC);
                    }
                }

                // Adjuntar archivo PDF
                if (pdfBytes != null && pdfBytes.Length > 0)
                {
                    using (var pdfStream = new MemoryStream(pdfBytes, writable: false))
                    {
                        var attachment = new Attachment(pdfStream, nombreArchivo, "application/pdf");
                        email.Attachments.Add(attachment);
                        // Enviar correo
                        smtpServer.Send(email);
                    }
                }
                resultado = true;
            }
            catch (Exception error_t)
            {
                // Aquí puedes registrar el error para mayor detalle
                Console.WriteLine($"Error al enviar el correo: {error_t.Message}");
                resultado = false;
            }
            return resultado;
        }
    }
}
