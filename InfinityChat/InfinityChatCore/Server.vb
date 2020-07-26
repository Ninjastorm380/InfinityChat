﻿Public Class Server : Inherits ServerBase

    Friend Overrides Sub ServerMain(Client As Networking.QueuedTcpClient)
        Dim CommandLimiter As New ThreadLimiter(5)
        Client.CreateQueue("COMMAND")
        While Client.Connected = True And Running = True
            If Client.HasData("COMMAND") = True Then
                Dim Data As Byte()() = Serialization.DeserializeArray(Client.Read("COMMAND"))
                Select Case System.Text.ASCIIEncoding.ASCII.GetString(Data(0))
                    Case "server.connections.disconnect"
                        Client.Write("COMMAND", Serialization.SerializeArray({System.Text.ASCIIEncoding.ASCII.GetBytes("server.connections.disconnect.return")}))
                        Client.Close()
                End Select
            End If
            CommandLimiter.Limit()
        End While
    End Sub

    Friend Overrides Sub ServerPing(Client As Networking.QueuedTcpClient)
        Dim PingLimiter As New ThreadLimiter(5)
        Client.CreateQueue("PING")
        While Client.Connected = True And Running = True
            Client.Write("PING", Serialization.SerializeArray({}))
            If Client.HasData("PING") = True Then Client.Read("PING")
            PingLimiter.Limit()
        End While
    End Sub
End Class

Public MustInherit Class ServerBase
    Public CryptographicKey As String
    Private listener As Net.Sockets.TcpListener
    Private ListenerThread As Threading.Thread
    Private IsRunning As Boolean
    Public Event ClientConnected As EventHandler(Of Networking.QueuedTcpClient)
    Public Event ClientDisconnected As EventHandler(Of Networking.QueuedTcpClient)
    Public Event ServerStarted As EventHandler
    Public Event ServerStopped As EventHandler
    Public ReadOnly Property Running() As Boolean
        Get
            Return Me.IsRunning
        End Get
    End Property

    Protected Sub New()
        Me.IsRunning = False
    End Sub
    Public Sub Start(Port As Integer)
#Disable Warning BC40000
        Me.listener = New Net.Sockets.TcpListener(Port)
#Enable Warning BC40000
        Me.ListenerThread = New Threading.Thread(AddressOf ListenerSub)
        Me.IsRunning = True
        Me.listener.Start()
        Me.ListenerThread.Start()
        RaiseEvent ServerStarted(Me, New EventArgs)
    End Sub
    Public Sub [Stop]()
        Me.IsRunning = False
        Me.listener.Stop()
        Me.listener = Nothing
        Me.ListenerThread.Abort()
        Me.ListenerThread = Nothing
        RaiseEvent ServerStopped(Me, New EventArgs)
    End Sub
    Private Sub ListenerSub()
        Dim ListenerLimiter As New ThreadLimiter(15)
        While IsRunning
            If listener.Pending() Then
                Dim Client As New Networking.QueuedTcpClient(Me.listener.AcceptSocket, CryptographicKey)
                Dim ServerMainThread As New Threading.Thread(Sub()
                                                                 Dim ServerClient As Networking.QueuedTcpClient = Client
                                                                 RaiseEvent ClientConnected(Me, ServerClient)
                                                                 ServerMain(Client)
                                                                 RaiseEvent ClientDisconnected(Me, ServerClient)
                                                             End Sub) : ServerMainThread.Start()
                Dim ServerPingThread As New Threading.Thread(Sub() ServerPing(Client)) : ServerPingThread.Start()
            End If
            ListenerLimiter.Limit()
        End While
    End Sub
    Friend MustOverride Sub ServerMain(Client As Networking.QueuedTcpClient)
    Friend MustOverride Sub ServerPing(Client As Networking.QueuedTcpClient)
End Class