Imports Bwl.Framework
Imports Bwl.Hardware.UsbModemControl
Imports Bwl.Network.ClientServer

Module Service

    Dim _app As New AppBase
    Dim _appFormDescriptor As New AutoFormDescriptor(_app.AutoUI, "form") With {.ShowLogger = True, .LoggerExtended = False, .FormWidth = 480, .FormHeight = 500}
    Dim WithEvents _exitButton As New AutoButton(_app.AutoUI, "CloseService")
    Dim _modem As New ModemControl(_app.RootLogger, _app.AutoUI)
    Dim _appServer As New RemoteAppServer(3191, _app, "UsbModemControl", RemoteAppBeaconMode.broadcast)
    Dim WithEvents _netServer As NetServer = _appServer.NetServer
    Dim _running As Boolean = True

    Sub Main()
        _modem.RunInThread()
        Do While _running
            Threading.Thread.Sleep(500)
        Loop
        _modem.StopThread()
    End Sub

    Private Sub _netServer_ReceivedMessage(message As NetMessage, client As ConnectedClient) Handles _netServer.ReceivedMessage
        If message.Part(0) = "GetBriefState" Then
            client.SendMessage(New NetMessage("S", "BriefState", _modem.StateText, _modem.AdditionalText))
        End If
    End Sub

    Private Sub _exitButton_Click(source As AutoButton) Handles _exitButton.Click
        _running = False
    End Sub
End Module
