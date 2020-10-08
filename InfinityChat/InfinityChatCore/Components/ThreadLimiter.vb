Public Class ThreadLimiter
    Private FPSStartTime As Long = System.Diagnostics.Stopwatch.GetTimestamp()
    Private FPSFrameCount As Long = 0
    Public Property IterationsPerSecond As Integer
    Sub New(Optional IterationsPerSecond As Integer = 60)
        Me.IterationsPerSecond = IterationsPerSecond
    End Sub
    Public Overridable Sub Limit()
        Dim freq As Long
        Dim frame As Long
        freq = System.Diagnostics.Stopwatch.Frequency
        frame = System.Diagnostics.Stopwatch.GetTimestamp()
        While (frame - FPSStartTime) * IterationsPerSecond < freq * FPSFrameCount
            Dim sleepTime As Integer = CInt(((FPSStartTime * IterationsPerSecond + freq * FPSFrameCount - frame * IterationsPerSecond) * 1000 / (freq * IterationsPerSecond)))
            If sleepTime > 0 Then System.Threading.Thread.Sleep(sleepTime)
            frame = System.Diagnostics.Stopwatch.GetTimestamp()
        End While
        If System.Threading.Interlocked.Increment(FPSFrameCount) > IterationsPerSecond Then
            FPSFrameCount = 0
            FPSStartTime = frame
        End If
    End Sub
End Class
