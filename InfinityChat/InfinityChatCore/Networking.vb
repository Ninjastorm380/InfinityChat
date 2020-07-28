Public Class Networking
    Public Class QueuedTcpClient : Inherits Net.Sockets.TcpClient
        Private CryptographicKey As String = Nothing
        Public Property ItemName As String = ""
        Public Shadows Property Connected As Boolean = False
        Private Stream As Net.Sockets.NetworkStream = Nothing
        Private ReadQueue As Dictionary(Of String, List(Of Byte()))
        Private WriteQueue As Dictionary(Of String, List(Of Byte()))
        Public Sub New(Address As String, Port As Integer, Key As String)
            MyBase.New(Address, Port)
            Connected = True
            CryptographicKey = Key
            Stream = Me.GetStream
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
            Dim DataAvailable As Boolean = False
            While Connected = True
                If StreamDisposed() = False AndAlso Stream.DataAvailable = True Then
                    Do Until StreamDisposed() = True OrElse Stream.DataAvailable = False
                        Dim Input(Available - 1) As Byte
                        Stream.Read(Input, 0, Input.Length)
                        Dim Data As Byte()() = Serialization.DeserializeArray(Cryptography.DecryptAES256(Input, CryptographicKey))
                        Dim Key As String = System.Text.ASCIIEncoding.ASCII.GetString(Data(0))
                        If ReadQueue.ContainsKey(Key) = True Then
                            ReadQueue(Key).Add(Data(1))
                        Else

                        End If
                    Loop
                End If
                If StreamDisposed() = False Then
                    For x = 0 To WriteQueue.Keys.Count - 1
                        Dim ID As String = WriteQueue.Keys(x)
                        Try
                            If WriteQueue(ID).Count > 0 Then
                                Dim Data As Byte() = WriteQueue(ID)(0)
                                If Data IsNot Nothing Then
                                    Dim SerializedData As Byte() = Serialization.SerializeArray({System.Text.ASCIIEncoding.ASCII.GetBytes(ID), Data})
                                    Dim Output As Byte() = Cryptography.EncryptAES256(SerializedData, CryptographicKey)
                                    Try : Stream.Write(Output, 0, Output.Length) : Catch StreamIOException As System.IO.IOException : Connected = False : End Try
                                Else
                                    Debug.Print(ItemName + " [" + Date.Now.ToString("yyyy/M/d - h:m:s") + "]: ERROR - DATA WAS NOTHING, REMOVING FROM QUEUE!")
                                End If
                                WriteQueue(ID).RemoveAt(0)
                                End If
                        Catch ex As InvalidOperationException
                        End Try
                    Next
                End If
                QueueLimiter.Limit()
            End While
        End Sub

        Public Function Read(ID As String) As Byte()
            Dim Data As Byte() = ReadQueue(ID)(0)
            ReadQueue(ID).RemoveAt(0)
            Return Data
        End Function
        Public Sub Write(ID As String, Data As Byte())
            WriteQueue(ID).Add(Data)
        End Sub
        Public Function HasData(ID As String) As Boolean
            Return ReadQueue(ID).Count > 0
        End Function
        Public Sub CreateQueue(ID As String)
            If ReadQueue.ContainsKey(ID) = False Then
                ReadQueue.Add(ID, New List(Of Byte()))
            End If
            If WriteQueue.ContainsKey(ID) = False Then
                WriteQueue.Add(ID, New List(Of Byte()))
            End If
        End Sub
        Public Function QueueExists(ID As String) As Boolean
            Return ReadQueue.ContainsKey(ID) And WriteQueue.ContainsKey(ID)
        End Function
        Public Sub DestroyQueue(ID As String)
            If ReadQueue.ContainsKey(ID) = True Then

                ReadQueue.Remove(ID)
            End If
            If WriteQueue.ContainsKey(ID) = True Then
                WriteQueue.Remove(ID)
            End If
        End Sub
    End Class
End Class
