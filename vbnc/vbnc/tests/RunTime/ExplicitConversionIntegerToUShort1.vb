Module ExplicitConversionIntegerToUShort1
    Function Main() As Integer
        Dim result As Boolean
        Dim value1 As Integer
        Dim value2 As UShort
        Dim const2 As UShort

        value1 = 50I
        value2 = CUShort(value1)
        const2 = CUShort(50I)

        result = value2 = const2

        If result = False Then
            System.Console.WriteLine("FAIL ExplicitConversionIntegerToUShort1")
            Return 1
        End If
    End Function
End Module
