<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
        Me.Renderer1 = New InfinityChat.Renderer()
        Me.SuspendLayout()
        '
        'Renderer1
        '
        Me.Renderer1.BackColor = System.Drawing.Color.Gray
        Me.Renderer1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Renderer1.ChatTitle = Nothing
        Me.Renderer1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Renderer1.FPS = 60
        Me.Renderer1.Location = New System.Drawing.Point(0, 0)
        Me.Renderer1.Name = "Renderer1"
        Me.Renderer1.Size = New System.Drawing.Size(784, 461)
        Me.Renderer1.TabIndex = 0
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(784, 461)
        Me.Controls.Add(Me.Renderer1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MinimumSize = New System.Drawing.Size(450, 500)
        Me.Name = "Form1"
        Me.Tag = ""
        Me.Text = "InfinityChat"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents Renderer1 As Renderer
End Class
