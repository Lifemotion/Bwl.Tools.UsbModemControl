Public Class ModemExtendedInfo
    Public Property GsmMode As String = "" 'Режим GSM
    Public Property Rssi As Integer = -127 ' Сила сигнала
    Public Property Network As String = "" ' Сеть
    Public Property LastDataflowReport As DateTime ' Отчёт о потоке данных
    Public Property LastLinkReport As DateTime ' Отчёт о режиме связи
    Public Property ModemNumber As String = ""
    Public Property Version As String = ""
End Class