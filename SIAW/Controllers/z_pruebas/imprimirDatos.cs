using System.ComponentModel;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Text;

namespace SIAW.Controllers.z_pruebas
{
    public class imprimirDatos
    {
        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool OpenPrinter(string printerName, out IntPtr printerHandle, IntPtr defaultSecurityAttributes);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool StartDocPrinter(IntPtr printerHandle, int level, [In] ref DOC_INFO_1 docInfo);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr printerHandle);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool WritePrinter(IntPtr printerHandle, [In] byte[] data, int length, out int written);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr printerHandle);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr printerHandle);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr printerHandle);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class DOC_INFO_1
        {
            public string DocName;
            public string OutputFile;
            public string DataType;
        }

        public static bool SendFileToPrinter(string printerName, string fileName)
        {
            IntPtr printerHandle;
            DOC_INFO_1 docInfo = new DOC_INFO_1
            {
                DocName = "Raw Document",
                OutputFile = null,
                DataType = "RAW"
            };

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                byte[] bytes = br.ReadBytes((int)fs.Length);

                if (!OpenPrinter(printerName, out printerHandle, IntPtr.Zero))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }

                try
                {
                    if (!StartDocPrinter(printerHandle, 1, ref docInfo))
                    {
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    }

                    if (!StartPagePrinter(printerHandle))
                    {
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    }

                    int bytesWritten;

                    if (!WritePrinter(printerHandle, bytes, bytes.Length, out bytesWritten))
                    {
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    }

                    EndPagePrinter(printerHandle);
                    EndDocPrinter(printerHandle);
                }
                finally
                {
                    ClosePrinter(printerHandle);
                }
            }

            return true;
        }
    }
}
