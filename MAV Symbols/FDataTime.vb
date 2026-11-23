' MAV Symbols - Текстовый редактор
' Copyright © 2026 Andrey Morozov (MAV-128)
' Лицензия: GNU GPL v3.0


Option Explicit On
Public Class FDataTime
    Public DT As String
    Public Result As Boolean

    ' ==================== ЛОКАЛИЗАЦИЯ ====================
    Public Sub ApplyLanguage()
        Me.Text = MAVLang.GetText("FDataTime_Title")
        Label1.Text = MAVLang.GetText("FDataTime_Label1")
        BOk.Text = MAVLang.GetText("FDataTime_BOk")
        BCancel.Text = MAVLang.GetText("FDataTime_BCancel")
    End Sub
    ' =====================================================


    Private Sub BOk_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles BOk.Click
        If CBFormat.SelectedItem Is Nothing Then Return
        DT = CBFormat.SelectedItem.ToString()
        Result = True
        Me.Close()
    End Sub


    Private Sub FDataTime_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        ' применяем текущий язык
        ApplyLanguage()

        ' получаем актуальную дату
        Dim a As Date = Now
        ' суффикс "г." для русского языка (в английском пустой)
        Dim yearSuffix As String = MAVLang.GetText("DateTime_YearSuffix")

        ' создаем список дат
        CBFormat.Items.Clear()
        CBFormat.Items.Add((Format(a, "HH:mm dd.MM.yyyy")))
        CBFormat.Items.Add((Format(a, "dd.MM.yyyy")))
        CBFormat.Items.Add((Format(a, "dd.MM.yy")))
        CBFormat.Items.Add((Format(a, "d.M.yy")))
        CBFormat.Items.Add((Format(a, "dd/MM/yy")))
        CBFormat.Items.Add((Format(a, "yyyy-MM-dd")))
        CBFormat.Items.Add((Format(a, "dd MMMM yyyy") & yearSuffix))
        CBFormat.Items.Add((Format(a, "HH:mm")))
        CBFormat.Items.Add((Format(a, "HH:mm:ss")))

        CBFormat.SelectedIndex = 0

    End Sub

    Private Sub CBFormat_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles CBFormat.DoubleClick
        BOk.PerformClick()
    End Sub



    Private Sub BCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles BCancel.Click
        Result = False
        Me.Close()
    End Sub
End Class