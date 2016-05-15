Imports Bwl.Framework

Public Class MegafonM150_2
    Inherits HuaweiE3372

    Public Sub New(portName As String, logger As Logger)
        MyBase.New(portName, "M150-2", logger)
    End Sub
End Class
