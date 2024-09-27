using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

public class CRC32
{
    // Esta es v2 del algoritmo CRC32 de Paul
    private readonly int[] crc32Table;
    private const int BUFFER_SIZE = 1024;

    public CRC32()
    {
        // Este es el polinomio oficial usado por CRC32 en PKZip.
        // Generalmente el polinomio se muestra en reverso (04C11DB7).
        const int dwPolynomial = unchecked((int)0xEDB88320);
        crc32Table = new int[256];

        for (int i = 0; i < 256; i++)
        {
            int dwCrc = i;
            for (int j = 8; j > 0; j--)
            {
                if ((dwCrc & 1) == 1)
                {
                    dwCrc = (dwCrc >> 1) ^ dwPolynomial;
                }
                else
                {
                    dwCrc >>= 1;
                }
            }
            crc32Table[i] = dwCrc;
        }
    }

    public int GetCrc32(Stream stream)
    {
        int crc32Result = unchecked((int)0xFFFFFFFF);
        byte[] buffer = new byte[BUFFER_SIZE];
        int readSize = BUFFER_SIZE;

        int count;
        while ((count = stream.Read(buffer, 0, readSize)) > 0)
        {
            for (int i = 0; i < count; i++)
            {
                int iLookup = (crc32Result & 0xFF) ^ buffer[i];
                crc32Result = ((crc32Result >> 8) & 0xFFFFFF) ^ crc32Table[iLookup];
            }
        }

        return ~crc32Result; // Notar el resultado final
    }
}
