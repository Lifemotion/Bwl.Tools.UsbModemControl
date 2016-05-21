Imports Bwl.Framework
Imports Bwl.Hardware.UsbModemControl
Imports Bwl.Network.ClientServer

Module Service

    Dim _app As New AppBase
    Dim _appFormDescriptor As New AutoFormDescriptor(_app.AutoUI, "form") With {.ShowLogger = True, .LoggerExtended = False, .FormWidth = 480, .FormHeight = 500}
    Dim _modem As New ModemControl(_app.RootLogger, _app.AutoUI)
    Dim _appServer As New RemoteAppServer(3191, _app, "UsbModemControl", RemoteAppBeaconMode.broadcast)

    Dim WithEvents _netServer As NetServer = _appServer.NetServer

    Sub Main()
        _modem.RunInThread()
        Do
            Console.WriteLine(Now.ToLongTimeString + " " + _modem.StateText + " " + _modem.AdditionalText)
            Threading.Thread.Sleep(2000)
            If Console.KeyAvailable Then
                Dim key = Console.ReadKey
                If key.Key = ConsoleKey.Escape Then End
            End If
        Loop
    End Sub

    Private Sub _netServer_ReceivedMessage(message As NetMessage, client As ConnectedClient) Handles _netServer.ReceivedMessage
        If message.Part(0) = "GetBriefState" Then
            client.SendMessage(New NetMessage("S", "BriefState", _modem.StateText, _modem.AdditionalText))
        End If
    End Sub

End Module
