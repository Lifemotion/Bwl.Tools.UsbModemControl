Public Class ModemInfo
	private _port as string=""
	private _model as string=""

    Public Sub New(port As String, model As String)
        _Port = port
        _Model = model
    End Sub

    Public Sub New()

    End Sub

    Public ReadOnly Property Port As String
            Get
            Return _port
        End Get
    End Property

    Public ReadOnly Property Model As String
        Get
            Return _model
        End Get
    End Property

    End Class