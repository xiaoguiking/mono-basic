Module ExplicitConversionSingleToLong1
    Function Main() As Integer
        Dim result As Boolean
        Dim value1 As Single
        Dim value2 As Long
        Dim const2 As Long

        value1 = 100.001!
        value2 = CLng(value1)
        const2 = CLng(100.001!)

        result = value2 = const2

        If result = False Then
            System.Console.WriteLine("FAIL ExplicitConversionSingleToLong1")
            Return 1
        End If
    End Function
End Module
