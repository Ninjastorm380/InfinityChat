Public Class Networking
    Friend Class InterlockedListDictionary
        Private Keylist As New List(Of String)
        Private Valuelist As New List(Of List(Of Byte()))
        Public Property Item(ByVal Key As String) As List(Of Byte())
            Get
                Dim Index As Integer = Keylist.IndexOf(Key)
                Return Valuelist(Index)
            End Get
            Set(value As List(Of Byte()))
                Dim Index As Integer = Keylist.IndexOf(Key)
                Valuelist(Index) = value
            End Set
        End Property
        Public Sub Add(Key As String, Value As List(Of Byte()))
            Keylist.Add(Key) : Valuelist.Add(Value)
        End Sub
        Public Sub Remove(Key As String)
            Dim ItemIndex As Integer = Keylist.IndexOf(Key)
            Keylist.RemoveAt(ItemIndex)
            Valuelist.RemoveAt(ItemIndex)
        End Sub
        Public Sub RemoveAt(Index As Integer)
            Keylist.RemoveAt(Index)
            Valuelist.RemoveAt(Index)
        End Sub
        Public Function Keys() As List(Of String)
            Return Keylist
        End Function
    End Class

    Public Class QueuedTcpClient : Inherits Net.Sockets.TcpClient
        Private Class QueueItem
            Public ID As String
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
            Dim QueueLimiter As New ThreadLimiter(150)
            Dim DataAvailable As Boolean = False
            While Connected = True
                If StreamDisposed() = False AndAlso Stream.DataAvailable = True Then
                    Do Until StreamDisposed() = True OrElse Stream.DataAvailable = False
                        Dim Input(Available - 1) As Byte
                        Stream.Read(Input, 0, Input.Length)
                        Dim Data As Byte()() = Serialization.DeserializeArray(Cryptography.DecryptAES256(Input, CryptographicKey))
                        Dim ID As String = System.Text.ASCIIEncoding.ASCII.GetString(Data(0))
                        ReadQueue.Item(ID).Add(Data(1))
                    Loop
                End If
                If StreamDisposed() = False Then
                    For x = 0 To WriteQueue.Keys.Count - 1
                        Dim ID As String = WriteQueue.Keys(x)
                        Try
                            If WriteQueue.Item(ID).Count > 0 Then
                                Dim Data As Byte() = WriteQueue.Item(ID)(0)
                                If Data IsNot Nothing Then
                                    WriteToLog("[" + Date.Now.ToString("yyyy/M/d @ hh:mm:ss") + "] - [" + ItemName + " Method: Write - Level: INFO]: Writing message data to stream.")
                                    Dim SerializedData As Byte() = Serialization.SerializeArray({System.Text.ASCIIEncoding.ASCII.GetBytes(ID), Data})
                                    Dim Output As Byte() = Cryptography.EncryptAES256(SerializedData, CryptographicKey)
                                    Try : Stream.Write(Output, 0, Output.Length) : Catch StreamIOException As System.IO.IOException : Connected = False : End Try
                                    WriteQueue.Item(ID).RemoveAt(0)
                                    WriteToLog("[" + Date.Now.ToString("yyyy/M/d @ hh:mm:ss") + "] - [" + ItemName + " Method: Write - Level: INFO]: Message data has been written to stream.")
                                Else
                                    WriteToLog("[" + Date.Now.ToString("yyyy/M/d @ hh:mm:ss") + "] - [" + ItemName + " Method: Write - Level: ERROR]: Message data was null. Message data has not been sent and will be removed from the write queue.")
                                    WriteQueue.Item(ID).RemoveAt(0)
                                    WriteToLog("[" + Date.Now.ToString("yyyy/M/d @ hh:mm:ss") + "] - [" + ItemName + " Method: Write - Level: INFO]: Null message data has been removed from write queue successfully.")

                                End If

                            End If
                        Catch ex As InvalidOperationException
                        End Try
                    Next
                End If
                QueueLimiter.Limit()
            End While
        End Sub

        Public Function Read(ID As String) As Byte()
            Dim Data As Byte() = ReadQueue.Item(ID)(0)
            ReadQueue.Item(ID).RemoveAt(0)
            Return Data
        End Function
        Public Sub Write(ByRef ID As String, ByRef Data As Byte())
            Dim ItemData As String = Convert.ToBase64String(Data)





            WriteQueue.Item(ID).Add(Data)




            Dim ItemList As List(Of Byte()) = WriteQueue.Item(ID)

            Dim found As Boolean = False
            For x = 0 To ItemList.Count - 1
                Dim ItemToCompare As String = Convert.ToBase64String(ItemList(x))
                found = (ItemToCompare = ItemData)
                If found = True Then
                    Exit For
                End If
            Next
            If found = True Then
                WriteToLog("[" + Date.Now.ToString("yyyy/M/d @ hh:mm:ss") + "] - [" + ItemName + " Method: Write - Level: INFO]: Message data was added to write queue successfuly.")
            Else
                WriteToLog("[" + Date.Now.ToString("yyyy/M/d @ hh:mm:ss") + "] - [" + ItemName + " Method: Write - Level: WARNING]: Unable to find message data in write queue after adding. Value in write queue may likely be null.")
            End If
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
