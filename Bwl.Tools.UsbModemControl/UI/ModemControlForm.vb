Imports Bwl.Framework

Public Class ModemControlForm
    Private _modem As ModemControl ' Модем

    Public Sub New(modem As ModemControl) ' Конструктор
        _modem = modem
        InitializeComponent()
    End Sub

    Private _displayingModems As Modem() = {} ' Отображение модемов

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Dim state As New List(Of String)
        state.Add("ModemControl " + _modem.State.ToString)

        state.Add("")

        If _modem.Modems IsNot Nothing Then

            For Each modem In _modem.Modems
                state.Add("Modem " + modem.ModemInfo.Port + " - " + modem.ModemInfo.Model)
                state.Add("--> Enabled: " + modem.Enabled.ToString)
                state.Add("--> State: " + modem.State.ToString)
                state.Add("--> LastLinkReport: " + modem.ExtendedInfo.LastLinkReport.ToLongTimeString)
                state.Add("--> LastDataflowReport: " + modem.ExtendedInfo.LastDataflowReport.ToLongTimeString)
                state.Add("--> GsmMode: " + modem.ExtendedInfo.GsmMode.ToString)
                state.Add("--> Rssi: " + modem.ExtendedInfo.Rssi.ToString)
                ' state.Add("--> Network: " + modem.ExtendedInfo.Network.ToString)
                state.Add("--> SIM Card Number: " + modem.ExtendedInfo.ModemNumber.ToString)
                state.Add("--> APN: " + modem.APN.ToString)
                state.Add("")
            Next

        End If

        Do While state.Count > modemsState.Items.Count
            modemsState.Items.Add("")
        Loop

        For i = 0 To state.Count - 1
            If modemsState.Items(i) <> state(i) Then
                modemsState.Items(i) = state(i)
            End If
        Next
    End Sub

    Private Sub ModemControlForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text += " " + Application.ProductVersion.ToString
        Try
            Dim time = IO.File.GetLastWriteTime(Application.ExecutablePath)
            Me.Text += " (" + time.ToString + ")"
        Catch ex As Exception
        End Try
    End Sub
End Class