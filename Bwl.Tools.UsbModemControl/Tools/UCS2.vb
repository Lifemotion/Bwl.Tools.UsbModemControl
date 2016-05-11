Imports System.Text

Public Class UCS2
    Public Shared Function DecodeUcs2(origin As String) As String
        If origin.Count() Mod 2 = 0 Then
            Dim list As New List(Of Short)()
            Dim bytes As New List(Of Byte)()
            Dim encode = Encoding.GetEncoding("UCS-2")

            For i As Integer = 0 To origin.Count() - 1 Step 4
                list.Add(Convert.ToInt16(origin.Substring(i, 4), 16))
            Next

            For Each item In list
                bytes.Add(CByte(item And 255))
                bytes.Add(CByte(item >> 8))
            Next
            Return encode.GetString(bytes.ToArray())
        End If

        Throw New Exception("Cannot decode string!")
    End Function
End Class
