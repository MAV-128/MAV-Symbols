' MAV Symbols - Текстовый редактор
' Copyright © 2026 Andrey Morozov (MAV-128)
' Лицензия: GNU GPL v3.0


Option Explicit On
Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.Text

' Элемент списка кодировок: объект кодировки + подпись
Friend NotInheritable Class MAVEncItem
    Public ReadOnly Property Enc As Encoding
    Public ReadOnly Property Title As String

    Public Sub New(enc As Encoding, title As String)
        Me.Enc = enc
        Me.Title = title
    End Sub

    Public Overrides Function ToString() As String
        Return Title
    End Function
End Class

' Общий каталог кодировок для диалогов «Открыть в кодировке» и «Сохранить в кодировке»
Friend NotInheritable Class MAVEncCatalog

    ' Популярные кодировки (кодовые страницы) в порядке показа
    Private Shared ReadOnly PopularCodePages As Integer() = {
        65001,  ' UTF-8
        1200,   ' UTF-16 LE
        1201,   ' UTF-16 BE
        1252,   ' Windows-1252 (Западная Европа)
        28591,  ' ISO-8859-1 (Latin-1)
        1251,   ' Windows-1251 (Кириллица)
        20866,  ' KOI8-R
        866,    ' CP866 (DOS, кириллица)
        20127,  ' US-ASCII
        936,    ' GBK / GB2312 (китайский упрощённый)
        54936,  ' GB18030
        950,    ' Big5 (китайский традиционный)
        932,    ' Shift_JIS (японский)
        51932,  ' EUC-JP
        949,    ' EUC-KR / UHC (корейский)
        1250    ' Windows-1250 (Центральная Европа)
    }

    ' Кодировки семейства Unicode (для них применимо понятие BOM)
    Public Shared Function IsUnicodeCp(cp As Integer) As Boolean
        Return cp = 65001 OrElse cp = 1200 OrElse cp = 1201 OrElse cp = 12000 OrElse cp = 12001
    End Function

    ' Полный список: сначала популярные, затем остальные по алфавиту
    Public Shared Function BuildItems() As List(Of MAVEncItem)
        Dim used As New HashSet(Of Integer)()
        Dim items As New List(Of MAVEncItem)()

        For Each cp As Integer In PopularCodePages
            Try
                Dim enc As Encoding = Encoding.GetEncoding(cp)
                If used.Add(enc.CodePage) Then
                    items.Add(New MAVEncItem(enc, enc.EncodingName & " (" & enc.WebName & ")"))
                End If
            Catch ex As Exception
                ' кодировка недоступна в этой среде - пропускаем
            End Try
        Next

        Dim others As New List(Of EncodingInfo)(Encoding.GetEncodings())
        others.Sort(Function(x, y) String.Compare(x.DisplayName, y.DisplayName, StringComparison.CurrentCultureIgnoreCase))
        For Each ei As EncodingInfo In others
            If Not used.Contains(ei.CodePage) Then
                Try
                    items.Add(New MAVEncItem(ei.GetEncoding(), ei.DisplayName & " (" & ei.Name & ")"))
                    used.Add(ei.CodePage)
                Catch ex As Exception
                End Try
            End If
        Next

        Return items
    End Function

    ' Кодировка с предсказуемой заменой непредставимых символов на '?' (без best-fit)
    Public Shared Function GetReplacing(cp As Integer) As Encoding
        Return Encoding.GetEncoding(cp, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback)
    End Function

End Class