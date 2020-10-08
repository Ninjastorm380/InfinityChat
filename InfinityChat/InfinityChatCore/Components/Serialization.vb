Public Class Serialization
    Public Shared Function SerializeArray(ByVal Data As Byte()()) As Byte()

        Dim OutputLength As Integer = 0
        For x = 0 To Data.Length - 1
            OutputLength += Data(x).Length
        Next
        OutputLength += (Data.Length * 4) + 4
        Dim DataOut(OutputLength - 1) As Byte

        Dim ItemIndex As Integer = 0
        Dim ItemCount As Byte() = BitConverter.GetBytes(Data.Length)
        DataOut(0) = ItemCount(0)
        DataOut(1) = ItemCount(1)
        DataOut(2) = ItemCount(2)
        DataOut(3) = ItemCount(3)
        ItemIndex += 4

        For x = 0 To Data.Length - 1
            Dim LengthBytes As Byte()
            LengthBytes = BitConverter.GetBytes(Data(x).Length)
            DataOut(0 + ItemIndex) = LengthBytes(0)
            DataOut(1 + ItemIndex) = LengthBytes(1)
            DataOut(2 + ItemIndex) = LengthBytes(2)
            DataOut(3 + ItemIndex) = LengthBytes(3)
            ItemIndex += 4
            For y = 0 To Data(x).Length - 1
                DataOut(y + ItemIndex) = Data(x)(y)
            Next
            ItemIndex += Data(x).Length
        Next

        Return DataOut
    End Function

    Public Shared Function DeserializeArray(ByVal Data As Byte()) As Byte()()

        Dim ByteIndex As Integer = 0

        Dim ItemCount As Integer = BitConverter.ToInt32({Data(0 + ByteIndex), Data(1 + ByteIndex), Data(2 + ByteIndex), Data(3 + ByteIndex)}, 0)
        ByteIndex += 4
        Dim DataOut(ItemCount - 1)() As Byte

        For Index = 0 To ItemCount - 1
            Dim ItemLength As Integer = BitConverter.ToInt32({Data(0 + ByteIndex), Data(1 + ByteIndex), Data(2 + ByteIndex), Data(3 + ByteIndex)}, 0)
            ByteIndex += 4
            Dim Item(ItemLength - 1) As Byte
            For x = 0 To ItemLength - 1
                Item(x) = Data(x + ByteIndex)
            Next
            DataOut(Index) = Item
            ByteIndex += ItemLength
        Next


        Return DataOut
    End Function
End Class
