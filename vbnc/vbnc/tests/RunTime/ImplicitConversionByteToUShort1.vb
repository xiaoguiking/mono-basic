Module ImplicitConversionByteToUShort1
    Function Main() As Integer
        Dim result As Boolean
        Dim value1 As Byte
        Dim value2 As UShort
        Dim const2 As UShort

        value1 = CByte(10)
        value2 = value1
        const2 = CByte(10)

        result = value2 = const2

        If result = False Then
            System.Console.WriteLine("FAIL ImplicitConversionByteToUShort1")
            Return 1
        End If
    End Function
End Module
