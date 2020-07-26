Public Class Client
    Private Client As Networking.QueuedTcpClient
    Public Event ClientConnected As EventHandler(Of Networking.QueuedTcpClient)
    Public Event ClientDisconnected As EventHandler(Of Networking.QueuedTcpClient)
    Public ReadOnly Property Connected As Boolean
        Get
            If Client IsNot Nothing Then Return Client.Connected Else Return False
        End Get
    End Property
    Private Sub ClientPing(Client As Networking.QueuedTcpClient)
        Dim PingLimiter As New ThreadLimiter(5)
        Client.CreateQueue("PING")
        While Client.Connected = True
            Client.Write("PING", Serialization.SerializeArray({}))
            If Client.HasData("PING") = True Then Client.Read("PING")
            PingLimiter.Limit()
        End While
    End Sub
    Private Sub ClientMain(Client As Networking.QueuedTcpClient)
        Dim CommandLimiter As New ThreadLimiter(10)
        Client.CreateQueue("COMMAND")
        While Client.Connected = True
            If Client.HasData("COMMAND") = True Then
                Dim Data As Byte()() = Serialization.DeserializeArray(Client.Read("COMMAND"))
                Select Case System.Text.ASCIIEncoding.ASCII.GetString(Data(0))
                    Case "server.connections.disconnect.return"
                        Client.Close()
                        Exit While
                End Select
            End If
            CommandLimiter.Limit()
        End While
    End Sub
    Public Sub Connect(Address As String, EncryptionKey As String)
        Dim AddressData As String() = Address.Split(":")
        Client = New Networking.QueuedTcpClient(AddressData(0), AddressData(1), EncryptionKey)
        Dim ServerMainMethodThread As New Threading.Thread(Sub()
                                                               RaiseEvent ClientConnected(Me, Client)
                                                               ClientMain(Client)
                                                               RaiseEvent ClientDisconnected(Me, Client)
                                                           End Sub) : ServerMainMethodThread.Start()
        Dim ServerPingMethodThread As New Threading.Thread(Sub() ClientPing(Client)) : ServerPingMethodThread.Start()
    End Sub
End Class
