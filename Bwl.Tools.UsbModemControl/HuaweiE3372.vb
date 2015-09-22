Imports System.IO.Ports

Public Class ModemExtendedInfo
    Public Property GsmMode As String = ""
    Public Property Rssi As Integer = -127
    Public Property Network As String = ""
    Public Property LastConnectedReport As DateTime
End Class

Public MustInherit Class Modem
    Protected _modemInfo As ModemInfo
    Protected _state As ModemState = ModemState.notDetected
    Protected _port As New IO.Ports.SerialPort

    Public Property ExtendedInfo As New ModemExtendedInfo
    Public Property Enabled As Boolean = True
    Public Property APN As String = "internet"

    Public ReadOnly Property ModemInfo As ModemInfo
        Get
            Return _modemInfo
        End Get
    End Property
    Public ReadOnly Property State As ModemState
        Get
            Return _state
        End Get
    End Property
    Public MustOverride Sub Check()

    Public Sub New(portName As String, model As String)
        _modemInfo = New ModemInfo(portName, model)
    End Sub
End Class

Public Enum ModemState
    notDetected
    connected
    connecting
    disabled
    fault
End Enum

Public Class HuaweiE3372
    Inherits Modem

    Public Sub New(portName As String)
        MyBase.New(portName, "E3372")
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
            _port.Write(vbCrLf + req + vbCrLf)
            Threading.Thread.Sleep(waitMs)
            Return _port.ReadExisting
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
        _state = ModemState.connecting

    End Sub

    Public Sub DisconnectModem()
        Dim response = ""
        response = Request("at^ndisconn=1,0")
        If response.Contains("OK" + vbCrLf) = False Then Throw New Exception("Modem at^ndisconn error")
    End Sub

    Public Sub ConnectModem()
        Dim response = ""

        response = Request("at+cgdcont=1,""ip"",""" + APN + """")
        If response.Contains("OK" + vbCrLf) = False Then Throw New Exception("Modem at+cgdcont error")
        response = Request("at^ndisconn=1,1")
        If response.Contains("OK" + vbCrLf) = False Then Throw New Exception("Modem at^ndisconn error")
        response = Request("at^DHCP?")
        response = Request("AT^SYSCFGEX=""00"",3FFFFFFF,1,2,800C5,, ")
        response = Request("AT^SYSCFGEX?")
        If response.Contains("^SYSCFGEX:""00"",3FFFFFFF,1,2,800C5") = False Then Throw New Exception("Modem at^ndisconn error")
        _lastConnectedReport = Now

        _state = ModemState.connected

    End Sub


    Private _lastConnectedReport As DateTime
    Public Sub CheckConnectedState()
        If (Now - _lastConnectedReport).TotalSeconds = 15 Then
            _state = ModemState.connecting
            ExtendedInfo.GsmMode = ""
        End If

        Dim read = _port.ReadExisting

        Dim lines = read.Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        For Each line In lines
            If line.Contains("^DSFLOWRPT:") Then _lastConnectedReport = Now : ExtendedInfo.LastConnectedReport = Now
            If line.Contains("^HCSQ:") Then
                Dim parts = line.Split({":", ","}, StringSplitOptions.RemoveEmptyEntries)
                If parts.Count > 1 Then
                    ExtendedInfo.GsmMode = parts(1)
                    ExtendedInfo.Rssi = CInt(parts(2)) - 121

                End If
            End If
        Next

    End Sub

    Public Overrides Sub Check()
        Try
            Select Case State
                Case ModemState.notDetected
                    OpenControlPort()
                    FirstModemCheck()
                Case ModemState.connecting
                    If Not Enabled Then DisconnectModem() Else ConnectModem()
                Case ModemState.connected
                    CheckConnectedState()
                    If Not Enabled Then DisconnectModem()
                Case ModemState.disabled
                    If Enabled Then _state = ModemState.connecting
            End Select
        Catch ex As Exception
        End Try
    End Sub

End Class
