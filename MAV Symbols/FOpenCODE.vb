' MAV Symbols - Текстовый редактор
' Copyright © 2026 Andrey Morozov (MAV-128)
' Лицензия: GNU GPL v3.0


Option Explicit On
Option Strict On
Imports System.IO
Imports System.Text
Public Class FOpenCODE
    ' Совместимость со старым кодом вызова
    Public Result As Boolean
    Public EncV As String
    Public FileN As String
    ' Готовый объект кодировки (Nothing, если отменили). Именно его нужно использовать вместо EncV
    Public Property SelectedEncoding As Encoding

    Private Const PreviewBytes As Integer = 64 * 1024
    Private Const PreviewLines As Integer = 300

    ' ==================== ЛОКАЛИЗАЦИЯ ====================
    Public Sub ApplyLanguage()
        Me.Text = MAVLang.GetText("FOpenCODE_Title")
        BOk.Text = MAVLang.GetText("FOpenCODE_BOk")
        BCancel.Text = MAVLang.GetText("FOpenCODE_BCancel")
        Label1.Text = MAVLang.GetText("FOpenCODE_Label1")
        Label2.Text = MAVLang.GetText("FOpenCODE_Label2")
        ' LDoc и LAd обновляются из FSymbols с уже переведёнными префиксами
    End Sub
    ' =====================================================

    Private Sub FOpenCODE_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'применяем текущий язык
        ApplyLanguage()
        Result = False
        EncV = Nothing
        SelectedEncoding = Nothing
        TextP.ReadOnly = True
        CBEnc.Sorted = False
        CBEnc.Items.Clear()

        Dim items As List(Of MAVEncItem) = MAVEncCatalog.BuildItems()
        CBEnc.Items.AddRange(items.ToArray())

        ' Выбор первого пункта (UTF-8) сам вызовет SelectedIndexChanged -> ShowPreview
        If CBEnc.Items.Count > 0 Then CBEnc.SelectedIndex = 0
    End Sub

    Private Sub CBEnc_SelectedIndexChanged(sender As Object, e As EventArgs) _
            Handles CBEnc.SelectedIndexChanged
        ShowPreview()
    End Sub

    Private Sub ShowPreview()
        Dim item As MAVEncItem = TryCast(CBEnc.SelectedItem, MAVEncItem)
        If item Is Nothing Then Return
        Try
            Dim enc As Encoding = item.Enc
            ' читаем только начало файла
            Dim buf(PreviewBytes - 1) As Byte
            Dim n As Integer
            Using fs As New FileStream(FileN, FileMode.Open, FileAccess.Read, FileShare.ReadWrite Or FileShare.Delete)
                n = fs.Read(buf, 0, buf.Length)
            End Using
            ' Decoder не ломает символ, обрезанный на границе буфера
            Dim dec As Decoder = enc.GetDecoder()
            Dim chars(enc.GetMaxCharCount(n) - 1) As Char
            Dim cnt As Integer = dec.GetChars(buf, 0, n, chars, 0, False)
            Dim text As String = New String(chars, 0, cnt).TrimStart(ChrW(&HFEFF)) ' убираем BOM

            ' ограничиваем число строк
            Dim sb As New StringBuilder()
            Using sr As New StringReader(text)
                Dim line As String = sr.ReadLine()
                Dim k As Integer = 0
                Do While line IsNot Nothing AndAlso k < PreviewLines
                    sb.AppendLine(line)
                    line = sr.ReadLine()
                    k += 1
                Loop
            End Using
            TextP.Text = sb.ToString()
        Catch ex As Exception
            ' ЛОКАЛИЗОВАННОЕ СООБЩЕНИЕ ОБ ОШИБКЕ
            TextP.Text = MAVLang.GetText("FOpenCODE_PreviewError") & ex.Message
        End Try
    End Sub

    Private Sub BOk_Click(sender As Object, e As EventArgs) Handles BOk.Click
        Dim item As MAVEncItem = TryCast(CBEnc.SelectedItem, MAVEncItem)
        If item Is Nothing Then Return
        SelectedEncoding = item.Enc
        EncV = item.Enc.WebName
        Result = True
        Me.Close()
    End Sub

    Private Sub BCancel_Click(sender As Object, e As EventArgs) Handles BCancel.Click
        SelectedEncoding = Nothing
        EncV = Nothing
        Result = False
        Me.Close()
    End Sub
End Class