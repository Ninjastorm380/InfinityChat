Public Class Cryptography
    Public Shared Function EncryptAES256(Data As Byte(), CryptographicKey As String) As Byte()
        Dim SHA256 As New System.Security.Cryptography.SHA256Managed
        Dim AES As New System.Security.Cryptography.AesManaged With {.Key = SHA256.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(CryptographicKey))} : AES.GenerateIV()
        Dim AESCryptoTransform As System.Security.Cryptography.ICryptoTransform = AES.CreateEncryptor
        Dim output As Byte() = Serialization.SerializeArray({AES.IV, AESCryptoTransform.TransformFinalBlock(Data, 0, Data.Length)})
        AESCryptoTransform.Dispose()
        AES.Dispose()
        SHA256.Dispose()
        Return output
    End Function
    Public Shared Function DecryptAES256(Data As Byte(), CryptographicKey As String) As Byte()
        Dim SHA256 As New System.Security.Cryptography.SHA256Managed
        Dim Seperated As Byte()() = Serialization.DeserializeArray(Data)
        Dim IV As Byte() = Seperated(0)
        Dim InputData As Byte() = Seperated(1)

        Dim AES As New System.Security.Cryptography.AesManaged With {.Key = SHA256.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(CryptographicKey)), .IV = IV}
        Dim AESCryptoTransform As System.Security.Cryptography.ICryptoTransform = AES.CreateDecryptor

        Dim Output As Byte() = AESCryptoTransform.TransformFinalBlock(InputData, 0, InputData.Length)

        AESCryptoTransform.Dispose()
        AES.Dispose()
        SHA256.Dispose()
        Return Output
    End Function
End Class
