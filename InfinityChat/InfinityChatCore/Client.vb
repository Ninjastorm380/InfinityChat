Public Class Client
    Private Client As Networking.QueuedTcpClient
    Public Event ClientConnected As EventHandler(Of Networking.QueuedTcpClient)
    Public Event ClientDisconnected As EventHandler(Of Networking.QueuedTcpClient)
    Private CoreRunning As Boolean = False
    Public ReadOnly Property Connected As Boolean
        Get
            If Client IsNot Nothing Then Return Client.Connected Else Return False
        End Get
    End Property
    Private Sub ClientPing(Client As Networking.QueuedTcpClient)
        Dim PingLimiter As New ThreadLimiter(1)
        Client.CreateQueue("PING")
        Do Until Client.QueueExists("PING") = True
            PingLimiter.Limit()
            Client.CreateQueue("PING")
        Loop
        Threading.Thread.Sleep(1000)
        PingLimiter.IterationsPerSecond = 5
        Dim PingMessage As Byte() = Serialization.SerializeArray({System.Text.ASCIIEncoding.ASCII.GetBytes("ping")})
        While Client.Connected = True And CoreRunning = True
            Client.Write("PING", PingMessage)
            If Client.HasData("PING") = True Then Client.Read("PING")
            PingLimiter.Limit()
        End While
    End Sub
    Private Sub ClientMain(Client As Networking.QueuedTcpClient)
        Dim CommandLimiter As New ThreadLimiter(1)
        Client.CreateQueue("COMMAND")
        Do Until Client.QueueExists("COMMAND") = True
            CommandLimiter.Limit()
            Client.CreateQueue("COMMAND")
        Loop
        Threading.Thread.Sleep(1000)
        CommandLimiter.IterationsPerSecond = 5
        While Client.Connected = True And CoreRunning = True
            If Client.HasData("COMMAND") = True Then
                Dim Data As Byte()() = Serialization.DeserializeArray(Client.Read("COMMAND"))
                Select Case System.Text.ASCIIEncoding.ASCII.GetString(Data(0))
                    Case "server.connections.disconnect.return"
                        CoreRunning = False
                        Exit While
                End Select
            End If
            CommandLimiter.Limit()
        End While
        Client.Close()
    End Sub
    Public Sub Connect(Address As String, EncryptionKey As String)
        If Connected = False Then
            CoreRunning = True
            Dim AddressData As String() = Address.Split(":")
            If Client IsNot Nothing Then Client.Dispose()
            Client = New Networking.QueuedTcpClient(AddressData(0), AddressData(1), EncryptionKey)
            Dim ServerMainMethodThread As New Threading.Thread(Sub()
                                                                   Client.ItemName = "Client"
                                                                   RaiseEvent ClientConnected(Me, Client)
                                                                   ClientMain(Client)
                                                                   RaiseEvent ClientDisconnected(Me, Client)
                                                               End Sub) : ServerMainMethodThread.Start()
            Dim ServerPingMethodThread As New Threading.Thread(Sub() ClientPing(Client)) : ServerPingMethodThread.Start()
        End If
    End Sub
    Public Sub Disconnect()
        If Connected = True Then Client.Write("COMMAND", Serialization.SerializeArray({System.Text.ASCIIEncoding.ASCII.GetBytes("server.connections.disconnect")}))
    End Sub
End Class
