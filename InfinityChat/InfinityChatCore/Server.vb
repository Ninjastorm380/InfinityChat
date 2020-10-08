Public Class Server : Inherits ServerBase
    Public Event ClientConnected As EventHandler(Of Networking.QueuedTcpClient)
    Public Event ClientDisconnected As EventHandler(Of Networking.QueuedTcpClient)

    Private Structure MetaHeader
        Public Filename As String
        Public ProgramName As String
        Public ProgramID As String
        Public SourcePath As String
    End Structure
    Friend Overrides Sub ServerMain(Client As Networking.QueuedTcpClient)
        Dim CommandLimiter As New ThreadLimiter(5)
        Client.CreateQueue("COMMAND")
        RaiseEvent ClientConnected(Me, Client)
        While Client.Connected = True And Running = True
            If Client.HasData("COMMAND") = True Then
                Dim Data As Byte()() = Serialization.DeserializeArray(Client.Read("COMMAND"))
                Dim Command As String = System.Text.ASCIIEncoding.ASCII.GetString(Data(0))
                Select Case Command
                    Case "connection.disconnect"
                        Exit While
                End Select
            End If
            CommandLimiter.Limit()
        End While
        If Client.Connected = True Then Client.Write("COMMAND", Serialization.SerializeArray({System.Text.ASCIIEncoding.ASCII.GetBytes("connection.disconnect")}))
        Client.Close()
        RaiseEvent ClientDisconnected(Me, Client)
    End Sub
End Class

Public MustInherit Class ServerBase
    Friend CryptographicKey As String
    Private listener As Net.Sockets.TcpListener
    Private ListenerThread As Threading.Thread
    Private IsRunning As Boolean
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
    Public Sub Start(Port As Integer, Key As String)
        If Me.IsRunning = False Then
            CryptographicKey = Key
#Disable Warning BC40000
            Me.listener = New Net.Sockets.TcpListener(Port)
#Enable Warning BC40000
            Me.ListenerThread = New Threading.Thread(AddressOf ListenerSub)
            Me.IsRunning = True
            Me.listener.Start()
            Me.ListenerThread.Start()
            RaiseEvent ServerStarted(Me, New EventArgs)
        End If
    End Sub
    Public Sub [Stop]()
        If Me.IsRunning = True Then
            Me.IsRunning = False
            Me.listener.Stop()
            Me.listener = Nothing
            Me.ListenerThread.Abort()
            Me.ListenerThread = Nothing
            RaiseEvent ServerStopped(Me, New EventArgs)
        End If
    End Sub
    Private Sub ListenerSub()
        Dim ListenerLimiter As New ThreadLimiter(10)
        While IsRunning
            If listener.Pending() Then
                Dim ServerMainThread As New Threading.Thread(AddressOf ServerMain) : ServerMainThread.Start(New Networking.QueuedTcpClient(Me.listener.AcceptSocket, CryptographicKey))
            End If
            ListenerLimiter.Limit()
        End While
    End Sub
    Friend MustOverride Sub ServerMain(Client As Networking.QueuedTcpClient)
End Class