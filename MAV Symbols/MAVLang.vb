' MAV Symbols - Текстовый редактор
' Copyright © 2026 Andrey Morozov (MAV-128)
' Лицензия: GNU GPL v3.0


Imports System.Collections.Generic

Public Class MAVLang
    ' Текущий язык
    Public Shared CurrentLanguage As String = "ru"

    ' Словари для хранения переводов
    Private Shared RU_Dict As New Dictionary(Of String, String)
    Private Shared EN_Dict As New Dictionary(Of String, String)
    Private Shared CurrentDict As Dictionary(Of String, String)

    Public Shared Sub Init()
        ' Повторный вызов безопасен (раньше второй вызов падал на дублирующихся ключах)
        RU_Dict.Clear()
        EN_Dict.Clear()

        ' === РУССКИЙ ЯЗЫК ===

        ' Главное меню - Файл
        RU_Dict.Add("MFile", "&Файл")
        RU_Dict.Add("MNew", "Создать")
        RU_Dict.Add("MNewWindow", "Новое окно")
        RU_Dict.Add("MOpen", "Открыть...")
        RU_Dict.Add("MOpenAS", "Открыть в кодировке...")
        RU_Dict.Add("MSave", "Сохранить")
        RU_Dict.Add("MSaveAs", "Сохранить как...")
        RU_Dict.Add("MSaveCODE", "Сохранить в кодировке...")
        RU_Dict.Add("MParamS", "Параметры страницы...")
        RU_Dict.Add("MPrint", "Печать...")
        RU_Dict.Add("MExit", "Выход")

        ' Главное меню - Правка
        RU_Dict.Add("MEdit", "&Правка")
        RU_Dict.Add("MUndo", "Отменить")
        RU_Dict.Add("MRedo", "Повторить")
        RU_Dict.Add("MCut", "Вырезать")
        RU_Dict.Add("MCopy", "Копировать")
        RU_Dict.Add("MPaste", "Вставить")
        RU_Dict.Add("MDelete", "Удалить")
        RU_Dict.Add("MCLClip", "Очистить буфер обмена")
        RU_Dict.Add("MSelall", "Выделить все")
        RU_Dict.Add("MDateTime", "Время и дата")
        RU_Dict.Add("MFind", "Поиск и замена")
        RU_Dict.Add("MPer", "Перейти...")
        RU_Dict.Add("MYandex", "Найти в Яндекс...")
        RU_Dict.Add("MGoogle", "Найти в Google...")

        ' Главное меню - Формат
        RU_Dict.Add("MFormat", "Фор&мат")
        RU_Dict.Add("MFont", "Шрифт")
        RU_Dict.Add("MPerenos", "Перенос по словам")
        RU_Dict.Add("MVURL", "Выделять URL")

        ' Главное меню - Вид
        RU_Dict.Add("MView", "&Вид")
        RU_Dict.Add("MStatusBar", "Строка состояния")
        RU_Dict.Add("MDarkT", "Темная тема")
        RU_Dict.Add("MLang", "&Язык")
        RU_Dict.Add("MLangRU", "Русский")
        RU_Dict.Add("MLangEN", "English")
        RU_Dict.Add("MMashtab", "Масштаб")
        RU_Dict.Add("MMUv", "Увеличить")
        RU_Dict.Add("MMUm", "Уменьшить")
        RU_Dict.Add("MMPoUm", "Восстановить масштаб по умолчанию")

        ' Главное меню - Справка
        RU_Dict.Add("MHelp", "&Справка")
        RU_Dict.Add("MInfo", "О программе")

        ' Статусбар (только те элементы, которые реально переводятся)
        RU_Dict.Add("slChang", "Измен.")
        RU_Dict.Add("slLines", "Строк")
        RU_Dict.Add("slRC", "Стр.Стлб")
        RU_Dict.Add("slINS", "INS")
        RU_Dict.Add("slNL", "Перенос")
        ' slEnc и slBOM НЕ переводим - это технические значения (UTF-8, BOM и т.д.)
        RU_Dict.Add("slScale", "масш.")
        RU_Dict.Add("MMUv2", "Увеличить")
        RU_Dict.Add("MMUm2", "Уменьшить")
        RU_Dict.Add("MMPoUm2", "Восстановить масштаб по умолчанию")

        ' Контекстное меню
        RU_Dict.Add("CMUndo", "Отменить")
        RU_Dict.Add("CMCut", "Вырезать")
        RU_Dict.Add("CMCopy", "Копировать")
        RU_Dict.Add("CMPaste", "Вставить")
        RU_Dict.Add("CMDel", "Удалить")
        RU_Dict.Add("CMBig", "ПРОПИСНЫЕ")
        RU_Dict.Add("CMSml", "строчные")
        RU_Dict.Add("CMSelall", "Выделить все")
        RU_Dict.Add("CMYandex", "Найти в Яндекс...")
        RU_Dict.Add("CMGoogle", "Найти в Google...")

        ' Панель поиска
        RU_Dict.Add("TBFindLabel", "Поиск:")
        RU_Dict.Add("TBButF", "Найти далее")
        RU_Dict.Add("TBButReg", "С учетом регистра")
        RU_Dict.Add("TBReplaceLabel", "Замена:")
        RU_Dict.Add("TBButZam", "Заменить")
        RU_Dict.Add("TBButZamV", "Заменить все")
        RU_Dict.Add("TBButClose", "Закрыть")

        ' Динамические тексты статусбара
        RU_Dict.Add("slRC_Prefix", "стр: ")
        RU_Dict.Add("slRC_Suffix", ", стлб: ")
        RU_Dict.Add("slChang_Modified", "изменён")
        RU_Dict.Add("slLines_Prefix", "строк: ")
        RU_Dict.Add("slINS_Ins", "Ins")
        RU_Dict.Add("slINS_Ovr", "Ovr")
        RU_Dict.Add("Scale_Percent", "%")

        ' Типы переноса строки (отображаются в статусбаре)
        RU_Dict.Add("NewLine_CRLF", "Windows (CRLF)")
        RU_Dict.Add("NewLine_LF", "Unix (LF)")
        RU_Dict.Add("NewLine_CR", "Mac OS (CR)")

        ' Динамические тексты диалогов и сообщений
        RU_Dict.Add("NewDocument", "Новый.txt")
        RU_Dict.Add("Msg_ErrorOpening", "Ошибка при открытии файла! ")
        RU_Dict.Add("Msg_SaveChanges", "Сохранить изменения в документе ")
        RU_Dict.Add("Msg_SaveInEncoding", "Сохранить документ в кодировке ")
        RU_Dict.Add("Msg_ErrorSaving", "Ошибка при сохранении файла! ")
        RU_Dict.Add("Filter_TextFiles", "Текстовые файлы")
        RU_Dict.Add("Filter_AllFiles", "Все файлы")
        RU_Dict.Add("Dialog_OpenAuto", "Открыть документ (Автоопределение кодировки)")
        RU_Dict.Add("Dialog_OpenManual", "Открыть документ (Ручной выбор кодировки)")
        RU_Dict.Add("Input_LineNumber", "Номер строки")
        RU_Dict.Add("Input_GoToLine", "Переход на строку")
        RU_Dict.Add("Msg_LineExceeds", "Номер строки превышает общее количество строк")
        RU_Dict.Add("Msg_OnlyPositive", "Вводить можно только числа больше нуля")
        RU_Dict.Add("Msg_OnlyNumbers", "Вводить можно только числа")
        RU_Dict.Add("Msg_LinkWarning", "Внимание! Вы хотите открыть ссылку не содержащую 'http://' или 'https://'. Вы уверены?")
        RU_Dict.Add("Document_Prefix", "Документ: ")
        RU_Dict.Add("Address_Prefix", "Адрес: ")
        RU_Dict.Add("Msg_LossyEncoding", "В тексте есть символы, которых нет в выбранной кодировке. Они будут заменены на «?» без возможности восстановления. Сохранить?")
        RU_Dict.Add("Msg_SaveDirectWarning", "Не удаётся создать временный файл в папке документа, поэтому безопасная замена файла невозможна. Файл можно записать только напрямую: копия оригинала будет сохранена во временной папке и восстановлена при ошибке записи, но при внезапном сбое (отключение питания, закрытие процесса) документ может оказаться повреждён. Продолжить?")
        RU_Dict.Add("Msg_SaveRestoreFailed", "Не удалось записать файл и автоматически восстановить его прежнее содержимое. Копия исходного файла сохранена: {0}")
        RU_Dict.Add("Msg_LoneSurrogates", "В тексте есть некорректные символы Unicode (непарные суррогаты), которые нельзя записать в файл. Они будут заменены, и восстановить их будет нельзя. Сохранить?")
        RU_Dict.Add("Msg_ReplaceNoMemory", "Недостаточно памяти для замены во всём документе. Документ не изменён.")
        RU_Dict.Add("Msg_ReplaceNoMemoryApply", "Недостаточно памяти для применения замены. Проверьте содержимое документа; при необходимости отмените замену (Ctrl+Z).")
        RU_Dict.Add("Msg_DecodeErrors", "Файл содержит байты, недопустимые в выбранной кодировке. Они будут показаны как «" & ChrW(&HFFFD) & "» и безвозвратно потеряются при сохранении. Лучше открыть файл через «Открыть в кодировке…». Открыть всё равно?")
        RU_Dict.Add("Msg_NulChars", "Файл содержит нулевые символы (U+0000) - вероятно, это не текстовый файл. Редактор может потерять их при сохранении. Открыть всё равно?")
        RU_Dict.Add("Msg_LossyNoOverwrite", "Чтобы не повредить исходный файл, сохранить этот документ можно будет только через «Сохранить как».")
        RU_Dict.Add("Print_Caption", "Печать")
        RU_Dict.Add("Print_DefaultDocTitle", "Документ")
        RU_Dict.Add("Print_NoPrinters", "В системе не установлено ни одного принтера.")
        RU_Dict.Add("Print_EmptyDoc", "Нечего печатать: документ пуст.")
        RU_Dict.Add("Print_RangeOutOfBounds", "Указанный диапазон страниц выходит за пределы документа.")
        RU_Dict.Add("Print_TotalPages", "Всего страниц: ")
        RU_Dict.Add("Print_Failed", "Не удалось выполнить печать.")
        RU_Dict.Add("Print_RangeInvalid", "Неверный диапазон страниц.")
        RU_Dict.Add("Print_RangeFromOne", "Нумерация страниц начинается с 1 — диапазон может начинаться только с 1.")
        RU_Dict.Add("Print_RangeToLess", "Номер последней страницы не может быть меньше номера первой.")
        RU_Dict.Add("Msg_BigFile", "Файл очень большой ({0} МБ). Открытие может занять много времени и памяти. Продолжить?")
        RU_Dict.Add("Msg_FileNotFound", "файл не найден.")
        RU_Dict.Add("Msg_NotFound", "Не удалось найти: ")
        RU_Dict.Add("Msg_Replaced", "Выполнено замен: {0}")
        RU_Dict.Add("Msg_UnexpectedError", "Произошла непредвиденная ошибка: {0}")
        RU_Dict.Add("Msg_RecoverySaved", "Несохранённый текст записан в папку восстановления. Рекомендуется сохранить документ под новым именем и перезапустить программу.")
        RU_Dict.Add("Msg_RecoveryFound", "Найден несохранённый документ предыдущего сеанса ({0}). Восстановить его?")
        RU_Dict.Add("NewLine_MixedSuffix", " (смешанные)")

        ' === Форма FDataTime (Время и дата) ===
        RU_Dict.Add("FDataTime_Title", "Время и дата")
        RU_Dict.Add("FDataTime_Label1", "Формат:")
        RU_Dict.Add("FDataTime_BOk", "&ОК")
        RU_Dict.Add("FDataTime_BCancel", "Отмена")
        RU_Dict.Add("DateTime_YearSuffix", " г.")

        ' === Форма FFont (Шрифт) ===
        RU_Dict.Add("FFont_Title", "Шрифт")
        RU_Dict.Add("FFont_GBSample", "Образец")
        RU_Dict.Add("FFont_LFontName", "Шрифт:")
        RU_Dict.Add("FFont_LItBl", "Начертание:")
        RU_Dict.Add("FFont_CBBold", "Полужирный")
        RU_Dict.Add("FFont_CBItalic", "Наклонный")
        RU_Dict.Add("FFont_LSize", "Размер:")
        RU_Dict.Add("FFont_BOk", "Применить")
        RU_Dict.Add("FFont_BCancel", "Отмена")

        ' === Форма FOpenCODE (Открыть в кодировке) ===
        RU_Dict.Add("FOpenCODE_Title", "Кодировка")
        RU_Dict.Add("FOpenCODE_BOk", "&ОК")
        RU_Dict.Add("FOpenCODE_BCancel", "Отмена")
        RU_Dict.Add("FOpenCODE_Label1", "Кодировка:")
        RU_Dict.Add("FOpenCODE_Label2", "Пример (первые 300 строк файла):")
        RU_Dict.Add("FOpenCODE_LDoc", "Документ: ")
        RU_Dict.Add("FOpenCODE_LAd", "Адрес: ")
        RU_Dict.Add("FOpenCODE_PreviewError", "Не удалось прочитать файл: ")

        ' === Форма FSaveCODE (Сохранить в кодировке) ===
        RU_Dict.Add("FSaveCODE_Title", "Кодировка")
        RU_Dict.Add("FSaveCODE_Label1", "Кодировка:")
        RU_Dict.Add("FSaveCODE_BOk", "&ОК")
        RU_Dict.Add("FSaveCODE_BCancel", "Отмена")
        RU_Dict.Add("FSaveCODE_LDoc", "Документ: ")
        RU_Dict.Add("FSaveCODE_GroupBox1", "ВНИМАНИЕ!")
        RU_Dict.Add("FSaveCODE_LInfo", "Символы, отсутствующие в выбранной кодировке, будут заменены на ""?"" при сохранении - это необратимо. Проверьте пример текста перед сохранением!")
        RU_Dict.Add("FSaveCODE_MLch", "Перенос по словам")

        ' === Форма FAbout (О программе) ===
        RU_Dict.Add("FAbout_Title", "О программе")
        RU_Dict.Add("FAbout_VersionPrefix", "Версия ")
        RU_Dict.Add("FAbout_DefaultDescription", "Текстовый редактор")
        RU_Dict.Add("FAbout_DefaultCopyright", "© 2026 Андрей Морозов")
        RU_Dict.Add("FAbout_LLabelAuthor", "Разработчик:")
        RU_Dict.Add("FAbout_LLabelLicense", "Лицензия:")
        RU_Dict.Add("FAbout_LLabelSource", "Исходный код:")
        RU_Dict.Add("FAbout_LLabelTech", "Среда:")
        RU_Dict.Add("FAbout_LLicenseLink", "Открыть текст лицензии")
        RU_Dict.Add("FAbout_LGitHubLink", "GitHub репозиторий проекта")
        RU_Dict.Add("FAbout_BOk", "&ОК")
        RU_Dict.Add("FAbout_ErrorTitle", "Ошибка")
        RU_Dict.Add("FAbout_ErrorOpenUrl", "Не удалось открыть ссылку.")

        ' Инициализируем английский словарь
        InitEnglish()

        ' Устанавливаем английский язык по умолчанию
        SetLanguage("en")
    End Sub

    Public Shared Sub SetLanguage(lang As String)
        'нормализуем и проверяем значение (защита от мусора в файле настроек)
        If String.IsNullOrEmpty(lang) Then lang = "ru"
        lang = lang.Trim.ToLowerInvariant()
        If lang <> "ru" AndAlso lang <> "en" Then lang = "ru"

        CurrentLanguage = lang
        If lang = "en" Then
            CurrentDict = EN_Dict
        Else
            CurrentDict = RU_Dict
        End If
    End Sub

    Public Shared Function GetText(key As String) As String
        If CurrentDict Is Nothing Then Return key
        If CurrentDict.ContainsKey(key) Then
            Return CurrentDict(key)
        End If
        Return key ' Если перевод не найден, возвращаем ключ
    End Function

    Public Shared Function HasTranslation(key As String) As Boolean
        If CurrentDict Is Nothing Then Return False
        Return CurrentDict.ContainsKey(key)
    End Function

    ' Отдельная инициализация словаря английского языка
    Private Shared Sub InitEnglish()
        ' Главное меню - Файл
        EN_Dict.Add("MFile", "&File")
        EN_Dict.Add("MNew", "New")
        EN_Dict.Add("MNewWindow", "New Window")
        EN_Dict.Add("MOpen", "Open...")
        EN_Dict.Add("MOpenAS", "Open with encoding...")
        EN_Dict.Add("MSave", "Save")
        EN_Dict.Add("MSaveAs", "Save As...")
        EN_Dict.Add("MSaveCODE", "Save with encoding...")
        EN_Dict.Add("MParamS", "Page Setup...")
        EN_Dict.Add("MPrint", "Print...")
        EN_Dict.Add("MExit", "Exit")

        ' Главное меню - Правка
        EN_Dict.Add("MEdit", "&Edit")
        EN_Dict.Add("MUndo", "Undo")
        EN_Dict.Add("MRedo", "Redo")
        EN_Dict.Add("MCut", "Cut")
        EN_Dict.Add("MCopy", "Copy")
        EN_Dict.Add("MPaste", "Paste")
        EN_Dict.Add("MDelete", "Delete")
        EN_Dict.Add("MCLClip", "Clear Clipboard")
        EN_Dict.Add("MSelall", "Select All")
        EN_Dict.Add("MDateTime", "Date and Time")
        EN_Dict.Add("MFind", "Find and Replace")
        EN_Dict.Add("MPer", "Go To...")
        EN_Dict.Add("MYandex", "Search in Yandex...")
        EN_Dict.Add("MGoogle", "Search in Google...")

        ' Главное меню - Формат
        EN_Dict.Add("MFormat", "F&ormat")
        EN_Dict.Add("MFont", "Font")
        EN_Dict.Add("MPerenos", "Word Wrap")
        EN_Dict.Add("MVURL", "Highlight URLs")

        ' Главное меню - Вид
        EN_Dict.Add("MView", "&View")
        EN_Dict.Add("MStatusBar", "Status Bar")
        EN_Dict.Add("MDarkT", "Dark Theme")
        EN_Dict.Add("MLang", "&Language")
        EN_Dict.Add("MLangRU", "Русский")
        EN_Dict.Add("MLangEN", "English")
        EN_Dict.Add("MMashtab", "Zoom")
        EN_Dict.Add("MMUv", "Zoom In")
        EN_Dict.Add("MMUm", "Zoom Out")
        EN_Dict.Add("MMPoUm", "Reset Zoom")

        ' Главное меню - Справка
        EN_Dict.Add("MHelp", "&Help")
        EN_Dict.Add("MInfo", "About")

        ' Статусбар
        EN_Dict.Add("slChang", "Mod.")
        EN_Dict.Add("slLines", "Lines")
        EN_Dict.Add("slRC", "Ln.Col")
        EN_Dict.Add("slINS", "INS")
        EN_Dict.Add("slNL", "Wrap")
        ' slEnc и slBOM НЕ переводим - это технические значения
        EN_Dict.Add("slScale", "zoom")
        EN_Dict.Add("MMUv2", "Zoom In")
        EN_Dict.Add("MMUm2", "Zoom Out")
        EN_Dict.Add("MMPoUm2", "Reset Zoom")

        ' Контекстное меню
        EN_Dict.Add("CMUndo", "Undo")
        EN_Dict.Add("CMCut", "Cut")
        EN_Dict.Add("CMCopy", "Copy")
        EN_Dict.Add("CMPaste", "Paste")
        EN_Dict.Add("CMDel", "Delete")
        EN_Dict.Add("CMBig", "UPPERCASE")
        EN_Dict.Add("CMSml", "lowercase")
        EN_Dict.Add("CMSelall", "Select All")
        EN_Dict.Add("CMYandex", "Search in Yandex...")
        EN_Dict.Add("CMGoogle", "Search in Google...")

        ' Панель поиска
        EN_Dict.Add("TBFindLabel", "Find:")
        EN_Dict.Add("TBButF", "Find Next")
        EN_Dict.Add("TBButReg", "Match Case")
        EN_Dict.Add("TBReplaceLabel", "Replace:")
        EN_Dict.Add("TBButZam", "Replace")
        EN_Dict.Add("TBButZamV", "Replace All")
        EN_Dict.Add("TBButClose", "Close")

        ' Динамические тексты статусбара
        EN_Dict.Add("slRC_Prefix", "Ln: ")
        EN_Dict.Add("slRC_Suffix", ", Col: ")
        EN_Dict.Add("slChang_Modified", "modified")
        EN_Dict.Add("slLines_Prefix", "Lines: ")
        EN_Dict.Add("slINS_Ins", "Ins")
        EN_Dict.Add("slINS_Ovr", "Ovr")
        EN_Dict.Add("Scale_Percent", "%")

        ' Типы переноса строки
        EN_Dict.Add("NewLine_CRLF", "Windows (CRLF)")
        EN_Dict.Add("NewLine_LF", "Unix (LF)")
        EN_Dict.Add("NewLine_CR", "Mac OS (CR)")

        ' Динамические тексты диалогов и сообщений
        EN_Dict.Add("NewDocument", "New.txt")
        EN_Dict.Add("Msg_ErrorOpening", "Error opening file! ")
        EN_Dict.Add("Msg_SaveChanges", "Save changes to document ")
        EN_Dict.Add("Msg_SaveInEncoding", "Save document in encoding ")
        EN_Dict.Add("Msg_ErrorSaving", "Error saving file! ")
        EN_Dict.Add("Filter_TextFiles", "Text files")
        EN_Dict.Add("Filter_AllFiles", "All files")
        EN_Dict.Add("Dialog_OpenAuto", "Open document (Auto-detect encoding)")
        EN_Dict.Add("Dialog_OpenManual", "Open document (Manual encoding selection)")
        EN_Dict.Add("Input_LineNumber", "Line number")
        EN_Dict.Add("Input_GoToLine", "Go to line")
        EN_Dict.Add("Msg_LineExceeds", "Line number exceeds total number of lines")
        EN_Dict.Add("Msg_OnlyPositive", "Only numbers greater than zero are allowed")
        EN_Dict.Add("Msg_OnlyNumbers", "Only numbers are allowed")
        EN_Dict.Add("Msg_LinkWarning", "Warning! You are about to open a link that does not contain 'http://' or 'https://'. Are you sure?")
        EN_Dict.Add("Document_Prefix", "Document: ")
        EN_Dict.Add("Address_Prefix", "Path: ")
        EN_Dict.Add("Msg_LossyEncoding", "The text contains characters not available in the selected encoding. They will be replaced with ""?"" irreversibly. Save anyway?")
        EN_Dict.Add("Msg_SaveDirectWarning", "A temporary file cannot be created in the document's folder, so the file cannot be replaced safely. It can only be written directly: a copy of the original will be kept in the temporary folder and restored if writing fails, but a sudden failure (power loss, process termination) may leave the document damaged. Continue?")
        EN_Dict.Add("Msg_SaveRestoreFailed", "The file could not be written and its previous contents could not be restored automatically. A copy of the original file was kept: {0}")
        EN_Dict.Add("Msg_LoneSurrogates", "The text contains invalid Unicode characters (unpaired surrogates) that cannot be written to a file. They will be replaced and cannot be recovered. Save anyway?")
        EN_Dict.Add("Msg_ReplaceNoMemory", "Not enough memory to replace in the whole document. The document has not been changed.")
        EN_Dict.Add("Msg_ReplaceNoMemoryApply", "Not enough memory to apply the replacement. Check the document contents and undo the replacement (Ctrl+Z) if necessary.")
        EN_Dict.Add("Msg_DecodeErrors", "The file contains bytes that are not valid in the selected encoding. They will show as """ & ChrW(&HFFFD) & """ and be permanently lost on save. Consider ""Open with encoding…"". Open anyway?")
        EN_Dict.Add("Msg_NulChars", "The file contains null characters (U+0000) - it is probably not a text file. The editor may lose them on save. Open anyway?")
        EN_Dict.Add("Msg_LossyNoOverwrite", "To protect the original file, this document can only be saved using ""Save As"".")
        EN_Dict.Add("Print_Caption", "Print")
        EN_Dict.Add("Print_DefaultDocTitle", "Document")
        EN_Dict.Add("Print_NoPrinters", "No printers are installed on this system.")
        EN_Dict.Add("Print_EmptyDoc", "Nothing to print: the document is empty.")
        EN_Dict.Add("Print_RangeOutOfBounds", "The specified page range is outside the document.")
        EN_Dict.Add("Print_TotalPages", "Total pages: ")
        EN_Dict.Add("Print_Failed", "Printing failed.")
        EN_Dict.Add("Print_RangeInvalid", "Invalid page range.")
        EN_Dict.Add("Print_RangeFromOne", "Page numbering starts at 1 — the range can only start from 1.")
        EN_Dict.Add("Print_RangeToLess", "The last page number cannot be less than the first page number.")
        EN_Dict.Add("Msg_BigFile", "The file is very large ({0} MB). Opening it may take a long time and a lot of memory. Continue?")
        EN_Dict.Add("Msg_FileNotFound", "file not found.")
        EN_Dict.Add("Msg_NotFound", "Cannot find: ")
        EN_Dict.Add("Msg_Replaced", "Replacements made: {0}")
        EN_Dict.Add("Msg_UnexpectedError", "An unexpected error occurred: {0}")
        EN_Dict.Add("Msg_RecoverySaved", "Unsaved text has been written to the recovery folder. Please save the document under a new name and restart the program.")
        EN_Dict.Add("Msg_RecoveryFound", "An unsaved document from the previous session was found ({0}). Restore it?")
        EN_Dict.Add("NewLine_MixedSuffix", " (mixed)")

        ' === Форма FDataTime (Время и дата) ===
        EN_Dict.Add("FDataTime_Title", "Date and Time")
        EN_Dict.Add("FDataTime_Label1", "Format:")
        EN_Dict.Add("FDataTime_BOk", "&OK")
        EN_Dict.Add("FDataTime_BCancel", "Cancel")
        EN_Dict.Add("DateTime_YearSuffix", "")

        ' === Форма FFont (Шрифт) ===
        EN_Dict.Add("FFont_Title", "Font")
        EN_Dict.Add("FFont_GBSample", "Sample")
        EN_Dict.Add("FFont_LFontName", "Font:")
        EN_Dict.Add("FFont_LItBl", "Style:")
        EN_Dict.Add("FFont_CBBold", "Bold")
        EN_Dict.Add("FFont_CBItalic", "Italic")
        EN_Dict.Add("FFont_LSize", "Size:")
        EN_Dict.Add("FFont_BOk", "Apply")
        EN_Dict.Add("FFont_BCancel", "Cancel")

        ' === Форма FOpenCODE (Открыть в кодировке) ===
        EN_Dict.Add("FOpenCODE_Title", "Encoding")
        EN_Dict.Add("FOpenCODE_BOk", "&OK")
        EN_Dict.Add("FOpenCODE_BCancel", "Cancel")
        EN_Dict.Add("FOpenCODE_Label1", "Encoding:")
        EN_Dict.Add("FOpenCODE_Label2", "Preview (first 300 lines of file):")
        EN_Dict.Add("FOpenCODE_LDoc", "Document: ")
        EN_Dict.Add("FOpenCODE_LAd", "Path: ")
        EN_Dict.Add("FOpenCODE_PreviewError", "Failed to read file: ")

        ' === Форма FSaveCODE (Сохранить в кодировке) ===
        EN_Dict.Add("FSaveCODE_Title", "Encoding")
        EN_Dict.Add("FSaveCODE_Label1", "Encoding:")
        EN_Dict.Add("FSaveCODE_BOk", "&OK")
        EN_Dict.Add("FSaveCODE_BCancel", "Cancel")
        EN_Dict.Add("FSaveCODE_LDoc", "Document: ")
        EN_Dict.Add("FSaveCODE_GroupBox1", "WARNING!")
        EN_Dict.Add("FSaveCODE_LInfo", "Characters not present in the selected encoding will be replaced with ""?"" when saving - this is irreversible. Check the text preview before saving!")
        EN_Dict.Add("FSaveCODE_MLch", "Word Wrap")

        ' === Форма FAbout (О программе) ===
        EN_Dict.Add("FAbout_Title", "About")
        EN_Dict.Add("FAbout_VersionPrefix", "Version ")
        EN_Dict.Add("FAbout_DefaultDescription", "Text editor")
        EN_Dict.Add("FAbout_DefaultCopyright", "© 2026 Andrey Morozov")
        EN_Dict.Add("FAbout_LLabelAuthor", "Developer:")
        EN_Dict.Add("FAbout_LLabelLicense", "License:")
        EN_Dict.Add("FAbout_LLabelSource", "Source code:")
        EN_Dict.Add("FAbout_LLabelTech", "Environment:")
        EN_Dict.Add("FAbout_LLicenseLink", "Open license text")
        EN_Dict.Add("FAbout_LGitHubLink", "Project GitHub repository")
        EN_Dict.Add("FAbout_BOk", "&OK")
        EN_Dict.Add("FAbout_ErrorTitle", "Error")
        EN_Dict.Add("FAbout_ErrorOpenUrl", "Failed to open link.")
    End Sub
End Class