Imports Bwl.Framework

Public MustInherit Class Modem
    Protected _modemInfo As ModemInfo ' Информация о модеме
    Protected _state As ModemState = ModemState.notDetected ' Состояние модема
    Protected _port As New IO.Ports.SerialPort ' Serial-порт модема
    Protected _logger As Logger

    Public Property ExtendedInfo As New ModemExtendedInfo ' Расширенная информация
    Public Property Enabled As Boolean = True ' Включён
    Public Property APN As String = "internet" ' Доступ к интернету

    Public ReadOnly Property ModemInfo As ModemInfo
        Get
            Return _modemInfo ' Вывод информации о модеме
        End Get
    End Property

    Public ReadOnly Property State As ModemState
        Get
            Return _state
        End Get
    End Property

    Public MustOverride Sub Check()

    Public Sub New(portName As String, model As String, logger As Logger)
        _logger = logger
        _modemInfo = New ModemInfo(portName, model)
    End Sub
End Class

