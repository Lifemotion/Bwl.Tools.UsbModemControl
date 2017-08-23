Imports System.IO.Ports
Imports System.Threading
Imports Bwl.Framework

Public Class ModemControl
    Private _logger As Logger
    private _state as ModemControlState = ModemControlState.modemsNotFound
    private _modems as New List(Of Modem)
    'Private _isConsoleMode As Boolean = False
    Private _noModemStateCount As Integer = 0
    Public Event NeedExitApp()

    ReadOnly Property State As ModemControlState
            Get
            Return _state
        End Get
    End Property

    ReadOnly Property Modems As  List(Of Modem)
           Get
            Return _modems
        End Get
    End Property

    Private _thread As Threading.Thread
    Private _autoui As AutoUI
    Private _autouiInfoList As AutoListbox
    Private _autouiState As AutoTextbox
    Private _autouiAdditional As AutoTextbox

    Public Sub New(logger As Logger, autoui As AutoUI)
        _logger = logger
        _autoui = autoui

        _autouiState = New AutoTextbox(_autoui, "State")
        _autouiAdditional = New AutoTextbox(_autoui, "Additional")
        _autouiInfoList = New AutoListbox(_autoui, "Modems Info")
    End Sub

    Public ReadOnly Property AutoUI As AutoUI
        Get
            Return _autoui
        End Get
    End Property

    Public ReadOnly Property StateText As String
        Get
            Return _autouiState.Text
        End Get
    End Property

    Public ReadOnly Property AdditionalText As String
        Get
            Return _autouiAdditional.Text
        End Get
    End Property

    Public Sub Run()
        Do
            _logger.AddInformation("MainThread")
            Try
                CreateStates()
            Catch ex As Exception
                _logger.AddError("ModemControl CreateStates Error: " + ex.Message)
            End Try
            Try
                Check()
            Catch ex As Exception
                _logger.AddError("ModemControl Check Error: " + ex.Message)
            End Try
            Try
                CreateStates()
            Catch ex As Exception
                _logger.AddError("ModemControl CreateStates Error: " + ex.Message)
            End Try
            Thread.Sleep(5000)
        Loop
    End Sub

    Private Sub CreateStates()
        Dim info = "NoModem"
        Dim add = "Modems not found"
        Dim list As New List(Of String)
        list.Add("ModemControl " + State.ToString)
        list.Add("")
        If Modems IsNot Nothing Then
            For Each modem In Modems
                list.Add("Modem " + modem.ModemInfo.Port + " - " + modem.ModemInfo.Model)
                list.Add("--> Enabled: " + modem.Enabled.ToString)
                list.Add("--> State: " + modem.State.ToString)
                list.Add("--> LastLinkReport: " + modem.ExtendedInfo.LastLinkReport.ToLongTimeString)
                list.Add("--> LastDataflowReport: " + modem.ExtendedInfo.LastDataflowReport.ToLongTimeString)
                list.Add("--> GsmMode: " + modem.ExtendedInfo.GsmMode.ToString)
                list.Add("--> Rssi: " + modem.ExtendedInfo.Rssi.ToString)
                ' state.Add("--> Network: " + modem.ExtendedInfo.Network.ToString)
                list.Add("--> SIM Card Number: " + modem.ExtendedInfo.ModemNumber.ToString)
                list.Add("--> APN: " + modem.APN.ToString)
                list.Add("")
                Dim lines = modem.ExtendedInfo.Version.Split(vbCrLf)
                For Each line In lines
                    list.Add("--> Version: " + line)
                Next
                If modem.State = ModemState.connected Then
                    If modem.ExtendedInfo.GsmMode > "" Then info = modem.ExtendedInfo.GsmMode Else info = "NoNetwork"
                    add = modem.ModemInfo.Model + ", " + modem.ExtendedInfo.Rssi.ToString + ", " + modem.APN.ToString
                Else
                    info = "Fault"
                    If modem.State = ModemState.connecting Then info = "Connecting"
                    add = modem.ModemInfo.Model + ", " + modem.ExtendedInfo.Rssi.ToString + ", " + modem.State.ToString
                End If
            Next
        End If

        'If _isConsoleMode Then
        '    Console.Clear()
        '    For Each s In list
        '        Console.WriteLine(s)
        '    Next
        'End If

        _autouiInfoList.Items.Replace(list.ToArray)
        _autouiState.Text = info
        _autouiAdditional.Text = add
    End Sub

    Public Function RunInThread()
        If _thread IsNot Nothing Then Throw New Exception
        _thread = New Thread(AddressOf Run)
        _thread.Start()
        Return _thread
    End Function

    Public Function RunInConsoleThread()
        '_isConsoleMode = True
        If _thread IsNot Nothing Then Throw New Exception
        _thread = New Thread(AddressOf Run)
        _thread.Start()
        Return _thread
    End Function

    Public Sub StopThread()
        Try
            _thread.Abort()
        Catch ex As Exception
        End Try
    End Sub

    Public Sub Check()
        Select Case State
            Case ModemControlState.modemsNotFound
                FindModems()
            Case ModemControlState.modemsFound
                For Each modem In Modems.ToArray
                    modem.Check()
                    If modem.State = ModemState.removed Then
                        Modems.Remove(modem)
                    End If
                Next
        End Select
        If Modems.Count = 0 Then
            _state = ModemControlState.modemsNotFound
            _noModemStateCount += 1
        Else
            _noModemStateCount = 0
        End If
        If _noModemStateCount > 6 Then
            _logger.AddWarning("NoModems 6 time -> restart")
            RaiseEvent NeedExitApp()
        End If
    End Sub

    Public Function DetectModems() As ModemInfo()
        Dim list As New List(Of ModemInfo)
        Dim ports = IO.Ports.SerialPort.GetPortNames
        For Each portName In ports
            Try
                _logger.AddDebug("Trying " + portName)
                If portName.ToLower.Contains("com") Or portName.ToLower.Contains("ttyusb") Then
                    Dim port As New IO.Ports.SerialPort(portName, 9600)
                    Try
                        port.Open()
                        port.WriteTimeout = 500
                        port.Write("Then ThenATI" + vbCrLf)
                        Thread.Sleep(500)
                        port.ReadTimeout = 500
                        Dim read = String.Empty
                        While port.BytesToRead > 0
                            read += port.ReadLine.ToLower() 'ReadExisting - не контролируется время таймаута и ПО зависает
                        End While
                        _logger.AddDebug("Readed from port " + read + port.BytesToRead.ToString)
                        If read.Contains("huawei") Then
                            Dim e3372 = "E3372"
                            If read.Contains(e3372.ToLower) Then list.Add(New ModemInfo(portName, e3372))
                            Dim e3372_megafon = "M150-2"
                            If read.Contains(e3372_megafon.ToLower) Then list.Add(New ModemInfo(portName, e3372_megafon))
                        End If
                    Catch ex As Exception
                        _logger.AddWarning(ex.ToString)
                    End Try
                    Try
                        port.Close()
                    Catch ex As Exception
                        _logger.AddWarning(ex.ToString)
                    End Try
                End If
            Catch ex As Exception
                _logger.AddWarning(ex.ToString)
            End Try
        Next
        Return list.ToArray
    End Function

    Private Sub FindModems()
        _logger.AddMessage("Trying To find modems...")
        Dim modems = DetectModems()
        If modems.Length = 0 Then
            _logger.AddMessage("Modems Not found")
        Else
            Dim list As New List(Of Modem)
            For Each mi In modems
                If mi.Model = "E3372" Then list.Add(New HuaweiE3372(mi.Port, _logger))
                If mi.Model = "M150-2" Then list.Add(New MegafonM150_2(mi.Port, _logger))
            Next
            _Modems = list
            _logger.AddMessage("Found modems: " + _Modems.Count.ToString)
            _State = ModemControlState.modemsFound
        End If
    End Sub
End Class
