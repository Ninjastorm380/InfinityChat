Public Class Networking
    Public Class QueuedTcpClient : Inherits Net.Sockets.TcpClient
        Private CryptographicKey As String = Nothing
        Public Shadows Property Connected As Boolean = False
        Private Stream As Net.Sockets.NetworkStream = Nothing
        Private ReadQueue As Dictionary(Of Byte(), List(Of Byte()))
        Private WriteQueue As Dictionary(Of Byte(), List(Of Byte()))
        Public Sub New(Address As String, Port As Integer, Key As String)
            MyBase.New(Address, Port)
            Connected = True
            CryptographicKey = Key
            Stream = Me.GetStream
            ReadQueue = New Dictionary(Of Byte(), List(Of Byte()))
            WriteQueue = New Dictionary(Of Byte(), List(Of Byte()))
            Dim QueueThread As New Threading.Thread(AddressOf QueueMethod) : QueueThread.Start()
        End Sub
        Public Sub New(Client As Net.Sockets.Socket, Key As String)
            MyBase.New
            Me.Client = Client
            Connected = True
            CryptographicKey = Key
            Stream = Me.GetStream

            ReadQueue = New Dictionary(Of Byte(), List(Of Byte()))
            WriteQueue = New Dictionary(Of Byte(), List(Of Byte()))
            Dim QueueThread As New Threading.Thread(AddressOf QueueMethod) : QueueThread.Start()
        End Sub
        Public Sub CreateQueue(ID As String)
            Dim ByteID As Byte() = System.Text.ASCIIEncoding.ASCII.GetBytes(ID)
            Try
                If ReadQueue.ContainsKey(ByteID) = False Then
                    ReadQueue.Add(ByteID, New List(Of Byte()))
                End If
                If WriteQueue.ContainsKey(ByteID) = False Then
                    WriteQueue.Add(ByteID, New List(Of Byte()))
                End If
            Catch ex As NullReferenceException
            End Try
        End Sub
        Public Sub CreateQueue(ID As Byte())
            Dim ByteID As Byte() = ID
            Try
                If ReadQueue.ContainsKey(ByteID) = False Then
                    ReadQueue.Add(ByteID, New List(Of Byte()))
                End If
                If WriteQueue.ContainsKey(ByteID) = False Then
                    WriteQueue.Add(ByteID, New List(Of Byte()))
                End If
            Catch ex As NullReferenceException
            End Try
        End Sub
        Private Sub QueueMethod()
            Dim QueueLimiter As New Limiter(150)
            Do While Connected = True

                If Stream.DataAvailable = True Then
                    Dim Input(Available - 1) As Byte
                    Stream.Read(Input, 0, Input.Length)
                    Dim Data As Byte()() = Serialization.DeserializeArray(Cryptography.DecryptAES256(Input, CryptographicKey))
                    If ReadQueue.ContainsKey(Data(0)) = False Then
                        CreateQueue(Data(0))
                    End If
                    ReadQueue(Data(0)).Add(Data(1))
                End If

                For x = 0 To WriteQueue.Keys.Count - 1
                    If WriteQueue(WriteQueue.Keys(x)).Count > 0 Then
                        Dim Data As Byte() = WriteQueue(WriteQueue.Keys(x))(0)
                        Dim Output As Byte() = Cryptography.EncryptAES256(Serialization.SerializeArray({WriteQueue.Keys(x), Data}), CryptographicKey)
                        Stream.Write(Output, 0, Output.Length)
                        WriteQueue(WriteQueue.Keys(x)).RemoveAt(0)
                    End If
                Next

                QueueLimiter.Limit()
            Loop
        End Sub
        Public Sub DestroyQueue(ID As String)
            Dim ByteID As Byte() = System.Text.ASCIIEncoding.ASCII.GetBytes(ID)
            If ReadQueue.ContainsKey(ByteID) = True Then

                ReadQueue.Remove(ByteID)
            End If
            If WriteQueue.ContainsKey(ByteID) = True Then
                WriteQueue.Remove(ByteID)
            End If
        End Sub
        Public Sub DestroyQueue(ID As Byte())
            Dim ByteID As Byte() = ID
            If ReadQueue.ContainsKey(ByteID) = True Then

                ReadQueue.Remove(ByteID)
            End If
            If WriteQueue.ContainsKey(ByteID) = True Then
                WriteQueue.Remove(ByteID)
            End If
        End Sub
        Public Function Read(ID As String) As Byte()
            Dim ByteID As Byte() = System.Text.ASCIIEncoding.ASCII.GetBytes(ID)
            If ReadQueue.ContainsKey(ByteID) = True Then
                Dim Data As Byte() = ReadQueue(ByteID)(0)
                ReadQueue(ByteID).RemoveAt(0)
                Return Data
            Else
                CreateQueue(ID)
                Return Nothing
            End If
        End Function
        Public Sub Write(ID As String, Data As Byte())
            Dim ByteID As Byte() = System.Text.ASCIIEncoding.ASCII.GetBytes(ID)
            If WriteQueue.ContainsKey(ByteID) = False Then
                CreateQueue(ByteID)
            End If
            WriteQueue(ByteID).Add(Data)
        End Sub
        Public Function HasData(ID As String) As Boolean
            Dim ByteID As Byte() = System.Text.ASCIIEncoding.ASCII.GetBytes(ID)
            If ReadQueue.ContainsKey(ByteID) = True Then
                Return ReadQueue(ByteID).Count > 0
            Else
                CreateQueue(ByteID)
                Return False
            End If
        End Function
        Public Function Read(ID As Byte()) As Byte()
            Dim ByteID As Byte() = ID
            If ReadQueue.ContainsKey(ByteID) = True Then
                Dim Data As Byte() = ReadQueue(ByteID)(0)
                ReadQueue(ByteID).RemoveAt(0)
                Return Data
            Else
                CreateQueue(ID)
                Return Nothing
            End If
        End Function
        Public Sub Write(ID As Byte(), Data As Byte())
            Dim ByteID As Byte() = ID
            If WriteQueue.ContainsKey(ByteID) = False Then
                CreateQueue(ByteID)
            End If
            WriteQueue(ByteID).Add(Data)
        End Sub
        Public Function HasData(ID As Byte()) As Boolean
            Dim ByteID As Byte() = ID
            If ReadQueue.ContainsKey(ByteID) = True Then
                Return ReadQueue(ByteID).Count > 0
            Else
                CreateQueue(ByteID)
                Return False
            End If
        End Function
    End Class
End Class
