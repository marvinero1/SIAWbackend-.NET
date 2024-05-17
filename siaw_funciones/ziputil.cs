using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip;

namespace siaw_funciones
{
    public class ziputil
    {
        public async Task Comprimir(string[] fileNames, string zipFic, bool crearAuto = false)
        {
            // Instancia el objeto Crc32
            Crc32 objCrc32 = new Crc32();
            ZipOutputStream strmZipOutputStream;

            if (string.IsNullOrEmpty(zipFic))
            {
                zipFic = ".";
                crearAuto = true;
            }

            if (crearAuto)
            {
                // Si hay que crear el nombre del fichero
                // este será el path indicado y la fecha actual
                zipFic = Path.Combine(zipFic, "ZIP" + DateTime.Now.ToString("dd-MM-yyyy") + ".zip");
            }

            // Crear el archivo ZIP
            strmZipOutputStream = new ZipOutputStream(File.Create(zipFic));

            // Nivel de compresión: 0-9
            // 0: sin compresión
            // 9: máxima compresión
            strmZipOutputStream.SetLevel(6);

            foreach (string strFile in fileNames)
            {
                if (!string.IsNullOrEmpty(strFile))
                {
                    using (FileStream strmFile = File.OpenRead(strFile))
                    {
                        byte[] abyBuffer = new byte[strmFile.Length];
                        await strmFile.ReadAsync(abyBuffer, 0, abyBuffer.Length);

                        // Guardar sólo el nombre del fichero
                        string sFile = Path.GetFileName(strFile);
                        ZipEntry theEntry = new ZipEntry(sFile)
                        {
                            // Guardar la fecha y hora de la última modificación
                            DateTime = File.GetLastWriteTime(strFile),
                            Size = strmFile.Length
                        };

                        strmFile.Close();
                        objCrc32.Reset();
                        objCrc32.Update(abyBuffer);
                        theEntry.Crc = objCrc32.Value;
                        strmZipOutputStream.PutNextEntry(theEntry);
                        await strmZipOutputStream.WriteAsync(abyBuffer, 0, abyBuffer.Length);
                    }
                }
            }

            strmZipOutputStream.Finish();
            strmZipOutputStream.Close();
        }




        public async Task DescomprimirArchivo(string directorio, string zipFic = "", string archivo = "", bool eliminar = false, bool renombrar = false)
        {
            // Descomprimir el contenido de zipFic en el directorio indicado.
            // Si zipFic no tiene la extensión .zip, se entenderá que es un directorio y
            // se procesará el primer fichero .zip de ese directorio.
            // Si eliminar es True se eliminará ese fichero zip después de descomprimirlo.
            // Si renombrar es True se añadirá al final .descomprimido
            if (!zipFic.ToLower().EndsWith(".zip"))
            {
                zipFic = Directory.GetFiles(zipFic, "*.zip")[0];
            }

            // Si no se ha indicado el directorio, usar el actual
            if (string.IsNullOrEmpty(directorio))
            {
                directorio = ".";
            }

            using (ZipInputStream zipInputStream = new ZipInputStream(File.OpenRead(zipFic)))
            {
                ZipEntry theEntry;
                while ((theEntry = zipInputStream.GetNextEntry()) != null)
                {
                    if (theEntry.Name == archivo)
                    {
                        string fileName = Path.Combine(directorio, Path.GetFileName(theEntry.Name));
                        // Daría error si no existe el path
                        FileStream streamWriter;
                        try
                        {
                            streamWriter = File.Create(fileName);
                        }
                        catch (DirectoryNotFoundException)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                            streamWriter = File.Create(fileName);
                        }

                        byte[] data = new byte[2048];
                        int size;
                        while ((size = await zipInputStream.ReadAsync(data, 0, data.Length)) > 0)
                        {
                            await streamWriter.WriteAsync(data, 0, size);
                        }
                        streamWriter.Close();
                    }
                }
            }

            // Cuando se hayan extraído los ficheros, renombrarlo
            if (renombrar)
            {
                File.Copy(zipFic, zipFic + ".descomprimido");
            }
            if (eliminar)
            {
                File.Delete(zipFic);
            }
        }



        public string ObtenerPrimerArchivoEnZip(string zipFilePath)
        {
            string primerArchivo = null;
            try
            {
                using (ZipFile zipFile = new ZipFile(File.OpenRead(zipFilePath)))
                {
                    if (zipFile.Count > 0)
                    {
                        ZipEntry primerEntrada = zipFile[0]; // Obtener la primera entrada en el archivo ZIP
                        primerArchivo = primerEntrada.Name; // Obtener el nombre del primer archivo en el ZIP
                    }
                }

                return primerArchivo;
            }
            catch (Exception)
            {

                throw;
            }
            
        }


    }
}
