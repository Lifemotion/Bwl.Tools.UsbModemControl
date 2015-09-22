Imports System.IO.Ports
Imports System.Threading
Imports Bwl.Framework

Public Class ModemInfo
    Public Sub New(port As String, model As String)
        Me.Port = port
        Me.Model = model
    End Sub
    Public Sub New()
        Me.Port = ""
        Me.Model = ""
    End Sub
    Public ReadOnly Property Port As String
    Public ReadOnly Property Model As String
End Class

Public Class ModemControl
    Private _logger As Logger
    ReadOnly Property State As ModemControlState = ModemControlState.modemsNotFound
    ReadOnly Property Modems As Modem()
    Private _thread As Threading.Thread

    Public Sub New(logger As Logger)
        _logger = logger
    End Sub

    Public Sub Run()
        Do
            Try
                Check()
            Catch ex As Exception
                _logger.AddError("ModemControl Check Error: " + ex.Message)
            End Try
            Thread.Sleep(1)
        Loop
    End Sub

    Public Sub RunInThread()
        If _thread IsNot Nothing Then Throw New Exception
        _thread = New Thread(AddressOf Run)
        _thread.Start()
    End Sub

    Public Sub Check()
        Select Case State
            Case ModemControlState.modemsNotFound
                FindModems()
            Case ModemControlState.modemsFound
                For Each modem In Modems
                    modem.Check
                Next
        End Select
    End Sub

    Public Shared Function DetectModems() As ModemInfo()
        Dim list As New List(Of ModemInfo)
        Dim ports = IO.Ports.SerialPort.GetPortNames
        For Each portName In ports
            Dim port As New IO.Ports.SerialPort(portName, 9600)
            Try
                port.Open()
                port.Write("ATI" + vbCrLf)
                Thread.Sleep(100)
                Dim read = port.ReadExisting.ToLower
                If read.Contains("huawei") Then
                    Dim e3372 = "E3372"
                    If read.Contains(e3372.ToLower) Then list.Add(New ModemInfo(portName, e3372))
                End If
            Catch ex As Exception
            End Try
            Try
                port.Close()
            Catch ex As Exception
            End Try
        Next
        Return list.ToArray
    End Function


    Private Sub FindModems()
        _logger.AddMessage("Trying to find modems...")
        Dim modems = DetectModems()
        If modems.Length = 0 Then
            _logger.AddMessage("Modems not found")
        Else
            Dim list As New List(Of Modem)
            For Each mi In modems
                If mi.Model = "E3372" Then list.Add(New HuaweiE3372(mi.Port))
            Next
            _Modems = list.ToArray
            _logger.AddMessage("Found modems: " + _Modems.Length.ToString)
            _State = ModemControlState.modemsFound
        End If
    End Sub
End Class
