' MAV Symbols - Текстовый редактор
' Copyright © 2026 Andrey Morozov (MAV-128)
' Лицензия: GNU GPL v3.0


Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports Microsoft.Win32

Public NotInheritable Class MAVPrint

    Private Const RegistryPath As String = "Software\MAVPrint\PrintSettings"
    Private Const DefaultMargin As Integer = 2000   ' 20 мм
    Private Const DefaultPaperWidth As Integer = 827
    Private Const DefaultPaperHeight As Integer = 1169
    Private Const DefaultPaperName As String = "A4"

    Private Shared _printDocument As PrintDocument = Nothing
    Private Shared _initialized As Boolean = False

    Private Shared _marginLeft As Integer = DefaultMargin
    Private Shared _marginRight As Integer = DefaultMargin
    Private Shared _marginTop As Integer = DefaultMargin
    Private Shared _marginBottom As Integer = DefaultMargin
    Private Shared _landscape As Boolean = False
    Private Shared _paperKind As PaperKind = PaperKind.A4
    Private Shared _paperWidth As Integer = DefaultPaperWidth
    Private Shared _paperHeight As Integer = DefaultPaperHeight
    Private Shared _paperName As String = DefaultPaperName

    Private Shared _lastPrinterName As String = String.Empty

    Private Shared _activeSession As PrintSession = Nothing




    Private Sub New()

    End Sub

    ' ===== Локализация (MAVLang). Если ключа нет в словаре, используется запасной русский текст =====

    Private Shared Function T(key As String, fallback As String) As String
        Try
            If MAVLang.HasTranslation(key) Then
                Dim s As String = MAVLang.GetText(key)
                If Not String.IsNullOrEmpty(s) Then Return s
            End If
        Catch
        End Try
        Return fallback
    End Function

    Private Shared ReadOnly Property DefaultDocumentTitle As String
        Get
            Return T("Print_DefaultDocTitle", "Документ")
        End Get
    End Property

    Private Shared ReadOnly Property PrintCaption As String
        Get
            Return T("Print_Caption", "Печать")
        End Get
    End Property

    Private Shared ReadOnly Property NoPrintersMessage As String
        Get
            Return T("Print_NoPrinters", "В системе не установлено ни одного принтера.")
        End Get
    End Property

    ' ===== Нативный диалог «Параметры страницы» =====

    Private Const PSD_MINMARGINS As Integer = &H1
    Private Const PSD_MARGINS As Integer = &H2
    Private Const PSD_INHUNDREDTHSOFMILLIMETERS As Integer = &H8
    Private Const PSD_DISABLEPRINTER As Integer = &H20
    Private Const PSD_NONETWORKBUTTON As Integer = &H200000

    Private Shared ReadOnly Is64Bit As Boolean = (IntPtr.Size = 8)

    ' Смещения в структуре PAGESETUPDLGW: x86 — pack 1 (размер 84), x64 — естественное выравнивание (размер 128).
    Private Shared ReadOnly PsdSize As Integer = If(Is64Bit, 128, 84)
    Private Shared ReadOnly PsdOwner As Integer = If(Is64Bit, 8, 4)
    Private Shared ReadOnly PsdDevMode As Integer = If(Is64Bit, 16, 8)
    Private Shared ReadOnly PsdDevNames As Integer = If(Is64Bit, 24, 12)
    Private Shared ReadOnly PsdFlags As Integer = If(Is64Bit, 32, 16)
    Private Shared ReadOnly PsdMinMargin As Integer = If(Is64Bit, 44, 28)
    Private Shared ReadOnly PsdMargin As Integer = If(Is64Bit, 60, 44)

    <DllImport("comdlg32.dll", EntryPoint:="PageSetupDlgW", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function PageSetupDlg(lppsd As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function GlobalFree(hMem As IntPtr) As IntPtr
    End Function

    ' Сотые доли мм -> сотые доли дюйма (единица Margins в .NET) — только для печати.
    Private Shared Function HmmToInch100(hmm As Integer) As Integer
        Return Math.Max(0, CInt(Math.Round(hmm / 25.4R)))
    End Function

    ' Старый формат реестра (сотые доли дюйма) -> сотые доли мм.
    Private Shared Function Inch100ToHmm(inch100 As Integer) As Integer
        Return Math.Max(0, CInt(Math.Round(inch100 * 25.4R)))
    End Function


    Private Shared Function HasInstalledPrinters() As Boolean
        Try
            Return PrinterSettings.InstalledPrinters.Count > 0
        Catch
            Return False
        End Try
    End Function

    Private Shared Sub Initialize()

        If _initialized Then
            Return
        End If

        LoadSettings()

        _printDocument = New PrintDocument()

        AddHandler _printDocument.BeginPrint, AddressOf PrintDocument_BeginPrint
        AddHandler _printDocument.PrintPage, AddressOf PrintDocument_PrintPage
        AddHandler _printDocument.EndPrint, AddressOf PrintDocument_EndPrint

        _initialized = True


        ApplyLastPrinter()
        ApplyDocumentPageSettings()

    End Sub



    Private Shared Sub ApplyLastPrinter()

        If _printDocument Is Nothing Then
            Return
        End If

        If String.IsNullOrWhiteSpace(_lastPrinterName) Then
            Return
        End If

        Try

            Dim settings As PrinterSettings = _printDocument.PrinterSettings
            Dim oldPrinterName As String = settings.PrinterName


            settings.PrinterName = _lastPrinterName

            If Not settings.IsValid Then

                settings.PrinterName = oldPrinterName

            End If

        Catch

        End Try

    End Sub




    Private Shared Sub EnsureDocument(textBox As RichTextBox, documentTitle As String)

        If textBox Is Nothing Then

            Throw New ArgumentNullException("textBox")

        End If

        Initialize()

        If String.IsNullOrWhiteSpace(documentTitle) Then

            _printDocument.DocumentName = DefaultDocumentTitle

        Else

            _printDocument.DocumentName = documentTitle

        End If

    End Sub



    Private Shared Function CreatePageSettingsCopy() As PageSettings

        Dim source As PageSettings = _printDocument.DefaultPageSettings
        Dim result As New PageSettings()



        result.Landscape = source.Landscape

        If source.Margins IsNot Nothing Then

            result.Margins = New Margins(source.Margins.Left, source.Margins.Right, source.Margins.Top, source.Margins.Bottom)

        End If

        If source.PaperSize IsNot Nothing Then

            result.PaperSize = source.PaperSize

        End If

        Return result

    End Function



    Public Shared Function ShowPageSetup(owner As IWin32Window, textBox As RichTextBox, Optional documentTitle As String = "") As DialogResult

        EnsureDocument(textBox, documentTitle)

        ' ИСПРАВЛЕНО (3): нет принтеров — понятное сообщение вместо необработанного исключения.
        If Not HasInstalledPrinters() Then
            MessageBox.Show(owner, NoPrintersMessage, PrintCaption, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return DialogResult.Cancel
        End If

        Dim pageSettings As PageSettings = CreatePageSettingsCopy()
        Dim printerSettings As PrinterSettings = _printDocument.PrinterSettings

        Dim hDevMode As IntPtr = IntPtr.Zero
        Dim hDevNames As IntPtr = IntPtr.Zero
        Dim data As IntPtr = IntPtr.Zero

        Try

            hDevMode = printerSettings.GetHdevmode(pageSettings)
            hDevNames = printerSettings.GetHdevnames()

            data = Marshal.AllocHGlobal(PsdSize)

            For i As Integer = 0 To PsdSize - 4 Step 4
                Marshal.WriteInt32(data, i, 0)
            Next

            Marshal.WriteInt32(data, 0, PsdSize)
            Marshal.WriteIntPtr(data, PsdOwner, If(owner IsNot Nothing, owner.Handle, IntPtr.Zero))
            Marshal.WriteIntPtr(data, PsdDevMode, hDevMode)
            Marshal.WriteIntPtr(data, PsdDevNames, hDevNames)
            Marshal.WriteInt32(data, PsdFlags, PSD_MARGINS Or PSD_MINMARGINS Or PSD_INHUNDREDTHSOFMILLIMETERS Or PSD_DISABLEPRINTER Or PSD_NONETWORKBUTTON)

            ' rtMinMargin уже обнулён (минимальные поля = 0). rtMargin: left, top, right, bottom — сотые доли мм.
            Marshal.WriteInt32(data, PsdMargin, _marginLeft)
            Marshal.WriteInt32(data, PsdMargin + 4, _marginTop)
            Marshal.WriteInt32(data, PsdMargin + 8, _marginRight)
            Marshal.WriteInt32(data, PsdMargin + 12, _marginBottom)

            Dim accepted As Boolean = PageSetupDlg(data)

            ' Диалог мог пересоздать глобальные блоки — берём актуальные дескрипторы.
            hDevMode = Marshal.ReadIntPtr(data, PsdDevMode)
            hDevNames = Marshal.ReadIntPtr(data, PsdDevNames)

            If Not accepted Then
                Return DialogResult.Cancel
            End If

            If hDevMode <> IntPtr.Zero Then
                pageSettings.SetHdevmode(hDevMode)
            End If

            ReadDocumentPageSettings(pageSettings)

            _marginLeft = Math.Max(0, Marshal.ReadInt32(data, PsdMargin))
            _marginTop = Math.Max(0, Marshal.ReadInt32(data, PsdMargin + 4))
            _marginRight = Math.Max(0, Marshal.ReadInt32(data, PsdMargin + 8))
            _marginBottom = Math.Max(0, Marshal.ReadInt32(data, PsdMargin + 12))

            SaveSettings()
            ApplyDocumentPageSettings()

            Return DialogResult.OK

        Finally

            If hDevMode <> IntPtr.Zero Then
                GlobalFree(hDevMode)
            End If

            If hDevNames <> IntPtr.Zero Then
                GlobalFree(hDevNames)
            End If

            If data <> IntPtr.Zero Then
                Marshal.FreeHGlobal(data)
            End If

        End Try

    End Function




    Private Shared Sub ReadDocumentPageSettings(settings As PageSettings)

        If settings Is Nothing Then
            Return
        End If

        _landscape = settings.Landscape

        If settings.PaperSize IsNot Nothing Then

            Dim paper As PaperSize = settings.PaperSize

            _paperKind = paper.Kind
            _paperWidth = Math.Max(1, paper.Width)
            _paperHeight = Math.Max(1, paper.Height)

            If Not String.IsNullOrEmpty(paper.PaperName) Then

                _paperName = paper.PaperName

            End If

        End If

    End Sub



    Private Shared Sub ApplyDocumentPageSettings()

        If _printDocument Is Nothing Then
            Return
        End If

        ' ИСПРАВЛЕНО (3): весь блок под Try — при отсутствии/недоступности принтера
        ' PaperSizes и DefaultPageSettings могут бросать InvalidPrinterException.
        Try
            Dim settings As PageSettings = _printDocument.DefaultPageSettings

            settings.Landscape = _landscape
            settings.Margins = New Margins(HmmToInch100(_marginLeft), HmmToInch100(_marginRight), HmmToInch100(_marginTop), HmmToInch100(_marginBottom))

            Dim paper As PaperSize = FindPaperSize(_printDocument.PrinterSettings)

            If paper IsNot Nothing Then
                settings.PaperSize = paper
            End If
        Catch
        End Try

    End Sub




    Private Shared Function FindPaperSize(printerSettings As PrinterSettings) As PaperSize

        If printerSettings Is Nothing Then
            Return Nothing
        End If


        If _paperKind <> PaperKind.Custom Then

            For Each paper As PaperSize In printerSettings.PaperSizes

                If paper.Kind = _paperKind Then

                    Return paper

                End If

            Next

        End If



        If Not String.IsNullOrWhiteSpace(_paperName) Then

            For Each paper As PaperSize In
                printerSettings.PaperSizes

                If String.Equals(paper.PaperName, _paperName, StringComparison.OrdinalIgnoreCase) Then

                    If paper.Width = _paperWidth AndAlso paper.Height = _paperHeight Then

                        Return paper

                    End If

                End If

            Next

        End If




        For Each paper As PaperSize In
            printerSettings.PaperSizes

            If paper.Width = _paperWidth AndAlso paper.Height = _paperHeight Then

                Return paper

            End If

        Next



        If _paperWidth > 0 AndAlso _paperHeight > 0 Then

            Try

                Return New PaperSize(If(String.IsNullOrWhiteSpace(_paperName), "Custom", _paperName), _paperWidth, _paperHeight)

            Catch

                Return Nothing

            End Try

        End If

        Return Nothing

    End Function




    Public Shared Function Print(owner As IWin32Window, textBox As RichTextBox, Optional documentTitle As String = "") As DialogResult

        EnsureDocument(textBox, documentTitle)


        Dim textToPrint As String = textBox.Text.TrimEnd()

        If textToPrint.Length = 0 Then
            MessageBox.Show(owner, T("Print_EmptyDoc", "Нечего печатать: документ пуст."), PrintCaption, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return DialogResult.Cancel
        End If

        If _activeSession IsNot Nothing Then
            Return DialogResult.Abort
        End If


        If Not HasInstalledPrinters() Then
            MessageBox.Show(owner, NoPrintersMessage, PrintCaption, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return DialogResult.Cancel
        End If

        Dim printFont As Font = textBox.Font

        If printFont Is Nothing Then
            printFont = SystemFonts.DefaultFont
        End If

        Dim session As New PrintSession(textToPrint, printFont)

        _activeSession = session


        Try


            ApplyLastPrinter()
            ApplyDocumentPageSettings()


            With _printDocument.PrinterSettings

                .MinimumPage = 0
                .MaximumPage = 9999
                .PrintRange = PrintRange.AllPages
                .FromPage = 1
                .ToPage = 1
            End With

            Using dialog As New PrintDialog()

                dialog.Document = _printDocument
                dialog.UseEXDialog = True
                dialog.AllowCurrentPage = False
                dialog.AllowSelection = False
                dialog.AllowSomePages = True
                dialog.AllowPrintToFile = True


                EnsureValidPageRange(_printDocument.PrinterSettings)

                Dim result As DialogResult

                Do

                    result = dialog.ShowDialog(owner)

                    If result <> DialogResult.OK Then
                        Return result
                    End If


                    Dim rangeError As String = ValidatePageRange(_printDocument.PrinterSettings)

                    If rangeError Is Nothing Then
                        Exit Do
                    End If

                    MessageBox.Show(owner, rangeError, PrintCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning)

                    ' Подставляем допустимые значения и показываем диалог снова.
                    EnsureValidPageRange(_printDocument.PrinterSettings)

                Loop

                SaveLastPrinter()

                _printDocument.Print()

            End Using


            If session.RangeOutOfBounds Then
                MessageBox.Show(owner,
                                T("Print_RangeOutOfBounds", "Указанный диапазон страниц выходит за пределы документа.") & Environment.NewLine &
                                T("Print_TotalPages", "Всего страниц: ") & session.TotalPages.ToString(CultureInfo.CurrentCulture) & ".",
                                PrintCaption, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return DialogResult.Cancel
            End If

            Return DialogResult.OK

        Catch ex As Exception

            MessageBox.Show(owner, T("Print_Failed", "Не удалось выполнить печать.") & Environment.NewLine & Environment.NewLine & ex.Message, PrintCaption, MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return DialogResult.Abort

        Finally

            CleanupPrintState()

        End Try

    End Function




    Private Shared Sub EnsureValidPageRange(settings As PrinterSettings)

        If settings Is Nothing Then
            Return
        End If

        Try

            If settings.FromPage < 1 Then
                settings.FromPage = 1
            End If

            If settings.ToPage < settings.FromPage Then
                settings.ToPage = settings.FromPage
            End If

        Catch
        End Try

    End Sub


    Private Shared Function ValidatePageRange(settings As PrinterSettings) As String

        If settings Is Nothing OrElse settings.PrintRange <> PrintRange.SomePages Then
            Return Nothing
        End If

        If settings.FromPage < 1 Then
            Return T("Print_RangeInvalid", "Неверный диапазон страниц.") & Environment.NewLine &
                   T("Print_RangeFromOne", "Нумерация страниц начинается с 1 — диапазон может начинаться только с 1.")
        End If

        If settings.ToPage < settings.FromPage Then
            Return T("Print_RangeInvalid", "Неверный диапазон страниц.") & Environment.NewLine &
                   T("Print_RangeToLess", "Номер последней страницы не может быть меньше номера первой.")
        End If

        Return Nothing

    End Function

    Private Shared Sub SaveLastPrinter()

        Try

            If _printDocument Is Nothing Then
                Return
            End If

            Dim printerSettings As PrinterSettings = _printDocument.PrinterSettings

            If printerSettings Is Nothing Then
                Return
            End If

            If String.IsNullOrWhiteSpace(printerSettings.PrinterName) Then

                Return

            End If

            _lastPrinterName = printerSettings.PrinterName

            Using key As RegistryKey = Registry.CurrentUser.CreateSubKey(RegistryPath)

                If key Is Nothing Then
                    Return
                End If

                key.SetValue("LastPrinterName", _lastPrinterName, RegistryValueKind.String)

            End Using

        Catch


        End Try

    End Sub


    Private Shared Sub PrintDocument_BeginPrint(sender As Object, e As PrintEventArgs)

        If _activeSession Is Nothing Then
            Return
        End If


        If Not _activeSession.BeginPrint(_printDocument.PrinterSettings, _printDocument.DefaultPageSettings) Then
            e.Cancel = True
        End If

    End Sub


    Private Shared Sub PrintDocument_PrintPage(sender As Object, e As PrintPageEventArgs)

        If _activeSession Is Nothing Then

            e.HasMorePages = False

            Return

        End If

        _activeSession.PrintPage(e)

    End Sub



    Private Shared Sub PrintDocument_EndPrint(sender As Object, e As PrintEventArgs)

        If _activeSession Is Nothing Then
            Return
        End If

        _activeSession.EndPrint()

    End Sub



    Private Shared Sub CleanupPrintState()

        If _activeSession IsNot Nothing Then

            _activeSession.Dispose()

            _activeSession = Nothing

        End If

    End Sub


    Private Shared Sub SaveSettings()

        Try

            Using key As RegistryKey = Registry.CurrentUser.CreateSubKey(RegistryPath)

                If key Is Nothing Then
                    Return
                End If

                key.SetValue("PaperKind", CInt(_paperKind), RegistryValueKind.DWord)

                key.SetValue("PaperWidth", _paperWidth, RegistryValueKind.DWord)

                key.SetValue("PaperHeight", _paperHeight, RegistryValueKind.DWord)

                key.SetValue("PaperName", If(String.IsNullOrEmpty(_paperName), DefaultPaperName, _paperName), RegistryValueKind.String)

                key.SetValue("Landscape", If(_landscape, 1, 0), RegistryValueKind.DWord)

                key.SetValue("MarginLeftHmm", _marginLeft, RegistryValueKind.DWord)

                key.SetValue("MarginRightHmm", _marginRight, RegistryValueKind.DWord)

                key.SetValue("MarginTopHmm", _marginTop, RegistryValueKind.DWord)

                key.SetValue("MarginBottomHmm", _marginBottom, RegistryValueKind.DWord)

            End Using

        Catch



        End Try

    End Sub



    Private Shared Sub LoadSettings()

        SetDefaultSettings()

        Try

            Using key As RegistryKey = Registry.CurrentUser.OpenSubKey(RegistryPath, False)

                If key Is Nothing Then
                    Return
                End If


                Dim savedKind As Integer = GetRegistryInteger(key, "PaperKind", CInt(PaperKind.A4))

                If [Enum].IsDefined(GetType(PaperKind), savedKind) Then

                    _paperKind = CType(savedKind, PaperKind)

                End If



                _paperWidth = Math.Max(1, GetRegistryInteger(key, "PaperWidth", DefaultPaperWidth))

                _paperHeight = Math.Max(1, GetRegistryInteger(key, "PaperHeight", DefaultPaperHeight))


                Dim savedName As Object = key.GetValue("PaperName", DefaultPaperName)

                If savedName IsNot Nothing Then

                    _paperName = Convert.ToString(savedName, CultureInfo.InvariantCulture)

                End If

                If String.IsNullOrWhiteSpace(_paperName) Then

                    _paperName = DefaultPaperName

                End If


                _landscape = GetRegistryInteger(key, "Landscape", 0) <> 0


                _marginLeft = LoadMarginHmm(key, "MarginLeftHmm", "MarginLeft")

                _marginRight = LoadMarginHmm(key, "MarginRightHmm", "MarginRight")

                _marginTop = LoadMarginHmm(key, "MarginTopHmm", "MarginTop")

                _marginBottom = LoadMarginHmm(key, "MarginBottomHmm", "MarginBottom")


                Dim savedPrinter As Object = key.GetValue("LastPrinterName", String.Empty
                    )

                If savedPrinter IsNot Nothing Then

                    _lastPrinterName = Convert.ToString(savedPrinter, CultureInfo.InvariantCulture
                        )

                End If

            End Using

        Catch

            SetDefaultSettings()

        End Try

    End Sub



    Private Shared Sub SetDefaultSettings()

        _paperKind = PaperKind.A4

        _paperWidth = DefaultPaperWidth

        _paperHeight = DefaultPaperHeight

        _paperName = DefaultPaperName

        _landscape = False

        _marginLeft = DefaultMargin

        _marginRight = DefaultMargin

        _marginTop = DefaultMargin

        _marginBottom = DefaultMargin

        _lastPrinterName = String.Empty

    End Sub



    ' Новое значение (сотые доли мм); если его нет — переносим старое (сотые доли дюйма).
    Private Shared Function LoadMarginHmm(key As RegistryKey, hmmName As String, oldInchName As String) As Integer

        Try

            If key.GetValue(hmmName) IsNot Nothing Then
                Return Math.Max(0, GetRegistryInteger(key, hmmName, DefaultMargin))
            End If

            If key.GetValue(oldInchName) IsNot Nothing Then
                Return Inch100ToHmm(GetRegistryInteger(key, oldInchName, 79))
            End If

        Catch

        End Try

        Return DefaultMargin

    End Function

    Private Shared Function GetRegistryInteger(key As RegistryKey, name As String, defaultValue As Integer) As Integer

        Try

            Dim value As Object = key.GetValue(name, defaultValue)

            If value Is Nothing Then
                Return defaultValue
            End If

            Return Convert.ToInt32(value)

        Catch

            Return defaultValue

        End Try

    End Function




    Public Shared ReadOnly Property Document As PrintDocument

        Get

            Initialize()

            Return _printDocument

        End Get

    End Property




    Public Shared Sub ResetPageSettings()

        Initialize()

        _paperKind = PaperKind.A4

        _paperWidth = DefaultPaperWidth

        _paperHeight = DefaultPaperHeight

        _paperName = DefaultPaperName

        _landscape = False

        _marginLeft = DefaultMargin

        _marginRight = DefaultMargin

        _marginTop = DefaultMargin

        _marginBottom = DefaultMargin

        ApplyDocumentPageSettings()

        SaveSettings()

    End Sub



    Private NotInheritable Class PrintSession
        Implements IDisposable


        Private ReadOnly _text As String

        Private ReadOnly _font As Font

        Private _position As Integer

        Private _pageNumber As Integer

        Private _finished As Boolean

        Private _printRange As PrintRange

        Private _fromPage As Integer

        Private _toPage As Integer

        Private _lineHeight As Single

        Private _currentLineStart As Integer = -1

        Private _currentLineEnd As Integer = -1

        Private _currentLine As String = Nothing

        Private _currentLineBoundaries As Integer() = Nothing
        Private _tabWidth As Single

        Private _totalPages As Integer = -1

        Private _rangeOutOfBounds As Boolean


        Private Const MaxWindowLength As Integer = 4096

        Private Const MinWindowReserve As Integer = 1024


        Private Const TabSizeInChars As Integer = 8

        Private Shared ReadOnly LineBreakChars As Char() = New Char() {ControlChars.Cr, ControlChars.Lf}

        Public ReadOnly Property RangeOutOfBounds As Boolean
            Get
                Return _rangeOutOfBounds
            End Get
        End Property

        Public ReadOnly Property TotalPages As Integer
            Get
                Return _totalPages
            End Get
        End Property



        Public Sub New(text As String, font As Font)

            _text = If(text, String.Empty)

            _font = If(font, SystemFonts.DefaultFont)

            _position = 0

            _pageNumber = 0

            _finished = False

        End Sub


        Public Function BeginPrint(printerSettings As PrinterSettings, pageSettings As PageSettings) As Boolean

            _position = 0
            _pageNumber = 0
            _finished = False
            _lineHeight = 0.0F
            _tabWidth = 0.0F
            _totalPages = -1
            _rangeOutOfBounds = False

            ResetLineCache()

            If printerSettings Is Nothing Then

                _printRange = PrintRange.AllPages
                _fromPage = 1
                _toPage = Integer.MaxValue

            Else

                _printRange = printerSettings.PrintRange

                If _printRange = PrintRange.SomePages Then

                    Dim requestedFrom As Integer = printerSettings.FromPage
                    Dim requestedTo As Integer = printerSettings.ToPage


                    If requestedFrom <= 0 AndAlso requestedTo <= 0 Then

                        _printRange = PrintRange.AllPages
                        _fromPage = 1
                        _toPage = Integer.MaxValue

                    Else

                        _fromPage = Math.Max(1, requestedFrom)

                        If requestedTo <= 0 Then
                            _toPage = Integer.MaxValue
                        Else
                            _toPage = Math.Max(_fromPage, requestedTo)
                        End If

                    End If

                Else

                    _fromPage = 1
                    _toPage = Integer.MaxValue

                End If

            End If


            If _printRange = PrintRange.SomePages AndAlso
               printerSettings IsNot Nothing AndAlso
               pageSettings IsNot Nothing Then

                _totalPages = CountPages(printerSettings, pageSettings)

                If _totalPages >= 0 AndAlso _fromPage > _totalPages Then
                    _rangeOutOfBounds = True
                    _finished = True
                    Return False
                End If

            End If

            Return True

        End Function

        Private Sub ResetLineCache()

            _currentLineStart = -1
            _currentLineEnd = -1
            _currentLine = Nothing
            _currentLineBoundaries = Nothing

        End Sub


        Private Function CountPages(printerSettings As PrinterSettings, pageSettings As PageSettings) As Integer

            Dim savedPosition As Integer = _position

            Try

                Dim pageBounds As Rectangle = pageSettings.Bounds
                Dim margins As Margins = pageSettings.Margins

                Dim bounds As New RectangleF(pageBounds.Left + margins.Left,
                                             pageBounds.Top + margins.Top,
                                             pageBounds.Width - margins.Left - margins.Right,
                                             pageBounds.Height - margins.Top - margins.Bottom)

                If bounds.Width <= 0.0F OrElse bounds.Height <= 0.0F Then
                    Return -1
                End If

                Using measureGraphics As Graphics = printerSettings.CreateMeasurementGraphics()

                    If measureGraphics Is Nothing Then
                        Return -1
                    End If

                    _lineHeight = _font.GetHeight(measureGraphics)

                    If _lineHeight <= 0.0F Then
                        _lineHeight = 1.0F
                    End If

                    _position = 0

                    Dim pages As Integer = 0

                    While _position < _text.Length

                        Dim oldPosition As Integer = _position

                        LayoutPage(measureGraphics, bounds, Nothing, False)

                        If _position <= oldPosition Then
                            ForceProgress()
                            If _position <= oldPosition Then
                                Exit While
                            End If
                        End If

                        pages += 1

                    End While

                    Return pages

                End Using

            Catch

                Return -1

            Finally

                _position = savedPosition
                _lineHeight = 0.0F
                _tabWidth = 0.0F
                ResetLineCache()

            End Try

        End Function



        Public Sub PrintPage(e As PrintPageEventArgs)

            If _finished Then

                e.HasMorePages = False

                Return

            End If


            If String.IsNullOrEmpty(_text) Then

                _finished = True

                e.HasMorePages = False

                Return

            End If


            Dim graphics As Graphics = e.Graphics

            Dim bounds As RectangleF = e.MarginBounds


            If bounds.Width <= 0.0F OrElse
               bounds.Height <= 0.0F Then

                _finished = True

                e.HasMorePages = False

                Return

            End If


            If _lineHeight <= 0.0F Then

                _lineHeight = _font.GetHeight(graphics)

                If _lineHeight <= 0.0F Then
                    _lineHeight = 1.0F
                End If

            End If


            While _pageNumber + 1 < _fromPage AndAlso _position < _text.Length

                Dim oldPosition As Integer = _position

                LayoutPage(graphics, bounds, Nothing, False)

                _pageNumber += 1

                If _position <= oldPosition Then

                    ForceProgress()

                    If _position <= oldPosition Then

                        _finished = True

                        e.HasMorePages = False

                        Return

                    End If

                End If

            End While


            If _position >= _text.Length Then

                _finished = True

                e.HasMorePages = False

                Return

            End If




            Using brush As New SolidBrush(Color.Black)

                Dim oldPosition As Integer = _position

                LayoutPage(graphics, bounds, brush, True)



                If _position <= oldPosition Then

                    ForceProgress()

                    If _position <= oldPosition Then

                        _finished = True

                        e.HasMorePages = False

                        Return

                    End If

                End If

            End Using


            _pageNumber += 1


            If _position >= _text.Length Then

                _finished = True

                e.HasMorePages = False

                Return

            End If


            If _printRange = PrintRange.SomePages Then

                If _pageNumber >= _toPage Then

                    _finished = True

                    e.HasMorePages = False

                    Return

                End If

            End If


            e.HasMorePages = True

        End Sub



        Private Function LayoutPage(graphics As Graphics, bounds As RectangleF, brush As Brush, draw As Boolean) As Boolean

            Dim startPosition As Integer = _position
            Dim y As Single = bounds.Top

            ' Строка выше страницы: печатаем по одному символу (графеме) на страницу.
            If _lineHeight > bounds.Height Then

                If _position >= _text.Length Then
                    Return False
                End If

                Dim tallLineEnd As Integer = FindLogicalLineEnd(_position)

                If tallLineEnd = _position Then
                    _position = GetPositionAfterLineBreak(tallLineEnd)
                    Return _position > startPosition
                End If

                PrepareCurrentLine(_position, tallLineEnd)

                Dim tallLocal As Integer = _position - _currentLineStart
                Dim safeLength As Integer = GetSafeElementLength(tallLocal, _currentLine.Length)

                If safeLength <= 0 Then
                    safeLength = Math.Min(1, _currentLine.Length - tallLocal)
                End If

                If draw AndAlso brush IsNot Nothing Then

                    Using tallFormat As New StringFormat(StringFormat.GenericTypographic)

                        tallFormat.Trimming = StringTrimming.None
                        tallFormat.FormatFlags = StringFormatFlags.NoWrap
                        ApplyTabStops(graphics, tallFormat)

                        Dim drawableLength As Integer = TrimDrawableLength(_currentLine, tallLocal, safeLength)

                        If drawableLength > 0 Then
                            DrawTextRange(graphics, brush, tallFormat, _currentLine, tallLocal, drawableLength, bounds.Left, y, bounds.Width)
                        End If

                    End Using

                End If

                _position += safeLength

                Return _position > startPosition

            End If

            Using format As New StringFormat(StringFormat.GenericTypographic)

                format.Trimming = StringTrimming.None
                format.FormatFlags = StringFormatFlags.NoWrap


                ApplyTabStops(graphics, format)

                While _position < _text.Length

                    If y + _lineHeight > bounds.Bottom + 0.01F Then
                        Exit While
                    End If

                    Dim lineEnd As Integer = FindLogicalLineEnd(_position)

                    If lineEnd = _position Then
                        y += _lineHeight
                        _position = GetPositionAfterLineBreak(lineEnd)
                        Continue While
                    End If

                    ' _position — начало очередной визуальной строки внутри логической строки.
                    While _position < lineEnd

                        If y + _lineHeight > bounds.Bottom + 0.01F Then
                            Exit While
                        End If


                        PrepareCurrentLine(_position, lineEnd)

                        Dim localPosition As Integer = _position - _currentLineStart
                        Dim remainingLength As Integer = _currentLine.Length - localPosition
                        Dim reachesLineEnd As Boolean = (_currentLineStart + _currentLine.Length >= lineEnd)

                        Dim fittedLength As Integer = FindCharactersThatFit(graphics, _currentLine, localPosition, remainingLength, _font, bounds.Width, format)

                        If fittedLength <= 0 Then

                            fittedLength = GetSafeElementLength(localPosition, _currentLine.Length)

                            If fittedLength <= 0 Then
                                fittedLength = Math.Min(1, remainingLength)
                            End If

                        End If

                        Dim actualLength As Integer = fittedLength

                        If fittedLength < remainingLength OrElse Not reachesLineEnd Then

                            Dim breakPosition As Integer = FindLastWordBreak(_currentLine, localPosition, fittedLength)

                            If breakPosition > localPosition Then
                                actualLength = breakPosition - localPosition
                            End If

                        End If

                        If actualLength <= 0 Then

                            actualLength = GetSafeElementLength(localPosition, _currentLine.Length)

                            If actualLength <= 0 Then
                                actualLength = Math.Min(1, remainingLength)
                            End If

                        End If

                        Dim drawableLength As Integer = TrimDrawableLength(_currentLine, localPosition, actualLength)

                        If draw AndAlso
                           brush IsNot Nothing AndAlso
                           drawableLength > 0 Then

                            DrawTextRange(graphics, brush, format, _currentLine, localPosition, drawableLength, bounds.Left, y, bounds.Width)

                        End If

                        _position += actualLength
                        y += _lineHeight

                    End While

                    ' Логическая строка закончилась — переходим за перевод строки.
                    ' Иначе страница закончилась посреди строки: внешний цикл выйдет по проверке высоты.
                    If _position >= lineEnd Then
                        _position = GetPositionAfterLineBreak(lineEnd)
                    End If

                End While

            End Using

            Return _position > startPosition

        End Function


        Private Sub ApplyTabStops(graphics As Graphics, format As StringFormat)

            If _tabWidth <= 0.0F Then

                _tabWidth = graphics.MeasureString(New String("0"c, TabSizeInChars), _font, New SizeF(Single.MaxValue, Single.MaxValue), format).Width

                If _tabWidth <= 0.0F Then
                    _tabWidth = Math.Max(1.0F, _lineHeight * 4.0F)
                End If

            End If

            format.SetTabStops(0.0F, New Single() {_tabWidth})

        End Sub




        Private Sub PrepareCurrentLine(position As Integer, lineEnd As Integer)

            ' Текущее окно подходит, если оно покрывает position и справа есть запас
            ' (либо доходит до конца логической строки).
            If _currentLine IsNot Nothing AndAlso
               _currentLineEnd = lineEnd AndAlso
               position >= _currentLineStart Then

                Dim cachedEnd As Integer = _currentLineStart + _currentLine.Length

                If position < cachedEnd AndAlso
                   (cachedEnd >= lineEnd OrElse cachedEnd - position >= MinWindowReserve) Then
                    Return
                End If

            End If

            Dim windowEnd As Integer = Math.Min(lineEnd, position + MaxWindowLength)

            ' Не разрезаем графему (суррогатная пара, комбинируемые знаки, ZWJ-последовательность)
            ' на границе окна.
            Dim guard As Integer = 0

            While windowEnd < lineEnd AndAlso
                  guard < 64 AndAlso
                  IsClusterContinuation(_text, windowEnd)

                windowEnd += 1
                guard += 1

            End While

            _currentLineStart = position
            _currentLineEnd = lineEnd
            _currentLine = _text.Substring(position, windowEnd - position)
            _currentLineBoundaries = StringInfo.ParseCombiningCharacters(_currentLine)

            NormalizeUnicodeBoundaries(_currentLine, _currentLineBoundaries)

        End Sub

        Private Shared Function IsClusterContinuation(text As String, index As Integer) As Boolean

            Dim ch As Char = text(index)

            If Char.IsLowSurrogate(ch) AndAlso index > 0 AndAlso Char.IsHighSurrogate(text(index - 1)) Then
                Return True
            End If

            If IsVariationSelector(ch) OrElse IsZeroWidthJoiner(ch) Then
                Return True
            End If

            If index > 0 AndAlso IsZeroWidthJoiner(text(index - 1)) Then
                Return True
            End If

            Select Case CharUnicodeInfo.GetUnicodeCategory(ch)
                Case UnicodeCategory.NonSpacingMark,
                     UnicodeCategory.SpacingCombiningMark,
                     UnicodeCategory.EnclosingMark
                    Return True
            End Select

            Return False

        End Function





        Private Function FindLogicalLineEnd(startPosition As Integer) As Integer

            If startPosition >= _text.Length Then
                Return _text.Length
            End If

            Dim index As Integer = _text.IndexOfAny(LineBreakChars, startPosition)

            If index < 0 Then
                Return _text.Length
            End If

            Return index

        End Function




        Private Function GetPositionAfterLineBreak(lineEnd As Integer) As Integer

            If lineEnd >= _text.Length Then

                Return lineEnd

            End If


            If _text(lineEnd) = ControlChars.Cr Then

                If lineEnd + 1 < _text.Length AndAlso _text(lineEnd + 1) = ControlChars.Lf Then

                    Return lineEnd + 2

                End If

                Return lineEnd + 1

            End If


            If _text(lineEnd) = ControlChars.Lf Then

                Return lineEnd + 1

            End If


            Return lineEnd

        End Function





        Private Function FindCharactersThatFit(graphics As Graphics, text As String, startPosition As Integer, maximumLength As Integer, font As Font, availableWidth As Single, format As StringFormat) As Integer

            If maximumLength <= 0 OrElse
               availableWidth <= 0.0F Then
                Return 0
            End If

            Dim boundaries As Integer() = _currentLineBoundaries

            If boundaries Is Nothing OrElse
               boundaries.Length = 0 Then
                Return 0
            End If

            Dim startIndex As Integer = FindBoundaryIndex(boundaries, startPosition)
            Dim maximumEnd As Integer = startPosition + maximumLength

            ' Последняя допустимая граница, не выходящая за maximumEnd.
            Dim lastIndex As Integer = FindBoundaryIndex(boundaries, maximumEnd)

            If lastIndex <= startIndex Then
                Return 0
            End If

            Dim bestIndex As Integer = startIndex
            Dim failIndex As Integer = lastIndex + 1
            Dim stepSize As Integer = 64

            While bestIndex < lastIndex

                Dim probe As Integer = Math.Min(bestIndex + stepSize, lastIndex)

                If FitsWidth(graphics, text, startPosition, boundaries(probe), font, availableWidth, format) Then
                    bestIndex = probe
                    stepSize *= 2
                Else
                    failIndex = probe
                    Exit While
                End If

            End While

            Dim low As Integer = bestIndex + 1
            Dim high As Integer = failIndex - 1

            While low <= high

                Dim middle As Integer = low + ((high - low) \ 2)

                If FitsWidth(graphics, text, startPosition, boundaries(middle), font, availableWidth, format) Then
                    bestIndex = middle
                    low = middle + 1
                Else
                    high = middle - 1
                End If

            End While

            If bestIndex <= startIndex Then
                Return 0
            End If

            Return boundaries(bestIndex) - startPosition

        End Function

        Private Shared Function FitsWidth(graphics As Graphics, text As String, startPosition As Integer, endPosition As Integer, font As Font, availableWidth As Single, format As StringFormat) As Boolean

            Dim length As Integer = endPosition - startPosition

            If length <= 0 Then
                Return True
            End If

            Dim sample As String = text.Substring(startPosition, length)
            Dim measured As SizeF = graphics.MeasureString(sample, font, New SizeF(Single.MaxValue, Single.MaxValue), format)

            Return measured.Width <= availableWidth

        End Function



        Private Shared Function FindBoundaryIndex(boundaries As Integer(), position As Integer) As Integer

            If boundaries Is Nothing OrElse
               boundaries.Length = 0 Then

                Return 0

            End If


            Dim low As Integer = 0

            Dim high As Integer = boundaries.Length - 1

            Dim result As Integer = 0


            While low <= high

                Dim middle As Integer = low + ((high - low) \ 2)


                If boundaries(middle) <= position Then

                    result = middle

                    low = middle + 1

                Else

                    high = middle - 1

                End If

            End While


            Return result

        End Function



        Private Function GetSafeElementLength(startPosition As Integer, endPosition As Integer) As Integer

            If endPosition <= startPosition Then

                Return 0

            End If


            Dim boundaries As Integer() = _currentLineBoundaries

            If boundaries Is Nothing OrElse
               boundaries.Length = 0 Then

                Return Math.Min(1, endPosition - startPosition)

            End If


            Dim index As Integer = FindBoundaryIndex(boundaries, startPosition)


            If index < boundaries.Length AndAlso
               boundaries(index) < startPosition Then

                index += 1

            End If


            Dim nextPosition As Integer

            If index < boundaries.Length Then

                nextPosition = boundaries(index)

            Else

                nextPosition = _currentLine.Length

            End If


            If nextPosition <= startPosition Then

                Return Math.Min(1, endPosition - startPosition)

            End If


            Return Math.Min(nextPosition - startPosition, endPosition - startPosition)

        End Function



        Private Function FindLastWordBreak(text As String, startPosition As Integer, length As Integer) As Integer

            Dim endPosition As Integer = startPosition + length

            Dim boundaries As Integer() = _currentLineBoundaries

            If boundaries Is Nothing OrElse
               boundaries.Length = 0 Then

                Return -1

            End If


            Dim index As Integer = FindBoundaryIndex(boundaries, startPosition)


            Dim best As Integer = -1


            While index < boundaries.Length

                Dim elementStart As Integer = boundaries(index)

                If elementStart >= endPosition Then

                    Exit While

                End If


                Dim elementEnd As Integer

                If index + 1 < boundaries.Length Then

                    elementEnd = boundaries(index + 1)

                Else

                    elementEnd = text.Length

                End If


                If elementEnd > endPosition Then

                    elementEnd = endPosition

                End If


                If elementEnd > elementStart Then

                    Dim firstChar As Char = text(elementStart)

                    If firstChar = " "c OrElse
                       firstChar = ControlChars.Tab Then

                        best = elementEnd

                    End If

                End If


                index += 1

            End While


            Return best

        End Function



        Private Shared Function TrimDrawableLength(text As String, startPosition As Integer, length As Integer) As Integer

            Dim result As Integer = length


            While result > 0

                Dim ch As Char = text(startPosition + result - 1)


                If ch <> " "c AndAlso
                   ch <> ControlChars.Tab Then

                    Exit While

                End If


                result -= 1

            End While


            Return result

        End Function



        ' StringFormat теперь создаётся один раз на страницу и передаётся сюда (с табуляцией).
        Private Sub DrawTextRange(graphics As Graphics, brush As Brush, format As StringFormat, text As String, startPosition As Integer, length As Integer, x As Single, y As Single, width As Single)

            If graphics Is Nothing OrElse
               brush Is Nothing OrElse
               format Is Nothing OrElse
               length <= 0 OrElse
               width <= 0.0F Then
                Return
            End If

            Dim printableText As String = text.Substring(startPosition, length)

            Dim rectangle As New RectangleF(x, y, width, Math.Max(1.0F, _lineHeight * 1.5F))

            graphics.DrawString(printableText, _font, brush, rectangle, format)

        End Sub



        Private Shared Sub NormalizeUnicodeBoundaries(text As String, ByRef boundaries As Integer())

            If String.IsNullOrEmpty(text) OrElse boundaries Is Nothing OrElse boundaries.Length = 0 Then

                Return

            End If


            Dim list As New List(Of Integer)

            list.Add(0)


            For i As Integer = 1 To boundaries.Length - 1

                Dim boundary As Integer = boundaries(i)

                If boundary <= 0 OrElse boundary >= text.Length Then

                    Continue For

                End If

                If Char.IsLowSurrogate(text(boundary)) AndAlso Char.IsHighSurrogate(text(boundary - 1)) Then

                    Continue For

                End If

                If IsVariationSelector(text(boundary)) Then

                    Continue For

                End If


                If boundary > 0 AndAlso
                   IsZeroWidthJoiner(text(boundary - 1)) Then

                    Continue For

                End If


                list.Add(boundary)

            Next


            If list.Count = 0 OrElse
               list(list.Count - 1) <> text.Length Then

                list.Add(text.Length)

            End If


            boundaries = list.ToArray()

        End Sub




        Private Shared Function IsVariationSelector(ch As Char) As Boolean

            Dim code As Integer = AscW(ch)

            Return (code >= &HFE00 AndAlso code <= &HFE0F)

        End Function




        Private Shared Function IsZeroWidthJoiner(ch As Char) As Boolean

            Return AscW(ch) = &H200D

        End Function



        Private Sub ForceProgress()

            If _position >= _text.Length Then
                Return
            End If

            Dim lineEnd As Integer = FindLogicalLineEnd(_position)

            If lineEnd = _position Then
                _position = GetPositionAfterLineBreak(lineEnd)
                Return
            End If

            PrepareCurrentLine(_position, lineEnd)

            Dim localPosition As Integer = _position - _currentLineStart
            Dim safeLength As Integer = GetSafeElementLength(localPosition, _currentLine.Length)

            If safeLength <= 0 Then
                safeLength = Math.Min(1, _currentLine.Length - localPosition)
            End If

            _position += safeLength

        End Sub




        Public Sub EndPrint()

            _finished = True

        End Sub



        Public Sub Dispose() Implements IDisposable.Dispose

            _currentLine = Nothing
            _currentLineBoundaries = Nothing

        End Sub

    End Class

End Class