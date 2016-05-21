Imports System.IO.Ports
Imports Bwl.Framework

Public Class HuaweiE3372
    Inherits Modem

    Public Sub New(portName As String, logger As Logger)
        Me.New(portName, "E3372", logger)
    End Sub

    Public Sub New(portName As String, identifier As String, logger As Logger)
        MyBase.New(portName, identifier, logger)
    End Sub

    Private Sub CloseControlPort()
        If _port IsNot Nothing Then
            Try
                _port.Close()
                _port.Dispose()
                _port = Nothing
            Catch ex As Exception
            End Try
        End If
    End Sub

    Private Sub OpenControlPort()
        CloseControlPort()
        _port = New SerialPort(ModemInfo.Port, 9600)
        _port.Open()
    End Sub

    Private Function Request(req As String, Optional waitMs As Integer = 500) As String
        Try
            _port.ReadExisting()
            _logger.AddDebug("PortWrite: " + req)
            _port.Write(vbCrLf + req + vbCrLf)
            Threading.Thread.Sleep(waitMs)
            Dim read = _port.ReadExisting
            _logger.AddDebug("PortRead: " + read)
            Return read
        Catch ex As Exception
            Return ""
        End Try
    End Function

    Private Sub FirstModemCheck()
        Dim ati = Request("ATI").ToLower
        If ati.Contains(ModemInfo.Model.ToLower) = False Then Throw New Exception("Modem not answerred to ATI command or model mismatch")
        Dim setportState = Request("at^setport?", 500)
        If setportState.Contains("^SETPORT:FF;12,16") = False Then
            Dim cmd = Request("at^setport=""FF;12,16""", 500)
            Dim setportStateNew = Request("at^setport?", 500)
            If setportStateNew.Contains("^SETPORT:FF;12,16") = False Then Throw New Exception("Modem at^setport failed!")
        End If
        _logger.AddInformation("Modem " + _modemInfo.Model + " " + _modemInfo.Port + " found, ready to connect")
        _state = ModemState.connecting
    End Sub

    Public Sub DisconnectModem()
        Dim response = ""
        response = Request("at^ndisconn=1,0")
        If response.Contains("OK" + vbCrLf) = False Then Throw New Exception("Modem at^ndisconn error")
    End Sub

    Public Sub ConnectModem()
        Dim response = ""
        'response = Request("AT+CFUN=1")
        _logger.AddInformation("Modem " + _modemInfo.Model + " " + _modemInfo.Port + " trying to connect")

        For i = 1 To 3
            response = Request("AT")
            If response.Contains("OK") Then Exit For
            Threading.Thread.Sleep(2000)
        Next
        If response.Contains("OK") = False Then
            'модем отвалился полностью
            _logger.AddError("Modem " + _modemInfo.Model + " " + _modemInfo.Port + " removed or completely fault")
            _state = ModemState.removed
            ExtendedInfo.GsmMode = ""
            ExtendedInfo.Rssi = -127
            CloseControlPort()
            CloseControlPort()
            CloseControlPort()

        End If

        response = Request("at+cgdcont=1,""ip"",""" + APN + """")
        If response.Contains("OK" + vbCrLf) = False Then Throw New Exception("Modem at+cgdcont error")
        response = Request("at^ndisconn=1,1")
        If response.Contains("OK" + vbCrLf) = False Then Throw New Exception("Modem at^ndisconn error")
        response = Request("at^DHCP?")
        response = Request("AT^SYSCFGEX=""00"",3FFFFFFF,1,2,800C5,, ")
        response = Request("AT^SYSCFGEX?")
        If response.Contains("^SYSCFGEX:""00"",3FFFFFFF,1,2,800C5") = False Then Throw New Exception("Modem at^ndisconn error")
        response = Request("AT^DSFLOWRPT=1")
        response = Request("AT^CURC=1")
        response = Request("AT^VERSION?")
        Dim lines = response.Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        ExtendedInfo.Version = ""
        For Each line In lines
            Dim parts = line.Split(":")
            If parts.Length > 2 AndAlso parts(2).Trim > "" Then ExtendedInfo.Version += line.Replace("^VERSION:", "") + vbCrLf
        Next
        ExtendedInfo.LastDataflowReport = Now
        ExtendedInfo.LastLinkReport = Now
        _state = ModemState.possiblyConnected
    End Sub

    Public Sub CheckConnectedState()
        If (Now - ExtendedInfo.LastLinkReport).TotalSeconds > 30 Then
            _state = ModemState.connecting
            ExtendedInfo.GsmMode = ""
            _logger.AddWarning("Modem " + _modemInfo.Model + " " + _modemInfo.Port + " no link reports to long, reconnecting")
        End If

        If (Now - ExtendedInfo.LastDataflowReport).TotalSeconds > 30 Then
            _state = ModemState.connecting
            _logger.AddWarning("Modem " + _modemInfo.Model + " " + _modemInfo.Port + " no dataflow reports to long, reconnecting")
            ExtendedInfo.GsmMode = ""
        End If

        Dim read = _port.ReadExisting
        _logger.AddDebug("PortRead: " + read)


        Dim lines = read.Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        For Each line In lines
            If line.Contains("^DSFLOWRPT:") Then
                ExtendedInfo.LastDataflowReport = Now
                If _state <> ModemState.connected Then
                    _state = ModemState.connected
                    _logger.AddInformation("Modem " + _modemInfo.Model + " " + _modemInfo.Port + " received dataflow report, connected!")
                End If
            End If
            If line.Contains("^HCSQ:") Then
                Dim parts = line.Split({":", ","}, StringSplitOptions.RemoveEmptyEntries)
                If parts.Count > 1 Then
                    ExtendedInfo.GsmMode = parts(1).Replace("""", "")
                    ExtendedInfo.Rssi = CInt(parts(2)) - 121
                    ExtendedInfo.LastLinkReport = Now
                    If (ExtendedInfo.ModemNumber = "") Then GetSimCardNumber()
                End If
            End If
        Next
    End Sub

    Public Sub GetSimCardNumber()
        Try
            Dim response = Request("AT+CUSD=1,""2A19AC3602"",15", 10000)
            If response.Contains("+CUSD: ") Then
                Dim responseList = response.Split(ControlChars.CrLf.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList()
                For Each line In responseList
                    If line.StartsWith("+CUSD: ") Then
                        Dim ucs2String = line.Split(ControlChars.Quote)(1)
                        Dim decodeResult = UCS2.DecodeUcs2(ucs2String)
                        ExtendedInfo.ModemNumber = "+" + decodeResult.Substring(11, decodeResult.Length - 11)
                    End If
                Next
            End If
        Catch ex As Exception
            ExtendedInfo.ModemNumber = "Error"
        End Try
    End Sub

    Public Overrides Sub Check()
        Try
            Select Case State
                Case ModemState.notDetected
                    OpenControlPort()
                    FirstModemCheck()
                Case ModemState.connecting
                    If Not Enabled Then DisconnectModem() Else ConnectModem()
                Case ModemState.possiblyConnected, ModemState.connected
                    CheckConnectedState()
                    If Not Enabled Then DisconnectModem()
                Case ModemState.disabled
                    If Enabled Then _state = ModemState.connecting
            End Select
        Catch ex As Exception
            _logger.AddWarning(ex.Message)
        End Try
    End Sub
End Class
