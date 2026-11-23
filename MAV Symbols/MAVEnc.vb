' MAV Symbols - Текстовый редактор
' Copyright © 2026 Andrey Morozov (MAV-128)
' Лицензия: GNU GPL v3.0


Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Text


Public Class MAVEnc

    ' ----------------------------------------------------------------------
    ' КОНСТАНТЫ

    Private Const WHOLE_FILE_LIMIT As Integer = 1048576 ' файлы до 1 МБ читаем целиком
    Private Const BLOCK_SIZE As Integer = 262144        ' для больших файлов: начало, середина и конец по 256 КБ

    Private Const CODE_BENIGN As Integer = -1   ' безобидный типографский символ (« » — – … и т.п.)
    Private Const CODE_GARBAGE As Integer = -2  ' мусор: псевдографика, неопределённые байты
    Private Const CODE_RARE As Integer = -3     ' редкие кириллические буквы (украинские, сербские и т.д.)

    Private FName As String 'путь к файлу, с которым работаем
    Private ResCalc As Boolean = False 'результат детекции уже вычислен

    ' ----------------------------------------------------------------------
    ' ПУБЛИЧНЫЕ СВОЙСТВА (интерфейс не менять)

    Public Property EncName As String 'название детектированной кодировки
    Public Property CodePage As Integer 'числовой ID кодировки (кодовая страница)
    Public Property BOM As Boolean 'есть ли у файла BOM
    Public Property Confidence As Double 'уверенность детекции победившей кодировки (0..1)
    Public Property DMethod As String 'метод, которым обнаружена кодировка
    Public Property IsDetected As Boolean = True 'удалось ли произвести детекцию
    Public Property AConfidences As String 'полная информация о детекции по всем кодировкам

    ' ----------------------------------------------------------------------
    ' ВНУТРЕННИЙ КЛАСС-КАНДИДАТ

    Private NotInheritable Class Candidate
        Public ReadOnly Name As String
        Public ReadOnly CodePage As Integer
        Public ReadOnly BOM As Boolean
        Public ReadOnly Method As String
        Public Confidence As Double
        Public Score As Double

        Public Sub New(encName As String, cp As Integer, hasBom As Boolean, conf As Double, detMethod As String)
            Name = encName
            CodePage = cp
            BOM = hasBom
            Confidence = conf
            Method = detMethod
        End Sub
    End Class

    ' ----------------------------------------------------------------------
    ' СТАТИЧЕСКИЕ ТАБЛИЦЫ (создаются один раз)

    ' ln частоты русских букв: индексы 0..31 = а..я (порядок Unicode), 32 = ё
    Private Shared ReadOnly LnRuFreq As Double() = New Double(32) {}
    ' Частые русские биграммы (индексы букв)
    Private Shared ReadOnly Bigram As Boolean(,) = New Boolean(32, 32) {}

    ' Для каждого байта 0x80..0xFF: индекс буквы (0..32) или CODE_*; Upper = заглавная буква
    Private Shared ReadOnly Code1251 As Integer() = New Integer(255) {}
    Private Shared ReadOnly Upper1251 As Boolean() = New Boolean(255) {}
    Private Shared ReadOnly CodeKoi As Integer() = New Integer(255) {}
    Private Shared ReadOnly UpperKoi As Boolean() = New Boolean(255) {}
    Private Shared ReadOnly Code866 As Integer() = New Integer(255) {}
    Private Shared ReadOnly Upper866 As Boolean() = New Boolean(255) {}

    ' Windows-1252: 1 = маловероятный байт, 2 = не определён в кодовой странице
    Private Shared ReadOnly W1252Flag As Integer() = New Integer(255) {}

    Shared Sub New()
        Dim freq As Double() = {
            8.01, 1.59, 4.54, 1.7, 2.98, 8.45, 0.94, 1.65, 7.35, 1.21, 3.49, 4.4, 3.21, 6.7, 10.97, 2.81,
            4.73, 5.47, 6.26, 2.62, 0.26, 0.97, 0.48, 1.44, 0.73, 0.36, 0.04, 1.9, 1.74, 0.33, 0.64, 2.01, 0.15
        }
        For i As Integer = 0 To 32
            LnRuFreq(i) = Math.Log(Math.Max(freq(i), 0.04) / 100.0)
        Next

        Dim bigrams As String() = {
            "ст", "но", "то", "на", "ен", "ов", "ни", "ра", "во", "ко", "по", "ро", "ре", "ол", "ос", "ал",
            "ли", "ет", "ер", "ан", "ор", "ка", "не", "пр", "ти", "ла", "ве", "ат", "ит", "ва", "ле", "ие",
            "ль", "ом", "ий", "ем", "од", "ны", "ск", "об", "да", "го", "ия", "ел", "те", "ас", "ть", "ин",
            "ил", "ид", "ми", "ик", "ме", "их", "ту", "ча", "ое", "ой", "ых", "от", "ра", "ие", "ны", "ем"
        }
        For Each p As String In bigrams
            Bigram(AscW(p(0)) - &H430, AscW(p(1)) - &H430) = True
        Next

        ' ---------------- Windows-1251 ----------------
        For b As Integer = &H80 To &HFF
            Code1251(b) = CODE_GARBAGE
        Next
        For Each b As Integer In New Integer() {&H82, &H84, &H85, &H86, &H87, &H88, &H89, &H8B, &H91, &H92, &H93, &H94,
                                                &H95, &H96, &H97, &H99, &H9B, &HA0, &HA7, &HA9, &HAB, &HAD, &HAE, &HB0,
                                                &HB1, &HB7, &HB9, &HBB}
            Code1251(b) = CODE_BENIGN
        Next
        For Each b As Integer In New Integer() {&H80, &H81, &H83, &H8A, &H8C, &H8D, &H8E, &H8F, &H90, &H9A, &H9C, &H9D,
                                                &H9E, &H9F, &HA1, &HA2, &HA3, &HA5, &HAA, &HAF, &HB2, &HB3, &HB4, &HBA,
                                                &HBC, &HBD, &HBE, &HBF}
            Code1251(b) = CODE_RARE
        Next
        For i As Integer = 0 To 31
            Code1251(&HC0 + i) = i
            Upper1251(&HC0 + i) = True
            Code1251(&HE0 + i) = i
        Next
        Code1251(&HA8) = 32
        Upper1251(&HA8) = True
        Code1251(&HB8) = 32

        ' ---------------- KOI8-R ----------------
        For b As Integer = &H80 To &HFF
            CodeKoi(b) = CODE_GARBAGE
        Next
        For Each b As Integer In New Integer() {&H9A, &H9C, &H9E, &HBF}
            CodeKoi(b) = CODE_BENIGN
        Next
        For Each b As Integer In New Integer() {&HA4, &HA6, &HA7, &HAD, &HB4, &HB6, &HB7, &HBD}
            CodeKoi(b) = CODE_RARE
        Next
        Dim koiLower As String = "юабцдефгхийклмнопярстужвьызшэщчъ"
        Dim koiUpper As String = "ЮАБЦДЕФГХИЙКЛМНОПЯРСТУЖВЬЫЗШЭЩЧЪ"
        For i As Integer = 0 To 31
            CodeKoi(&HC0 + i) = AscW(koiLower(i)) - &H430
            CodeKoi(&HE0 + i) = AscW(koiUpper(i)) - &H410
            UpperKoi(&HE0 + i) = True
        Next
        CodeKoi(&HA3) = 32
        CodeKoi(&HB3) = 32
        UpperKoi(&HB3) = True

        ' ---------------- CP866 ----------------
        For b As Integer = &H80 To &HFF
            Code866(b) = CODE_GARBAGE
        Next
        For Each b As Integer In New Integer() {&HF8, &HFA, &HFC, &HFF}
            Code866(b) = CODE_BENIGN
        Next
        For b As Integer = &HF2 To &HF7
            Code866(b) = CODE_RARE
        Next
        For i As Integer = 0 To 31
            Code866(&H80 + i) = i
            Upper866(&H80 + i) = True
        Next
        For i As Integer = 0 To 15
            Code866(&HA0 + i) = i
            Code866(&HE0 + i) = 16 + i
        Next
        Code866(&HF0) = 32
        Upper866(&HF0) = True
        Code866(&HF1) = 32

        ' ---------------- Windows-1252 ----------------
        For Each b As Integer In New Integer() {&H81, &H8D, &H8F, &H90, &H9D}
            W1252Flag(b) = 2
        Next
        For Each b As Integer In New Integer() {&HA4, &HA5, &HA6, &HA8, &HAC, &HAF, &HB4, &HB6, &HB8}
            W1252Flag(b) = 1
        Next
    End Sub

    ' ----------------------------------------------------------------------
    ' ПУБЛИЧНЫЙ API

    Public Sub New(filePath As String)
        FName = filePath
    End Sub

    Public Function DetectEnc() As MAVEnc
        If ResCalc Then
            Return Me
        End If

        ResetProperties()

        If String.IsNullOrEmpty(FName) OrElse Not File.Exists(FName) Then
            Throw New FileNotFoundException("Файл не найден", FName)
        End If

        CalcEncoding()

        ResCalc = True
        Return Me
    End Function

    Public Function GetAllEncodingsWithConfidence() As String
        Dim result = DetectEnc()
        Return result.AConfidences
    End Function

    ' ----------------------------------------------------------------------
    ' ОСНОВНОЙ АЛГОРИТМ

    Private Sub CalcEncoding()
        Dim wholeFile As Boolean = True
        Dim fileLen As Long = 0
        Dim segs As List(Of Byte()) = ReadSample(wholeFile, fileLen)
        Dim data As Byte() = Concat(segs)

        ' --- Пустой файл ---
        If data.Length = 0 Then
            Apply(New Candidate("UTF-8", 65001, False, 0, "Пустой файл"), New List(Of Candidate)())
            Return
        End If

        ' --- 1. BOM ---
        Dim bomResult As Candidate = DetectByBOM(data, fileLen)
        If bomResult IsNot Nothing Then
            Apply(bomResult, New List(Of Candidate) From {bomResult})
            Return
        End If

        ' Предварительная статистика: нулевые и "высокие" байты
        Dim nulCount As Integer = 0
        Dim highCount As Integer = 0
        For i As Integer = 0 To data.Length - 1
            Dim b As Byte = data(i)
            If b = 0 Then
                nulCount += 1
            ElseIf b >= &H80 Then
                highCount += 1
            End If
        Next

        ' --- 2. UTF-16 без BOM ---
        If nulCount > 0 Then
            Dim u16 As Candidate = DetectUtf16NoBom(data)
            If u16 IsNot Nothing Then
                Apply(u16, New List(Of Candidate) From {u16})
                Return
            End If
        End If

        ' --- 3. Строгий ASCII ---
        If highCount = 0 Then
            Dim asc As New Candidate("ASCII", 20127, False, If(wholeFile, 1.0, 0.95), "Strict ASCII Check")
            Apply(asc, New List(Of Candidate) From {asc})
            Return
        End If

        ' --- 4. Строгий UTF-8 ---
        Dim u8Valid As Integer = 0
        Dim u8Invalid As Integer = 0
        For s As Integer = 0 To segs.Count - 1
            ScanUtf8(segs(s), s = 0, s = segs.Count - 1, u8Valid, u8Invalid)
        Next

        If u8Valid > 0 AndAlso (u8Invalid = 0 OrElse (u8Valid >= 20 AndAlso u8Invalid <= u8Valid * 0.01)) Then
            ' Вероятность того, что не-UTF-8 текст случайно даст n валидных последовательностей, ~0.15^n
            Dim conf As Double = Math.Min(0.999, 1.0 - Math.Pow(0.15, u8Valid))
            Dim method As String = "Strict UTF-8 Validation"
            If u8Invalid > 0 Then
                conf *= 0.9
                method &= " (with errors)"
            End If
            Dim u8 As New Candidate("UTF-8", 65001, False, conf, method)
            Apply(u8, New List(Of Candidate) From {u8})
            Return
        End If

        ' --- 5. Статистические модели ---
        Dim ranked As List(Of Candidate) = AnalyzeLegacy(data, highCount)

        If ranked(0).Score < 0.08 Then
            ' Ни одна модель не подошла — берём системную ANSI-кодировку (как Блокнот)
            Dim fb As Candidate = GetAnsiFallback()
            ranked.Add(fb)
            Apply(fb, ranked)
        Else
            Apply(ranked(0), ranked)
        End If
    End Sub

    Private Sub Apply(c As Candidate, all As List(Of Candidate))
        EncName = c.Name
        CodePage = c.CodePage
        BOM = c.BOM
        Confidence = c.Confidence
        DMethod = c.Method
        IsDetected = True
        AConfidences = BuildAllConfidencesText(all)
    End Sub

    Private Sub ResetProperties()
        EncName = Nothing
        CodePage = 0
        BOM = False
        Confidence = 0
        DMethod = Nothing
        IsDetected = True
        AConfidences = Nothing
    End Sub

    ' ----------------------------------------------------------------------
    ' ЧТЕНИЕ ВЫБОРКИ

    Private Function ReadSample(ByRef wholeFile As Boolean, ByRef fileLen As Long) As List(Of Byte())
        Dim segs As New List(Of Byte())
        Using fs As New FileStream(FName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite Or FileShare.Delete)
            fileLen = fs.Length
            If fileLen <= WHOLE_FILE_LIMIT Then
                wholeFile = True
                segs.Add(ReadBlock(fs, 0, CInt(fileLen)))
            Else
                wholeFile = False
                ' Смещения чётные, чтобы не нарушать выравнивание UTF-16
                Dim midStart As Long = ((fileLen \ 2) - (BLOCK_SIZE \ 2)) And Not 1L
                Dim tailStart As Long = (fileLen - BLOCK_SIZE) And Not 1L
                segs.Add(ReadBlock(fs, 0, BLOCK_SIZE))
                segs.Add(ReadBlock(fs, midStart, BLOCK_SIZE))
                segs.Add(ReadBlock(fs, tailStart, CInt(fileLen - tailStart)))
            End If
        End Using
        Return segs
    End Function

    Private Shared Function ReadBlock(fs As FileStream, offset As Long, count As Integer) As Byte()
        Dim buf(count - 1) As Byte
        fs.Position = offset
        Dim total As Integer = 0
        While total < count
            Dim r As Integer = fs.Read(buf, total, count - total)
            If r <= 0 Then Exit While
            total += r
        End While
        If total < count Then
            ReDim Preserve buf(total - 1)
        End If
        Return buf
    End Function

    Private Shared Function Concat(segs As List(Of Byte())) As Byte()
        If segs.Count = 1 Then Return segs(0)
        Dim total As Integer = 0
        For Each s As Byte() In segs
            total += s.Length
        Next
        Dim res(total - 1) As Byte
        Dim pos As Integer = 0
        For Each s As Byte() In segs
            Buffer.BlockCopy(s, 0, res, pos, s.Length)
            pos += s.Length
        Next
        Return res
    End Function

    ' ----------------------------------------------------------------------
    ' BOM / UTF-16 / UTF-8

    Private Shared Function DetectByBOM(d As Byte(), fileLen As Long) As Candidate
        If d.Length >= 4 Then
            If d(0) = &HFF AndAlso d(1) = &HFE AndAlso d(2) = 0 AndAlso d(3) = 0 AndAlso fileLen Mod 4 = 0 Then
                Return New Candidate("UTF-32 LE", 12000, True, 1.0, "BOM")
            End If
            If d(0) = 0 AndAlso d(1) = 0 AndAlso d(2) = &HFE AndAlso d(3) = &HFF Then
                Return New Candidate("UTF-32 BE", 12001, True, 1.0, "BOM")
            End If
        End If
        If d.Length >= 3 AndAlso d(0) = &HEF AndAlso d(1) = &HBB AndAlso d(2) = &HBF Then
            Return New Candidate("UTF-8", 65001, True, 1.0, "BOM")
        End If
        If d.Length >= 2 Then
            If d(0) = &HFF AndAlso d(1) = &HFE Then
                Return New Candidate("UTF-16 LE", 1200, True, 1.0, "BOM")
            End If
            If d(0) = &HFE AndAlso d(1) = &HFF Then
                Return New Candidate("UTF-16 BE", 1201, True, 1.0, "BOM")
            End If
        End If
        Return Nothing
    End Function


    Private Shared Function DetectUtf16NoBom(d As Byte()) As Candidate
        Dim limit As Integer = (Math.Min(d.Length, 65536) \ 2) * 2
        Dim pairs As Integer = limit \ 2
        If pairs < 4 Then Return Nothing

        Dim nulEven As Integer = 0
        Dim nulOdd As Integer = 0
        For i As Integer = 0 To limit - 2 Step 2
            If d(i) = 0 Then nulEven += 1
            If d(i + 1) = 0 Then nulOdd += 1
        Next

        Dim le As Boolean
        If nulOdd >= 0.05 * pairs AndAlso nulEven * 10 <= nulOdd Then
            le = True
        ElseIf nulEven >= 0.05 * pairs AndAlso nulOdd * 10 <= nulEven Then
            le = False
        Else
            Return Nothing
        End If

        If Utf16Plausibility(d, limit, le) < 0.95 Then Return Nothing

        Dim conf As Double = Math.Min(0.99, 0.75 + 0.24 * Math.Min(1.0, pairs / 100.0))
        Return New Candidate(If(le, "UTF-16 LE", "UTF-16 BE"), If(le, 1200, 1201), False, conf, "UTF-16 pattern analysis (no BOM)")
    End Function

    Private Shared Function Cu16(d As Byte(), i As Integer, littleEndian As Boolean) As Integer
        If littleEndian Then
            Return CInt(d(i)) Or (CInt(d(i + 1)) << 8)
        End If
        Return (CInt(d(i)) << 8) Or CInt(d(i + 1))
    End Function

    Private Shared Function Utf16Plausibility(d As Byte(), limit As Integer, littleEndian As Boolean) As Double
        Dim good As Integer = 0
        Dim total As Integer = 0
        Dim i As Integer = 0
        While i + 1 < limit
            Dim cu As Integer = Cu16(d, i, littleEndian)
            total += 1
            If cu >= &HD800 AndAlso cu <= &HDBFF Then
                If i + 3 < limit Then
                    Dim lo As Integer = Cu16(d, i + 2, littleEndian)
                    If lo >= &HDC00 AndAlso lo <= &HDFFF Then
                        good += 1
                        i += 4
                        Continue While
                    End If
                End If
            ElseIf IsPlausibleUnit(cu) Then
                good += 1
            End If
            i += 2
        End While
        If total = 0 Then Return 0
        Return good / total
    End Function

    Private Shared Function IsPlausibleUnit(cu As Integer) As Boolean
        Return cu = 9 OrElse cu = 10 OrElse cu = 13 OrElse
               (cu >= &H20 AndAlso cu <= &H7E) OrElse
               (cu >= &HA0 AndAlso cu <= &H24F) OrElse
               (cu >= &H370 AndAlso cu <= &H52F) OrElse
               (cu >= &H590 AndAlso cu <= &H6FF) OrElse
               (cu >= &H1E00 AndAlso cu <= &H1EFF) OrElse
               (cu >= &H2000 AndAlso cu <= &H2BFF) OrElse
               (cu >= &H3000 AndAlso cu <= &H30FF) OrElse
               (cu >= &H3400 AndAlso cu <= &H4DBF) OrElse
               (cu >= &H4E00 AndAlso cu <= &H9FFF) OrElse
               (cu >= &HAC00 AndAlso cu <= &HD7A3) OrElse
               (cu >= &HF900 AndAlso cu <= &HFAFF) OrElse
               (cu >= &HFF00 AndAlso cu <= &HFFEF)
    End Function


    Private Shared Sub ScanUtf8(seg As Byte(), isFirst As Boolean, isLast As Boolean, ByRef valid As Integer, ByRef invalid As Integer)
        Dim n As Integer = seg.Length
        Dim i As Integer = 0

        If Not isFirst Then
            ' блок мог начаться посреди символа — пропускаем хвостовые байты
            While i < n AndAlso i < 3 AndAlso (seg(i) And &HC0) = &H80
                i += 1
            End While
        End If

        While i < n
            Dim b As Integer = seg(i)
            If b < &H80 Then
                i += 1
                Continue While
            End If

            Dim need As Integer
            Dim lo As Integer = &H80
            Dim hi As Integer = &HBF

            If b >= &HC2 AndAlso b <= &HDF Then
                need = 1
            ElseIf b = &HE0 Then
                need = 2
                lo = &HA0
            ElseIf b = &HED Then
                need = 2
                hi = &H9F
            ElseIf b >= &HE1 AndAlso b <= &HEF Then
                need = 2
            ElseIf b = &HF0 Then
                need = 3
                lo = &H90
            ElseIf b >= &HF1 AndAlso b <= &HF3 Then
                need = 3
            ElseIf b = &HF4 Then
                need = 3
                hi = &H8F
            Else
                invalid += 1
                i += 1
                Continue While
            End If

            If i + need >= n Then
                ' последовательность оборвана: на стыке блоков это нормально, в конце файла — ошибка
                If isLast Then invalid += 1
                Exit While
            End If

            Dim ok As Boolean = seg(i + 1) >= lo AndAlso seg(i + 1) <= hi
            For k As Integer = 2 To need
                If ok AndAlso (seg(i + k) And &HC0) <> &H80 Then ok = False
            Next

            If ok Then
                valid += 1
                i += need + 1
            Else
                invalid += 1
                i += 1
            End If
        End While
    End Sub

    ' ----------------------------------------------------------------------
    ' СТАТИСТИЧЕСКИЕ МОДЕЛИ (кодировки без самосинхронизации)

    Private Shared Function AnalyzeLegacy(data As Byte(), highCount As Integer) As List(Of Candidate)
        Dim names As String() = {"Windows-1251", "KOI8-R", "CP866", "Windows-1252", "GB18030", "Shift_JIS", "EUC-KR"}
        Dim pages As Integer() = {1251, 20866, 866, 1252, 54936, 932, 51949}
        Dim methods As String() = {
            "Cyrillic model: letter frequency + bigrams + case",
            "Cyrillic model: letter frequency + bigrams + case",
            "Cyrillic model: letter frequency + bigrams + case",
            "Single-byte Latin analysis (isolated high bytes)",
            "GB18030 structure + hanzi distribution",
            "Shift_JIS structure + kana distribution",
            "EUC-KR structure + hangul distribution"
        }

        Dim scores(6) As Double
        scores(0) = ScoreCyrillic(data, Code1251, Upper1251)
        scores(1) = ScoreCyrillic(data, CodeKoi, UpperKoi)
        scores(2) = ScoreCyrillic(data, Code866, Upper866)
        scores(3) = ScoreWindows1252(data)
        scores(4) = ScoreGB18030(data)
        scores(5) = ScoreShiftJIS(data)
        scores(6) = ScoreEucKr(data)

        Dim sumSq As Double = 0
        For k As Integer = 0 To 6
            sumSq += scores(k) * scores(k)
        Next

        ' чем меньше "высоких" байтов, тем меньше доказательств
        Dim sizeFactor As Double = 1.0 - Math.Exp(-highCount / 6.0)

        Dim list As New List(Of Candidate)
        For k As Integer = 0 To 6
            Dim share As Double = If(sumSq > 0, scores(k) * scores(k) / sumSq, 0.0)
            Dim conf As Double = Math.Min(0.99, share * scores(k) * sizeFactor)
            Dim c As New Candidate(names(k), pages(k), False, conf, methods(k))
            c.Score = scores(k)
            list.Add(c)
        Next

        Return list.OrderByDescending(Function(x) x.Confidence).ThenByDescending(Function(x) x.Score).ToList()
    End Function

    Private Shared Function Lin(x As Double, x0 As Double, x1 As Double, y0 As Double, y1 As Double) As Double
        Dim t As Double = (x - x0) / (x1 - x0)
        If t < 0 Then t = 0
        If t > 1 Then t = 1
        Return y0 + t * (y1 - y0)
    End Function


    Private Shared Function ScoreCyrillic(data As Byte(), code As Integer(), upper As Boolean()) As Double
        Dim letters As Integer = 0
        Dim wordRuns As Integer = 0
        Dim pairs As Integer = 0
        Dim hits As Integer = 0
        Dim caseViol As Integer = 0
        Dim highTokens As Integer = 0
        Dim garbage As Integer = 0
        Dim rare As Integer = 0
        Dim benign As Integer = 0
        Dim sumLog As Double = 0
        Dim prev As Integer = -1
        Dim prevLower As Boolean = False

        For i As Integer = 0 To data.Length - 1
            Dim b As Integer = data(i)
            If b < &H80 Then
                prev = -1
                prevLower = False
                Continue For
            End If

            highTokens += 1
            Dim c As Integer = code(b)
            If c >= 0 Then
                letters += 1
                sumLog += LnRuFreq(c)
                Dim isUp As Boolean = upper(b)
                If prev < 0 Then
                    wordRuns += 1
                Else
                    pairs += 1
                    If Bigram(prev, c) Then hits += 1
                End If
                If prevLower AndAlso isUp Then caseViol += 1
                prev = c
                prevLower = Not isUp
            Else
                prev = -1
                prevLower = False
                Select Case c
                    Case CODE_BENIGN
                        benign += 1
                    Case CODE_RARE
                        rare += 1
                    Case Else
                        garbage += 1
                End Select
            End If
        Next

        If letters = 0 Then Return 0.02

        Dim f1 As Double = (sumLog - 3.9 * 4) / (letters + 4)        ' средний ln-вероятности буквы (со сглаживанием)
        Dim f3 As Double = caseViol / letters                        ' "аБвГд" — нарушения регистра
        Dim f4 As Double = (garbage + 0.5 * rare) / highTokens       ' мусорные символы
        Dim fb As Double = benign / highTokens                       ' типографские символы
        Dim wordLen As Double = letters / wordRuns                   ' средняя длина кириллических слов

        ' Биграммы: вес доказательства зависит от числа пар
        Dim f2raw As Double = If(pairs > 0, hits / pairs, 0.19)
        Dim wB As Double = pairs / (pairs + 10.0)
        Dim sBigram As Double = 1.0 - wB * (1.0 - Lin(f2raw, 0.12, 0.26, 0.03, 1.0))

        Dim s As Double = sBigram
        s *= Lin(f1, -4.4, -3.4, 0.03, 1.0)
        s *= Math.Exp(-30 * f3)
        s *= Math.Exp(-6 * f4)
        s *= Math.Exp(-3 * Math.Max(0, fb - 0.15))
        s *= Lin(wordLen, 1.3, 2.4, 0.05, 1.0)
        Return s
    End Function


    Private Shared Function ScoreWindows1252(data As Byte()) As Double
        Dim high As Integer = 0
        Dim runs As Integer = 0
        Dim unlikely As Integer = 0
        Dim undef As Integer = 0
        Dim prevHigh As Boolean = False

        For i As Integer = 0 To data.Length - 1
            Dim b As Integer = data(i)
            If b >= &H80 Then
                high += 1
                If Not prevHigh Then runs += 1
                prevHigh = True
                If W1252Flag(b) = 2 Then
                    undef += 1
                ElseIf W1252Flag(b) = 1 Then
                    unlikely += 1
                End If
            Else
                prevHigh = False
            End If
        Next

        If high = 0 Then Return 0

        Dim runLen As Double = high / runs
        Dim s As Double = Lin(runLen, 3.2, 1.6, 0.02, 1.0)
        s *= Math.Exp(-8.0 * unlikely / high)
        s *= Math.Exp(-15.0 * undef / high)
        Return s
    End Function


    Private Shared Function ScoreGB18030(data As Byte()) As Double
        Dim n As Integer = data.Length
        Dim valid2 As Integer = 0
        Dim valid4 As Integer = 0
        Dim invalid As Integer = 0
        Dim hanzi As Integer = 0
        Dim hiLead As Integer = 0
        Dim lvl2 As Integer = 0
        Dim i As Integer = 0

        While i < n
            Dim b1 As Integer = data(i)
            If b1 < &H80 Then
                i += 1
            ElseIf b1 = &H80 OrElse b1 = &HFF Then
                invalid += 1
                i += 1
            ElseIf i + 1 >= n Then
                Exit While
            Else
                Dim b2 As Integer = data(i + 1)
                If b2 >= &H30 AndAlso b2 <= &H39 Then
                    If i + 3 < n AndAlso data(i + 2) >= &H81 AndAlso data(i + 2) <= &HFE AndAlso
                       data(i + 3) >= &H30 AndAlso data(i + 3) <= &H39 Then
                        valid4 += 1
                        i += 4
                    Else
                        invalid += 1
                        i += 1
                    End If
                ElseIf b2 >= &H40 AndAlso b2 <= &HFE AndAlso b2 <> &H7F Then
                    valid2 += 1
                    If b1 >= &HB0 AndAlso b1 <= &HF7 AndAlso b2 >= &HA1 Then
                        hanzi += 1
                        If b1 >= &HC9 Then hiLead += 1
                        If b1 >= &HD8 Then lvl2 += 1
                    End If
                    i += 2
                Else
                    invalid += 1
                    i += 1
                End If
            End If
        End While

        Dim tokens As Integer = valid2 + valid4 + invalid
        If tokens = 0 OrElse valid2 + valid4 = 0 Then Return 0

        Dim sInv As Double = Math.Exp(-25.0 * invalid / tokens)
        If valid2 = 0 Then Return 0.3 * sInv

        Dim hanziRate As Double = hanzi / valid2
        Dim sHanzi As Double = Lin(hanziRate, 0.25, 0.6, 0.03, 1.0)

        ' В китайском ~40% иероглифов имеют старший байт C9..F7, в корейском (хангыль B0..C8) почти нет
        Dim g As Double = (hiLead + 0.12 * 6) / (hanzi + 6)
        Dim sHigh As Double = Lin(g, 0.03, 0.18, 0.08, 1.0)

        ' Иероглифы 2-го уровня (D8..F7) в китайском редки (~5%), а в строчной кириллице 1251 (E0..FF) их большинство
        Dim l2 As Double = (lvl2 + 0.1) / (hanzi + 2)
        Dim sL2 As Double = Lin(l2, 0.15, 0.45, 1.0, 0.05)

        Return sInv * sHanzi * sHigh * sL2
    End Function


    Private Shared Function ScoreShiftJIS(data As Byte()) As Double
        Dim n As Integer = data.Length
        Dim hira As Integer = 0
        Dim kata As Integer = 0
        Dim punct As Integer = 0
        Dim fullw As Integer = 0
        Dim kanji As Integer = 0
        Dim otherPair As Integer = 0
        Dim kata1 As Integer = 0
        Dim invalid As Integer = 0
        Dim i As Integer = 0

        While i < n
            Dim b1 As Integer = data(i)
            If b1 < &H80 Then
                i += 1
            ElseIf b1 >= &HA1 AndAlso b1 <= &HDF Then
                kata1 += 1
                i += 1
            ElseIf (b1 >= &H81 AndAlso b1 <= &H9F) OrElse (b1 >= &HE0 AndAlso b1 <= &HFC) Then
                If i + 1 >= n Then Exit While
                Dim b2 As Integer = data(i + 1)
                If b2 >= &H40 AndAlso b2 <= &HFC AndAlso b2 <> &H7F Then
                    If b1 = &H81 Then
                        punct += 1
                    ElseIf b1 = &H82 Then
                        If b2 >= &H9F AndAlso b2 <= &HF1 Then
                            hira += 1
                        Else
                            fullw += 1
                        End If
                    ElseIf b1 = &H83 AndAlso b2 >= &H40 AndAlso b2 <= &H96 Then
                        kata += 1
                    ElseIf (b1 >= &H88 AndAlso b1 <= &H9F) OrElse (b1 >= &HE0 AndAlso b1 <= &HEA) Then
                        kanji += 1
                    Else
                        otherPair += 1
                    End If
                    i += 2
                Else
                    invalid += 1
                    i += 1
                End If
            Else
                invalid += 1
                i += 1
            End If
        End While

        Dim pairs As Integer = hira + kata + punct + fullw + kanji + otherPair
        Dim tokens As Integer = pairs + kata1 + invalid
        If pairs = 0 OrElse tokens = 0 Then Return 0

        Dim sInv As Double = Math.Exp(-25.0 * invalid / tokens)
        Dim sKata As Double = Math.Exp(-8.0 * kata1 / tokens)
        Dim jEv As Double = (hira + kata + 0.5 * punct + 0.3 * fullw) / pairs
        Dim sJ As Double = Lin(jEv, 0.03, 0.3, 0.1, 1.0)
        Dim classified As Double = (pairs - otherPair) / pairs
        Dim sClass As Double = Lin(classified, 0.5, 0.85, 0.05, 1.0)

        Return sInv * sKata * sJ * sClass
    End Function


    Private Shared Function ScoreEucKr(data As Byte()) As Double
        Dim n As Integer = data.Length
        Dim hangul As Integer = 0
        Dim symbol As Integer = 0
        Dim hanja As Integer = 0
        Dim other As Integer = 0
        Dim invalid As Integer = 0
        Dim i As Integer = 0

        While i < n
            Dim b1 As Integer = data(i)
            If b1 < &H80 Then
                i += 1
            ElseIf i + 1 >= n Then
                Exit While
            Else
                Dim b2 As Integer = data(i + 1)
                If b1 >= &HA1 AndAlso b1 <= &HFE AndAlso b2 >= &HA1 AndAlso b2 <= &HFE Then
                    If b1 >= &HB0 AndAlso b1 <= &HC8 Then
                        hangul += 1
                    ElseIf b1 <= &HAC Then
                        symbol += 1
                    ElseIf b1 >= &HCA Then
                        hanja += 1
                    Else
                        other += 1
                    End If
                    i += 2
                Else
                    invalid += 1
                    i += 1
                End If
            End If
        End While

        Dim pairs As Integer = hangul + symbol + hanja + other
        If pairs = 0 Then Return 0

        Dim sInv As Double = Math.Exp(-25.0 * invalid / (pairs + invalid))
        Dim hRate As Double = (hangul + 0.5 * symbol) / pairs
        Dim sHan As Double = Lin(hRate, 0.3, 0.7, 0.03, 1.0)
        Dim outRate As Double = hanja / pairs
        Dim sOut As Double = Math.Exp(-10.0 * Math.Max(0, outRate - 0.04))

        Return sInv * sHan * sOut
    End Function

    ' ----------------------------------------------------------------------
    ' ФОЛБЭК И ФОРМИРОВАНИЕ ТЕКСТА

    Private Shared Function GetAnsiFallback() As Candidate
        Dim cp As Integer = 1252
        Try
            Dim v As Integer = CultureInfo.CurrentCulture.TextInfo.ANSICodePage
            If v > 0 AndAlso v <> 65001 Then cp = v
        Catch
        End Try

        Dim name As String
        Select Case cp
            Case 932
                name = "Shift_JIS"
            Case 936
                name = "GBK"
            Case 949
                name = "CP949"
            Case 950
                name = "Big5"
            Case 1250 To 1258, 874
                name = "Windows-" & cp.ToString(CultureInfo.InvariantCulture)
            Case Else
                name = "CP" & cp.ToString(CultureInfo.InvariantCulture)
        End Select

        Return New Candidate(name, cp, False, 0.1, "Fallback (system ANSI code page)")
    End Function

    Private Shared Function BuildAllConfidencesText(results As List(Of Candidate)) As String
        If results Is Nothing OrElse results.Count = 0 Then
            Return "Нет данных о кодировках"
        End If

        ' Дедупликация по имени (без учёта регистра) с максимальной достоверностью
        Dim unique As New Dictionary(Of String, Candidate)(StringComparer.OrdinalIgnoreCase)
        For Each r As Candidate In results
            If r Is Nothing OrElse String.IsNullOrEmpty(r.Name) Then Continue For

            Dim existing As Candidate = Nothing
            If unique.TryGetValue(r.Name, existing) Then
                If r.Confidence > existing.Confidence Then unique(r.Name) = r
            Else
                unique.Add(r.Name, r)
            End If
        Next

        If unique.Count = 0 Then
            Return "Нет данных о кодировках"
        End If

        Dim sorted As List(Of Candidate) = unique.Values.OrderByDescending(Function(x) x.Confidence).ToList()
        Dim bomResults As New List(Of Candidate)
        Dim nonBomResults As New List(Of Candidate)
        For Each r As Candidate In sorted
            If r.BOM Then
                bomResults.Add(r)
            Else
                nonBomResults.Add(r)
            End If
        Next

        Dim sb As New StringBuilder(sorted.Count * 64)

        If bomResults.Count > 0 Then
            sb.AppendLine("Кодировки с BOM:")
            For Each r As Candidate In bomResults
                Dim pct As String = r.Confidence.ToString("P2")
                sb.AppendLine(r.Name & " (CP " & r.CodePage & "): " & pct)
            Next
            sb.AppendLine()
        End If

        If nonBomResults.Count > 0 Then
            If bomResults.Count > 0 Then
                sb.AppendLine("Кодировки без BOM:")
            End If
            For Each r As Candidate In nonBomResults
                Dim cpInfo As String = If(r.CodePage > 0, " (CP " & r.CodePage & ")", "")
                Dim pct As String = r.Confidence.ToString("P2")
                sb.AppendLine(r.Name & cpInfo & ": " & pct)
            Next
        End If

        Return sb.ToString()
    End Function

End Class