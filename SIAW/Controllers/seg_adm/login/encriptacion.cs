using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace SIAW.Controllers.seg_adm.login
{
    public class encriptacion
    {
        public string EncryptToMD5Base64(string text)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(text);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                string base64String = Convert.ToBase64String(hashBytes);
                // Verificar que el hash no tenga más de 40 caracteres
                if (base64String.Length > 40)
                {
                    throw new Exception("El resultado cifrado excede los 40 caracteres permitidos.");
                }

                
                return base64String;
            }
        }
    }
}