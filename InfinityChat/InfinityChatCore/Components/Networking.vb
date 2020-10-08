Public Class Networking
    Public Class QueuedTcpClient : Inherits Net.Sockets.TcpClient
        Private CryptographicKey As String = Nothing
        Private IsConnected As Boolean = False : Public Shadows Property Connected As Boolean
            Get
                IsConnected = Not StreamDisposed()
                Return IsConnected
            End Get
            Set(value As Boolean)
                IsConnected = value
            End Set
        End Property
        Private Stream As Net.Sockets.NetworkStream = Nothing
        Private ReadQueue As Dictionary(Of String, List(Of Byte()))
        Private WriteQueue As Dictionary(Of String, List(Of Byte()))
        Private Closing As Boolean = False
        Public RemoteEndpoint As Net.IPEndPoint
        Public LocalEndpoint As Net.IPEndPoint
        Public Sub New(Address As String, Port As Integer, Key As String)
            MyBase.New(Address, Port)
            Connected = True
            CryptographicKey = Key
            Stream = Me.GetStream
            RemoteEndpoint = Client.RemoteEndPoint
            LocalEndpoint = Client.LocalEndPoint
            ReadQueue = New Dictionary(Of String, List(Of Byte()))
            WriteQueue = New Dictionary(Of String, List(Of Byte()))
            Dim QueueThread As New Threading.Thread(AddressOf QueueMethod) : QueueThread.Start()
        End Sub
        Public Sub New(Client As Net.Sockets.Socket, Key As String)
            MyBase.New
            Me.Client = Client
            Connected = True
            CryptographicKey = Key
            Stream = Me.GetStream
            RemoteEndpoint = Client.RemoteEndPoint
            LocalEndpoint = Client.LocalEndPoint
            ReadQueue = New Dictionary(Of String, List(Of Byte()))
            WriteQueue = New Dictionary(Of String, List(Of Byte()))
            Dim QueueThread As New Threading.Thread(AddressOf QueueMethod) : QueueThread.Start()
        End Sub
        Private Function StreamDisposed() As Boolean
            Try
                Dim DataAvailable = Stream.DataAvailable
                Return False
            Catch StreamDisposedError As ObjectDisposedException
                Connected = False
                Return True
            End Try
        End Function
        Private Sub QueueMethod()
            Dim QueueLimiter As New ThreadLimiter(150)
            While Connected = True
                Connected = Not StreamDisposed()
                Try
                    If Connected = True AndAlso Stream.DataAvailable = True Then
                        Dim SizeBytes(3) As Byte
                        Stream.Read(SizeBytes, 0, SizeBytes.Length)
                        Dim Size As Integer = BitConverter.ToInt32(SizeBytes, 0)
                        Dim Input(Size - 1) As Byte
                        Stream.Read(Input, 0, Input.Length)
                        Dim Data As Byte()() = Serialization.DeserializeArray(Cryptography.DecryptAES256(Input, CryptographicKey))
                        Dim ID As String = System.Text.ASCIIEncoding.ASCII.GetString(Data(0))
                        If Closing = False Then ReadQueue.Item(ID).Add(Data(1))
                    End If
                Catch ex As ObjectDisposedException
                    Connected = False
                End Try
                For x = 0 To WriteQueue.Keys.Count - 1
                    Dim ID As String = WriteQueue.Keys(x)
                    Try
                        If Connected = True Then
                            If WriteQueue.Item(ID).Count > 0 Then
                                QueueLimiter.IterationsPerSecond += 10
                                If WriteQueue.Item(ID)(0) Is Nothing Then
                                    Dim IterationCounter As Integer = 0
                                    Dim IPS As Integer = QueueLimiter.IterationsPerSecond
                                    QueueLimiter.IterationsPerSecond = 100
                                    Do Until WriteQueue.Item(ID)(0) IsNot Nothing
                                        QueueLimiter.Limit()
                                        IterationCounter += 1

                                        If IterationCounter > 10000 Then
                                            Exit Do
                                        End If
                                    Loop
                                    QueueLimiter.IterationsPerSecond = IPS
                                    Debug.Print(IterationCounter)
                                End If
                                Dim Data As Byte() = WriteQueue.Item(ID)(0)
                                Dim SerializedData As Byte() = Serialization.SerializeArray({System.Text.ASCIIEncoding.ASCII.GetBytes(ID), Data})
                                Dim Output As Byte() = Cryptography.EncryptAES256(SerializedData, CryptographicKey)
                                Try
                                    Dim SizeBytes As Byte() = BitConverter.GetBytes(Output.Length)
                                    Stream.Write(SizeBytes, 0, SizeBytes.Length)
                                    Stream.Write(Output, 0, Output.Length)
                                Catch StreamIOException As System.IO.IOException
                                    Connected = False
                                End Try
                                WriteQueue.Item(ID).RemoveAt(0)
                            Else
                                If QueueLimiter.IterationsPerSecond > 50 Then QueueLimiter.IterationsPerSecond -= 10
                            End If
                        End If
                    Catch ex As InvalidOperationException
                    End Try
                Next
                QueueLimiter.Limit()
            End While
        End Sub
        Public Function Read(ID As String) As Byte()
            If Closing = False Then
                Dim Data As Byte() = ReadQueue.Item(ID)(0)
                ReadQueue.Item(ID).RemoveAt(0)
                Return Data
            Else
                Return Nothing
            End If
        End Function
        Public Sub Write(ByRef ID As String, ByRef Data As Byte())
            If Closing = False Then
                WriteQueue.Item(ID).Add(Data)
            End If
        End Sub
        Public Function HasData(ID As String) As Boolean
            If Closing = False Then
                Return ReadQueue.Item(ID).Count > 0
            Else
                Return Nothing
            End If
        End Function
        Public Sub CreateQueue(ID As String)
            If Closing = False Then
                If ReadQueue.ContainsKey(ID) = False Then
                    ReadQueue.Add(ID, New List(Of Byte()))
                End If
                If WriteQueue.ContainsKey(ID) = False Then
                    WriteQueue.Add(ID, New List(Of Byte()))
                End If
            End If
        End Sub
        Public Function QueueExists(ID As String) As Boolean
            If Closing = False Then
                Return ReadQueue.ContainsKey(ID) And WriteQueue.ContainsKey(ID)
            Else
                Return Nothing
            End If
        End Function
        Public Sub DestroyQueue(ID As String)
            If Closing = False Then
                If ReadQueue.Keys.Contains(ID) = True Then
                    ReadQueue.Remove(ID)
                End If
                If WriteQueue.Keys.Contains(ID) = True Then
                    WriteQueue.Remove(ID)
                End If
            End If
        End Sub
        Public Shadows Sub Close()
            Closing = True
            Dim WaitLimiter As New ThreadLimiter(100)
            Dim Total As Integer
            Do
                Total = 0
                For x = 0 To ReadQueue.Keys.Count - 1
                    Total += ReadQueue(ReadQueue.Keys(x)).Count
                Next
                For x = 0 To WriteQueue.Keys.Count - 1
                    Total += WriteQueue(WriteQueue.Keys(x)).Count
                Next
                WaitLimiter.Limit()
            Loop Until Total = 0
            MyBase.Close()
        End Sub
    End Class
    Public Class Netstream : Inherits System.Net.Sockets.NetworkStream
        Sub New(NetworkSocket As System.Net.Sockets.Socket)
            MyBase.New(NetworkSocket)

        End Sub
        Public Shadows Sub Dispose()
            ' Dispose of unmanaged resources.
            Dispose()
            ' Suppress finalization.
            GC.SuppressFinalize(Me)
        End Sub

    End Class
End Class