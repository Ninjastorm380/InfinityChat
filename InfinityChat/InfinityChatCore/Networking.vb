Public Class Networking


    Public Class QueuedTcpClient : Inherits Net.Sockets.TcpClient
        Private Class QueueItem
            Public Data As Byte()
        End Class

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
            Dim QueueLimiter As New ThreadLimiter(75)
            Dim DataAvailable As Boolean = False
            Debug.Print(ItemName + " - Connected = " + Connected.ToString)
            While Connected = True
                Connected = Not StreamDisposed()
                Try
                    If Stream.DataAvailable = True Then
                        Dim SizeBytes(3) As Byte
                        Stream.Read(SizeBytes, 0, SizeBytes.Length)
                        Dim Size As Integer = BitConverter.ToInt32(SizeBytes, 0)
                        Dim Input(Size - 1) As Byte
                        Stream.Read(Input, 0, Input.Length)
                        Dim Data As Byte()() = Serialization.DeserializeArray(Cryptography.DecryptAES256(Input, CryptographicKey))
                        Dim ID As String = System.Text.ASCIIEncoding.ASCII.GetString(Data(0))
                        ReadQueue.Item(ID).Add(Data(1))
                    End If
                Catch ex As ObjectDisposedException
                    Connected = False
                    End Try

                For x = 0 To WriteQueue.Keys.Count - 1
                    Dim ID As String = WriteQueue.Keys(x)
                    Try
                        If WriteQueue.Item(ID).Count > 0 Then
                            If WriteQueue.Item(ID)(0) Is Nothing Then
                                Dim IterationCounter As Integer = 0
                                QueueLimiter.IterationsPerSecond = 100
                                Do Until WriteQueue.Item(ID)(0) IsNot Nothing
                                    QueueLimiter.Limit()
                                    IterationCounter += 1

                                    If IterationCounter > 10000 Then
                                        Exit Do
                                    End If
                                Loop
                                QueueLimiter.IterationsPerSecond = 150
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
                        End If
                    Catch ex As InvalidOperationException
                    End Try
                Next
                QueueLimiter.Limit()
            End While
        End Sub

        Public Function Read(ID As String) As Byte()
            Dim Data As Byte() = ReadQueue.Item(ID)(0)
            ReadQueue.Item(ID).RemoveAt(0)
            Return Data
        End Function
        Public Sub Write(ByRef ID As String, ByRef Data As Byte())
            WriteQueue.Item(ID).Add(Data)
        End Sub
        Public Function HasData(ID As String) As Boolean

            Return ReadQueue.Item(ID).Count > 0
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
            If ReadQueue.Keys.Contains(ID) = True Then
                ReadQueue.Remove(ID)
            End If
            If WriteQueue.Keys.Contains(ID) = True Then
                WriteQueue.Remove(ID)
            End If
        End Sub
        Shared DebugWriterQueue As New List(Of String)
        Shared DebugWriterRunning As Boolean = False
        Private Sub WriteToLog(Message As String)
            If DebugWriterRunning = True Then DebugWriterQueue.Add(Message)
        End Sub
        Public Shared Sub EnableDebug()
            DebugWriterRunning = True
            Dim DebugWriterThread As New Threading.Thread(Sub()

                                                              While DebugWriterRunning = True
                                                                  If DebugWriterQueue.Count > 0 Then
                                                                      Do Until DebugWriterQueue.Count = 0
                                                                          Dim Stream As New IO.FileStream("./debug.log", IO.FileMode.Append)
                                                                          Dim MessageData As Byte() = System.Text.ASCIIEncoding.ASCII.GetBytes(DebugWriterQueue(0) + vbCrLf)
                                                                          Stream.Write(MessageData, 0, MessageData.Length)
                                                                          Stream.Flush()
                                                                          Stream.Close()
                                                                          DebugWriterQueue.RemoveAt(0)
                                                                      Loop
                                                                  End If
                                                                  Threading.Thread.Sleep(100)
                                                              End While
                                                              DebugWriterRunning = False

                                                          End Sub) : DebugWriterThread.Start()
        End Sub
        Public Shared Sub DisableDebug()
            DebugWriterRunning = False
        End Sub
    End Class
End Class
