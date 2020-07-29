Public Class Form1
    Private WithEvents Client As New InfinityChatCore.Client
    Private WithEvents Server As New InfinityChatCore.Server
    Private Sub ServerOnline(Sender As Object, e As EventArgs) Handles Server.ServerStarted
        Label1.Text = "Server Status: Online"
    End Sub
    Private Sub ServerOffline(Sender As Object, e As EventArgs) Handles Server.ServerStopped
        Label1.Text = "Server Status: Offline"
    End Sub
    Private Sub ServerClientConnected(sender As Object, e As InfinityChatCore.Networking.QueuedTcpClient) Handles Server.ClientConnected

    End Sub
    Private Sub ServerClientDisconnected(sender As Object, e As InfinityChatCore.Networking.QueuedTcpClient) Handles Server.ClientDisconnected

    End Sub
    Private Sub ClientConnected(sender As Object, e As InfinityChatCore.Networking.QueuedTcpClient) Handles Client.ClientConnected
        Invoke(Sub()
                   Label2.Text = "Client Status: Connected"
               End Sub)
    End Sub
    Private Sub ClientDisconnected(sender As Object, e As InfinityChatCore.Networking.QueuedTcpClient) Handles Client.ClientDisconnected
        Invoke(Sub()
                   Label2.Text = "Client Status: Disconnected"
               End Sub)
    End Sub
    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Server.Start(19463, "D@35EC")
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        Server.Stop()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Client.Connect("127.0.0.1:19463", "D@35EC")
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Client.Disconnect()
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        InfinityChatCore.Networking.QueuedTcpClient.EnableDebug()
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        InfinityChatCore.Networking.QueuedTcpClient.DisableDebug()
    End Sub
End Class
