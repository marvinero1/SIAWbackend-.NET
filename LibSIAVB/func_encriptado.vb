Imports System.IO
Imports System.Security.Cryptography
Imports System.Text

Public Class func_encriptado




    Public Async Function EncryptData(ByVal plainText As String, ByVal outName As String) As Task

        Dim cdk As New System.Security.Cryptography.PasswordDeriveBytes("P@$$w0rd", System.Text.Encoding.ASCII.GetBytes("pertec"))
        Dim iv As Byte() = {0, 0, 0, 0, 0, 0, 0, 0}
        Dim key As Byte() = cdk.GetBytes(64 / 8)
        Dim IV2 As Byte() = {21, 22, 23, 24, 25, 26, 27, 28}

        ' Convertir el texto plano a bytes usando UTF-8
        Dim plainBytes As Byte() = Encoding.UTF8.GetBytes(plainText)

        ' Crear el archivo de salida
        Using fout As New FileStream(outName, FileMode.OpenOrCreate, FileAccess.Write)
            fout.SetLength(0)

            ' Crear el proveedor de servicios DES
            Using des As New DESCryptoServiceProvider()
                ' Crear el stream de encriptación
                Using encStream As New CryptoStream(fout, des.CreateEncryptor(key, IV2), CryptoStreamMode.Write)
                    ' Escribir bytes encriptados en el archivo de salida
                    Await encStream.WriteAsync(plainBytes, 0, plainBytes.Length)
                End Using
            End Using
        End Using
    End Function

    Public Async Function DecryptData(ByVal inName As String, ByVal outName As String) As Task
        Dim cdk As New System.Security.Cryptography.PasswordDeriveBytes("P@$$w0rd", System.Text.Encoding.ASCII.GetBytes("pertec"))
        Dim iv As Byte() = {0, 0, 0, 0, 0, 0, 0, 0}
        Dim key As Byte() = cdk.GetBytes(64 / 8)
        Dim IV2 As Byte() = {21, 22, 23, 24, 25, 26, 27, 28}

        'Create the file streams to handle the input and output files.
        Dim fin As New System.IO.FileStream(inName, System.IO.FileMode.Open, System.IO.FileAccess.Read)
        Dim fout As New System.IO.FileStream(outName, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write)
        fout.SetLength(0)

        'Create variables to help with read and write.
        Dim bin(4096) As Byte 'This is intermediate storage for the encryption.
        Dim rdlen As Long = 0 'This is the total number of bytes written.
        Dim totlen As Long = fin.Length 'Total length of the input file.
        Dim len As Integer 'This is the number of bytes to be written at a time.
        Dim des As New System.Security.Cryptography.DESCryptoServiceProvider
        Dim encStream As New System.Security.Cryptography.CryptoStream(fout, des.CreateDecryptor(key, IV2), System.Security.Cryptography.CryptoStreamMode.Write)
        '        Console.WriteLine("Encrypting...")
        'Read from the input file, then encrypt and write to the output file.
        While rdlen < totlen
            len = Await fin.ReadAsync(bin, 0, 4096)
            Await encStream.WriteAsync(bin, 0, len)
            rdlen = Convert.ToInt32(rdlen + len / des.BlockSize * des.BlockSize)
            '            Console.WriteLine("Processed {0} bytes, {1} bytes total", len, rdlen)
        End While
        encStream.Close()
        fin.Close()
        fout.Close()
    End Function




End Class
