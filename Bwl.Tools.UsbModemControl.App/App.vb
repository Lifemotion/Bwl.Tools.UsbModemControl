Imports Bwl.Network.ClientServer
Imports Bwl.Framework

Public Module App
    Dim _app As New AppBase
    Dim _appFormDescriptor As New AutoFormDescriptor(_app.AutoUI, "form") With {.ShowLogger = True}
    Dim _modem As New ModemControl(_app.RootLogger, _app.AutoUI)
    Dim _appServer As New RemoteAppServer(3191, _app)
    Dim WithEvents _netServer As NetServer = _appServer.NetServer

    Sub Main()
        _modem.RunInThread()

        'запуск локального интерфейса, если он не нужен, можно закомментировать
        Application.EnableVisualStyles()
        Application.Run(AutoUIForm.Create(_app))
        Application.Exit()
        End
    End Sub

    Private Sub _netServer_ReceivedMessage(message As NetMessage, client As ConnectedClient) Handles _netServer.ReceivedMessage
        If message.Part(0) = "GetBriefState" Then
            client.SendMessage(New NetMessage("S", "BriefState", _modem.StateText, _modem.AdditionalText))
        End If
    End Sub
End Module
