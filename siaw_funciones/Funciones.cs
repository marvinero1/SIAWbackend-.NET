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
using System.Net.Http;
using Microsoft.Data.SqlClient;
using siaw_DBContext.Models;
using System.Drawing;

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

            var connection = _context.Database.GetDbConnection();

            // await connection.OpenAsync();

            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                // Reutilizar la conexión para múltiples comandos
                using (var command = connection.CreateCommand())
                {
                    // Obtener la fecha actual en formato 'yyyy-MM-dd'
                    command.CommandText = "SELECT FORMAT(GETDATE(), 'yyyy-MM-dd') AS FechaActual";
                    var result = await command.ExecuteScalarAsync();

                    if (result != null)
                    {
                        resultado = (DateTime)result;
                        Console.WriteLine($"Fecha actual: {resultado}"); // Muestra la fecha en formato yyyy-MM-dd
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            /*
            finally
            {
                // Cerrar la conexión después de su uso
                await connection.CloseAsync();
            }
            */
            return resultado.Date;
        }
        public async Task<string> hora_del_servidor_cadena(DBContext _context)
        {
            string resultado = "";
            var connection = _context.Database.GetDbConnection();

            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                // Reutilizar esta conexión para múltiples comandos
                using (var command = connection.CreateCommand())
                {
                    // Primer comando
                    command.CommandText = "SELECT DATEPART(HOUR, GETDATE())";
                    var hourResult = await command.ExecuteScalarAsync();
                    int hour = Convert.ToInt32(hourResult);

                    // Segundo comando
                    command.CommandText = "SELECT DATEPART(MINUTE, GETDATE())";
                    var minuteResult = await command.ExecuteScalarAsync();
                    int minute = Convert.ToInt32(minuteResult);

                    // Formato final
                    resultado = $"{hour:D2}:{minute:D2}";
                    Console.WriteLine(resultado);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            /*
            finally
            {
                // Siempre cerrar la conexión cuando termines
                await connection.CloseAsync();
            }
            */
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







        public async Task<bool> Verificar_Conexion_Internet()
        {
            // Retorna True si existe conexion a la pagina solicitada
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5); // Establecer un tiempo de espera
                    HttpResponseMessage response = await client.GetAsync("https://www.google.com/");
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
















        ///////////////////////////// ENVIAR EMAILS
        public async Task<bool> EnviarEmail(string emailDestino, List<string>? emailsCC, string emailOrigenCredencial, string pwdEmailCredencialOrigen, string tituloMail, string cuerpoMail, byte[] pdfBytes, string nombreArchivo)
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
                if (emailOrigenCredencial.EndsWith("@pertec.com.bo", StringComparison.OrdinalIgnoreCase))  // para que envie por Gmail
                {
                    smtpServer.Host = "smtp.gmail.com";
                    smtpServer.Port = 465;
                    smtpServer.EnableSsl = true;
                }
                else if (emailOrigenCredencial.EndsWith("@int.pertec.com.bo", StringComparison.OrdinalIgnoreCase))  // para que envie por titan de hostinger
                {
                    smtpServer.Host = "smtp.titan.email";
                    smtpServer.Port = 587; 
                    smtpServer.EnableSsl = true;
                }
                else
                {
                    throw new Exception("Provedor de correo no soportado.");
                }

                email.From = new MailAddress(emailOrigenCredencial);
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
                        await smtpServer.SendMailAsync(email);
                    }
                }
                else
                {
                    await smtpServer.SendMailAsync(email);
                }
                // Enviar correo
                // await smtpServer.SendMailAsync(email);
                resultado = true;
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"Error SMTP al enviar el correo: {smtpEx.StatusCode} - {smtpEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error general al enviar el correo: {ex.Message}");
                return false;
            }
            return resultado;
        }








        public async Task<string> EnviarEmailAsync(
        string emailOrigen,
        string emailDestino,
        List<string>? emailsCC,
        string emailOrigenCredencial,
        string pwdEmailCredencialOrigen,
        string tituloMail,
        string cuerpoMail,
        byte[] pdfBytes,
        string nombreArchivo)
        {
            try
            {
                // Desencriptar la contraseña si es necesario
                string passwordDesencriptado = EncryptionHelper.DecryptString(pwdEmailCredencialOrigen);

                using (SmtpClient smtpClient = new SmtpClient())
                {
                    // Configurar el servidor SMTP según el dominio del correo electrónico de origen
                    if (emailOrigen.EndsWith("@pertec.com.bo", StringComparison.OrdinalIgnoreCase))
                    {
                        smtpClient.Host = "smtp.gmail.com";
                        smtpClient.Port = 587;
                        smtpClient.EnableSsl = true;
                    }
                    else if (emailOrigen.EndsWith("@int.pertec.com.bo", StringComparison.OrdinalIgnoreCase))
                    {
                        smtpClient.Host = "smtp.titan.email";
                        smtpClient.Port = 587;
                        smtpClient.EnableSsl = true;
                    }
                    else
                    {
                        throw new Exception("Proveedor de correo no soportado.");
                    }

                    smtpClient.Credentials = new NetworkCredential(emailOrigenCredencial, passwordDesencriptado);
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.Timeout = 20000; // Tiempo de espera en milisegundos

                    using (MailMessage email = new MailMessage())
                    {
                        email.From = new MailAddress(emailOrigen);
                        // email.To.Add(new MailAddress(emailDestino));
                        email.Subject = tituloMail;
                        email.Body = cuerpoMail;
                        email.IsBodyHtml = true;

                        // Agregar destinatarios en CC si existen
                        if (emailsCC != null && emailsCC.Count > 0)
                        {
                            foreach (var emailCC in emailsCC)
                            {
                                email.To.Add(new MailAddress(emailCC));
                            }
                        }

                        // Adjuntar archivo PDF si existe
                        if (pdfBytes != null && pdfBytes.Length > 0)
                        {
                            using (var pdfStream = new MemoryStream(pdfBytes))
                            {
                                var attachment = new Attachment(pdfStream, nombreArchivo, "application/pdf");
                                email.Attachments.Add(attachment);

                                await smtpClient.SendMailAsync(email);
                            }
                        }
                        else
                        {
                            await smtpClient.SendMailAsync(email);
                        }
                    }
                }
                /*
                Console.WriteLine("Correo enviado exitosamente.");
                return true;
                */
                return ("Correo enviado exitosamente.");
            }
            catch (SmtpException smtpEx)
            {
                /*
                Console.WriteLine($"Error SMTP al enviar el correo: {smtpEx.StatusCode} - {smtpEx.Message}");
                return false;
                */
                return ($"Error SMTP al enviar el correo: {smtpEx.StatusCode} - {smtpEx.Message}");
            }
            catch (Exception ex)
            {
                /*
                Console.WriteLine($"Error general al enviar el correo: {ex.Message}");
                return false;
                */
                return ($"Error general al enviar el correo: {ex.Message}");
            }
        }


        public async Task<(bool result, string msg)> EnviarEmailFacturas(string emailDestino, string emailOrigenCredencial, string pwdEmailCredencialOrigen, string tituloMail, string cuerpoMail, byte[]? pdfBytes, string nombreArchivo, byte[]? xmlBytes, string nombreArchivoXml, bool adjuntaArchivos)
        {
            bool resultado = true;
            string msg = "";
            try
            {
                SmtpClient smtpServer = new SmtpClient("smtp.gmail.com", 587);
                smtpServer.Credentials = new NetworkCredential(emailOrigenCredencial, pwdEmailCredencialOrigen);
                smtpServer.EnableSsl = true;
                smtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpServer.Timeout = 10000; // Tiempo de espera en milisegundos

                MailMessage email = new MailMessage();

                // smtpServer.UseDefaultCredentials = false;
                // pwdEmailCredencialOrigen = EncryptionHelper.DecryptString(pwdEmailCredencialOrigen);


                // Configurar el servidor SMTP para dominio de Google
                // smtpServer.Host = "smtp.gmail.com";
                // smtpServer.Port = 465;
                // smtpServer.Port = 587;  // Cambiado a 587 para TLS
                // SmtpClient smtpServer = new SmtpClient("smtp.gmail.com", 587); // Usar puerto 587 para TLS


                email.From = new MailAddress(emailOrigenCredencial);
                email.To.Add(emailDestino);
                email.Subject = tituloMail;
                email.IsBodyHtml = false;  // No permitir HTML en el cuerpo del correo
                email.Body = cuerpoMail;

                if (adjuntaArchivos)
                {
                    // Adjuntar archivo PDF
                    if (pdfBytes != null && pdfBytes.Length > 0)
                    {
                        var pdfStream = new MemoryStream(pdfBytes); // No usar 'using' para evitar que se cierre antes de enviar
                        var attachment = new Attachment(pdfStream, nombreArchivo, "application/pdf");
                        email.Attachments.Add(attachment);
                    }
                    // Adjuntar archivo XML
                    if (xmlBytes != null && xmlBytes.Length > 0)
                    {
                        var xmlStream = new MemoryStream(xmlBytes); // No usar 'using'
                        var xmlAttachment = new Attachment(xmlStream, nombreArchivoXml, "application/xml");
                        email.Attachments.Add(xmlAttachment);
                    }

                    try
                    {
                        if (email.Attachments.Count == 2)
                        {
                            await smtpServer.SendMailAsync(email);
                            resultado = true;
                        }
                        else
                        {
                            resultado = false;
                            msg = "No se pudo adjuntar el PDF o XML consulte con el administrador.";
                        }
                    }
                    finally
                    {
                        // Cerrar manualmente los streams después del envío
                        foreach (var attachment in email.Attachments)
                        {
                            if (attachment.ContentStream != null)
                            {
                                attachment.ContentStream.Close();
                                attachment.ContentStream.Dispose();
                            }
                        }
                    }
                }
                else
                {
                    await smtpServer.SendMailAsync(email);
                    resultado = true;
                }
                
                
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"Error SMTP al enviar el correo: {smtpEx.StatusCode} - {smtpEx.Message}");
                return (false, $"Error SMTP al enviar el correo: {smtpEx.StatusCode} - {smtpEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error general al enviar el correo: {ex.Message}");
                return (false, $"Error general al enviar el correo: {ex.Message}");
            }
            return (resultado,msg);
        }












        public async Task<string> Fecha_TimeStamp(DateTime mifecha, string hora)
        {
            string _fecha_convertido = "";
            DateTime mifecha_aux = mifecha.Date;

            string fecha = $"{mifecha_aux:dd/MM/yyyy} {hora}";
            mifecha = DateTime.Parse(fecha);

            // Debe devolver este formato
            // 2022-03-11T08:07:01.404
            _fecha_convertido = mifecha.ToString("yyyy-MM-ddTHH:mm:ss");
            _fecha_convertido += ".999";

            return await Task.FromResult(_fecha_convertido);
        }

        public async Task<DateTime> FechaDelServidor_TimeStamp(DBContext _context)
        {
            DateTime resultado = DateTime.Now;

            var connection = _context.Database.GetDbConnection();

            await connection.OpenAsync();

            try
            {
                // Reutilizar la conexión para múltiples comandos
                using (var command = connection.CreateCommand())
                {
                    // Obtener la fecha actual en formato 'yyyy-MM-dd'
                    command.CommandText = "SELECT GETDATE() AS FechaActual";
                    var result = await command.ExecuteScalarAsync();

                    if (result != null)
                    {
                        resultado = (DateTime)result;
                        Console.WriteLine($"Fecha actual: {resultado}"); // Muestra la fecha en formato yyyy-MM-dd
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                // Cerrar la conexión después de su uso
                await connection.CloseAsync();
            }

            return resultado;
        }

        public string[] CortarCadena_CUF(string cadena, int ancho)
        {
            string resultado = "";
            string resultado2 = "";
            string[] resultadoMatriz = new string[2];

            if (cadena.Length > ancho)
            {
                resultado = cadena.Substring(0, ancho);
            }
            else
            {
                resultado = cadena.Trim();
            }
            resultadoMatriz[0] = resultado;

            if (cadena.Length > ancho)
            {
                resultado2 = cadena.Substring(ancho, cadena.Length - ancho);
            }
            else
            {
                resultado2 = cadena.Trim();
            }
            resultadoMatriz[1] = resultado2;

            return resultadoMatriz;
        }


        public string[] Dividir_cadena_en_filas(string micadena, int ancho)
        {
            string[] cadena;
            string[] resultado = new string[30];
            int i;
            string aux = "";
            int j = 0;

            // Si la cadena es menor o igual al ancho, devolverla directamente en el primer elemento de resultado
            if (micadena.Length <= ancho)
            {
                resultado[j] = micadena;
                return resultado;
            }
            else
            {
                // Dividir la cadena por espacios
                cadena = micadena.Trim().Split(' ');

                for (i = 0; i < cadena.Length; i++)
                {
                    if (i == 0)
                    {
                        resultado[j] = cadena[i];
                    }
                    else
                    {
                        aux = resultado[j] + " " + cadena[i];
                        if (aux.Length <= ancho)
                        {
                            resultado[j] += " " + cadena[i];
                        }
                        else
                        {
                            j++;

                            if (resultado[j] == null)
                            {
                                resultado[j] = cadena[i];
                            }
                            else
                            {
                                resultado[j] += " " + cadena[i];
                            }
                        }
                    }
                }
            }

            return resultado;
        }



    }
}
