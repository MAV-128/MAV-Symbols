' MAV Symbols - Текстовый редактор
' Copyright © 2026 Andrey Morozov (MAV-128)
' Лицензия: GNU GPL v3.0


Option Explicit On
Imports System
Imports System.Text
Imports System.IO
Public Class FSaveCODE
    Public Result As Boolean
    Public BOM As Boolean              ' на входе: BOM документа; на выходе: BOM, выбранный пользователем
    Public EncV As String
    Public Samp As String
    Public InitialCodePage As Integer = 65001   ' кодировка, которая будет выбрана при открытии диалога
    ' Готовый объект кодировки (Nothing, если отменили). Используйте его вместо EncV
    Public Property SelectedEncoding As Encoding

    Private Const PreviewChars As Integer = 100000 ' в превью показываем только начало текста

    Private _bomUtf8 As Boolean         ' выбор BOM для UTF-8
    Private _bomOther As Boolean = True ' выбор BOM для UTF-16/UTF-32 (по умолчанию BOM нужен)
    Private _loadingList As Boolean
    Private _syncingBom As Boolean

    ' ==================== ЛОКАЛИЗАЦИЯ ====================
    Public Sub ApplyLanguage()
        Me.Text = MAVLang.GetText("FSaveCODE_Title")
        Label1.Text = MAVLang.GetText("FSaveCODE_Label1")
        BOk.Text = MAVLang.GetText("FSaveCODE_BOk")
        BCancel.Text = MAVLang.GetText("FSaveCODE_BCancel")
        ' LDoc заполняется из FSymbols с префиксом
        GroupBox1.Text = MAVLang.GetText("FSaveCODE_GroupBox1")
        LInfo.Text = MAVLang.GetText("FSaveCODE_LInfo")
        MLch.Text = MAVLang.GetText("FSaveCODE_MLch")
        ' BOMch НЕ переводим - это технический термин
    End Sub
    ' =====================================================

    Private Sub FSaveCODE_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        'применяем текущий язык
        ApplyLanguage()
        Result = False
        EncV = Nothing
        SelectedEncoding = Nothing
        ' BOM синхронизируем с документом: для каждой группы кодировок берём BOM документа только если сам документ
        ' в этой группе (UTF-16 без BOM не получит BOM лишь из-за открытия диалога, а UTF-8 - из-за BOM у UTF-16).
        ' Для остальных случаев: UTF-8 по умолчанию без BOM, UTF-16/32 по умолчанию с BOM.
        _bomUtf8 = BOM AndAlso InitialCodePage = 65001
        If InitialCodePage = 1200 OrElse InitialCodePage = 1201 OrElse InitialCodePage = 12000 OrElse InitialCodePage = 12001 Then
            _bomOther = BOM
        Else
            _bomOther = True
        End If

        _loadingList = True
        CBEnc.Items.Clear()
        Dim items As List(Of MAVEncItem) = MAVEncCatalog.BuildItems()
        CBEnc.Items.AddRange(items.ToArray())

        ' Выбираем текущую кодировку документа (если её нет в списке - первый пункт, UTF-8)
        Dim idx As Integer = 0
        For i As Integer = 0 To items.Count - 1
            If items(i).Enc.CodePage = InitialCodePage Then
                idx = i
                Exit For
            End If
        Next
        If CBEnc.Items.Count > 0 Then CBEnc.SelectedIndex = idx
        _loadingList = False

        MLch.Checked = TextP.WordWrap
        UpdateForSelection()
    End Sub

    Private Sub BCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles BCancel.Click
        Result = False
        SelectedEncoding = Nothing
        Me.Close()
    End Sub

    Private Sub BOk_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles BOk.Click
        Dim item As MAVEncItem = TryCast(CBEnc.SelectedItem, MAVEncItem)
        If item Is Nothing Then Return
        SelectedEncoding = item.Enc
        EncV = item.Enc.WebName
        BOM = BOMch.Enabled AndAlso BOMch.Checked
        Result = True
        Me.Close()
    End Sub

    Private Sub CBEnc_MouseDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles CBEnc.MouseDoubleClick
        BOk.PerformClick()
    End Sub

    Private Sub CBEnc_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CBEnc.SelectedIndexChanged
        If _loadingList Then Return
        UpdateForSelection()
    End Sub

    ' Обновляет состояние чекбокса BOM и превью под выбранную кодировку
    Private Sub UpdateForSelection()
        Dim item As MAVEncItem = TryCast(CBEnc.SelectedItem, MAVEncItem)
        If item Is Nothing Then Return
        Dim cp As Integer = item.Enc.CodePage

        _syncingBom = True
        If MAVEncCatalog.IsUnicodeCp(cp) Then
            ' BOM доступен для всех Unicode-кодировок (UTF-16/32 без BOM автодетект распознаёт плохо)
            BOMch.Enabled = True
            BOMch.Checked = If(cp = 65001, _bomUtf8, _bomOther)
        Else
            BOMch.Enabled = False
            BOMch.Checked = False
        End If
        _syncingBom = False

        ' Обновляем превью текста в выбранной кодировке
        ShowPreview(item.Enc)
    End Sub

    Private Sub BOMch_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles BOMch.CheckedChanged
        If _syncingBom Then Return
        Dim item As MAVEncItem = TryCast(CBEnc.SelectedItem, MAVEncItem)
        If item Is Nothing Then Return
        If item.Enc.CodePage = 65001 Then
            _bomUtf8 = BOMch.Checked
        ElseIf MAVEncCatalog.IsUnicodeCp(item.Enc.CodePage) Then
            _bomOther = BOMch.Checked
        End If
    End Sub

    ' ------------------------------------------------------------------
    ' Показывает в TextP, как будет выглядеть начало текста Samp при сохранении
    ' в кодировке enc (символы, непредставимые в enc, заменяются на '?').
    ' Для больших документов показывается только первые PreviewChars символов.
    ' ------------------------------------------------------------------
    Private Sub ShowPreview(enc As Encoding)
        If TextP Is Nothing Then Return
        If String.IsNullOrEmpty(Samp) Then
            TextP.Text = String.Empty
            Return
        End If

        Dim s As String = Samp
        If s.Length > PreviewChars Then
            Dim n As Integer = PreviewChars
            If Char.IsHighSurrogate(s(n - 1)) Then n -= 1 ' не режем суррогатную пару
            s = s.Substring(0, n)
        End If

        Try
            Dim safe As Encoding = MAVEncCatalog.GetReplacing(enc.CodePage)
            Dim preview As String = safe.GetString(safe.GetBytes(s))
            ' Для TextBox приводим все переводы строк к \r\n
            preview = preview.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Replace(vbLf, vbCrLf)
            TextP.Text = preview
        Catch ex As Exception
            TextP.Text = s
        End Try
    End Sub

    Private Sub MLch_CheckedChanged(sender As Object, e As EventArgs) Handles MLch.CheckedChanged
        TextP.WordWrap = MLch.Checked
    End Sub
End Class