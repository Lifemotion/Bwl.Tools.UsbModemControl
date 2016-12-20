Imports System.Threading
Imports Bwl.Network.ClientServer
Imports Bwl.Framework

Public Module App
    Private _app As New AppBase
    Private _appFormDescriptor As New AutoFormDescriptor(_app.AutoUI, "form") With {.ShowLogger = True, .LoggerExtended = False, .FormWidth = 480, .FormHeight = 500}
    Private WithEvents _appForm As AutoUIForm
    Private _modem As New ModemControl(_app.RootLogger, _app.AutoUI)
    Private WithEvents _netServer As New NetServer(3191)
    Private _appServer As New RemoteAppServer(_netServer, _app)
    Private _appBeacon As New NetBeacon(3191, "UsbModemControl", True, True)

    Private WithEvents _localServer As IMessageTransport
    Private _localRemoting As RemoteAppServer
    Private _netBeacon As NetBeacon
    Private WithEvents _port As IntegerSetting

    Private _managerCheckThread As Thread

    Sub Main()
        _modem.RunInThread()

        ' Нижеуказанный код создаёт сервер для удалённого подключения (например, по локальной сети)
        ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        _port = _app.RootStorage.CreateChildStorage("remoteAppSettings", "Настройки удалённого подключения").
            CreateIntegerSetting("port", 2460, "Порт для подключения", "Требуется перезагрузка")

        _localServer = New NetServer()
        _localRemoting = New RemoteAppServer(_localServer, _app.RootStorage, _app.RootLogger, _app.AutoUI)
        _netBeacon = New NetBeacon(_port.Value, "Tools.UsbModemControl", True, True)

        _localServer.Open("*:" + _port.Value.ToString, "")
        _localServer.RegisterMe("UsbModemControl", "", "Tools", "")
        _app.RootLogger.AddMessage("Сетевой сервер запущен, порт " + _port.Value.ToString())
        ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        'запуск локального интерфейса, если он не нужен, можно закомментировать
        Application.EnableVisualStyles()
        _appForm = AutoUIForm.Create(_app)
        _appForm.Icon = My.Resources.Icon

        _appForm.Show()

        _managerCheckThread = New Thread(Sub()
                                             ToolsManagerCheckThread()
                                         End Sub)
        _managerCheckThread.IsBackground = True
        _managerCheckThread.Start()

        Application.Run()
    End Sub


    Private Sub ToolsManagerCheckThread()
        Do
            _appForm.Invoke(Sub()
                                _appForm.Visible = Not CheckToolsManager()
                            End Sub)
            Thread.Sleep(5000)
        Loop
    End Sub
    Private Function CheckToolsManager() As Boolean
        Dim res = False
        Try

            Dim processes = Diagnostics.Process.GetProcesses()
            If processes IsNot Nothing AndAlso processes.Any Then
                For Each pr In processes
                    If pr.ProcessName.Contains("Tools.Manager") Then res = True
                Next
            End If

        Catch ex As Exception
            _app.RootLogger.AddError(ex.ToString())
        End Try
        Return res
    End Function

    Private Sub LocalServerReceivedMessage(message As NetMessage) Handles _localServer.ReceivedMessage
        If message.Part(0) = "AppLogic" Then
            If message.Part(1) = "CloseApp" Then
                _modem.StopThread()
                Application.Exit()
            End If
        End If
    End Sub

    Private Sub _netServer_ReceivedMessage(message As NetMessage, client As ConnectedClient) Handles _netServer.ReceivedMessage
        If message.Part(0) = "GetBriefState" Then
            client.SendMessage(New NetMessage("S", "BriefState", _modem.StateText, _modem.AdditionalText))
        End If
    End Sub

    Private Sub FormClosed() Handles _appForm.FormClosed
        _modem.StopThread()
        Application.Exit()
    End Sub

End Module
