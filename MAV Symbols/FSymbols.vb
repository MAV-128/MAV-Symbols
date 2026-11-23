' MAV Symbols - Текстовый редактор
' Copyright © 2026 Andrey Morozov (MAV-128)
' Лицензия: GNU GPL v3.0


Option Explicit On
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Globalization

Public Class FSymbols
    Dim WordW As Boolean 'перенос по словам
    Dim StatusB As Boolean 'отображение статусбара
    Dim FindP As Boolean 'панель поиска
    Dim Red As Boolean 'были ли совершены изменения в открытом документе
    Dim DocN As String 'только имя открытого документа
    Dim SaveD As String 'полный путь к открытому документу и его имя
    Dim WURL As Boolean 'подсвечивание ссылок в текстовом поле
    Dim ActENC As String 'кодировка открытого файла (для отображения)
    Dim ActCodePage As Integer = 65001 'реальная кодовая страница для сохранения/чтения
    Dim UTFbom As Boolean 'наличие BOM у файла
    Dim ZoomF As Single 'масштаб текста в текстовом поле
    Dim LSymbols As Long 'число символов в документе без пробелов
    Dim NewLineType As String = "CRLF" 'тип переноса строки (CRLF, LF, CR)
    Dim NewLineMixed As Boolean 'в открытом файле были смешанные переводы строк (при сохранении приводятся к одному типу)
    Private _lineCount As Integer = 1 'закэшированное число строк
    Private _linesDirty As Boolean = True 'число строк нужно пересчитать
    Private _recoveryDirty As Boolean 'есть изменения, ещё не записанные в файл восстановления
    Private _restoredDoc As Boolean 'в редакторе документ без привязки к файлу (восстановленный после сбоя или открытый с потерями): имя в DocN сохраняем при смене языка
    Private _suggestDir As String = "" 'папка, предлагаемая в диалоге "Сохранить как" для документа, открытого с потерями
    Private _savedBounds As Rectangle = Rectangle.Empty 'положение окна из файла настроек
    Private _savedMaximized As Boolean 'окно было развёрнуто при прошлом закрытии
    Private _inErrorHandler As Boolean 'защита от повторного входа в обработчик необработанных исключений
    Private Const BigFileLimit As Long = 134217728L 'файлы больше 128 МБ открываем только с подтверждением

    ' Темная тема приложения. Свойство-обёртка над MAVDarkT.IsDark
    Public Property DarkT As Boolean
        Get
            Return MAVDarkT.IsDark
        End Get
        Set(value As Boolean)
            MAVDarkT.SetDark(value)
        End Set
    End Property

    Private _loading As Boolean 'Флаг для предотвращения срабатывания TextChanged при программной загрузке
    Private Const EM_SETRECT As Integer = &HB3

    <StructLayout(LayoutKind.Sequential)>
    Private Structure RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure

    <DllImport("user32.dll")>
    Private Shared Function GetKeyState(ByVal nVirtKey As Integer) As Short
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function SendMessage(ByVal hWnd As IntPtr, ByVal msg As Integer, ByVal wParam As Integer, ByRef lParam As RECT) As Integer
    End Function

    Private Const VK_INSERT As Integer = &H2D

    <DllImport("user32.dll")>
    Private Shared Function SetProcessDPIAware() As Boolean
    End Function

    ' Таймер для дебаунса обновления статусбара (оптимизация производительности)
    Private WithEvents TStatus As New System.Windows.Forms.Timer With {.Interval = 120}

    ' Таймер автосохранения во временный файл восстановления (на случай сбоя или выключения питания)
    Private WithEvents TAuto As New System.Windows.Forms.Timer With {.Interval = 30000}

    Public Sub New()
        EnsureCodePages()
        Try
            If Environment.OSVersion.Version.Major >= 6 Then
                SetProcessDPIAware()
            End If
        Catch ex As Exception
            ' DPI-режим не критичен для работы
        End Try
        InitializeComponent()
    End Sub


    Private Shared Sub EnsureCodePages()
        Try
            Dim t As Type = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages", False)
            If t Is Nothing Then Return
            Dim inst As Object = t.GetProperty("Instance").GetValue(Nothing, Nothing)
            Dim m As System.Reflection.MethodInfo = GetType(Encoding).GetMethod("RegisterProvider")
            If inst IsNot Nothing AndAlso m IsNot Nothing Then m.Invoke(Nothing, New Object() {inst})
        Catch ex As Exception
        End Try
    End Sub

    ' ==================== ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ КОДИРОВОК И СОХРАНЕНИЯ ====================
    Private Shared Function IsUnicodeCp(cp As Integer) As Boolean
        Return cp = 65001 OrElse cp = 1200 OrElse cp = 1201 OrElse cp = 12000 OrElse cp = 12001
    End Function

    ' Кодировка для ЗАПИСИ: непредставимые символы заменяются на '?' предсказуемо (без best-fit)
    Private Shared Function BuildEncoding(cp As Integer, withBom As Boolean) As Encoding
        Select Case cp
            Case 65001 : Return New UTF8Encoding(withBom)
            Case 1200 : Return New UnicodeEncoding(False, withBom)
            Case 1201 : Return New UnicodeEncoding(True, withBom)
            Case 12000 : Return New UTF32Encoding(False, withBom)
            Case 12001 : Return New UTF32Encoding(True, withBom)
            Case Else : Return Encoding.GetEncoding(cp, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback)
        End Select
    End Function

    ' Кодировка для СТРОГОГО чтения: некорректные байты вызывают DecoderFallbackException
    Private Shared Function BuildStrictEncoding(cp As Integer) As Encoding
        Select Case cp
            Case 65001 : Return New UTF8Encoding(True, True)
            Case 1200 : Return New UnicodeEncoding(False, True, True)
            Case 1201 : Return New UnicodeEncoding(True, True, True)
            Case 12000 : Return New UTF32Encoding(False, True, True)
            Case 12001 : Return New UTF32Encoding(True, True, True)
            Case Else : Return Encoding.GetEncoding(cp, EncoderFallback.ReplacementFallback, DecoderFallback.ExceptionFallback)
        End Select
    End Function

    ' Единое отображаемое имя кодировки (для статусбара и заголовков)
    Private Shared Function FriendlyEncName(cp As Integer, fallback As String) As String
        Select Case cp
            Case 65001 : Return "UTF-8"
            Case 1200 : Return "UTF-16 LE"
            Case 1201 : Return "UTF-16 BE"
            Case 12000 : Return "UTF-32 LE"
            Case 12001 : Return "UTF-32 BE"
            Case 1251 : Return "Windows-1251"
            Case 1252 : Return "Windows-1252"
            Case 20866 : Return "KOI8-R"
            Case 866 : Return "CP866"
            Case 54936 : Return "GB18030"
            Case 932 : Return "Shift_JIS"
            Case 51949 : Return "EUC-KR"
            Case Else : Return If(String.IsNullOrEmpty(fallback), "CP" & cp.ToString(CultureInfo.InvariantCulture), fallback)
        End Select
    End Function

    Private Shared Function EncNameFromCp(cp As Integer) As String
        Dim n As String = ""
        Try
            n = Encoding.GetEncoding(cp).WebName
        Catch ex As Exception
        End Try
        Return FriendlyEncName(cp, n)
    End Function

    Private Shared Function ApplyNewLine(text As String, nl As String) As String
        If text.IndexOf(ControlChars.Cr) >= 0 Then text = text.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)
        Select Case nl
            Case "CRLF" : Return text.Replace(vbLf, vbCrLf)
            Case "CR" : Return text.Replace(vbLf, vbCr)
            Case Else : Return text
        End Select
    End Function

    ' Не удалось создать временный файл рядом с документом (нет прав на создание файлов в папке)
    Private NotInheritable Class DirectWriteRequiredException
        Inherits IOException
        Public Sub New(message As String, inner As Exception)
            MyBase.New(message, inner)
        End Sub
    End Class

    ' Безопасная запись: сначала во временный файл рядом с целевым, затем атомарная подмена.
    ' Если сбой произойдёт посреди записи, исходный файл останется нетронутым.
    ' Если временный файл создать нельзя, выбрасывается DirectWriteRequiredException: прямую запись поверх
    ' оригинала (WriteDocumentInPlace) вызывающий код выполняет только после подтверждения пользователя.
    Private Shared Sub WriteDocument(filePath As String, text As String, enc As Encoding)
        WriteDocumentAtomic(Path.GetFullPath(filePath), text, enc)
    End Sub

    Private Shared Function OpenTempForWrite(tmp As String) As FileStream
        Try
            Return New FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None)
        Catch ex As UnauthorizedAccessException
            Throw New DirectWriteRequiredException(ex.Message, ex)
        End Try
    End Function

    Private Shared Sub WriteDocumentAtomic(fullPath As String, text As String, enc As Encoding)
        Dim dir As String = Path.GetDirectoryName(fullPath)
        Dim tmp As String = Path.Combine(dir, "~" & Path.GetFileName(fullPath) & "." & Guid.NewGuid().ToString("N").Substring(0, 8) & ".tmp")
        Try
            Using fs As FileStream = OpenTempForWrite(tmp)
                Using sw As New StreamWriter(fs, enc, 65536)
                    sw.Write(text)
                    sw.Flush()
                    fs.Flush(True) ' сбрасываем данные на диск до подмены
                End Using
            End Using

            If File.Exists(fullPath) Then
                ReplaceExisting(tmp, fullPath)
            Else
                File.Move(tmp, fullPath)
            End If
        Finally
            Try
                If File.Exists(tmp) Then File.Delete(tmp)
            Catch ex As Exception
            End Try
        End Try
    End Sub

    ' Подменяет оригинал полностью записанным временным файлом.
    ' Ни один из путей не пишет в оригинал напрямую: при любой неудаче исходный файл остаётся нетронутым.
    Private Shared Sub ReplaceExisting(tmp As String, fullPath As String)
        Try
            File.Replace(tmp, fullPath, Nothing, True)
            Return
        Catch ex As Exception
            ' Файл только для чтения перезаписывать нельзя никаким способом
            If File.Exists(fullPath) AndAlso (File.GetAttributes(fullPath) And FileAttributes.ReadOnly) <> 0 Then Throw
        End Try

        ' Некоторые файловые системы/сетевые папки не поддерживают Replace
        If Not File.Exists(fullPath) Then
            File.Move(tmp, fullPath)
        Else
            SwapByRename(tmp, fullPath)
        End If
    End Sub

    ' Замена через переименования: оригинал -> .bak, временный файл -> оригинал. При ошибке оригинал возвращается на место.
    Private Shared Sub SwapByRename(tmp As String, fullPath As String)
        Dim bak As String = Path.Combine(Path.GetDirectoryName(fullPath), "~" & Path.GetFileName(fullPath) & "." & Guid.NewGuid().ToString("N").Substring(0, 8) & ".bak")
        File.Move(fullPath, bak) ' не удалось - оригинал не тронут, исключение уходит наверх
        Try
            File.Move(tmp, fullPath)
        Catch ex As Exception
            Try
                File.Move(bak, fullPath) ' откат
            Catch ex2 As Exception
            End Try
            Throw
        End Try
        Try
            File.Delete(bak)
        Catch ex As Exception
        End Try
    End Sub

    ' Запись прямо в существующий файл (только после подтверждения пользователя, когда безопасная замена невозможна).
    ' Перед записью копия оригинала сохраняется во временной папке; при ошибке содержимое восстанавливается.
    Private Shared Sub WriteDocumentInPlace(filePath As String, text As String, enc As Encoding)
        Dim fullPath As String = Path.GetFullPath(filePath)

        If Not File.Exists(fullPath) Then
            ' Оригинала нет - терять нечего
            Dim created As Boolean = False
            Try
                Using fs As New FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)
                    created = True
                    Using sw As New StreamWriter(fs, enc, 65536)
                        sw.Write(text)
                        sw.Flush()
                        fs.Flush(True)
                    End Using
                End Using
            Catch ex As Exception
                If created Then
                    Try
                        File.Delete(fullPath) ' не оставляем недописанный файл
                    Catch ex2 As Exception
                    End Try
                End If
                Throw
            End Try
            Return
        End If

        Dim bak As String = Path.Combine(Path.GetTempPath(), "MAVSymbols_" & Guid.NewGuid().ToString("N") & ".bak")
        File.Copy(fullPath, bak, True) ' нет копии - файл не трогаем
        Dim modified As Boolean = False
        Try
            Using fs As New FileStream(fullPath, FileMode.Open, FileAccess.Write, FileShare.Read)
                modified = True
                fs.SetLength(0)
                Using sw As New StreamWriter(fs, enc, 65536)
                    sw.Write(text)
                    sw.Flush()
                    fs.Flush(True)
                End Using
            End Using
        Catch ex As Exception
            If modified Then
                Try
                    File.Copy(bak, fullPath, True)
                Catch ex2 As Exception
                    ' Копия остаётся во временной папке: сообщаем пользователю, где она лежит
                    Throw New IOException(String.Format(LT("Msg_SaveRestoreFailed", "Не удалось записать файл и автоматически восстановить его прежнее содержимое. Копия исходного файла сохранена: {0}"), bak), ex)
                End Try
            End If
            Try
                File.Delete(bak)
            Catch ex3 As Exception
            End Try
            Throw
        End Try
        Try
            File.Delete(bak)
        Catch ex As Exception
        End Try
    End Sub

    ' Локализованная строка; если ключа нет в MAVLang - используется запасной (русский) текст
    Private Shared Function LT(key As String, fallback As String) As String
        Try
            If MAVLang.HasTranslation(key) Then
                Dim s As String = MAVLang.GetText(key)
                If Not String.IsNullOrEmpty(s) Then Return s
            End If
        Catch ex As Exception
        End Try
        Return fallback
    End Function

    ' Кодировка для ПРОВЕРКИ представимости текста: любой символ, который нельзя записать (в том числе одиночный
    ' суррогат в Unicode-кодировках), вызывает EncoderFallbackException
    Private Shared Function BuildStrictWriteEncoding(cp As Integer) As Encoding
        Select Case cp
            Case 65001 : Return New UTF8Encoding(False, True)
            Case 1200 : Return New UnicodeEncoding(False, False, True)
            Case 1201 : Return New UnicodeEncoding(True, False, True)
            Case 12000 : Return New UTF32Encoding(False, False, True)
            Case 12001 : Return New UTF32Encoding(True, False, True)
            Case Else : Return Encoding.GetEncoding(cp, EncoderFallback.ExceptionFallback, DecoderFallback.ReplacementFallback)
        End Select
    End Function

    Private Function ConfirmLossless(text As String, cp As Integer) As Boolean
        Try
            BuildStrictWriteEncoding(cp).GetByteCount(text)
            Return True
        Catch ex As EncoderFallbackException
            ' Unicode-кодировки (и GB18030) могут не принять только одиночные суррогаты, остальные - любые "чужие" символы
            Dim key As String = If(IsUnicodeCp(cp) OrElse cp = 54936, "Msg_LoneSurrogates", "Msg_LossyEncoding")
            Return MessageBox.Show(MAVLang.GetText(key), Application.ProductName,
                                   MessageBoxButtons.YesNo, MessageBoxIcon.Warning) = Windows.Forms.DialogResult.Yes
        End Try
    End Function

    Private Function SaveToPath(filePath As String, cp As Integer, withBom As Boolean) As Boolean
        Try
            Dim text As String = ApplyNewLine(PrimeTEXT.Text, NewLineType)
            If Not ConfirmLossless(text, cp) Then Return False
            Dim target As Encoding = BuildEncoding(cp, withBom)
            Try
                WriteDocument(filePath, text, target)
            Catch ex As DirectWriteRequiredException
                ' Безопасная замена невозможна: прямую запись поверх существующего файла делаем только с согласия пользователя
                If File.Exists(Path.GetFullPath(filePath)) Then
                    If MessageBox.Show(LT("Msg_SaveDirectWarning", "Не удаётся создать временный файл в папке документа, поэтому безопасная замена файла невозможна. Файл можно записать только напрямую: копия оригинала будет сохранена во временной папке и восстановлена при ошибке записи, но при внезапном сбое (отключение питания, закрытие процесса) документ может оказаться повреждён. Продолжить?"),
                                       Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                                       MessageBoxDefaultButton.Button2) <> Windows.Forms.DialogResult.Yes Then Return False
                End If
                WriteDocumentInPlace(filePath, text, target)
            End Try
            NewLineMixed = False ' после сохранения переводы строк единообразны
            UpdateNewLineLabel()
            Return True
        Catch ex As Exception
            MessageBox.Show(MAVLang.GetText("Msg_ErrorSaving") & ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    Private Function ShowSaveDialog(title As String) As String
        Using sd As New SaveFileDialog
            sd.FileName = DocN
            sd.Title = title
            If Not String.IsNullOrEmpty(_suggestDir) AndAlso Directory.Exists(_suggestDir) Then sd.InitialDirectory = _suggestDir
            sd.Filter = MAVLang.GetText("Filter_TextFiles") & "|*.txt|" & MAVLang.GetText("Filter_AllFiles") & "|*.*"
            sd.FilterIndex = If(String.Equals(Path.GetExtension(DocN), ".txt", StringComparison.OrdinalIgnoreCase), 1, 2)
            If sd.ShowDialog() = Windows.Forms.DialogResult.OK Then Return sd.FileName
        End Using
        Return Nothing
    End Function

    Private Function SaveDocument() As Boolean
        If SaveD = "" Then Return SaveDocumentAs(True) = Windows.Forms.DialogResult.OK
        If Not SaveToPath(SaveD, ActCodePage, UTFbom) Then Return False
        Red = False
        DeleteRecovery()
        ObnovlenieTextL()
        ObnovlenieFUN()
        Return True
    End Function

    Private Function SaveDocumentAs(ByVal AutoFormat As Boolean) As DialogResult
        Dim cp As Integer = ActCodePage
        Dim bom As Boolean = UTFbom
        Dim encName As String = ActENC

        If Not AutoFormat Then
            ' Каждый раз новый экземпляр формы: никакого "залипшего" состояния от прошлого вызова
            Using f As New FSaveCODE
                f.LDoc.Text = MAVLang.GetText("FSaveCODE_LDoc") & DocN
                f.BOM = UTFbom
                f.Samp = PrimeTEXT.Text
                f.InitialCodePage = ActCodePage
                f.ApplyLanguage()
                f.ShowDialog()
                If Not f.Result OrElse f.SelectedEncoding Is Nothing Then Return Windows.Forms.DialogResult.Cancel

                cp = f.SelectedEncoding.CodePage
                encName = FriendlyEncName(cp, f.SelectedEncoding.WebName)
                bom = f.BOM
            End Using
        End If

        Dim showBom As Boolean = bom AndAlso IsUnicodeCp(cp)
        Dim target As String = ShowSaveDialog(MAVLang.GetText("Msg_SaveInEncoding") & encName & If(showBom, " BOM", ""))
        If target Is Nothing Then Return Windows.Forms.DialogResult.Cancel

        If Not SaveToPath(target, cp, bom) Then Return Windows.Forms.DialogResult.Cancel

        ActCodePage = cp : ActENC = encName : UTFbom = bom
        slEnc.Text = encName
        slBOM.Text = If(showBom, "BOM", "")
        DocN = Path.GetFileName(target)
        SaveD = target
        _restoredDoc = False
        _suggestDir = ""
        Me.Text = Application.ProductName + " - " + DocN
        Red = False
        DeleteRecovery()
        ObnovlenieTextL()
        ObnovlenieFUN()
        Return Windows.Forms.DialogResult.OK
    End Function

    Private Function SaveDocumentClose() As DialogResult
        If Not Red Then Return Windows.Forms.DialogResult.None
        Select Case MessageBox.Show(MAVLang.GetText("Msg_SaveChanges") & DocN & "?", Application.ProductName,
                                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)
            Case Windows.Forms.DialogResult.Yes
                If Not SaveDocument() Then Return Windows.Forms.DialogResult.Cancel
                Return Windows.Forms.DialogResult.Yes
            Case Windows.Forms.DialogResult.No
                Return Windows.Forms.DialogResult.No
            Case Else
                Return Windows.Forms.DialogResult.Cancel
        End Select
    End Function

    ' ==================== ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ ОТКРЫТИЯ ====================
    Private Shared Function ReadAllTextShared(filePath As String, enc As Encoding) As String
        Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite Or FileShare.Delete)
            Using sr As New StreamReader(fs, enc, False)
                Return sr.ReadToEnd()
            End Using
        End Using
    End Function

    Private Shared Function FileStartsWith(filePath As String, pre As Byte()) As Boolean
        If pre.Length = 0 Then Return False
        Dim buf(pre.Length - 1) As Byte
        Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite Or FileShare.Delete)
            ' Цикл чтения для корректной работы с медленными сетевыми дисками
            Dim total As Integer = 0
            While total < buf.Length
                Dim n As Integer = fs.Read(buf, total, buf.Length - total)
                If n <= 0 Then Exit While
                total += n
            End While
            If total < buf.Length Then Return False
        End Using
        For i As Integer = 0 To pre.Length - 1
            If buf(i) <> pre(i) Then Return False
        Next
        Return True
    End Function

    Private Shared Function DetectNewLineType(text As String, ByRef mixed As Boolean) As String
        Dim crlf, lf, cr As Integer
        Dim i As Integer = 0
        While i < text.Length
            Dim c As Char = text(i)
            If c = ControlChars.Cr Then
                If i + 1 < text.Length AndAlso text(i + 1) = ControlChars.Lf Then
                    crlf += 1
                    i += 1
                Else
                    cr += 1
                End If
            ElseIf c = ControlChars.Lf Then
                lf += 1
            End If
            i += 1
        End While
        Dim kinds As Integer = 0
        If crlf > 0 Then kinds += 1
        If lf > 0 Then kinds += 1
        If cr > 0 Then kinds += 1
        mixed = kinds > 1
        If crlf >= lf AndAlso crlf >= cr Then Return "CRLF"
        If lf >= cr Then Return "LF"
        Return "CR"
    End Function

    ' Читает весь файл СТРОГО: если в нём есть байты, недопустимые в выбранной кодировке, спрашивает пользователя
    ' (иначе они молча превратятся в U+FFFD и будут потеряны при сохранении). Возвращает Nothing, если пользователь отказался.
    ' lossy = True, если пользователь согласился открыть файл с заменой недопустимых байтов: такой документ нельзя
    ' сохранять поверх оригинала (см. OpenDocument).
    Private Function ReadDocumentText(filePath As String, cp As Integer, ByRef lossy As Boolean) As String
        lossy = False
        Try
            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite Or FileShare.Delete)
                Using sr As New StreamReader(fs, BuildStrictEncoding(cp), False)
                    Return sr.ReadToEnd()
                End Using
            End Using
        Catch ex As DecoderFallbackException
            ' переходим к вопросу пользователю
        End Try

        If MessageBox.Show(MAVLang.GetText("Msg_DecodeErrors") & vbCrLf & vbCrLf & LT("Msg_LossyNoOverwrite", "Чтобы не повредить исходный файл, сохранить этот документ можно будет только через ""Сохранить как""."), Application.ProductName,
                           MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) <> Windows.Forms.DialogResult.Yes Then
            Return Nothing
        End If
        lossy = True
        Return ReadAllTextShared(filePath, BuildEncoding(cp, True))
    End Function

    ' Присваивание текста редактору. Текст, начинающийся с "{\", вставляется через SelectedText,
    ' чтобы RichTextBox ни при каких условиях не принял обычный текст за RTF-разметку.
    Private Sub SetEditorText(text As String)
        If text.StartsWith("{\", StringComparison.Ordinal) Then
            PrimeTEXT.Clear()
            PrimeTEXT.SelectedText = text
            PrimeTEXT.Select(0, 0)
        Else
            PrimeTEXT.Text = text
        End If
    End Sub

    Private Sub UpdateNewLineLabel()
        slNL.Text = MAVLang.GetText("NewLine_" & NewLineType) & If(NewLineMixed, MAVLang.GetText("NewLine_MixedSuffix"), "")
    End Sub

    Private Function OpenDocument(ByVal FileName As String, ByVal AutoEnc As Boolean) As Boolean
        Try
            If Not File.Exists(FileName) Then
                MessageBox.Show(MAVLang.GetText("Msg_ErrorOpening") & MAVLang.GetText("Msg_FileNotFound"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return False
            End If

            Dim fileLen As Long = New FileInfo(FileName).Length
            If fileLen > BigFileLimit Then
                If MessageBox.Show(String.Format(MAVLang.GetText("Msg_BigFile"), CInt(fileLen \ 1048576L)), Application.ProductName,
                                   MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) <> Windows.Forms.DialogResult.Yes Then Return False
            End If

            Dim cp As Integer
            Dim encName As String
            Dim bom As Boolean

            If AutoEnc Then
                Dim res As MAVEnc = New MAVEnc(FileName).DetectEnc()
                cp = res.CodePage : bom = res.BOM
                If cp = 20127 Then cp = 65001 ' ASCII -> UTF-8, чтобы не терять не-ASCII символы при сохранении
                encName = FriendlyEncName(cp, res.EncName)
            Else
                ' Каждый раз новый экземпляр формы: превью всегда соответствует выбранному файлу
                Using f As New FOpenCODE
                    f.FileN = FileName
                    f.LDoc.Text = MAVLang.GetText("FOpenCODE_LDoc") & Path.GetFileName(FileName)
                    f.LAd.Text = MAVLang.GetText("FOpenCODE_LAd") & FileName
                    f.ApplyLanguage()
                    f.ShowDialog()
                    If Not f.Result OrElse f.SelectedEncoding Is Nothing Then Return False
                    cp = f.SelectedEncoding.CodePage
                    encName = FriendlyEncName(cp, f.SelectedEncoding.WebName)
                End Using
                bom = FileStartsWith(FileName, BuildEncoding(cp, True).GetPreamble())
            End If

            ' Читаем ДО любых изменений состояния: при ошибке редактор и SaveD остаются нетронутыми
            Dim text As String
            Dim lossy As Boolean = False
            Me.UseWaitCursor = True
            Try
                text = ReadDocumentText(FileName, cp, lossy)
            Finally
                Me.UseWaitCursor = False
            End Try
            If text Is Nothing Then Return False

            If text.IndexOf(ChrW(0)) >= 0 Then
                If MessageBox.Show(MAVLang.GetText("Msg_NulChars") & vbCrLf & vbCrLf & LT("Msg_LossyNoOverwrite", "Чтобы не повредить исходный файл, сохранить этот документ можно будет только через ""Сохранить как""."), Application.ProductName,
                                   MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) <> Windows.Forms.DialogResult.Yes Then Return False
                lossy = True ' NUL-символы редактор не сохраняет
            End If

            _loading = True
            Try
                Dim mixed As Boolean
                NewLineType = DetectNewLineType(text, mixed)
                NewLineMixed = mixed
                PrimeTEXT.ZoomFactor = ZoomF ' Устанавливаем ZoomFactor ДО присвоения Text, чтобы избежать двойного layout
                SetEditorText(text)
                PrimeTEXT.ClearUndo()
            Finally
                _loading = False
            End Try
            _linesDirty = True

            ActCodePage = cp : ActENC = encName : UTFbom = bom
            slEnc.Text = encName
            slBOM.Text = If(bom, "BOM", "")
            DocN = Path.GetFileName(FileName)
            If lossy Then
                ' Текст в редакторе уже не совпадает с файлом: привязку к оригиналу снимаем, Ctrl+S откроет "Сохранить как"
                SaveD = ""
                _restoredDoc = True
                _suggestDir = Path.GetDirectoryName(FileName)
            Else
                SaveD = FileName
                _restoredDoc = False
                _suggestDir = ""
            End If
            Me.Text = Application.ProductName + " - " + DocN
            Red = False
            DeleteRecovery()
            TFocus()
            ObnovlenieFUN()
            ObnovlenieTextL()
            Return True
        Catch ex As Exception
            Me.UseWaitCursor = False
            MessageBox.Show(MAVLang.GetText("Msg_ErrorOpening") & ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    ' ==================== БЕЗОПАСНЫЙ БУФЕР ОБМЕНА И URL ====================
    Private Function SafeSetClipboard(text As String) As Boolean
        Try
            If String.IsNullOrEmpty(text) Then Return False
            ' RichTextBox отдаёт текст с \n; в буфер обмена Windows нужно класть CRLF, иначе другие программы склеят строки
            text = text.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Replace(vbLf, vbCrLf)
            Clipboard.SetText(text)
            Return True
        Catch ex As ExternalException
            Return False
        End Try
    End Function

    Private Function SafeGetClipboardText() As String
        Try
            If Clipboard.ContainsText() Then Return Clipboard.GetText()
        Catch ex As ExternalException
        End Try
        Return Nothing
    End Function

    Private Sub OpenUrl(url As String)
        Try
            Process.Start(New ProcessStartInfo(url) With {.UseShellExecute = True})
        Catch ex As Exception
            MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    ' ==================== СИСТЕМА ЛОКАЛИЗАЦИИ ====================
    Private Sub ApplyLanguageToForm()
        ApplyLanguageToControl(Me)
        If CMenu IsNot Nothing Then
            For Each item As ToolStripItem In CMenu.Items
                ApplyLanguageToToolStripItem(item)
            Next
        End If
        If String.IsNullOrEmpty(SaveD) AndAlso Not _restoredDoc Then
            DocN = MAVLang.GetText("NewDocument")
            Me.Text = Application.ProductName + " - " + DocN
        End If
        ObnovlenieFUN()
        UpdateLanguageCheckmarks()
    End Sub

    Private Sub ApplyLanguageToControl(ctrl As Control)
        If MAVLang.HasTranslation(ctrl.Name) Then ctrl.Text = MAVLang.GetText(ctrl.Name)
        For Each child As Control In ctrl.Controls
            ApplyLanguageToControl(child)
        Next
        If TypeOf ctrl Is ToolStrip Then
            Dim ts As ToolStrip = CType(ctrl, ToolStrip)
            For Each item As ToolStripItem In ts.Items
                ApplyLanguageToToolStripItem(item)
            Next
        End If
    End Sub

    Private Sub ApplyLanguageToToolStripItem(item As ToolStripItem)
        If MAVLang.HasTranslation(item.Name) Then item.Text = MAVLang.GetText(item.Name)
        If TypeOf item Is ToolStripDropDownItem Then
            Dim dropDown As ToolStripDropDownItem = CType(item, ToolStripDropDownItem)
            For Each child As ToolStripItem In dropDown.DropDownItems
                ApplyLanguageToToolStripItem(child)
            Next
        End If
    End Sub

    Private Sub UpdateLanguageCheckmarks()
        MLangRU.Checked = (MAVLang.CurrentLanguage = "ru")
        MLangEN.Checked = (MAVLang.CurrentLanguage = "en")
    End Sub

    Private Sub MLangRU_Click(sender As Object, e As EventArgs) Handles MLangRU.Click
        MAVLang.SetLanguage("ru")
        ApplyLanguageToForm()
    End Sub

    Private Sub MLangEN_Click(sender As Object, e As EventArgs) Handles MLangEN.Click
        MAVLang.SetLanguage("en")
        ApplyLanguageToForm()
    End Sub

    ' ==================== ОПТИМИЗАЦИЯ И ОБНОВЛЕНИЕ UI ====================
    Private Sub FSymbols_ResizeBegin(sender As Object, e As EventArgs) Handles Me.ResizeBegin
        If LSymbols > 50000 Then TRS.Enabled = True
    End Sub

    Private Sub FSymbols_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        If LSymbols > 50000 Then
            TRS.Enabled = False
            ObnovlenieRazmFUN()
        End If
    End Sub

    Private Sub FSymbols_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        If Not TRS.Enabled Then ObnovlenieRazmFUN()
    End Sub

    Private Sub SetAllMargins(rtBox As RichTextBox, left As Integer, top As Integer, right As Integer, bottom As Integer)
        If Not rtBox.IsHandleCreated Then Return
        Dim rect As New RECT
        rect.Left = left
        rect.Top = top
        rect.Right = rtBox.ClientSize.Width - right
        rect.Bottom = rtBox.ClientSize.Height - bottom
        SendMessage(rtBox.Handle, EM_SETRECT, 0, rect)
    End Sub

    Public Sub ObnovlenieTextL()
        ' Оптимизация: TextLength работает за O(1), в отличие от .Text.Count()
        LSymbols = PrimeTEXT.TextLength
    End Sub

    Private Sub RequestStatus()
        TStatus.Stop()
        TStatus.Start()
    End Sub

    Private Sub TStatus_Tick(sender As Object, e As EventArgs) Handles TStatus.Tick
        TStatus.Stop()
        ObnovlenieTextL()
        ObnovlenieFUN()
    End Sub

    Private Sub TAuto_Tick(sender As Object, e As EventArgs) Handles TAuto.Tick
        If Red AndAlso _recoveryDirty Then WriteRecovery()
    End Sub

    ' Число строк: кэшируется и пересчитывается только после изменения текста / режима переноса
    Private Function GetLineCount() As Integer
        If _linesDirty Then
            If Not WordW OrElse PrimeTEXT.TextLength > 1000000 Then
                ' Быстро и без копирования текста (при переносе по словам считает визуальные строки)
                _lineCount = PrimeTEXT.GetLineFromCharIndex(PrimeTEXT.TextLength) + 1
            Else
                _lineCount = PrimeTEXT.Lines.Length
            End If
            _linesDirty = False
        End If
        Return _lineCount
    End Function

    Public Sub ObnovlenieFUN()
        MUndo.Enabled = PrimeTEXT.CanUndo
        MRedo.Enabled = PrimeTEXT.CanRedo

        Dim hasSelection As Boolean = (PrimeTEXT.SelectionLength > 0)
        MCut.Enabled = hasSelection
        MCopy.Enabled = hasSelection
        MDelete.Enabled = hasSelection
        MYandex.Enabled = hasSelection
        CMYandex.Enabled = hasSelection
        MGoogle.Enabled = hasSelection
        CMGoogle.Enabled = hasSelection
        CMBig.Enabled = hasSelection
        CMSml.Enabled = hasSelection
        CMCut.Enabled = hasSelection
        CMCopy.Enabled = hasSelection
        CMDel.Enabled = hasSelection

        MPrint.Enabled = (PrimeTEXT.TextLength > 0)
        CMUndo.Enabled = PrimeTEXT.CanUndo

        MFind.Checked = FindP
        SBStandart.Visible = StatusB
        MStatusBar.Checked = StatusB
        TBFind.Visible = FindP

        PrimeTEXT.WordWrap = WordW
        MPerenos.Checked = WordW
        slRC.Visible = Not WordW
        MPer.Enabled = Not WordW

        PrimeTEXT.DetectUrls = WURL
        MVURL.Checked = WURL

        Dim a As Long = PrimeTEXT.GetLineFromCharIndex(PrimeTEXT.SelectionStart)
        Dim b As Long = PrimeTEXT.SelectionStart - PrimeTEXT.GetFirstCharIndexFromLine(CInt(a))
        slRC.Text = MAVLang.GetText("slRC_Prefix") & (a + 1) & MAVLang.GetText("slRC_Suffix") & (b + 1)

        slChang.Text = If(Red, MAVLang.GetText("slChang_Modified"), "")

        Dim insertState As Short = GetKeyState(VK_INSERT)
        slINS.Text = If((insertState And &H1) = 0, MAVLang.GetText("slINS_Ins"), MAVLang.GetText("slINS_Ovr"))

        slLines.Text = MAVLang.GetText("slLines_Prefix") & GetLineCount().ToString()

        ' Масштаб мог измениться колесом мыши (Ctrl+колесо) - синхронизируем сохранённое значение
        ZoomF = PrimeTEXT.ZoomFactor
        MMUv.Enabled = (PrimeTEXT.ZoomFactor < 5)
        MMUm.Enabled = (PrimeTEXT.ZoomFactor > 0.2)

        slScale.Text = CInt(Math.Round(PrimeTEXT.ZoomFactor * 100)) & MAVLang.GetText("Scale_Percent")
        UpdateNewLineLabel()
    End Sub

    Public Sub ObnovlenieRazmFUN()
        If Me.WindowState = FormWindowState.Minimized Then Return ' у свёрнутого окна клиентская область нулевая
        Dim TH As Integer = MSStandart.Height
        Dim BH As Integer = 0
        If StatusB Then BH += SBStandart.Height
        If FindP Then BH += TBFind.Height

        PrimeTEXT.Left = 0
        PrimeTEXT.Top = TH
        PrimeTEXT.Width = Math.Max(0, Me.ClientSize.Width)
        PrimeTEXT.Height = Math.Max(0, Me.ClientSize.Height - BH - TH)
        PrimeTEXT.Refresh()
    End Sub

    ' ==================== НАСТРОЙКИ ====================
    Private Shared ReadOnly SettingsFile As String = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MAV Symbols", "settings.mavst")

    Private Shared Function ParseSingle(s As String, def As Single) As Single
        Dim v As Single
        If Single.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, v) Then Return v
        If Single.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, v) Then Return v
        Return def
    End Function

    Private Shared Function ParseBool(s As String, def As Boolean) As Boolean
        Dim v As Boolean
        If Boolean.TryParse(s, v) Then Return v
        Return def
    End Function

    Private Shared Function DefaultUiLanguage() As String
        Return If(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName = "ru", "ru", "en")
    End Function

    Private Shared Function ParseInt(s As String, def As Integer) As Integer
        Dim v As Integer
        If Integer.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, v) Then Return v
        Return def
    End Function

    ' Формат файла настроек: строки "Ключ=Значение" (UTF-8). Неизвестные ключи игнорируются, отсутствующие берут значения
    ' по умолчанию, поэтому новые параметры можно добавлять в любое время, не ломая старые файлы.
    ' SettingsVersion нужно увеличить только если изменится смысл уже существующих ключей.
    Private Const SettingsVersion As Integer = 1

    Private Shared Function ReadSettingsFile() As System.Collections.Generic.Dictionary(Of String, String)
        Dim d As New System.Collections.Generic.Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        If Not File.Exists(SettingsFile) Then Return d
        For Each line As String In File.ReadAllLines(SettingsFile, Encoding.UTF8)
            Dim p As Integer = line.IndexOf("="c)
            If p <= 0 Then Continue For
            Dim k As String = line.Substring(0, p).Trim()
            If k.Length = 0 Then Continue For
            d(k) = line.Substring(p + 1).Trim()
        Next
        Return d
    End Function

    Public Sub LoadSettings()
        Dim fName As String = "Consolas"
        Dim fSize As Single = 11
        Dim fStyle As FontStyle = FontStyle.Regular
        StatusB = True : WordW = True : WURL = True
        ZoomF = 1
        Dim dark As Boolean = False
        Dim lang As String = ""
        _savedBounds = Rectangle.Empty
        _savedMaximized = False

        Try
            If Not Directory.Exists(Path.GetDirectoryName(SettingsFile)) Then
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile))
            End If

            Dim d As System.Collections.Generic.Dictionary(Of String, String) = ReadSettingsFile()
            Dim s As String = Nothing

            If d.TryGetValue("FontName", s) AndAlso s <> "" Then fName = s
            If d.TryGetValue("FontSize", s) Then fSize = ParseSingle(s, fSize)
            If d.TryGetValue("FontStyle", s) Then [Enum].TryParse(Of FontStyle)(s, fStyle)
            If d.TryGetValue("StatusBar", s) Then StatusB = ParseBool(s, StatusB)
            If d.TryGetValue("WordWrap", s) Then WordW = ParseBool(s, WordW)
            If d.TryGetValue("DetectUrls", s) Then WURL = ParseBool(s, WURL)
            If d.TryGetValue("Zoom", s) Then ZoomF = Math.Min(5.0F, Math.Max(0.2F, ParseSingle(s, 1.0F)))
            If d.TryGetValue("DarkTheme", s) Then dark = ParseBool(s, False)
            If d.TryGetValue("Language", s) Then lang = s.Trim().ToLowerInvariant()

            ' Положение и размер окна (применяются в ApplySavedWindowPlacement)
            Dim w As Integer = 0, h As Integer = 0, l As Integer = 0, tp As Integer = 0
            If d.TryGetValue("WinWidth", s) Then w = ParseInt(s, 0)
            If d.TryGetValue("WinHeight", s) Then h = ParseInt(s, 0)
            If d.TryGetValue("WinLeft", s) Then l = ParseInt(s, 0)
            If d.TryGetValue("WinTop", s) Then tp = ParseInt(s, 0)
            If w >= 200 AndAlso h >= 150 Then _savedBounds = New Rectangle(l, tp, w, h)
            If d.TryGetValue("WindowState", s) Then _savedMaximized = String.Equals(s, "Maximized", StringComparison.OrdinalIgnoreCase)
        Catch ex As Exception
            ' Повреждённый файл настроек не критичен: остаются значения по умолчанию
        End Try

        Try
            PrimeTEXT.Font = New Font(fName, Math.Max(6.0F, fSize), fStyle)
        Catch ex As Exception
            PrimeTEXT.Font = New Font("Consolas", 11)
        End Try

        PrimeTEXT.ZoomFactor = ZoomF
        MAVDarkT.SetDarkValue(dark)
        MAVLang.SetLanguage(If(lang = "", DefaultUiLanguage(), lang))
    End Sub

    ' Восстанавливает положение окна, только если оно хотя бы частично попадает на один из существующих мониторов
    ' (монитор мог быть отключён с прошлого запуска)
    Private Sub ApplySavedWindowPlacement()
        If _savedBounds.IsEmpty Then Return
        Dim visibleOk As Boolean = False
        For Each scr As Screen In Screen.AllScreens
            Dim inter As Rectangle = Rectangle.Intersect(scr.WorkingArea, _savedBounds)
            If inter.Width >= 100 AndAlso inter.Height >= 50 Then
                visibleOk = True
                Exit For
            End If
        Next
        If Not visibleOk Then Return
        Me.StartPosition = FormStartPosition.Manual
        Me.Bounds = _savedBounds
        If _savedMaximized Then Me.WindowState = FormWindowState.Maximized
    End Sub

    Public Sub SaveSettings()
        Dim tmp As String = SettingsFile & ".tmp"
        Try
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile))
            Dim inv As CultureInfo = CultureInfo.InvariantCulture
            ZoomF = PrimeTEXT.ZoomFactor
            Dim b As Rectangle = If(Me.WindowState = FormWindowState.Normal, Me.Bounds, Me.RestoreBounds)

            Dim sb As New StringBuilder()
            sb.AppendLine("Version=" & SettingsVersion.ToString(inv))
            sb.AppendLine("FontName=" & PrimeTEXT.Font.Name)
            sb.AppendLine("FontSize=" & PrimeTEXT.Font.Size.ToString(inv))
            sb.AppendLine("FontStyle=" & PrimeTEXT.Font.Style.ToString())
            sb.AppendLine("StatusBar=" & StatusB.ToString())
            sb.AppendLine("WordWrap=" & WordW.ToString())
            sb.AppendLine("DetectUrls=" & WURL.ToString())
            sb.AppendLine("Zoom=" & ZoomF.ToString(inv))
            sb.AppendLine("DarkTheme=" & DarkT.ToString())
            sb.AppendLine("Language=" & MAVLang.CurrentLanguage)
            sb.AppendLine("WindowState=" & If(Me.WindowState = FormWindowState.Maximized, "Maximized", "Normal"))
            sb.AppendLine("WinLeft=" & b.Left.ToString(inv))
            sb.AppendLine("WinTop=" & b.Top.ToString(inv))
            sb.AppendLine("WinWidth=" & b.Width.ToString(inv))
            sb.AppendLine("WinHeight=" & b.Height.ToString(inv))

            ' Пишем во временный файл и подменяем: сбой посреди записи не оставит наполовину записанных настроек
            File.WriteAllText(tmp, sb.ToString(), New UTF8Encoding(False))
            If File.Exists(SettingsFile) Then
                Try
                    File.Replace(tmp, SettingsFile, Nothing, True)
                Catch ex As Exception
                    File.Copy(tmp, SettingsFile, True)
                End Try
            Else
                File.Move(tmp, SettingsFile)
            End If
        Catch ex As Exception
            ' Настройки не сохранились — не повод ронять закрытие окна
        Finally
            Try
                If File.Exists(tmp) Then File.Delete(tmp)
            Catch ex As Exception
            End Try
        End Try
    End Sub

    ' ==================== ОБРАБОТЧИКИ СОБЫТИЙ ФОРМЫ ====================
    Private Sub FSymbols_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        MAVLang.Init()

        ' Страховка от сбоев: при необработанной ошибке пытаемся сохранить несохранённый текст в папку восстановления
        AddHandler Application.ThreadException, AddressOf OnUiThreadException
        AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf OnDomainException

        SBStandart.ShowItemToolTips = True
        PrimeTEXT.AllowDrop = True
        Me.BackColor = SBStandart.BackColor

        DocN = MAVLang.GetText("NewDocument")
        SaveD = ""
        Me.Text = Application.ProductName + " - " + DocN

        WordW = True : PrimeTEXT.WordWrap = True
        FindP = False
        NewLineType = "CRLF"
        NewLineMixed = False
        UpdateNewLineLabel()
        slBOM.Text = ""
        UTFbom = False
        ActCodePage = 65001
        ActENC = "UTF-8"
        slEnc.Text = ActENC

        LoadSettings()
        ApplySavedWindowPlacement()

        DocN = MAVLang.GetText("NewDocument")
        Me.Text = Application.ProductName + " - " + DocN
        ApplyLanguageToForm()

        ' Если файл из командной строки не открылся, остаёмся с пустым документом (сообщение об ошибке уже показано)
        If My.Application.CommandLineArgs.Count > 0 Then
            OpenDocument(My.Application.CommandLineArgs(0).ToString, True)
        End If

        ObnovlenieFUN()
        SetAllMargins(PrimeTEXT, 18, 4, 18, 0)
        ObnovlenieRazmFUN()
        MAVDarkT.SetDark(DarkT)
        MDarkT.Checked = DarkT
        Red = False
        TAuto.Start()
    End Sub

    Private Sub FSymbols_Shown(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Me.Shown
        ' Предлагаем восстановить документ после аварийного завершения (только при обычном запуске без файла)
        If My.Application.CommandLineArgs.Count = 0 Then TryRestoreRecovery()
    End Sub

    Private Sub FSymbols_FormClosing(ByVal sender As System.Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles MyBase.FormClosing
        If e.CloseReason = CloseReason.WindowsShutDown OrElse e.CloseReason = CloseReason.TaskManagerClosing Then
            ' При выключении Windows / снятии задачи диалог не показываем (он блокирует выключение):
            ' несохранённый текст записываем в папку восстановления и предложим вернуть при следующем запуске
            If Red Then WriteRecovery()
        ElseIf SaveDocumentClose() = Windows.Forms.DialogResult.Cancel Then
            e.Cancel = True
            Return
        Else
            DeleteRecovery()
        End If
        SaveSettings()
    End Sub

    Private Sub FSymbols_FormClosed(ByVal sender As System.Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles MyBase.FormClosed
        TAuto.Stop()
        RemoveHandler Application.ThreadException, AddressOf OnUiThreadException
        RemoveHandler AppDomain.CurrentDomain.UnhandledException, AddressOf OnDomainException
    End Sub

    Private Sub MExit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MExit.Click
        Me.Close() ' Безопаснее, чем Application.Exit()
    End Sub

    ' ==================== РАБОТА С ДОКУМЕНТОМ ====================
    Private Sub MNew_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MNew.Click
        If SaveDocumentClose() = Windows.Forms.DialogResult.Cancel Then Exit Sub

        _loading = True
        Try
            PrimeTEXT.ZoomFactor = ZoomF
            PrimeTEXT.Text = ""
            PrimeTEXT.ClearUndo()
        Finally
            _loading = False
        End Try
        _linesDirty = True

        SaveD = ""
        _restoredDoc = False
        _suggestDir = ""
        DocN = MAVLang.GetText("NewDocument")
        Me.Text = Application.ProductName + " - " + DocN

        ActENC = "UTF-8" : ActCodePage = 65001 : UTFbom = False
        NewLineType = "CRLF"
        NewLineMixed = False

        slEnc.Text = ActENC
        slBOM.Text = ""
        UpdateNewLineLabel()

        Red = False
        DeleteRecovery()
        ObnovlenieTextL()
        ObnovlenieFUN()
    End Sub

    Private Sub MOpen_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MOpen.Click
        Using OD As New OpenFileDialog
            OD.CheckFileExists = True
            OD.CheckPathExists = True
            OD.Filter = MAVLang.GetText("Filter_TextFiles") & "|*.txt|" & MAVLang.GetText("Filter_AllFiles") & "|*.*"
            OD.FilterIndex = 2
            OD.DefaultExt = "txt"
            OD.Title = MAVLang.GetText("Dialog_OpenAuto")
            If OD.ShowDialog() = Windows.Forms.DialogResult.OK Then
                ' Вопрос о сохранении задаём только после того, как файл реально выбран
                If SaveDocumentClose() = Windows.Forms.DialogResult.Cancel Then Return
                OpenDocument(OD.FileName, True)
            End If
        End Using
    End Sub

    Private Sub MOpenAS_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MOpenAS.Click
        Using OD As New OpenFileDialog
            OD.CheckFileExists = True
            OD.CheckPathExists = True
            OD.Filter = MAVLang.GetText("Filter_TextFiles") & "|*.txt|" & MAVLang.GetText("Filter_AllFiles") & "|*.*"
            OD.FilterIndex = 2
            OD.DefaultExt = "txt"
            OD.Title = MAVLang.GetText("Dialog_OpenManual")
            If OD.ShowDialog() = Windows.Forms.DialogResult.OK Then
                If SaveDocumentClose() = Windows.Forms.DialogResult.Cancel Then Return
                OpenDocument(OD.FileName, False)
            End If
        End Using
    End Sub

    Private Sub MSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MSave.Click
        SaveDocument()
    End Sub

    Private Sub MSaveAs_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MSaveAs.Click
        SaveDocumentAs(True)
    End Sub

    Private Sub MSaveCODE_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MSaveCODE.Click
        SaveDocumentAs(False)
    End Sub

    Private Sub PrimeTEXT_DragDrop(ByVal sender As Object, ByVal e As System.Windows.Forms.DragEventArgs) Handles PrimeTEXT.DragDrop
        Dim files As String() = TryCast(e.Data.GetData(DataFormats.FileDrop), String())
        If files Is Nothing OrElse files.Length = 0 OrElse Not File.Exists(files(0)) Then Return

        Dim fname As String = files(0)
        Me.BeginInvoke(New MethodInvoker(Sub()
                                             If SaveDocumentClose() = Windows.Forms.DialogResult.Cancel Then Return
                                             If OpenDocument(fname, True) Then TDD.Enabled = True
                                         End Sub))
    End Sub

    Private Sub PrimeTEXT_DragEnter(ByVal sender As Object, ByVal e As System.Windows.Forms.DragEventArgs) Handles PrimeTEXT.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
        Else
            e.Effect = DragDropEffects.None
        End If
    End Sub

    ' ==================== РЕДАКТИРОВАНИЕ И БУФЕР ОБМЕНА ====================
    Private Sub PrimeTEXT_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PrimeTEXT.TextChanged
        _linesDirty = True
        If _loading Then Return
        TDD.Enabled = False ' любая правка (включая Delete/Backspace) отменяет cleanup drag&drop
        Red = True
        _recoveryDirty = True
        RequestStatus()
    End Sub

    ' Нативные сочетания RichEdit, применяющие визуальное форматирование (в обычном тексте оно не сохраняется).
    ' Блокируем в KeyDown, а не через RichTextShortcutsEnabled: так не затрагиваются сочетания клавиш пунктов меню.
    Private Shared Function IsRichFormatKey(e As KeyEventArgs) As Boolean
        Select Case e.KeyCode
            Case Keys.B, Keys.I, Keys.U, Keys.E, Keys.R, Keys.J,
                 Keys.D1, Keys.D2, Keys.D5, Keys.NumPad1, Keys.NumPad2, Keys.NumPad5
                ' Ctrl+B/I/U - начертание, Ctrl+E/R/J - выравнивание, Ctrl+1/2/5 - интервалы
                Return Not e.Shift
            Case Keys.L
                ' Ctrl+L - выравнивание, Ctrl+Shift+L - маркированный список
                Return True
            Case Keys.A, Keys.K
                ' Ctrl+Shift+A - все прописные, Ctrl+Shift+K - капитель (обычный Ctrl+A - "Выделить всё" - не трогаем)
                Return e.Shift
            Case Keys.Oemplus
                ' Ctrl+= и Ctrl+Shift+= - нижний/верхний индекс
                Return True
            Case Keys.Oemcomma, Keys.OemPeriod
                ' Ctrl+Shift+, и Ctrl+Shift+. - размер шрифта
                Return e.Shift
        End Select
        Return False
    End Function

    Private Sub PrimeTEXT_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles PrimeTEXT.KeyDown
        Dim ctrlOnly As Boolean = e.Control AndAlso Not e.Alt ' Ctrl+Alt = AltGr на ряде раскладок: не трогаем

        If ctrlOnly AndAlso Not e.Shift AndAlso (e.KeyCode = Keys.C OrElse e.KeyCode = Keys.Insert) Then
            ' Копирование только обычным текстом (как и пункт меню), без RTF в буфере
            e.SuppressKeyPress = True
            SafeSetClipboard(PrimeTEXT.SelectedText)
        ElseIf (ctrlOnly AndAlso Not e.Shift AndAlso e.KeyCode = Keys.X) OrElse (e.Shift AndAlso Not e.Control AndAlso e.KeyCode = Keys.Delete) Then
            e.SuppressKeyPress = True
            If SafeSetClipboard(PrimeTEXT.SelectedText) Then PrimeTEXT.SelectedText = ""
        ElseIf (ctrlOnly AndAlso e.KeyCode = Keys.V) OrElse (e.Shift AndAlso Not e.Control AndAlso e.KeyCode = Keys.Insert) Then
            e.SuppressKeyPress = True
            Dim t As String = SafeGetClipboardText()
            If t IsNot Nothing Then PrimeTEXT.SelectedText = t
        ElseIf ctrlOnly AndAlso IsRichFormatKey(e) Then
            e.SuppressKeyPress = True
        End If
        RequestStatus()
    End Sub

    Private Sub PrimeTEXT_MouseWheel(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles PrimeTEXT.MouseWheel
        ' Ctrl+колесо меняет масштаб: обновим статусбар и сохранённое значение масштаба
        If Control.ModifierKeys = Keys.Control Then RequestStatus()
    End Sub

    Private Sub PrimeTEXT_KeyUp(ByVal sender As System.Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles PrimeTEXT.KeyUp
        RequestStatus()
    End Sub

    Private Sub PrimeTEXT_MouseUp(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles PrimeTEXT.MouseUp
        RequestStatus()
    End Sub

    Private Sub MCut_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MCut.Click
        If PrimeTEXT.Focused Then
            If SafeSetClipboard(PrimeTEXT.SelectedText) Then PrimeTEXT.SelectedText = ""
        End If
        If FText1.Focused Then
            Try : FText1.Cut() : Catch ex As ExternalException : End Try
        End If
        If ZText1.Focused Then
            Try : ZText1.Cut() : Catch ex As ExternalException : End Try
        End If
        RequestStatus()
        ObnovlenieTextL()
    End Sub

    Private Sub MCopy_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MCopy.Click
        If PrimeTEXT.Focused Then SafeSetClipboard(PrimeTEXT.SelectedText)
        If FText1.Focused Then
            Try : FText1.Copy() : Catch ex As ExternalException : End Try
        End If
        If ZText1.Focused Then
            Try : ZText1.Copy() : Catch ex As ExternalException : End Try
        End If
        RequestStatus()
    End Sub

    Private Sub MPaste_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MPaste.Click
        If PrimeTEXT.Focused Then
            Dim t As String = SafeGetClipboardText()
            If t IsNot Nothing Then PrimeTEXT.SelectedText = t
        End If
        If FText1.Focused Then
            Try : FText1.Paste() : Catch ex As ExternalException : End Try
        End If
        If ZText1.Focused Then
            Try : ZText1.Paste() : Catch ex As ExternalException : End Try
        End If
        RequestStatus()
        ObnovlenieTextL()
    End Sub

    Private Sub MDelete_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MDelete.Click
        If PrimeTEXT.Focused Then PrimeTEXT.SelectedText = ""
        If FText1.Focused Then FText1.SelectedText = ""
        If ZText1.Focused Then ZText1.SelectedText = ""
        ObnovlenieTextL()
    End Sub

    Private Sub MCLClip_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MCLClip.Click
        Try
            Clipboard.Clear()
        Catch ex As ExternalException
        End Try
        RequestStatus()
    End Sub

    Private Sub MSelall_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MSelall.Click
        If PrimeTEXT.Focused Then PrimeTEXT.SelectAll()
        If FText1.Focused Then FText1.SelectAll()
        If ZText1.Focused Then ZText1.SelectAll()
    End Sub

    Private Sub MUndo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MUndo.Click
        If PrimeTEXT.Focused Then PrimeTEXT.Undo()
        If FText1.Focused Then FText1.Undo()
        If ZText1.Focused Then ZText1.Undo()
        ObnovlenieTextL()
    End Sub

    Private Sub MRedo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MRedo.Click
        ' У однострочных полей поиска/замены нет настоящего Redo (Win32 EDIT только переключает Undo), поэтому здесь только основной текст
        If PrimeTEXT.Focused Then PrimeTEXT.Redo()
        ObnovlenieTextL()
    End Sub

    Private Sub CMUndo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CMUndo.Click
        MUndo.PerformClick()
    End Sub
    Private Sub CMCut_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CMCut.Click
        MCut.PerformClick()
    End Sub
    Private Sub CMCopy_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CMCopy.Click
        MCopy.PerformClick()
    End Sub
    Private Sub CMPaste_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CMPaste.Click
        MPaste.PerformClick()
    End Sub
    Private Sub CMDel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CMDel.Click
        MDelete.PerformClick()
    End Sub
    Private Sub CMSelall_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CMSelall.Click
        MSelall.PerformClick()
    End Sub

    ' ==================== ПОИСК И ЗАМЕНА ====================
    Private Sub MFind_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MFind.Click
        FindP = Not FindP
        ObnovlenieFUN()
        ObnovlenieRazmFUN()
        If FindP Then
            FText1.Focus()
            ' Подставляем выделенный фрагмент только если он однострочный; иначе оставляем прежний текст поиска
            If PrimeTEXT.SelectionLength > 0 AndAlso PrimeTEXT.SelectedText.IndexOf(vbLf) < 0 Then
                FText1.Text = PrimeTEXT.SelectedText
            End If
            FText1.SelectAll()
        End If
    End Sub

    ' Поиск следующего вхождения от каретки; при достижении конца документа продолжает с начала
    Private Function FindNextOccurrence() As Boolean
        If String.IsNullOrEmpty(FText1.Text) Then Return False
        Dim opts As RichTextBoxFinds = If(TBButReg.CheckState = CheckState.Unchecked, RichTextBoxFinds.None, RichTextBoxFinds.MatchCase)
        Dim startPos As Integer = PrimeTEXT.SelectionStart + PrimeTEXT.SelectionLength
        Dim a As Integer = -1
        If startPos <= PrimeTEXT.TextLength Then a = PrimeTEXT.Find(FText1.Text, startPos, opts)
        If a < 0 Then a = PrimeTEXT.Find(FText1.Text, 0, opts)
        If a < 0 Then Return False
        PrimeTEXT.ScrollToCaret()
        Return True
    End Function

    Private Sub TBButF_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TBButF.Click
        If String.IsNullOrEmpty(FText1.Text) Then Return
        If Not FindNextOccurrence() Then
            MessageBox.Show(MAVLang.GetText("Msg_NotFound") & FText1.Text, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Private Sub TBButZam_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TBButZam.Click
        If String.IsNullOrEmpty(FText1.Text) Then Return
        Dim cmp As StringComparison = If(TBButReg.CheckState = CheckState.Unchecked, StringComparison.OrdinalIgnoreCase, StringComparison.Ordinal)
        If PrimeTEXT.SelectionLength > 0 AndAlso String.Equals(PrimeTEXT.SelectedText, FText1.Text, cmp) Then
            PrimeTEXT.SelectedText = ZText1.Text
        End If
        If Not FindNextOccurrence() Then Beep()
    End Sub

    ' Максимальная длина текста после замены (предел длины строки .NET - около 1,07 млрд символов)
    Private Const MaxReplacedLength As Long = 1000000000L

    Private Sub TBButZamV_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TBButZamV.Click
        Dim findText As String = FText1.Text
        If String.IsNullOrEmpty(findText) Then Return
        Dim repl As String = ZText1.Text

        Dim cmp As StringComparison = If(TBButReg.CheckState = CheckState.Unchecked, StringComparison.OrdinalIgnoreCase, StringComparison.Ordinal)
        Dim src As String = PrimeTEXT.Text

        ' Проход 1: только считаем вхождения - память под результат пока не выделяется
        Dim count As Integer = 0
        Dim pos As Integer = 0
        Do
            Dim idx As Integer = src.IndexOf(findText, pos, cmp)
            If idx < 0 Then Exit Do
            count += 1
            pos = idx + findText.Length
        Loop

        If count = 0 Then
            MessageBox.Show(MAVLang.GetText("Msg_NotFound") & findText, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' Точная длина результата известна заранее: буфер выделяется один раз, без удвоений ёмкости
        Dim newLen As Long = CLng(src.Length) + CLng(count) * CLng(repl.Length - findText.Length)
        If newLen > MaxReplacedLength Then
            MessageBox.Show(MAVLang.GetText("Msg_ReplaceNoMemory"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim oldStart As Integer = PrimeTEXT.SelectionStart
        Dim result As String = Nothing
        Me.UseWaitCursor = True
        Try
            ' Проход 2: сборка результата. Документ до этого момента не изменяется, поэтому при нехватке памяти он остаётся как был
            Try
                Dim sb As New StringBuilder(CInt(newLen))
                pos = 0
                Do
                    Dim idx As Integer = src.IndexOf(findText, pos, cmp)
                    If idx < 0 Then Exit Do
                    sb.Append(src, pos, idx - pos).Append(repl)
                    pos = idx + findText.Length
                Loop
                sb.Append(src, pos, src.Length - pos)
                src = Nothing ' исходная копия больше не нужна - освобождаем до создания итоговой строки
                result = sb.ToString()
            Catch ex As OutOfMemoryException
                src = Nothing
                result = Nothing
                MessageBox.Show(MAVLang.GetText("Msg_ReplaceNoMemory"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End Try

            ' Применяем одной операцией (одно действие в истории отмены)
            Try
                PrimeTEXT.SelectAll()
                PrimeTEXT.SelectedText = result
            Catch ex As OutOfMemoryException
                result = Nothing
                MessageBox.Show(MAVLang.GetText("Msg_ReplaceNoMemoryApply"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End Try
            result = Nothing
        Finally
            Me.UseWaitCursor = False
        End Try

        ' Возвращаем каретку примерно на прежнее место (а не в начало документа)
        PrimeTEXT.SelectionStart = Math.Min(oldStart, PrimeTEXT.TextLength)
        PrimeTEXT.SelectionLength = 0
        PrimeTEXT.ScrollToCaret()
        ObnovlenieTextL()
        MessageBox.Show(String.Format(MAVLang.GetText("Msg_Replaced"), count), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub TBButClose_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TBButClose.Click
        MFind.PerformClick()
    End Sub

    Private Sub FText1_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles FText1.KeyPress
        If e.KeyChar = Convert.ToChar(13) Then
            e.Handled = True
            TBButF.PerformClick()
        End If
    End Sub

    Private Sub FText1_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles FText1.TextChanged
        TBButZamV.Enabled = Not String.IsNullOrEmpty(FText1.Text)
    End Sub

    Private Sub ZText1_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles ZText1.KeyPress
        If e.KeyChar = Convert.ToChar(13) Then
            e.Handled = True
            TBButZam.PerformClick()
        End If
    End Sub

    ' ==================== ФОРМАТИРОВАНИЕ И ВИД ====================
    Private Sub MFont_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MFont.Click
        Using FontD As New FFont()
            Dim AntiRed As Boolean = Red
            FontD.Font = PrimeTEXT.Font
            FontD.ApplyLanguage()
            If FontD.ShowDialog = Windows.Forms.DialogResult.OK Then
                PrimeTEXT.Font = FontD.Font
            End If
            Red = AntiRed
            _linesDirty = True
            ObnovlenieFUN()
        End Using
    End Sub

    Private Sub MPerenos_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MPerenos.Click
        WordW = Not WordW
        _linesDirty = True
        ObnovlenieFUN()
        SetAllMargins(PrimeTEXT, 18, 4, 18, 0)
    End Sub

    Private Sub MVURL_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MVURL.Click
        WURL = Not WURL
        ObnovlenieFUN()
    End Sub

    Private Sub MStatusBar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MStatusBar.Click
        StatusB = Not StatusB
        ObnovlenieFUN()
        ObnovlenieRazmFUN()
    End Sub

    Private Sub CMBig_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CMBig.Click
        PrimeTEXT.SelectedText = PrimeTEXT.SelectedText.ToUpper()
    End Sub

    Private Sub CMSml_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CMSml.Click
        PrimeTEXT.SelectedText = PrimeTEXT.SelectedText.ToLower()
    End Sub

    Private Sub MMUv_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MMUv.Click
        PrimeTEXT.ZoomFactor += 0.1F
        ZoomF = PrimeTEXT.ZoomFactor
        ObnovlenieFUN()
    End Sub

    Private Sub MMUm_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MMUm.Click
        PrimeTEXT.ZoomFactor -= 0.1F
        ZoomF = PrimeTEXT.ZoomFactor
        ObnovlenieFUN()
    End Sub

    Private Sub MMPoUm_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MMPoUm.Click
        PrimeTEXT.ZoomFactor = 1.0F
        ZoomF = PrimeTEXT.ZoomFactor
        ObnovlenieFUN()
    End Sub

    Private Sub MMUv2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MMUv2.Click
        MMUv.PerformClick()
    End Sub

    Private Sub MMUm2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MMUm2.Click
        MMUm.PerformClick()
    End Sub

    Private Sub MMPoUm2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MMPoUm2.Click
        MMPoUm.PerformClick()
    End Sub

    Private Sub MDarkT_Click(sender As Object, e As EventArgs) Handles MDarkT.Click
        Dim a As Boolean = Red
        DarkT = Not DarkT
        MDarkT.Checked = DarkT
        Red = a
    End Sub

    ' ==================== ССЫЛКИ И ВНЕШНИЕ ДЕЙСТВИЯ ====================
    Private Sub PrimeTEXT_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkClickedEventArgs) Handles PrimeTEXT.LinkClicked
        Dim url As String = e.LinkText
        Dim p As Integer = url.IndexOf(":"c)
        Dim scheme As String = If(p > 0, url.Substring(0, p).ToLowerInvariant(), "")

        Select Case scheme
            Case "http", "https", "mailto"
                OpenUrl(url)
            Case Else
                If MessageBox.Show(MAVLang.GetText("Msg_LinkWarning") & vbCrLf & vbCrLf & url, Application.ProductName,
                                   MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) = Windows.Forms.DialogResult.Yes Then
                    OpenUrl(url)
                End If
        End Select
    End Sub

    ' Запрос ограничиваем: слишком длинные URL не принимают ни браузеры, ни Uri.EscapeDataString
    Private Shared Function QueryFromSelection(s As String) As String
        Const MaxLen As Integer = 1500
        If s.Length > MaxLen Then
            Dim n As Integer = MaxLen
            If Char.IsHighSurrogate(s(n - 1)) Then n -= 1 ' не режем суррогатную пару
            s = s.Substring(0, n)
        End If
        Return Uri.EscapeDataString(s)
    End Function

    Private Sub SYandex()
        If Not String.IsNullOrWhiteSpace(PrimeTEXT.SelectedText) Then
            Dim searchQuery As String = QueryFromSelection(PrimeTEXT.SelectedText)
            OpenUrl("https://yandex.ru/search/?text=" & searchQuery)
        Else
            OpenUrl("https://yandex.ru")
        End If
    End Sub

    Private Sub SGoogle()
        If Not String.IsNullOrWhiteSpace(PrimeTEXT.SelectedText) Then
            Dim searchQuery As String = QueryFromSelection(PrimeTEXT.SelectedText)
            OpenUrl("https://www.google.com/search?q=" & searchQuery)
        Else
            OpenUrl("https://www.google.com")
        End If
    End Sub

    Private Sub MYandex_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MYandex.Click
        SYandex()
    End Sub
    Private Sub CMYandex_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CMYandex.Click
        SYandex()
    End Sub
    Private Sub MGoogle_Click(sender As Object, e As EventArgs) Handles MGoogle.Click
        SGoogle()
    End Sub
    Private Sub CMGoogle_Click(sender As Object, e As EventArgs) Handles CMGoogle.Click
        SGoogle()
    End Sub

    Private Sub MNewWindow_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MNewWindow.Click
        Try
            Process.Start(Application.ExecutablePath)
        Catch ex As Exception
            MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub MDateTime_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MDateTime.Click
        Using a As New FDataTime
            a.ApplyLanguage()
            If a.ShowDialog() = Windows.Forms.DialogResult.OK AndAlso a.Result Then
                PrimeTEXT.SelectedText = a.DT
            End If
        End Using
    End Sub

    Private Sub MInfo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MInfo.Click
        FAbout.ApplyLanguage()
        FAbout.ShowDialog()
    End Sub

    Private Sub MParamS_Click(sender As Object, e As EventArgs) Handles MParamS.Click
        MAVPrint.ShowPageSetup(Me, PrimeTEXT, DocN)
    End Sub

    Private Sub MPrint_Click(sender As Object, e As EventArgs) Handles MPrint.Click
        If PrimeTEXT.TextLength = 0 Then Exit Sub
        MAVPrint.Print(Me, PrimeTEXT, DocN)
    End Sub

    ' ИСПРАВЛЕНО: Защита от OverflowException при вводе огромных чисел
    Private Sub MPer_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MPer.Click
        Dim RES As String = InputBox(MAVLang.GetText("Input_LineNumber"), MAVLang.GetText("Input_GoToLine"))
        If String.IsNullOrWhiteSpace(RES) Then Return

        Dim lineNum As Integer
        If Not Integer.TryParse(RES.Trim(), lineNum) Then
            MessageBox.Show(MAVLang.GetText("Msg_OnlyNumbers"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        If lineNum <= 0 Then
            MessageBox.Show(MAVLang.GetText("Msg_OnlyPositive"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' Оптимизированный подсчет строк для больших файлов
        Dim totalLines As Integer = GetLineCount()

        If lineNum > totalLines Then
            MessageBox.Show(MAVLang.GetText("Msg_LineExceeds"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim PL As Integer = PrimeTEXT.GetFirstCharIndexFromLine(lineNum - 1)
        If PL >= 0 Then
            PrimeTEXT.Select(PL, 0)
            PrimeTEXT.Focus()
            PrimeTEXT.ScrollToCaret()
        End If
    End Sub

    Public Sub TFocus()
        PrimeTEXT.Focus()
    End Sub

    ' ==================== ВОССТАНОВЛЕНИЕ ПОСЛЕ СБОЕВ ====================
    Private Shared ReadOnly RecoveryDir As String = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MAV Symbols", "recovery")

    Private ReadOnly _recoveryFile As String = Path.Combine(RecoveryDir, "doc-" & Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture) & ".mavrec")

    ' Записывает несохранённый документ в файл восстановления (формат: 5 строк заголовка + текст в UTF-8)
    ' Возвращает True, только если файл восстановления реально записан.
    ' Может вызываться и из фонового потока (необработанное исключение): тогда работа переносится в UI-поток с таймаутом.
    Private Function WriteRecovery() As Boolean
        Try
            If PrimeTEXT.IsDisposed Then Return False
            If PrimeTEXT.InvokeRequired Then
                Dim ar As IAsyncResult = PrimeTEXT.BeginInvoke(New Func(Of Boolean)(AddressOf WriteRecovery))
                If ar.AsyncWaitHandle.WaitOne(3000) Then Return CBool(PrimeTEXT.EndInvoke(ar))
                Return False ' UI-поток завис - не ждём бесконечно
            End If
            If PrimeTEXT.TextLength > 50000000 Then Return False
            Directory.CreateDirectory(RecoveryDir)
            Dim tmp As String = _recoveryFile & ".tmp"
            Using sw As New StreamWriter(tmp, False, New UTF8Encoding(False))
                sw.WriteLine(SaveD)
                sw.WriteLine(DocN)
                sw.WriteLine(ActCodePage.ToString(CultureInfo.InvariantCulture))
                sw.WriteLine(UTFbom.ToString())
                sw.WriteLine(NewLineType)
                sw.Write(PrimeTEXT.Text)
            End Using
            ' Атомарная замена: старая копия не удаляется до того, как новая полностью записана
            If File.Exists(_recoveryFile) Then
                File.Replace(tmp, _recoveryFile, Nothing, True)
            Else
                File.Move(tmp, _recoveryFile)
            End If
            _recoveryDirty = False
            Return True
        Catch ex As Exception
            Return False ' восстановление - best-effort, не должно само вызывать ошибки
        End Try
    End Function

    Private Sub DeleteRecovery()
        _recoveryDirty = False
        Try
            If File.Exists(_recoveryFile) Then File.Delete(_recoveryFile)
        Catch ex As Exception
        End Try
    End Sub

    ' Жив ли процесс, которому принадлежит файл восстановления (тогда это чужой работающий экземпляр - не трогаем)
    Private Shared Function IsRecoveryOwnerAlive(filePath As String) As Boolean
        Try
            Dim nm As String = Path.GetFileNameWithoutExtension(filePath) ' doc-1234
            Dim pid As Integer
            If Not nm.StartsWith("doc-", StringComparison.Ordinal) OrElse Not Integer.TryParse(nm.Substring(4), pid) Then Return False
            If pid = Process.GetCurrentProcess().Id Then Return True
            Try
                Dim p As Process = Process.GetProcessById(pid)
                If Not String.Equals(p.ProcessName, Process.GetCurrentProcess().ProcessName, StringComparison.OrdinalIgnoreCase) Then Return False
                Try
                    ' Файл пишется уже после старта своего процесса; если процесс с этим PID стартовал позже - PID переиспользован
                    Return p.StartTime <= File.GetLastWriteTime(filePath).AddSeconds(2)
                Catch ex As Exception
                    Return True ' время старта недоступно - на всякий случай считаем, что владелец жив
                End Try
            Catch ex As ArgumentException
                Return False ' процесса с таким PID нет
            End Try
        Catch ex As Exception
            Return True ' не уверены - на всякий случай не трогаем
        End Try
    End Function

    Private Sub TryRestoreRecovery()
        Try
            If Not Directory.Exists(RecoveryDir) Then Return

            Dim best As String = Nothing
            Dim bestTime As DateTime = DateTime.MinValue
            For Each f As String In Directory.GetFiles(RecoveryDir, "doc-*.mavrec")
                If IsRecoveryOwnerAlive(f) Then Continue For
                Dim t As DateTime = File.GetLastWriteTime(f)
                If t < DateTime.Now.AddDays(-14) Then
                    Try
                        File.Delete(f) ' совсем старые копии не копим
                    Catch ex As Exception
                    End Try
                    Continue For
                End If
                If best Is Nothing OrElse t > bestTime Then
                    best = f
                    bestTime = t
                End If
            Next
            If best Is Nothing Then Return

            Dim srcPath, docName, cpS, bomS, nl, text As String
            Using sr As New StreamReader(best, New UTF8Encoding(False), False)
                srcPath = sr.ReadLine()
                docName = sr.ReadLine()
                cpS = sr.ReadLine()
                bomS = sr.ReadLine()
                nl = sr.ReadLine()
                text = sr.ReadToEnd()
            End Using

            If nl Is Nothing OrElse String.IsNullOrEmpty(text) Then
                File.Delete(best) ' пустой или повреждённый файл
                Return
            End If

            If String.IsNullOrWhiteSpace(docName) Then docName = MAVLang.GetText("NewDocument")
            Dim answer As DialogResult = MessageBox.Show(String.Format(MAVLang.GetText("Msg_RecoveryFound"), docName), Application.ProductName,
                                                         MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If answer = Windows.Forms.DialogResult.Yes Then
                Dim cp As Integer = 65001
                If Not Integer.TryParse(cpS, cp) Then cp = 65001
                Dim bom As Boolean = False
                Boolean.TryParse(bomS, bom)

                _loading = True
                Try
                    NewLineType = If(nl = "LF" OrElse nl = "CR", nl, "CRLF")
                    NewLineMixed = False
                    PrimeTEXT.ZoomFactor = ZoomF
                    SetEditorText(text)
                    PrimeTEXT.ClearUndo()
                Finally
                    _loading = False
                End Try
                _linesDirty = True

                ActCodePage = cp
                ActENC = EncNameFromCp(cp)
                UTFbom = bom
                slEnc.Text = ActENC
                slBOM.Text = If(bom AndAlso IsUnicodeCp(cp), "BOM", "")
                ' SaveD намеренно пустой: сохранение пойдёт через "Сохранить как", оригинал не будет перезаписан случайно
                SaveD = ""
                _restoredDoc = True
                DocN = docName
                Me.Text = Application.ProductName + " - " + DocN
                Red = True
                _recoveryDirty = True
                ObnovlenieTextL()
                ObnovlenieFUN()
            End If
            File.Delete(best)
        Catch ex As Exception
            ' восстановление не должно мешать запуску
        End Try
    End Sub

    Private Sub OnUiThreadException(ByVal sender As Object, ByVal e As System.Threading.ThreadExceptionEventArgs)
        If _inErrorHandler Then Return
        _inErrorHandler = True
        Try
            Dim saved As Boolean = Red AndAlso WriteRecovery()
            Dim msg As String = String.Format(MAVLang.GetText("Msg_UnexpectedError"), e.Exception.Message)
            If saved Then msg &= vbCrLf & vbCrLf & MAVLang.GetText("Msg_RecoverySaved")
            MessageBox.Show(msg, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
        Catch ex As Exception
        Finally
            _inErrorHandler = False
        End Try
    End Sub

    Private Sub OnDomainException(ByVal sender As Object, ByVal e As UnhandledExceptionEventArgs)
        Try
            If Red Then WriteRecovery()
        Catch ex As Exception
        End Try
    End Sub

    ' ==================== ТАЙМЕРЫ ====================
    Private Sub TDD_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TDD.Tick
        ' Этот таймер используется только для Drag&Drop, чтобы корректно обработать состояние
        Red = False
        PrimeTEXT.ClearUndo()
        ObnovlenieFUN()
        TDD.Enabled = False
    End Sub

    Private Sub TRS_Tick(sender As Object, e As EventArgs) Handles TRS.Tick
        ObnovlenieRazmFUN()
    End Sub

End Class