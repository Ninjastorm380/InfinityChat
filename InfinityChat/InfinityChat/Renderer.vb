Imports System.Collections.ObjectModel
Imports System.Drawing

Public Class Renderer

    Public Property FPS As Integer = 30

    Private buffer As BufferedGraphics
    Private bufferContext As BufferedGraphicsContext
    Private items As ObservableCollection(Of ChatMessage)
    Private Suspended As Boolean = False
    Private Rendering As Boolean = False
    Public Property ChatTitle As String
    Dim RenderBounds As Rectangle
    Dim OffsetRenderBounds As Rectangle
    Public Sub New()
        Me.DoubleBuffered = True
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.Opaque Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        Me.buffer = Nothing
        Me.bufferContext = BufferedGraphicsManager.Current
        Me.items = New ObservableCollection(Of ChatMessage)
        Me.Suspended = False

        Dim RenderFPSLimiter As New ThreadLimiter(FPS)
        If System.ComponentModel.LicenseManager.UsageMode = System.ComponentModel.LicenseUsageMode.Runtime Then
            Dim PaintThread As New Threading.Thread(Sub()
                                                        If Me.Created = False Then
                                                            Do Until Me.Created = True
                                                                RenderFPSLimiter.Limit()
                                                            Loop
                                                        End If
                                                        While Me.Created = True
                                                            If Width <= 0 Or Height <= 0 Then
                                                                Suspended = True
                                                            Else
                                                                Suspended = False
                                                            End If
                                                            If Rendering = False And Suspended = False Then
                                                                Rendering = True
                                                                Dim GFX As Graphics = Me.CreateGraphics
                                                                RenderBounds = New Rectangle(0, 0, Width, Height)
                                                                OnPaint(New PaintEventArgs(GFX, RenderBounds))
                                                                GFX.Dispose()
                                                                Rendering = False
                                                            End If

                                                            Debug.Print(RenderFPSLimiter.Limit())
                                                        End While
                                                    End Sub) With {.Name = "Rendering Backbuffer Thread"}
            PaintThread.Start()
            Dim GCLimiter As New ThreadLimiter(RenderFPSLimiter.IterationsPerSecond / 4)
            Dim GCThread As New Threading.Thread(Sub()
                                                     If Me.Created = False Then
                                                         Do Until Me.Created = True
                                                             GCLimiter.Limit()
                                                         Loop
                                                     End If
                                                     While Me.Created = True
                                                         If Suspended = False Then
                                                             GC.Collect(1, GCCollectionMode.Forced, False, True)
                                                         End If
                                                         GCLimiter.Limit()
                                                     End While
                                                 End Sub) With {.Name = "Rendering GC Thread"}
            GCThread.Start()
        End If
        InitializeComponent()
    End Sub
    Public Sub AddMessage(Message As ChatMessage)
        ChatView.Add(Message)
    End Sub
    Private ScrollOffset As Integer = 0
    Dim ComputedHeight As Single = 0
    Dim ComputedHeightInstant As Single = 0
    Dim Hitboxes As New Dictionary(Of String, Hitbox)
    Dim HideSettings As Boolean = True
    Dim HideServerList As Boolean = True
    Dim HideChannelList As Boolean = True
    Dim HideUserSettingsAndLogin As Boolean = True

    Dim ComputingHeight As Boolean = False
    Dim ChatView As New ChatRenderView(BackgroundImage, BackColor)
    Protected Shadows Sub OnPaint(e As PaintEventArgs)
        Dim Ylocation As Integer = 0
        ChatView.SetBackColor(BackColor)
        ChatView.SetBackground(BackgroundImage)
        Dim CapturedScrollOffset As Integer = ScrollOffset
        Dim ImageOffset As Integer = 5
        Dim ImageOffsetY As Integer = 23
        OffsetRenderBounds = New Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height)
        If buffer IsNot Nothing Then buffer.Dispose()
        bufferContext.Invalidate()
        buffer = bufferContext.Allocate(e.Graphics, RenderBounds)
        buffer.Graphics.CompositingQuality = Drawing2D.CompositingQuality.HighSpeed
        buffer.Graphics.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias
        Dim BackgroundBrush As New SolidBrush(BackColor)
        buffer.Graphics.FillRectangle(BackgroundBrush, RenderBounds)
        If BackgroundImage IsNot Nothing Then buffer.Graphics.DrawImage(BackgroundImage, RenderBounds)

        Dim ButtonFont As Font = New Font(Font, FontStyle.Italic + FontStyle.Bold)
        ChatView.Font = ButtonFont

        ChatView.Render(buffer.Graphics, RenderBounds)

        buffer.Graphics.FillRectangle(BackgroundBrush, 0, 0, RenderBounds.Width, 23)
        Dim BannerImage As New Bitmap(RenderBounds.Width, 23, Imaging.PixelFormat.Format32bppPArgb)
        Dim BannerGFX As Graphics = Graphics.FromImage(BannerImage)
        If BackgroundImage IsNot Nothing Then BannerGFX.DrawImage(BackgroundImage, 0, 0, RenderBounds.Width, RenderBounds.Height)
        BannerGFX.Dispose()
        buffer.Graphics.DrawImage(BannerImage, 0, 0)
        BannerImage.Dispose()
        BackgroundBrush.Color = Color.FromArgb(205, 64, 64, 64)
        buffer.Graphics.FillRectangle(BackgroundBrush, 3, 3, RenderBounds.Width - 6, 18)
        buffer.Graphics.DrawLine(Pens.Black, 3, 2, RenderBounds.Width - 4, 2)
        buffer.Graphics.DrawLine(Pens.Black, 3, 21, RenderBounds.Width - 4, 21)
        buffer.Graphics.DrawLine(Pens.Black, 2, 3, 2, 20)
        buffer.Graphics.DrawLine(Pens.Black, RenderBounds.Width - 3, 3, RenderBounds.Width - 3, 20)

        'Settings Button Graphic And Hitbox
        Dim r As New Rectangle(RenderBounds.Width - 20, 4, 16, 16)
        If Hitboxes.ContainsKey("SettingsButton") = False Then
            Hitboxes.Add("SettingsButton", New Hitbox With {.Rectangle = r})
        Else
            Hitboxes("SettingsButton").Rectangle = r
        End If
        buffer.Graphics.DrawImage(My.Resources.Settings, RenderBounds.Width - 20, 4)

        If ChatView.Visible = True Then
            'Server Name / Show Server List Button Graphic And Hitbox

            Dim ServerNameFontSize As SizeF = buffer.Graphics.MeasureString("server test name", ButtonFont) 'TODO: Add Server Name Variable
            Dim ServerNameYLocation As Single = (20 / 2) - (ServerNameFontSize.Height / 2)


            Dim ServerTopY As Single = (20 / 2) - 6
            Dim ServerBottomY As Single = (20 / 2) + 9

            BackgroundBrush.Color = Color.FromArgb(255, 61, 61, 61)
            buffer.Graphics.FillRectangle(BackgroundBrush, 5, 4, ServerNameFontSize.Width + 2, 16)
            buffer.Graphics.DrawString("server test name", ButtonFont, Brushes.White, New PointF(5, ServerNameYLocation + 2))
            buffer.Graphics.DrawLine(Pens.Black, 5, ServerTopY, ServerNameFontSize.Width + 6, ServerTopY)
            buffer.Graphics.DrawLine(Pens.Black, 5, ServerBottomY, ServerNameFontSize.Width + 6, ServerBottomY)
            buffer.Graphics.DrawLine(Pens.Black, 4, ServerTopY + 1, 4, ServerBottomY - 1)
            buffer.Graphics.DrawLine(Pens.Black, ServerNameFontSize.Width + 7, ServerTopY + 1, ServerNameFontSize.Width + 7, ServerBottomY - 1)

            'Channel Name / Show Channel List Button Graphic And Hitbox
            Dim ChannelNameFontSize As SizeF = buffer.Graphics.MeasureString("channel test name", ButtonFont) 'TODO: Add Channel Name Variable
            Dim ChannelNameYLocation As Single = (20 / 2) - (ChannelNameFontSize.Height / 2)

            Dim ChannelTopY As Single = (20 / 2) - 10
            Dim ChannelBottomY As Single = (20 / 2) + 9
            buffer.Graphics.DrawString("channel test name", ButtonFont, Brushes.White, New PointF(ServerNameFontSize.Width + 9, ChannelNameYLocation + 2))
            'Login / User Account Settings Button Graphic And Hitbox
            Dim LoginNameFontSize As SizeF = buffer.Graphics.MeasureString("login", Font) 'TODO: Add Login Name Variable
            Dim LoginNameYLocation As Single = (30 / 2) - (LoginNameFontSize.Height / 2)
        End If


        'Frame
        buffer.Graphics.DrawRectangle(Pens.Black, 0, 0, RenderBounds.Width - 1, RenderBounds.Height - 1)

        'Commit Graphics Update
        If Me.Created = True Then buffer.Render(e.Graphics)
        BackgroundBrush.Dispose()
    End Sub
    Dim ScrollVelocity As Integer = 0
    Dim Scrolling As Boolean = False
    Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
        If ChatView.Visible = True Then ChatView.OnScrollwheel(e)
    End Sub
    Protected Overrides Sub Onsizechanged(e As EventArgs)
        If ChatView.Visible = True Then ChatView.OnResize(e)
    End Sub

    Private Sub Renderer_MouseMove(sender As Object, e As MouseEventArgs) Handles MyBase.MouseMove
        For x = 0 To Hitboxes.Keys.Count - 1
            If Hitboxes(Hitboxes.Keys(x)).Rectangle.Contains(e.Location) Then
                Hitboxes(Hitboxes.Keys(x)).Hovering = True
            Else
                Hitboxes(Hitboxes.Keys(x)).Hovering = False
            End If
        Next
        ChatView.OnMouseMove(e)
    End Sub

    Private Sub Renderer_MouseUp(sender As Object, e As MouseEventArgs) Handles MyBase.MouseUp
        For x = 0 To Hitboxes.Keys.Count - 1
            Debug.Print(Hitboxes.Keys(x) + " - [Clicked]:" + Hitboxes(Hitboxes.Keys(x)).Hovering.ToString)
            If Hitboxes(Hitboxes.Keys(x)).Hovering = True Then
                Select Case Hitboxes.Keys(x)
                    Case "SettingsButton"
                        ChatView.Visible = True
                End Select
            End If
        Next
        If ChatView.Visible = True Then ChatView.OnMouseUp(e)
    End Sub

    Private Sub Renderer_MouseDown(sender As Object, e As MouseEventArgs) Handles MyBase.MouseDown
        If ChatView.Visible = True Then ChatView.OnMouseDown(e)
    End Sub
End Class

Public Class CustomScrollbar
    Public Property Height As Integer
    Public Property Width As Integer
    Protected moChannelColor As Color = Color.Empty
    Protected moUpArrowImage As Image = Nothing
    Protected moDownArrowImage As Image = Nothing
    Protected moThumbArrowImage As Image = Nothing
    Protected moThumbTopImage As Image = Nothing
    Protected moThumbTopSpanImage As Image = Nothing
    Protected moThumbBottomImage As Image = Nothing
    Protected moThumbBottomSpanImage As Image = Nothing
    Protected moThumbMiddleImage As Image = Nothing
    Protected moLargeChange As Integer = 10
    Protected moSmallChange As Integer = 1
    Protected moMinimum As Integer = 0
    Protected moMaximum As Integer = 100
    Protected moValue As Integer = 0
    Protected X As Integer = 0
    Protected Y As Integer = 0
    Private nClickPoint As Integer
    Protected moThumbTop As Integer = 0
    Protected moAutoSize As Boolean = False
    Private moThumbDown As Boolean = False
    Private moThumbDragging As Boolean = False
    Public Event Scroll As EventHandler
    Public Event ValueChanged As EventHandler

    Private Function GetThumbHeight() As Integer
        Dim nTrackHeight As Integer = (Me.Height - (UpArrowImage.Height + DownArrowImage.Height))
        Dim fThumbHeight As Single = (CSng(LargeChange) / CSng(Maximum)) * nTrackHeight
        Dim nThumbHeight As Integer = CInt(fThumbHeight)

        If nThumbHeight > nTrackHeight Then
            nThumbHeight = nTrackHeight
            fThumbHeight = nTrackHeight
        End If

        If nThumbHeight < 56 Then
            nThumbHeight = 56
            fThumbHeight = 56
        End If

        Return nThumbHeight
    End Function

    Public Sub New()
        InitializeComponent()

        moChannelColor = Color.FromArgb(51, 166, 3)
        UpArrowImage = My.Resources.TopButton
        DownArrowImage = My.Resources.BottomButton
        ThumbBottomImage = My.Resources.ThumbCapBottom
        ThumbBottomSpanImage = My.Resources.ThumbFiller
        ThumbTopImage = My.Resources.ThumbCapTop
        ThumbTopSpanImage = My.Resources.ThumbFiller
        ThumbMiddleImage = My.Resources.ThumbCenter
    End Sub
    Public Property Size As Size
        Get
            Return New Size(Width, Height)
        End Get
        Set(value As Size)
            Width = value.Width
            Height = value.Height
        End Set
    End Property
    Public Property Location As Point
        Get
            Return New Point(X, Y)
        End Get
        Set(value As Point)
            X = value.X
            Y = value.Y
        End Set
    End Property
    Public Property LargeChange As Integer
        Get
            Return moLargeChange
        End Get
        Set(ByVal value As Integer)
            moLargeChange = value
        End Set
    End Property


    Public Property SmallChange As Integer
        Get
            Return moSmallChange
        End Get
        Set(ByVal value As Integer)
            moSmallChange = value
        End Set
    End Property


    Public Property Minimum As Integer
        Get
            Return moMinimum
        End Get
        Set(ByVal value As Integer)
            moMinimum = value
        End Set
    End Property


    Public Property Maximum As Integer
        Get
            Return moMaximum
        End Get
        Set(ByVal value As Integer)
            moMaximum = value

        End Set
    End Property


    Public Property Value As Integer
        Get
            Return moValue
        End Get
        Set(ByVal value As Integer)
            Try
                moValue = value
                Dim nTrackHeight As Integer = (Me.Height - (UpArrowImage.Height + DownArrowImage.Height))
                Dim fThumbHeight As Single = (CSng(LargeChange) / CSng(Maximum)) * nTrackHeight
                Dim nThumbHeight As Integer = CInt(fThumbHeight)

                If nThumbHeight > nTrackHeight Then
                    nThumbHeight = nTrackHeight
                    fThumbHeight = nTrackHeight
                End If

                If nThumbHeight < 56 Then
                    nThumbHeight = 56
                    fThumbHeight = 56
                End If

                Dim nPixelRange As Integer = nTrackHeight - nThumbHeight
                Dim nRealRange As Integer = (Maximum - Minimum) - LargeChange
                Dim fPerc As Single = 0.0F

                If nRealRange <> 0 Then
                    fPerc = CSng(moValue) / CSng(nRealRange)
                End If

                Dim fTop As Single = fPerc * nPixelRange
                moThumbTop = CInt(fTop)
            Catch ex As InvalidOperationException
            End Try
        End Set
    End Property


    Public Property ChannelColor As Color
        Get
            Return moChannelColor
        End Get
        Set(ByVal value As Color)
            moChannelColor = value
        End Set
    End Property


    Public Property UpArrowImage As Image
        Get
            Return moUpArrowImage
        End Get
        Set(ByVal value As Image)
            moUpArrowImage = value
        End Set
    End Property


    Public Property DownArrowImage As Image
        Get
            Return moDownArrowImage
        End Get
        Set(ByVal value As Image)
            moDownArrowImage = value
        End Set
    End Property


    Public Property ThumbTopImage As Image
        Get
            Return moThumbTopImage
        End Get
        Set(ByVal value As Image)
            moThumbTopImage = value
        End Set
    End Property


    Public Property ThumbTopSpanImage As Image
        Get
            Return moThumbTopSpanImage
        End Get
        Set(ByVal value As Image)
            moThumbTopSpanImage = value
        End Set
    End Property


    Public Property ThumbBottomImage As Image
        Get
            Return moThumbBottomImage
        End Get
        Set(ByVal value As Image)
            moThumbBottomImage = value
        End Set
    End Property


    Public Property ThumbBottomSpanImage As Image
        Get
            Return moThumbBottomSpanImage
        End Get
        Set(ByVal value As Image)
            moThumbBottomSpanImage = value
        End Set
    End Property


    Public Property ThumbMiddleImage As Image
        Get
            Return moThumbMiddleImage
        End Get
        Set(ByVal value As Image)
            moThumbMiddleImage = value
        End Set
    End Property

    Public Sub OnPaint(ByVal e As PaintEventArgs)
        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor



        Dim oBrush As Brush = New SolidBrush(moChannelColor)
        Dim oWhiteBrush As Brush = New SolidBrush(Color.FromArgb(255, 255, 255))
        e.Graphics.FillRectangle(oBrush, 0, 1, Me.Width - 2, Me.Height - 2)
        If UpArrowImage IsNot Nothing Then
            e.Graphics.DrawImage(UpArrowImage, 0, 0)
        End If
        Dim nTrackHeight As Integer = (Me.Height - (UpArrowImage.Height + DownArrowImage.Height))
        Dim fThumbHeight As Single = (CSng(LargeChange) / CSng(Maximum)) * nTrackHeight
        Dim nThumbHeight As Integer = CInt(fThumbHeight)

        If nThumbHeight > nTrackHeight Then
            nThumbHeight = nTrackHeight
            fThumbHeight = nTrackHeight
        End If

        If nThumbHeight < 56 Then
            nThumbHeight = 56
            fThumbHeight = 56
        End If
        'Width = ThumbMiddleImage.Width
        Dim fSpanHeight As Single = (fThumbHeight - (ThumbMiddleImage.Height + ThumbTopImage.Height + ThumbBottomImage.Height)) / 2.0F
        Dim nSpanHeight As Integer = CInt(fSpanHeight)
        Dim nTop As Integer = moThumbTop
        nTop += UpArrowImage.Height
        e.Graphics.DrawImage(ThumbTopImage, 0, nTop)
        nTop += ThumbTopImage.Height
        e.Graphics.DrawImage(ThumbTopSpanImage, 0.0F, CSng(nTop), CSng(Me.Width - 2), CSng(fSpanHeight * 1.3))
        nTop += nSpanHeight
        e.Graphics.DrawImage(ThumbMiddleImage, 0, nTop)
        nTop += ThumbMiddleImage.Height
        e.Graphics.DrawImage(ThumbBottomSpanImage, New Rectangle(0, nTop, Me.Width - 2, fSpanHeight * 1.3))

        nTop += nSpanHeight
        e.Graphics.DrawImage(ThumbBottomImage, 0, nTop)
        e.Graphics.DrawLine(Pens.Black, Me.Width - 3, 1, Me.Width - 3, Me.Height - 2)
        e.Graphics.DrawLine(Pens.Black, 0, 1, 0, Me.Height - 2)
        If DownArrowImage IsNot Nothing Then
            e.Graphics.DrawImage(DownArrowImage, 0, (Me.Height - DownArrowImage.Height))
        End If
    End Sub



    Private Sub InitializeComponent()
    End Sub

    Public Sub OnMouseDown(ByVal e As MouseEventArgs)
        Dim ptPoint As Point = e.Location
        Dim nTrackHeight As Integer = (Me.Height - (UpArrowImage.Height + DownArrowImage.Height))
        Dim fThumbHeight As Single = (CSng(LargeChange) / CSng(Maximum)) * nTrackHeight
        Dim nThumbHeight As Integer = CInt(fThumbHeight)

        If nThumbHeight > nTrackHeight Then
            nThumbHeight = nTrackHeight
            fThumbHeight = nTrackHeight
        End If

        If nThumbHeight < 56 Then
            nThumbHeight = 56
            fThumbHeight = 56
        End If

        Dim nTop As Integer = moThumbTop
        nTop += UpArrowImage.Height
        Dim thumbrect As Rectangle = New Rectangle(New Point(1, nTop), New Size(ThumbMiddleImage.Width, nThumbHeight))

        If thumbrect.Contains(ptPoint) Then
            nClickPoint = (ptPoint.Y - nTop)
            Me.moThumbDown = True
        End If

        Dim uparrowrect As Rectangle = New Rectangle(New Point(1, 0), New Size(UpArrowImage.Width, UpArrowImage.Height))

        If uparrowrect.Contains(ptPoint) Then
            Dim nRealRange As Integer = (Maximum - Minimum) - LargeChange
            Dim nPixelRange As Integer = (nTrackHeight - nThumbHeight)

            If nRealRange > 0 Then

                If nPixelRange > 0 Then

                    If (moThumbTop - SmallChange) < 0 Then
                        moThumbTop = 0
                    Else
                        moThumbTop -= SmallChange
                    End If

                    Dim fPerc As Single = CSng(moThumbTop) / CSng(nPixelRange)
                    Dim fValue As Single = fPerc * (Maximum - LargeChange)
                    moValue = CInt(fValue)
                    RaiseEvent ValueChanged(Me, New EventArgs())
                    RaiseEvent Scroll(Me, New EventArgs())
                End If
            End If
        End If

        Dim downarrowrect As Rectangle = New Rectangle(New Point(1, UpArrowImage.Height + nTrackHeight), New Size(UpArrowImage.Width, UpArrowImage.Height))

        If downarrowrect.Contains(ptPoint) Then
            Dim nRealRange As Integer = (Maximum - Minimum) - LargeChange
            Dim nPixelRange As Integer = (nTrackHeight - nThumbHeight)

            If nRealRange > 0 Then

                If nPixelRange > 0 Then

                    If (moThumbTop + SmallChange) > nPixelRange Then
                        moThumbTop = nPixelRange
                    Else
                        moThumbTop += SmallChange
                    End If

                    Dim fPerc As Single = CSng(moThumbTop) / CSng(nPixelRange)
                    Dim fValue As Single = fPerc * (Maximum - LargeChange)
                    moValue = CInt(fValue)
                    RaiseEvent ValueChanged(Me, New EventArgs())
                    RaiseEvent Scroll(Me, New EventArgs())
                End If
            End If
        End If
    End Sub

    Public Sub OnMouseUp(ByVal e As MouseEventArgs)
        Me.moThumbDown = False
        Me.moThumbDragging = False
    End Sub

    Private Sub MoveThumb(ByVal y As Integer)
        Try
            Dim nRealRange As Integer = Maximum - Minimum
            Dim nTrackHeight As Integer = (Me.Height - (UpArrowImage.Height + DownArrowImage.Height))
            Dim fThumbHeight As Single = (CSng(LargeChange) / CSng(Maximum)) * nTrackHeight
            Dim nThumbHeight As Integer = CInt(fThumbHeight)

            If nThumbHeight > nTrackHeight Then
                nThumbHeight = nTrackHeight
                fThumbHeight = nTrackHeight
            End If

            If nThumbHeight < 56 Then
                nThumbHeight = 56
                fThumbHeight = 56
            End If

            Dim nSpot As Integer = nClickPoint
            Dim nPixelRange As Integer = (nTrackHeight - nThumbHeight)

            If moThumbDown AndAlso nRealRange > 0 Then

                If nPixelRange > 0 Then
                    Dim nNewThumbTop As Integer = y - (UpArrowImage.Height + nSpot)

                    If nNewThumbTop < 0 Then
                        nNewThumbTop = 0
                        moThumbTop = nNewThumbTop
                    ElseIf nNewThumbTop > nPixelRange Then
                        nNewThumbTop = nPixelRange
                        moThumbTop = nNewThumbTop
                    Else
                        moThumbTop = y - (UpArrowImage.Height + nSpot)
                    End If

                    Dim fPerc As Single = CSng(moThumbTop) / CSng(nPixelRange)
                    Dim fValue As Single = fPerc * (Maximum - LargeChange)
                    moValue = CInt(fValue)
                End If
            End If
        Catch ex As InvalidOperationException
        End Try
    End Sub

    Public Sub OnMouseMove(ByVal e As MouseEventArgs)
        If moThumbDown = True Then
            Me.moThumbDragging = True
        End If

        If Me.moThumbDragging Then
            MoveThumb(e.Y)
            RaiseEvent ValueChanged(Me, New EventArgs())
            RaiseEvent Scroll(Me, New EventArgs())
        End If


    End Sub
End Class

Friend Class ChatRenderView : Inherits RenderViewBase
    Private IsVisible As Boolean = True
    Private HitboxCollection As New Dictionary(Of String, Hitbox)
    Private ScrollVelocity As Integer = 0
    Private Scrolling As Boolean = False
    Private ScrollOffset As Integer = 0
    Private CurrentComputedHeight As Single = 0
    Private WithEvents VScroller As VScrollBar = New VScrollBar
    Private RenderBounds As Rectangle
    Private IsComputingDimensions As Boolean = False
    Private MessageCollectionBase As ChatMessage() = {}
    Private MessageCollection As New List(Of ChatMessage)
    Private ImageOffset As Integer = 3
    Private ImageOffsetY As Integer = 19
    Private WithEvents CustomScroller As New CustomScrollbar
    Private BackgroundImage As Bitmap
    Private BackColor As Color
    Sub New(ByRef BkgndRef As Bitmap, ByRef BackColorRef As Color)
        AddHandler CustomScroller.Scroll, AddressOf CustomScrollerScrolling
        CustomScroller.ChannelColor = Color.FromArgb(0, 0, 0, 0)
        CustomScroller.ThumbBottomSpanImage = My.Resources.ThumbFiller
        CustomScroller.ThumbTopSpanImage = My.Resources.ThumbFiller
        CustomScroller.ThumbBottomImage = My.Resources.ThumbCapBottom
        CustomScroller.ThumbTopImage = My.Resources.ThumbCapTop
        CustomScroller.ThumbMiddleImage = My.Resources.ThumbCenter
        CustomScroller.UpArrowImage = My.Resources.TopButton
        CustomScroller.DownArrowImage = My.Resources.BottomButton
        BackgroundImage = BkgndRef
        BackColor = BackColorRef
    End Sub
    Public Sub SetBackground(ByRef BkgndRef As Bitmap)
        BackgroundImage = BkgndRef
    End Sub
    Public Sub SetBackColor(ByRef BackColorRef As Color)
        BackColor = BackColorRef
    End Sub
    Public Sub Add(Message As ChatMessage)
        MessageCollection.Add(Message)
        MessageCollectionBase = MessageCollection.ToArray
    End Sub
    Public Overrides Property Visible As Boolean
        Get
            Return IsVisible
        End Get
        Set(value As Boolean)
            IsVisible = value
        End Set
    End Property

    Public Property Font As Font

    Public Overrides ReadOnly Property Hitboxes As Dictionary(Of String, Hitbox)
        Get
            Return HitboxCollection
        End Get
    End Property
    Dim LastWidth As Integer
    Public Overrides Sub Render(GFX As Graphics, Bounds As Rectangle)
        Dim CapturedMessages As ChatMessage() = MessageCollectionBase
        Dim ServerNameFontSize As SizeF = GFX.MeasureString("server test name", Font) 'TODO: Add Server Name Variable
        Dim r = New Rectangle(4, 4, ServerNameFontSize.Width + 4, 16)
        If Hitboxes.ContainsKey("ServerButton") = False Then
            Hitboxes.Add("ServerButton", New Hitbox With {.Rectangle = r})
        Else
            Hitboxes("ServerButton").Rectangle = r
        End If
        RenderBounds = Bounds
        Dim CapturedScrollOffset As Integer = ScrollOffset
        If Visible = True Then
            If IsComputingDimensions = False Then
                IsComputingDimensions = True
                Dim ComputeThread As New Threading.Thread(Sub()
                                                              If CapturedMessages.Length > 0 Then
                                                                  Dim LocalComputedHeight As Single = 0
                                                                  Dim ArrayZeroedLength As Integer = CapturedMessages.Length - 1
                                                                  Dim LastMessage As ChatMessage = CapturedMessages(ArrayZeroedLength)
                                                                  Dim CurrentWidth As Integer = RenderBounds.Width - (ImageOffset * 2) - 19
                                                                  For x = 0 To ArrayZeroedLength
                                                                      Dim Item As ChatMessage = CapturedMessages(x)
                                                                      If Item.Width <> CurrentWidth Then
                                                                          Item.ComputeMessageDimensions(CurrentWidth)
                                                                      End If
                                                                      If Item Is LastMessage Then LocalComputedHeight += Item.Height + (ImageOffset * 2) + ImageOffsetY + (MessageCollectionBase.Length * 2) + 23 Else LocalComputedHeight += Item.Height + ImageOffset - 1
                                                                      LastWidth = Item.Width
                                                                  Next
                                                                  CurrentComputedHeight = LocalComputedHeight
                                                              End If
                                                              IsComputingDimensions = False
                                                          End Sub)
                ComputeThread.Start()
            End If

            If CapturedScrollOffset > CurrentComputedHeight - RenderBounds.Height Then
                CapturedScrollOffset = CurrentComputedHeight - RenderBounds.Height
                ScrollVelocity = 0
            ElseIf CapturedScrollOffset < 0 Then
                CapturedScrollOffset = 0
                ScrollVelocity = 0
            End If

            Dim BackgroundBrush As New SolidBrush(Nothing)
            BackgroundBrush.Color = Color.FromArgb(205, 64, 64, 64)
            GFX.FillRectangle(BackgroundBrush, 3, 24, RenderBounds.Width - 6, RenderBounds.Height - 48)
            BackgroundBrush.Color = BackColor
            If MessageCollectionBase.Length > 0 Then

                Dim ArrayZeroedLength As Integer = CapturedMessages.Length - 1
                Dim LastMessage As ChatMessage = CapturedMessages(ArrayZeroedLength)
                Dim MessageRenderOffset As Integer = 0
                For x = 0 To ArrayZeroedLength
                    Dim Item As ChatMessage = CapturedMessages(x)
                    If CapturedMessages(x).IsRendering = False Then
                        Dim ViewLocation As New Rectangle(4, MessageRenderOffset - CapturedScrollOffset + (ImageOffset * 2) + ImageOffsetY, Item.Width, Item.Height)
                        If RenderBounds.IntersectsWith(ViewLocation) = True Then
                            Item.RenderMessage()
                            GFX.DrawImage(Item.RenderOutput, ViewLocation)
                        End If
                    End If
                    If Item.RenderOutput IsNot Nothing Then
                        Item.RenderOutput.Dispose()
                        Item.RenderOutput = Nothing
                    End If
                    If Item Is LastMessage Then MessageRenderOffset += Item.Height + (ImageOffset * 2) + ImageOffsetY + (MessageCollectionBase.Length * 2) + 23 Else MessageRenderOffset += Item.Height + 5 - 1
                Next
            End If

            CustomScroller.Maximum = Math.Abs(CurrentComputedHeight - RenderBounds.Height + 10)
            CustomScroller.Location = New Point(RenderBounds.Width - CustomScroller.Width - 2, 25)
            CustomScroller.Size = New Size(18, RenderBounds.Height - 50)
            CustomScroller.Value = CapturedScrollOffset
            Dim ScrollerBMP As Bitmap = New Bitmap(CustomScroller.Size.Width, CustomScroller.Height, Imaging.PixelFormat.Format32bppPArgb)
            Dim TMPGFX As Graphics = Graphics.FromImage(ScrollerBMP)
            CustomScroller.OnPaint(New PaintEventArgs(TMPGFX, New Rectangle(0, 0, ScrollerBMP.Width, ScrollerBMP.Height)))
            GFX.DrawImage(ScrollerBMP, CustomScroller.Location)
            ScrollerBMP.Dispose()
            TMPGFX.Dispose()
            GFX.FillRectangle(BackgroundBrush, 0, RenderBounds.Height, RenderBounds.Width, 20)

            Dim BannerImage As New Bitmap(RenderBounds.Width, 24, Imaging.PixelFormat.Format32bppPArgb)
            Dim BannerGFX As Graphics = Graphics.FromImage(BannerImage)
            If BackgroundImage IsNot Nothing Then BannerGFX.DrawImage(BackgroundImage, 0, (RenderBounds.Height * -1) + 24, RenderBounds.Width, RenderBounds.Height)
            BannerGFX.Dispose()
            GFX.DrawImage(BannerImage, 0, RenderBounds.Height - 23)
            BannerImage.Dispose()
            BackgroundBrush.Color = Color.FromArgb(205, 64, 64, 64)


            GFX.DrawLine(Pens.Black, 3, RenderBounds.Height - 24, RenderBounds.Width - 4, RenderBounds.Height - 24)
            GFX.DrawLine(Pens.Black, 3, 23, RenderBounds.Width - 4, 23)
            GFX.DrawLine(Pens.Black, 2, 24, 2, RenderBounds.Height - 25)
            GFX.DrawLine(Pens.Black, RenderBounds.Width - 3, 24, RenderBounds.Width - 3, RenderBounds.Height - 25)


            GFX.FillRectangle(BackgroundBrush, 3, RenderBounds.Height - 21, RenderBounds.Width - 6, 18)
            GFX.DrawLine(Pens.Black, 3, RenderBounds.Height - 22, RenderBounds.Width - 4, RenderBounds.Height - 22)
            GFX.DrawLine(Pens.Black, 3, RenderBounds.Height - 3, RenderBounds.Width - 4, RenderBounds.Height - 3)
            GFX.DrawLine(Pens.Black, 2, RenderBounds.Height - 4, 2, RenderBounds.Height - 21)
            GFX.DrawLine(Pens.Black, RenderBounds.Width - 3, RenderBounds.Height - 4, RenderBounds.Width - 3, RenderBounds.Height - 21)
            GFX.DrawImage(My.Resources.Send_png, RenderBounds.Width - 20, RenderBounds.Height - 20)
            BackgroundBrush.Dispose()
        End If
    End Sub
    Private Sub CustomScrollerScrolling(sender As Object, e As EventArgs)
        ScrollOffset = CustomScroller.Value
    End Sub
    Public Overrides Sub OnMouseUp(e As MouseEventArgs)
        If Visible = True Then
            Dim RelativeCoords As MouseEventArgs = New MouseEventArgs(e.Button, e.Clicks, e.X - CustomScroller.Location.X, e.Y - CustomScroller.Location.Y, e.Delta)
            For x = 0 To Hitboxes.Keys.Count - 1
                If Hitboxes(Hitboxes.Keys(x)).Hovering = True Then
                    Select Case Hitboxes.Keys(x)
                        Case "ServerButton"
                            Visible = Not Visible
                    End Select
                End If
                'Hitboxes(Hitboxes.Keys(x)).Hovering = False
            Next
            CustomScroller.OnMouseUp(RelativeCoords)

        End If
    End Sub

    Public Overrides Sub OnMouseMove(e As MouseEventArgs)
        ' If Visible = True Then
        Dim RelativeCoords As MouseEventArgs = New MouseEventArgs(e.Button, e.Clicks, e.X - CustomScroller.Location.X, e.Y - CustomScroller.Location.Y, e.Delta)
            For x = 0 To Hitboxes.Keys.Count - 1
                If Hitboxes(Hitboxes.Keys(x)).Rectangle.Contains(e.Location) Then
                    Hitboxes(Hitboxes.Keys(x)).Hovering = True
                Else
                    Hitboxes(Hitboxes.Keys(x)).Hovering = False
                End If
            Next
            CustomScroller.OnMouseMove(RelativeCoords)
        'End If

    End Sub

    Public Overrides Sub OnMouseDown(e As MouseEventArgs)
        If Visible = True Then
            Dim RelativeCoords As MouseEventArgs = New MouseEventArgs(e.Button, e.Clicks, e.X - CustomScroller.Location.X, e.Y - CustomScroller.Location.Y, e.Delta)
            CustomScroller.OnMouseDown(RelativeCoords)
        End If
    End Sub

    Public Overrides Sub OnScrollwheel(e As MouseEventArgs)
        If Visible = True Then
            ScrollVelocity += e.Delta / 16
            If Scrolling = False Then
                Scrolling = True
                Dim t As New Threading.Thread(Sub()
                                                  Dim ScrollLimiter As New ThreadLimiter(60)
                                                  Do
                                                      Dim CapturedScrolloffset As Integer = ScrollOffset


                                                      If CurrentComputedHeight <= RenderBounds.Height Then
                                                          CapturedScrolloffset = 0

                                                          ScrollVelocity = 0
                                                          Exit Do
                                                      Else
                                                      End If




                                                      If ScrollOffset - ScrollVelocity > CurrentComputedHeight - RenderBounds.Height Then
                                                          CapturedScrolloffset = CurrentComputedHeight - RenderBounds.Height
                                                          ScrollVelocity = 0
                                                      ElseIf ScrollOffset - ScrollVelocity < 0 Then
                                                          CapturedScrolloffset = 0
                                                          ScrollVelocity = 0
                                                      End If



                                                      CapturedScrolloffset -= ScrollVelocity
                                                      If ScrollVelocity < 0 Then
                                                          ScrollVelocity -= (ScrollVelocity \ 18) - 1
                                                      ElseIf ScrollVelocity > 0 Then
                                                          ScrollVelocity -= (ScrollVelocity \ 18) + 1
                                                      End If
                                                      ScrollOffset = CapturedScrolloffset
                                                      CustomScroller.Value = CapturedScrolloffset
                                                      ScrollLimiter.Limit()
                                                  Loop Until ScrollVelocity = 0
                                                  ScrollVelocity = 0
                                                  Scrolling = False
                                              End Sub)

                t.Start()
            End If
        End If
    End Sub

    Public Overrides Sub OnResize(e As EventArgs)
    End Sub
End Class 'Chat RenderView
Friend Class Hitbox
    Public Property Rectangle As Rectangle
    Public Property Hovering As Boolean = False
End Class
Friend MustInherit Class RenderViewBase 'RenderView Abstraction
    Public MustOverride Sub Render(GFX As Graphics, Bounds As Rectangle)
    Public MustOverride Property Visible As Boolean
    Public MustOverride ReadOnly Property Hitboxes As Dictionary(Of String, Hitbox)
    Public MustOverride Sub OnMouseUp(e As MouseEventArgs)
    Public MustOverride Sub OnMouseMove(e As MouseEventArgs)
    Public MustOverride Sub OnMouseDown(e As MouseEventArgs)
    Public MustOverride Sub OnScrollwheel(e As MouseEventArgs)
    Public MustOverride Sub OnResize(e As EventArgs)
End Class 'RenderView Abstraction
Public Class ChatMessage
    Private _Width As Single = Nothing
    Private _CurrentWidth As Double = Nothing
    Private _Height As Single = Nothing
    Private OptimizedUserPFP As Bitmap = Nothing
    Private OptimizedMessageImage As Bitmap = Nothing

    Private _MessageText As String = Nothing
    Private _MessageTextWords As String() = Nothing
    Private _MessageTextLines As List(Of String) = Nothing
    Private _IsRendering As Boolean = False
    Private _IsComputing As Boolean = False
    Private WidthChanged As Boolean = False

    Public Property UserPFP As Bitmap
        Get
            Return OptimizedUserPFP
        End Get
        Set(value As Bitmap)
            If OptimizedUserPFP IsNot Nothing Then OptimizedUserPFP.Dispose()
            OptimizedUserPFP = New Bitmap(32, 32, Imaging.PixelFormat.Format32bppPArgb)
            Dim TMPGFX As Graphics = Graphics.FromImage(OptimizedUserPFP)
            TMPGFX.DrawImage(value, 0, 0, 32, 32)
            TMPGFX.Dispose()
        End Set
    End Property
    Public Property MessageImage As Bitmap
        Get
            Return OptimizedMessageImage
        End Get
        Set(value As Bitmap)
            If OptimizedMessageImage IsNot Nothing Then OptimizedMessageImage.Dispose()
            OptimizedMessageImage = New Bitmap(value.Width, value.Height, Imaging.PixelFormat.Format32bppPArgb)
            Dim TMPGFX As Graphics = Graphics.FromImage(OptimizedMessageImage)
            TMPGFX.DrawImage(value, 0, 0)
            TMPGFX.Dispose()
        End Set
    End Property
    Public ReadOnly Property Height As Integer
        Get
            Return _Height
        End Get
    End Property
    Public ReadOnly Property Width As Integer
        Get
            Return _Width
        End Get
    End Property
    Public Property MessageText As String
        Get
            Return _MessageText
        End Get
        Set(value As String)
            _MessageText = value
            _MessageTextWords = MessageText.Split({" "}, StringSplitOptions.RemoveEmptyEntries)
            If _MessageTextWords.Length = 0 Then
                _MessageTextWords = {value}
            End If
            If _MessageTextLines Is Nothing = True Then _MessageTextLines = New List(Of String) Else _MessageTextLines.Clear()
            _MessageTextLines.Add(_MessageText)
        End Set
    End Property
    Public ReadOnly Property IsRendering As Boolean
        Get
            Return _IsRendering
        End Get
    End Property
    Public ReadOnly Property IsComputing As Boolean
        Get
            Return _IsComputing
        End Get
    End Property
    Public Property MessageFont As Font = Nothing
    Public Property MessageForecolor As Color = Nothing
    Public Property MessageBackcolor As Color = Nothing
    Public Property RenderOutput As Bitmap = Nothing
    Public Property UserHandle As String = Nothing
    Public Property UserName As String = Nothing



    Public Sub ComputeMessageDimensions(Width As Double)
        _IsComputing = True
        Dim DummyBitmap As New Bitmap(1, 1) : Dim DummyGraphics As Graphics = Graphics.FromImage(DummyBitmap)
        WidthChanged = True
        _Width = Width
            _Height = 0
            Dim LineBuilder As New System.Text.StringBuilder
            Dim LineSize As SizeF
            If _MessageTextLines IsNot Nothing Then _MessageTextLines.Clear() Else _MessageTextLines = New List(Of String)
            If _MessageText <> Nothing Then
                Dim LastWord As String = _MessageTextWords(_MessageTextWords.Length - 1)
                Dim WordArray As String() = _MessageTextWords.ToArray
                Dim ZeroedListLength As Integer = WordArray.Length - 1
                Dim Word As String

                For x = 0 To ZeroedListLength
                    Word = WordArray(x)
                    If Word = LastWord And x = ZeroedListLength Then
                        LineBuilder.Append(Word)
                        Dim Measurable As String = String.Join("", {LineBuilder.ToString})
                        LineSize = DummyGraphics.MeasureString(Measurable, MessageFont)
                        _MessageTextLines.Add(Measurable)
                        LineBuilder.Clear()
                    Else

                        Dim Measurable As String = String.Join("", {LineBuilder.ToString, Word, " "})
                        LineSize = DummyGraphics.MeasureString(Measurable, MessageFont)


                        If LineSize.Width <= _Width Then
                            LineBuilder.Clear()
                            LineBuilder.Append(Measurable)
                        Else
                            _MessageTextLines.Add(LineBuilder.ToString)
                            LineBuilder.Clear()
                            LineBuilder.Append(Word)
                            LineBuilder.Append(" ")
                        End If
                    End If
                Next
            End If
            _Height += LineSize.Height * CSng(_MessageTextLines.Count)
            _CurrentWidth = _Width
        _Height += 36
        If RenderOutput Is Nothing Then
            WidthChanged = True
        End If
        DummyGraphics.Dispose()
        DummyBitmap.Dispose()
        _IsComputing = False
    End Sub
    Public Sub RenderMessage()
        If WidthChanged = True Or RenderOutput Is Nothing Then
            _IsRendering = True
            WidthChanged = False
            Const VerticalContentOffset As Integer = 36
            Dim RenderOffset As Single = 0
            Dim ZeroedCount As Integer = _MessageTextLines.Count - 1
            Dim CapturedLines As String() = _MessageTextLines.ToArray
            Using RenderedBitmap = New Bitmap(_Width, _Height, Imaging.PixelFormat.Format32bppPArgb)
                Using GFX As Graphics = Graphics.FromImage(RenderedBitmap)
                    Using BackgroundBrush As New SolidBrush(MessageBackcolor)
                        Using Fontbrush As New SolidBrush(MessageForecolor)
                            SyncLock RenderedBitmap
                                SyncLock GFX
                                    Dim LineSize As SizeF
                                    GFX.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias
                                    LineSize = GFX.MeasureString("|", MessageFont)
                                    GFX.Clear(Color.Transparent)
                                    GFX.FillRectangle(BackgroundBrush, 1, 1, _Width - 2, _Height - 2)
                                    GFX.DrawLine(Pens.Black, 0, 35, CSng(_Width - 1), 35)
                                    GFX.DrawImage(OptimizedUserPFP, 2, 2, 32, 32)
                                    GFX.DrawLine(Pens.Black, 1, 0, CSng(_Width - 2), 0)
                                    GFX.DrawLine(Pens.Black, 0, 1, 0, CSng(_Height - 2))
                                    GFX.DrawLine(Pens.Black, CSng(_Width - 1), 1, CSng(_Width - 1), CSng(_Height - 2))
                                    GFX.DrawLine(Pens.Black, 1, CSng(_Height - 1), CSng(_Width - 2), CSng(_Height - 1))
                                    GFX.DrawString(UserName, New Font(MessageFont.FontFamily, 11, FontStyle.Regular), Fontbrush, 34, 3)
                                    GFX.DrawString(UserHandle, New Font(MessageFont.FontFamily, 8, FontStyle.Regular), Fontbrush, 35, 19)
                                    If ZeroedCount > -1 Then
                                        For x = 0 To ZeroedCount
                                            GFX.DrawString(CapturedLines(x), MessageFont, Fontbrush, 0, RenderOffset + VerticalContentOffset)
                                            RenderOffset += LineSize.Height
                                        Next
                                    End If
                                    RenderOutput = RenderedBitmap.Clone
                                End SyncLock
                            End SyncLock
                        End Using
                    End Using
                End Using
            End Using
            CapturedLines = Nothing
            _IsRendering = False
        End If
    End Sub
End Class