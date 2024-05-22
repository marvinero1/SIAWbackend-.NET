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

    Public Async Function DecryptData(ByVal inName As String) As Task(Of String)
        Dim cdk As New System.Security.Cryptography.PasswordDeriveBytes("P@$$w0rd", System.Text.Encoding.ASCII.GetBytes("pertec"))
        Dim iv As Byte() = {0, 0, 0, 0, 0, 0, 0, 0}
        Dim key As Byte() = cdk.GetBytes(64 / 8)
        Dim IV2 As Byte() = {21, 22, 23, 24, 25, 26, 27, 28}

        ' Crear el flujo de archivos para manejar el archivo de entrada.
        Using fin As New System.IO.FileStream(inName, System.IO.FileMode.Open, System.IO.FileAccess.Read)
            ' Crear variables para ayudar con la lectura y escritura.
            Dim bin(4096) As Byte ' Almacenamiento intermedio para la desencriptación.
            Dim rdlen As Long = 0 ' Número total de bytes escritos.
            Dim totlen As Long = fin.Length ' Longitud total del archivo de entrada.
            Dim len As Integer ' Número de bytes que se escribirán a la vez.
            Dim des As New System.Security.Cryptography.DESCryptoServiceProvider
            Dim decryptedBytes As New List(Of Byte)()

            Using encStream As New System.Security.Cryptography.CryptoStream(fin, des.CreateDecryptor(key, IV2), System.Security.Cryptography.CryptoStreamMode.Read)
                ' Leer del archivo de entrada, luego desencriptar.
                While rdlen < totlen
                    len = Await encStream.ReadAsync(bin, 0, 4096)
                    If len = 0 Then Exit While
                    decryptedBytes.AddRange(bin.Take(len))
                    rdlen += len
                End While
            End Using

            ' Convertir los bytes desencriptados a texto usando UTF-8.
            Dim decryptedText As String = System.Text.Encoding.UTF8.GetString(decryptedBytes.ToArray())
            Return decryptedText
        End Using
    End Function




End Class
