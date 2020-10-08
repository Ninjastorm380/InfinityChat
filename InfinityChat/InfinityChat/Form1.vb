Public Class Form1
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Renderer1.BackgroundImage = Image.FromFile("Background.jpg")


        Dim PFP As Bitmap = Bitmap.FromFile(Application.StartupPath + "\UserPFP.png", False)
        Dim TestSelect As Integer = 2
        Select Case TestSelect
            Case 1
                For x = 1 To 10
                    Renderer1.AddMessage(New ChatMessage With {.UserPFP = PFP, .UserName = "joe4life", .UserHandle = "joe-schmoe", .MessageBackcolor = Color.FromArgb(205, 255, 255, 255), .MessageText = "hello world", .MessageFont = Me.Font, .MessageForecolor = Color.Black})
                    Renderer1.AddMessage(New ChatMessage With {.UserPFP = PFP, .UserName = "joe4life", .UserHandle = "joe-schmoe", .MessageBackcolor = Color.FromArgb(235, 64, 64, 64), .MessageText = "hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world", .MessageFont = Me.Font, .MessageForecolor = Color.White})
                    Renderer1.AddMessage(New ChatMessage With {.UserPFP = PFP, .UserName = "joe4life", .UserHandle = "joe-schmoe", .MessageBackcolor = Color.FromArgb(205, 0, 255, 255), .MessageText = "hello joe", .MessageFont = Me.Font, .MessageForecolor = Color.Black})
                    Renderer1.AddMessage(New ChatMessage With {.UserPFP = PFP, .UserName = "joe4life", .UserHandle = "joe-schmoe", .MessageBackcolor = Color.FromArgb(205, 255, 0, 255), .MessageText = "hello guy", .MessageFont = Me.Font, .MessageForecolor = Color.Black})
                    Renderer1.AddMessage(New ChatMessage With {.UserPFP = PFP, .UserName = "joe4life", .UserHandle = "joe-schmoe", .MessageBackcolor = Color.FromArgb(205, 255, 255, 0), .MessageText = "banana", .MessageFont = Me.Font, .MessageForecolor = Color.Black})
                    Renderer1.AddMessage(New ChatMessage With {.UserPFP = PFP, .UserName = "joe4life", .UserHandle = "joe-schmoe", .MessageBackcolor = Color.FromArgb(205, 255, 0, 0), .MessageText = "toad", .MessageFont = Me.Font, .MessageForecolor = Color.Black})
                    Renderer1.AddMessage(New ChatMessage With {.UserPFP = PFP, .UserName = "joe4life", .UserHandle = "joe-schmoe", .MessageBackcolor = Color.FromArgb(205, 0, 0, 255), .MessageText = "potato", .MessageFont = Me.Font, .MessageForecolor = Color.White})
                    Renderer1.AddMessage(New ChatMessage With {.UserPFP = PFP, .UserName = "joe4life", .UserHandle = "joe-schmoe", .MessageBackcolor = Color.FromArgb(205, 0, 255, 0), .MessageText = "potato", .MessageFont = Me.Font, .MessageForecolor = Color.Black})
                    Renderer1.AddMessage(New ChatMessage With {.UserPFP = PFP, .UserName = "joe4life", .UserHandle = "joe-schmoe", .MessageBackcolor = Color.FromArgb(195, 40, 40, 40), .MessageText = "hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world ", .MessageFont = Me.Font, .MessageForecolor = Color.White})

                Next
            Case 2
                For x = 1 To 100
                    Renderer1.AddMessage(New ChatMessage With {.UserPFP = PFP, .UserName = "joe4life", .UserHandle = "joe-schmoe", .MessageBackcolor = Color.FromArgb(195, 40, 40, 40), .MessageText = "hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world hello world ", .MessageFont = Me.Font, .MessageForecolor = Color.White})
                Next
            Case 3

        End Select


    End Sub
End Class
