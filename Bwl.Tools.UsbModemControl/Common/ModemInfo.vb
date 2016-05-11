Public Class ModemInfo
    Public Sub New(port As String, model As String)
        Me.Port = port
        Me.Model = model
    End Sub
    Public Sub New()
        Me.Port = ""
        Me.Model = ""
    End Sub
    Public ReadOnly Property Port As String
    Public ReadOnly Property Model As String
End Class