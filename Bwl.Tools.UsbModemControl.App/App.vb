Imports Bwl.Framework

Public Module App
    Dim _app As New AppBase
    Dim _modem As New ModemControl(_app.RootLogger)
    Dim _modemForm As New ModemControlForm(_modem)

    Sub Main()
        Application.EnableVisualStyles()
        _modem.RunInThread()
        _modemForm.Show()
        Application.Run()
    End Sub
End Module
