using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class GZip
{
    FileStream streamFonte;
    FileStream streamDestino;
    GZipStream streamCompactado;
    GZipStream streamDescompactado;
    public void ComprimirGZIP()
    {
        // Byte array from string.
        byte[] array = Encoding.ASCII.GetBytes(new string('X', 10000));

        // Call Compress.
        byte[] c = CompressGZIP(array);

        // Write bytes.
        File.WriteAllBytes("C:\\compress.gz", c);
    }
    public byte[] CompressGZIP(byte[] raw)
    {
        // Clean up memory with using-statements.
        using (MemoryStream memory = new MemoryStream())
        {
            // Create compression stream.
            using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
            {
                // Write.
                gzip.Write(raw, 0, raw.Length);
            }
            // Return array.
            return memory.ToArray();
        }
    }
    public async Task<byte[]> CompressGZipAsync(string input, Encoding encoding = null)
    {
        encoding ??= Encoding.Unicode; // Asigna Encoding.Unicode si encoding es null
        byte[] bytes = encoding.GetBytes(input);

        using (var stream = new MemoryStream())
        {
            using (var zipStream = new GZipStream(stream, CompressionMode.Compress))
            {
                await zipStream.WriteAsync(bytes, 0, bytes.Length); // Operación de escritura asíncrona
            }
            return stream.ToArray();
        }
    }
    public string DecompressString(byte[] bytes)
    {
        using (MemoryStream ms = new MemoryStream(bytes))
        {
            using (GZipStream ds = new GZipStream(ms, CompressionMode.Decompress))
            {
                using (StreamReader sr = new StreamReader(ds))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
    public byte[] CompressString(string input)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            // Optional: Use CompressionLevel.Optimal if you want a higher compression level
            // using (GZipStream cs = new GZipStream(ms, CompressionLevel.Optimal))
            using (GZipStream cs = new GZipStream(ms, CompressionMode.Compress))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                cs.Write(bytes, 0, bytes.Length);

                // Required to ensure the last chunk is written
                cs.Close();

                return ms.ToArray();
            }
        }
    }
    public bool compactaArchivo(string archivoOrigen, string archivoDestino)
    {
        bool resultado = true;
        FileStream streamFonte = null;
        FileStream streamDestino = null;
        GZipStream streamCompactado = null;

        try
        {
            // ----- Descompacta a string compactada previamente.
            //       Primeiro, cria a entrada do arquivo stream
            streamFonte = new FileStream(archivoOrigen, FileMode.Open, FileAccess.Read);

            // ----- Cria a saída do arquivo stream
            streamDestino = new FileStream(archivoDestino, FileMode.Create, FileAccess.Write);

            // ----- Os bytes serão processados por um compressor de streams
            streamCompactado = new GZipStream(streamDestino, CompressionMode.Compress, true);

            // ----- Processa os bytes de um arquivo para outro
            const int tamanoBloque = 4096;
            byte[] buffer = new byte[tamanoBloque];
            int bytesLidos;

            do
            {
                bytesLidos = streamFonte.Read(buffer, 0, tamanoBloque);
                if (bytesLidos == 0) break;
                streamCompactado.Write(buffer, 0, bytesLidos);
            } while (true);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error Servidor: " + ex.Message);
            resultado = false;
        }
        finally
        {
            // ----- Fecha todos os streams
            resultado = true; // Esto parece un poco redundante, ya que lo estableces en true nuevamente aquí
            streamFonte?.Close();
            streamCompactado?.Close();
            streamDestino?.Close();
        }

        return resultado;
    }
    public async Task<bool> CompactaArchivoAsync(string archivoOrigen, string archivoDestino)
    {
        try
        {
            using (var streamFuente = new FileStream(archivoOrigen, FileMode.Open, FileAccess.Read))
            using (var streamDestino = new FileStream(archivoDestino, FileMode.Create, FileAccess.Write))
            using (var streamCompactado = new GZipStream(streamDestino, CompressionMode.Compress, true))
            {
                const int tamanoBloque = 4096;
                byte[] buffer = new byte[tamanoBloque];
                int bytesLeidos;

                while ((bytesLeidos = await streamFuente.ReadAsync(buffer, 0, tamanoBloque)) > 0) // Lectura asíncrona
                {
                    await streamCompactado.WriteAsync(buffer, 0, bytesLeidos); // Escritura asíncrona
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            // Manejo de excepciones
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    public void descompactaArquivo(string archivoOrigen, string archivoDestino)
    {
        // ----- Compacata o contéudo do arquivo e
        //       guarda o resultado em um novo arquivo
        FileStream streamFonte = null;
        FileStream streamDestino = null;
        GZipStream streamDescompactado = null;

        try
        {
            streamFonte = new FileStream(archivoOrigen, FileMode.Open, FileAccess.Read);
            streamDestino = new FileStream(archivoDestino, FileMode.Create, FileAccess.Write);

            // ----- Os bytes serão processados através de um decompressor de stream
            streamDescompactado = new GZipStream(streamFonte, CompressionMode.Decompress, true);

            // ----- Processa os bytes de um arquivo para outro
            const int tamanoBloque = 4096;
            byte[] buffer = new byte[tamanoBloque];
            int bytesLidos;

            do
            {
                bytesLidos = streamDescompactado.Read(buffer, 0, tamanoBloque);
                if (bytesLidos == 0) break;
                streamDestino.Write(buffer, 0, bytesLidos);
            } while (true);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error Servidor: " + ex.Message);
        }
        finally
        {
            // ----- Fecha todos os arquivos
            streamFonte?.Close();
            streamDescompactado?.Close();
            streamDestino?.Close();
        }
    }
    public async Task DescompactaArchivoAsync(string archivoOrigen, string archivoDestino)
    {
        try
        {
            using (var streamFuente = new FileStream(archivoOrigen, FileMode.Open, FileAccess.Read))
            using (var streamDestino = new FileStream(archivoDestino, FileMode.Create, FileAccess.Write))
            using (var streamDescompactado = new GZipStream(streamFuente, CompressionMode.Decompress, true))
            {
                const int tamanoBloque = 4096;
                byte[] buffer = new byte[tamanoBloque];
                int bytesLeidos;

                while ((bytesLeidos = await streamDescompactado.ReadAsync(buffer, 0, tamanoBloque)) > 0) // Lectura asíncrona
                {
                    await streamDestino.WriteAsync(buffer, 0, bytesLeidos); // Escritura asíncrona
                }
            }
        }
        catch (Exception ex)
        {
            // Manejo de excepciones
            Console.WriteLine(ex.Message);
        }
    }

    public byte[] DecompressGZip(byte[] bytesToDecompress)
    {
        using (var stream = new GZipStream(new MemoryStream(bytesToDecompress), CompressionMode.Decompress))
        {
            const int size = 4096;
            byte[] buffer = new byte[size];

            using (var memoryStream = new MemoryStream())
            {
                int count;
                do
                {
                    count = stream.Read(buffer, 0, size);
                    if (count > 0)
                    {
                        memoryStream.Write(buffer, 0, count);
                    }
                } while (count > 0);

                return memoryStream.ToArray();
            }
        }
    }
    public async Task<byte[]> DecompressGZipAsync(byte[] bytesToDecompress)
    {
        using (var stream = new GZipStream(new MemoryStream(bytesToDecompress), CompressionMode.Decompress))
        {
            const int size = 4096;
            byte[] buffer = new byte[size];

            using (var memoryStream = new MemoryStream())
            {
                int count;
                do
                {
                    count = await stream.ReadAsync(buffer, 0, size); // Operación de lectura asíncrona
                    if (count > 0)
                    {
                        await memoryStream.WriteAsync(buffer, 0, count); // Operación de escritura asíncrona
                    }
                } while (count > 0);
                return memoryStream.ToArray();
            }
        }
    }

    //public byte[] CompressGZip(string input, Encoding encoding = null)
    //{
    //    encoding ??= Encoding.Unicode; // Asigna Encoding.Unicode si encoding es null
    //    byte[] bytes = encoding.GetBytes(input);

    //    using (var stream = new MemoryStream())
    //    {
    //        using (var zipStream = new GZipStream(stream, CompressionMode.Compress))
    //        {
    //            zipStream.Write(bytes, 0, bytes.Length);
    //            zipStream.Close(); // Asegura que se complete la compresión antes de convertir el MemoryStream a array
    //        }
    //        return stream.ToArray();
    //    }
    //}


}
