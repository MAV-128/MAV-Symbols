' MAV Symbols - Текстовый редактор
' Copyright © 2026 Andrey Morozov (MAV-128)
' Лицензия: GNU GPL v3.0


Option Explicit On
Public Class FFont
    Public Shadows Property Font As Font
    Private ReadOnly _allFonts As New List(Of String)
    Private _isUpdating As Boolean = False

    Public Sub ApplyLanguage()
        Me.Text = MAVLang.GetText("FFont_Title")
        GBSample.Text = MAVLang.GetText("FFont_GBSample")
        LFontName.Text = MAVLang.GetText("FFont_LFontName")
        LItBl.Text = MAVLang.GetText("FFont_LItBl")
        CBBold.Text = MAVLang.GetText("FFont_CBBold")
        CBItalic.Text = MAVLang.GetText("FFont_CBItalic")
        LSize.Text = MAVLang.GetText("FFont_LSize")
        BOk.Text = MAVLang.GetText("FFont_BOk")
        BCancel.Text = MAVLang.GetText("FFont_BCancel")
    End Sub

    Private Sub FFont_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ApplyLanguage()

        ' Оба ComboBox всегда в OwnerDrawFixed
        CBFontName.DrawMode = DrawMode.OwnerDrawFixed
        CBFontName.ItemHeight = 22
        CBFontName.DropDownStyle = ComboBoxStyle.Simple

        CBSize.DrawMode = DrawMode.OwnerDrawFixed
        CBSize.DropDownStyle = ComboBoxStyle.Simple

        For Each ff As FontFamily In FontFamily.Families
            If ff.IsStyleAvailable(FontStyle.Regular) Then
                _allFonts.Add(ff.Name)
            End If
        Next
        _allFonts.Sort()
        CBFontName.Items.Clear()
        CBFontName.Items.AddRange(_allFonts.ToArray())

        Dim sizes As Integer() = {8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72}
        CBSize.Items.Clear()
        For Each s In sizes
            CBSize.Items.Add(s.ToString())
        Next

        If Me.Font Is Nothing Then
            Me.Font = New Font("Microsoft Sans Serif", 10.0F, FontStyle.Regular)
        End If
        ApplyFont(Me.Font)
    End Sub

    Private Sub ApplyFont(f As Font)
        _isUpdating = True
        CBFontName.Text = f.FontFamily.Name
        CBSize.Text = CInt(f.Size).ToString()
        CBBold.Checked = f.Bold
        CBItalic.Checked = f.Italic
        _isUpdating = False
        UpdateSampleFont()
    End Sub

    ' Каждое имя шрифта рисуем им самим. Цвета берём из MAVDarkT (учитывает тему).
    ' Метод называется <ИмяЭлемента>_DrawItem, поэтому MAVDarkT не вмешивается в отрисовку.
    Private Sub CBFontName_DrawItem(sender As Object, e As DrawItemEventArgs) Handles CBFontName.DrawItem
        If e.Index < 0 Then Return

        Dim fontName As String = CBFontName.Items(e.Index).ToString()
        Dim drawFont As Font = Nothing
        Dim ownsFont As Boolean = False
        Try
            drawFont = New Font(fontName, 10.0F)
            ownsFont = True
        Catch
            drawFont = CBFontName.Font   ' это шрифт самого контрола - его нельзя освобождать
        End Try

        Try
            Dim backColor As Color
            Dim textColor As Color
            MAVDarkT.GetItemColors(e.State, backColor, textColor)

            Using brush As New SolidBrush(backColor)
                e.Graphics.FillRectangle(brush, e.Bounds)
            End Using

            Dim rect As Rectangle = e.Bounds
            rect.Inflate(-2, 0)

            Using sf As New StringFormat With {
                .LineAlignment = StringAlignment.Center,
                .Alignment = StringAlignment.Near,
                .Trimming = StringTrimming.EllipsisCharacter,
                .FormatFlags = StringFormatFlags.NoWrap
            }
                Using brush As New SolidBrush(textColor)
                    e.Graphics.DrawString(fontName, drawFont, brush, rect, sf)
                End Using
            End Using

            e.DrawFocusRectangle()
        Finally
            If ownsFont Then drawFont.Dispose()
        End Try
    End Sub

    Private Sub CBFontName_TextChanged(sender As Object, e As EventArgs) Handles CBFontName.TextChanged
        If _isUpdating Then Return
        HighlightMatch()
    End Sub

    Private Sub HighlightMatch()
        _isUpdating = True
        Dim text As String = CBFontName.Text
        If Not String.IsNullOrWhiteSpace(text) Then
            Dim match = _allFonts.FirstOrDefault(Function(f) f.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            If match IsNot Nothing Then
                Dim index As Integer = CBFontName.Items.IndexOf(match)
                If index >= 0 Then
                    CBFontName.SelectedIndex = index
                End If
            End If
            CBFontName.Text = text
            CBFontName.SelectionStart = text.Length
            CBFontName.SelectionLength = 0
        End If
        _isUpdating = False
    End Sub

    Private Function GetSelectedFont() As Font
        Dim fontName As String = CBFontName.Text
        Dim fontSize As Single
        If Not Single.TryParse(CBSize.Text, fontSize) Then
            fontSize = 10.0F
        End If
        Dim style As FontStyle = FontStyle.Regular
        If CBBold.Checked Then style = style Or FontStyle.Bold
        If CBItalic.Checked Then style = style Or FontStyle.Italic
        Try
            Return New Font(fontName, fontSize, style)
        Catch
            Return Me.Font
        End Try
    End Function

    Private Sub UpdateSampleFont()
        LSample.Font = GetSelectedFont()
    End Sub

    Private Sub CBFontName_SelectedIndexChanged(sender As Object, e As EventArgs) Handles CBFontName.SelectedIndexChanged
        UpdateSampleFont()
    End Sub

    Private Sub CBSize_TextChanged(sender As Object, e As EventArgs) Handles CBSize.TextChanged
        If _isUpdating Then Return
        UpdateSampleFont()
    End Sub

    Private Sub CBSize_SelectedIndexChanged(sender As Object, e As EventArgs) Handles CBSize.SelectedIndexChanged
        UpdateSampleFont()
    End Sub

    Private Sub CBBold_CheckedChanged(sender As Object, e As EventArgs) Handles CBBold.CheckedChanged
        UpdateSampleFont()
    End Sub

    Private Sub CBItalic_CheckedChanged(sender As Object, e As EventArgs) Handles CBItalic.CheckedChanged
        UpdateSampleFont()
    End Sub

    Private Sub CBSize_KeyPress(sender As Object, e As KeyPressEventArgs) Handles CBSize.KeyPress
        If Not Char.IsControl(e.KeyChar) AndAlso
           Not Char.IsDigit(e.KeyChar) AndAlso
           e.KeyChar <> "."c Then
            e.Handled = True
        End If
    End Sub

    Private Sub BOk_Click(sender As Object, e As EventArgs) Handles BOk.Click
        Me.Font = GetSelectedFont()
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub BCancel_Click(sender As Object, e As EventArgs) Handles BCancel.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)
        If Me.DialogResult = DialogResult.None Then
            Me.DialogResult = DialogResult.Cancel
        End If
    End Sub
End Class