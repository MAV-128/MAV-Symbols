' MAV Symbols - Текстовый редактор
' Copyright © 2026 Andrey Morozov (MAV-128)
' Лицензия: GNU GPL v3.0


Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Module MAVDarkT
    Private Const DWMWA_USE_IMMERSIVE_DARK_MODE As Integer = 20
    Private ReadOnly DarkBack As Color = Color.FromArgb(45, 45, 45)
    Private ReadOnly DarkMain As Color = Color.Black
    Private ReadOnly DarkText As Color = Color.White
    Private ReadOnly DarkBorder As Color = Color.FromArgb(70, 70, 70)
    Private ReadOnly DarkControl As Color = Color.FromArgb(60, 60, 60)
    Private ReadOnly DarkInput As Color = Color.FromArgb(30, 30, 30)
    Private ReadOnly DarkInputText As Color = Color.FromArgb(220, 220, 220)
    Private ReadOnly DarkSelected As Color = Color.FromArgb(70, 70, 70)
    ' Цвета ссылок (LinkLabel) в тёмной теме - подобраны для читаемости на сером фоне
    Private ReadOnly DarkLink As Color = Color.FromArgb(86, 156, 214)       ' #569CD6
    Private ReadOnly DarkLinkActive As Color = Color.FromArgb(156, 220, 254) ' #9CDCFE
    Private ReadOnly DarkLinkVisited As Color = Color.FromArgb(197, 134, 192) ' #C586C0

    ' ==================== СОСТОЯНИЕ ТЕМЫ ====================
    Private _isDark As Boolean

    Public ReadOnly Property IsDark As Boolean
        Get
            Return _isDark
        End Get
    End Property

    ' Отметка "форма уже оформлена такой-то темой"
    Private NotInheritable Class ThemeMark
        Public IsDark As Boolean
    End Class

    ' Исходные свойства контрола (чтобы светлая тема возвращала именно их)
    Private NotInheritable Class ControlState
        Public BackColor As Color
        Public ForeColor As Color
        Public FlatStyle As FlatStyle
        Public UseVisualStyleBackColor As Boolean
        Public BorderStyle As BorderStyle
        Public DrawMode As DrawMode
        Public ItemHeight As Integer
        ' Исходные цвета ссылок (используются только для LinkLabel)
        Public LinkColor As Color
        Public ActiveLinkColor As Color
        Public VisitedLinkColor As Color
    End Class

    Private ReadOnly FormMarks As New ConditionalWeakTable(Of Form, ThemeMark)
    Private ReadOnly ControlStates As New ConditionalWeakTable(Of Control, ControlState)



    ' Делегаты для корректного RemoveHandler
    Private ReadOnly ListBoxPaintHandler As New PaintEventHandler(AddressOf ListBoxDarkBorder_Paint)
    Private ReadOnly ListBoxDrawItemHandler As New DrawItemEventHandler(AddressOf ListBoxDark_DrawItem)
    Private ReadOnly GroupBoxPaintHandler As New PaintEventHandler(AddressOf GroupBoxDark_Paint)
    Private ReadOnly ComboBoxDrawItemHandler As New DrawItemEventHandler(AddressOf ComboBoxDark_DrawItem)
    Private ReadOnly ComboBoxDropDownHandler As New EventHandler(AddressOf ComboBoxDropDown)
    Private ReadOnly NativeThemeHandler As New EventHandler(AddressOf NativeThemeHandleCreated)
    Private ReadOnly TextColorHandler As New EventHandler(AddressOf TextBoxColors_Changed)
    Private ReadOnly FormClosedHandler As New FormClosedEventHandler(AddressOf OnFormClosed)

    ' ==================== ПЕРЕХВАТ ПОКАЗА ФОРМ (CBT-hook) ====================
    Private Const WH_CBT As Integer = 5
    Private Const HCBT_ACTIVATE As Integer = 5
    Private Const WM_SETREDRAW As Integer = &HB

    Private Delegate Function CbtHookProc(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
    Private CbtProc As CbtHookProc          ' держим ссылку, иначе GC удалит делегат
    Private CbtHook As IntPtr = IntPtr.Zero
    Private InCbt As Boolean

    <DllImport("user32.dll", SetLastError:=True)>
    Private Function SetWindowsHookEx(idHook As Integer, lpfn As CbtHookProc, hMod As IntPtr, dwThreadId As UInteger) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Function CallNextHookEx(hhk As IntPtr, nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
    End Function

    <DllImport("kernel32.dll")>
    Private Function GetCurrentThreadId() As UInteger
    End Function

    ' NOSIZE | NOMOVE | NOZORDER | NOACTIVATE | FRAMECHANGED
    Private Const SWP_FRAME As UInteger = &H37UI

    <DllImport("user32.dll")>
    Private Function SetWindowPos(hWnd As IntPtr, hWndInsertAfter As IntPtr,
                                  x As Integer, y As Integer, cx As Integer, cy As Integer,
                                  uFlags As UInteger) As Boolean
    End Function

    <DllImport("user32.dll", EntryPoint:="SendMessageW")>
    Private Function SendMessageRaw(hWnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
    End Function



    Private ReadOnly ListBoxFontHandler As New EventHandler(AddressOf ListBoxFontChanged)

    Private Sub UpdateListBoxItemHeight(lb As ListBox)
        ' При OwnerDrawFixed Windows не пересчитывает ItemHeight при смене DPI/шрифта.
        If lb.DrawMode = DrawMode.OwnerDrawFixed Then
            Dim h As Integer = lb.Font.Height
            If h <= 0 Then h = CInt(Math.Ceiling(lb.Font.Size * 1.5))
            lb.ItemHeight = h + 2   ' небольшой отступ для читаемости
        End If
    End Sub

    Private Sub ListBoxFontChanged(sender As Object, e As EventArgs)
        Dim lb = TryCast(sender, ListBox)
        If lb Is Nothing Then Return
        UpdateListBoxItemHeight(lb)
    End Sub



    ' ==================== ПУБЛИЧНЫЙ API ====================

    ' Включает/выключает тёмную тему во всём приложении.
    ' Устанавливает состояние темы без перерисовки форм.
    ' Используется при начальной загрузке настроек, когда формы ещё не показаны.
    Public Sub SetDarkValue(isDark As Boolean)
        _isDark = isDark
        InstallHook()
    End Sub

    ' Включает/выключает тёмную тему во всём приложении (с перерисовкой всех открытых форм).
    Public Sub SetDark(isDark As Boolean)
        SetDarkValue(isDark)
        Dim forms As New List(Of Form)
        For Each f As Form In Application.OpenForms
            forms.Add(f)
        Next
        Dim activeForm As Form = Form.ActiveForm
        For Each f As Form In forms
            If f.IsDisposed Then Continue For
            ' Заголовок окна перекрашивается системой только при показе окна, поэтому
            ' видимую немодальную форму быстро скрываем и показываем снова.
            ' (Модальный диалог скрывать нельзя - ShowDialog завершится.)
            Dim reshow As Boolean = f.Visible AndAlso Not f.Modal
            If reshow Then f.Visible = False
            Try
                ApplyDarkToForm(f, isDark)
            Finally
                If reshow Then f.Visible = True
            End Try
        Next
        If activeForm IsNot Nothing AndAlso Not activeForm.IsDisposed AndAlso activeForm.Visible Then
            activeForm.Activate()
        End If
    End Sub

    ' Применить текущую тему к форме вручную (нужно только если вы добавили контролы после показа формы).
    Public Sub ApplyToForm(form As Form)
        ApplyDarkToForm(form, _isDark)
    End Sub

    ' Цвета пункта списка для собственной отрисовки (ComboBox/ListBox) в текущей теме.
    Public Sub GetItemColors(state As DrawItemState, ByRef backColor As Color, ByRef textColor As Color)
        Dim isSel As Boolean = (state And DrawItemState.Selected) = DrawItemState.Selected
        If _isDark Then
            If isSel Then
                backColor = DarkSelected
                textColor = Color.White
            Else
                backColor = DarkInput
                textColor = DarkInputText
            End If
        Else
            If isSel Then
                backColor = SystemColors.Highlight
                textColor = SystemColors.HighlightText
            Else
                backColor = SystemColors.Window
                textColor = SystemColors.WindowText
            End If
        End If
    End Sub

    Private Sub InstallHook()
        If CbtHook <> IntPtr.Zero Then Return
        Try
            CbtProc = New CbtHookProc(AddressOf CbtCallback)
            CbtHook = SetWindowsHookEx(WH_CBT, CbtProc, IntPtr.Zero, GetCurrentThreadId())
        Catch
        End Try
    End Sub

    ' Вызывается системой перед активацией любого окна нашего потока
    Private Function CbtCallback(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
        If nCode = HCBT_ACTIVATE AndAlso Not InCbt Then
            InCbt = True
            Try
                Dim f As Form = TryCast(Control.FromHandle(wParam), Form)
                If f IsNot Nothing AndAlso Not f.IsDisposed Then
                    Dim mark As ThemeMark = Nothing
                    If Not FormMarks.TryGetValue(f, mark) OrElse mark.IsDark <> _isDark Then
                        ApplyDarkToForm(f, _isDark)
                    End If
                End If
            Catch
            Finally
                InCbt = False
            End Try
        End If
        Return CallNextHookEx(CbtHook, nCode, wParam, lParam)
    End Function

    ' ==================== WinAPI для ComboBox ====================
    <StructLayout(LayoutKind.Sequential)>
    Private Structure CB_RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure COMBOBOXINFO
        Public cbSize As Integer
        Public rcItem As CB_RECT
        Public rcButton As CB_RECT
        Public stateButton As Integer
        Public hwndCombo As IntPtr
        Public hwndEdit As IntPtr
        Public hwndList As IntPtr
    End Structure

    Private Const CB_GETCOMBOBOXINFO As Integer = &H164

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Function SendMessage(hWnd As IntPtr, msg As Integer,
                                wParam As IntPtr,
                                ByRef lParam As COMBOBOXINFO) As IntPtr
    End Function
    ' ============================================================

    <DllImport("dwmapi.dll", PreserveSig:=True)>
    Private Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Integer, attrSize As Integer) As Integer
    End Function

    <DllImport("uxtheme.dll", CharSet:=CharSet.Unicode)>
    Friend Function SetWindowTheme(hwnd As IntPtr,
                                   pszSubAppName As String,
                                   pszSubIdList As String) As Integer
    End Function

    Public Sub ApplyMAVDarkT(hwnd As IntPtr, DM As Integer)
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, DM, Marshal.SizeOf(DM))
    End Sub

    ' ==================== ПРИМЕНЕНИЕ ТЕМЫ ====================

    ' Оставлено для совместимости: оформляет форму явно заданной темой.
    Public Sub ApplyDarkToForm(form As Form, isDark As Boolean)
        If form Is Nothing OrElse form.IsDisposed Then Return

        ' Гарантируем, что состояния ВСЕХ контролов сохранены ДО начала перекраски.
        ' Это критично для корректного возврата к светлой теме.
        If isDark Then
            SaveAllControlsState(form)
        End If

        Dim hwnd As IntPtr = form.Handle
        SendMessageRaw(hwnd, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero)   ' без мерцания
        Try
            ApplyDarkToControl(form, isDark)
            ApplyFormContextMenus(form, isDark)
            ApplyMAVDarkT(hwnd, If(isDark, 1, 0))
        Finally
            SendMessageRaw(hwnd, WM_SETREDRAW, New IntPtr(1), IntPtr.Zero)
            form.Invalidate(True)
        End Try
        ' Заголовок окна (DWM) сам не перерисовывается при смене режима - заставляем.
        ' Размер и положение окна не меняются.
        If form.Visible Then
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_FRAME)
            RedrawWindow(hwnd, IntPtr.Zero, IntPtr.Zero, RDW_FRAME_REDRAW)
        End If
        Dim mark As ThemeMark = Nothing
        If FormMarks.TryGetValue(form, mark) Then
            mark.IsDark = isDark
        Else
            FormMarks.Add(form, New ThemeMark With {.IsDark = isDark})
            RemoveHandler form.FormClosed, FormClosedHandler
            AddHandler form.FormClosed, FormClosedHandler
        End If
    End Sub

    ' Рекурсивно сохраняет состояние всех контролов формы (вызывается ОДИН РАЗ при первом переходе в тёмную тему)
    Private Sub SaveAllControlsState(ctrl As Control)
        SaveState(ctrl)
        For Each child As Control In ctrl.Controls
            SaveAllControlsState(child)
        Next
    End Sub

    ' Вызывается при закрытии формы - удаляет её из списка "уже оформленных".
    ' Нужно для корректной работы default instance в VB.NET: при повторном ShowDialog
    ' хук должен снова применить тему, иначе заголовок окна останется светлым.
    Private Sub OnFormClosed(sender As Object, e As FormClosedEventArgs)
        Dim f As Form = TryCast(sender, Form)
        If f Is Nothing Then Return
        FormMarks.Remove(f)
    End Sub

    Private Sub ApplyDarkToControl(ctrl As Control, isDark As Boolean)
        ' MenuStrip / ToolStrip / StatusStrip: своя логика, внутрь не заходим
        If TypeOf ctrl Is ToolStrip Then
            ApplyToolStripControl(DirectCast(ctrl, ToolStrip), isDark)
            ApplyOwnContextMenu(ctrl, isDark)
            Return
        End If

        ApplyColorsToControl(ctrl, isDark)
        ApplyNativeTheme(ctrl, isDark)
        ApplyOwnContextMenu(ctrl, isDark)

        For Each child As Control In ctrl.Controls
            ApplyDarkToControl(child, isDark)
        Next
    End Sub

    Private Sub ApplyToolStripControl(ts As ToolStrip, isDark As Boolean)
        If TypeOf ts Is StatusStrip Then
            ApplyStatusStrip(DirectCast(ts, StatusStrip), isDark)
            If isDark Then
                ts.Renderer = New DarkRender()
            Else
                ts.Renderer = New WRender()
            End If
        ElseIf TypeOf ts Is MenuStrip Then
            ApplyToolStrip(ts, isDark, True)
            If isDark Then
                ts.Renderer = New DarkRender()
            Else
                ts.Renderer = New WRender()
            End If
        Else
            ApplyToolStrip(ts, isDark, False)
            If isDark Then
                ts.Renderer = New DarkRender()
            Else
                ts.Renderer = New ToolStripSystemRenderer()
            End If
        End If
    End Sub

    ' ContextMenuStrip, привязанный к контролу
    Private Sub ApplyOwnContextMenu(ctrl As Control, isDark As Boolean)
        Dim cms As ContextMenuStrip = ctrl.ContextMenuStrip
        If cms IsNot Nothing Then ApplyToolStrip(cms, isDark, True)
    End Sub

    ' ContextMenuStrip, объявленные в форме, но ни к чему не привязанные (используются из кода)
    Private Sub ApplyFormContextMenus(form As Form, isDark As Boolean)
        Try
            For Each fi As FieldInfo In form.GetType().GetFields(BindingFlags.Instance Or BindingFlags.Public Or BindingFlags.NonPublic)
                If GetType(ContextMenuStrip).IsAssignableFrom(fi.FieldType) Then
                    Dim cms As ContextMenuStrip = TryCast(fi.GetValue(form), ContextMenuStrip)
                    If cms IsNot Nothing Then ApplyToolStrip(cms, isDark, True)
                End If
            Next
        Catch
        End Try
    End Sub

    ' ==================== НАТИВНАЯ ТЕМА (скроллбары и т.п.) ====================
    Private Sub SetThemeSafe(hwnd As IntPtr, themeName As String)
        Try
            SetWindowTheme(hwnd, themeName, Nothing)
        Catch
        End Try
    End Sub

    Private Sub ApplyNativeTheme(ctrl As Control, isDark As Boolean)
        Dim themeName As String = If(isDark, "DarkMode_Explorer", "Explorer")

        If TypeOf ctrl Is TextBoxBase Then
            SetThemeSafe(ctrl.Handle, themeName)
            ' при пересоздании handle (WordWrap и т.п.) тема скроллбаров слетает - переприменяем
            Dim tb As TextBoxBase = DirectCast(ctrl, TextBoxBase)
            RemoveHandler tb.HandleCreated, NativeThemeHandler
            RemoveHandler tb.TextChanged, TextColorHandler
            RemoveHandler tb.ReadOnlyChanged, TextColorHandler
            If isDark Then
                AddHandler tb.HandleCreated, NativeThemeHandler
                ' некоторые состояния (ReadOnly, смена текста) сбрасывают цвета - возвращаем
                AddHandler tb.TextChanged, TextColorHandler
                AddHandler tb.ReadOnlyChanged, TextColorHandler
            End If

        ElseIf TypeOf ctrl Is ListBox Then
            SetThemeSafe(ctrl.Handle, themeName)
            RemoveHandler ctrl.HandleCreated, NativeThemeHandler
            If isDark Then AddHandler ctrl.HandleCreated, NativeThemeHandler

        ElseIf TypeOf ctrl Is ComboBox Then
            Dim cb As ComboBox = DirectCast(ctrl, ComboBox)
            SetThemeSafe(cb.Handle, themeName)
            ApplyComboBoxPartsTheme(cb, isDark)
            SetComboBorder(cb, isDark)

            RemoveHandler cb.HandleCreated, NativeThemeHandler
            RemoveHandler cb.DropDown, ComboBoxDropDownHandler
            If isDark Then
                AddHandler cb.HandleCreated, NativeThemeHandler
                AddHandler cb.DropDown, ComboBoxDropDownHandler
            End If

        ElseIf TypeOf ctrl Is CheckBox Then
            SetThemeSafe(ctrl.Handle, themeName)
        End If
    End Sub

    Private Sub NativeThemeHandleCreated(sender As Object, e As EventArgs)
        If Not _isDark Then Return
        Dim c As Control = TryCast(sender, Control)
        If c Is Nothing Then Return
        SetThemeSafe(c.Handle, "DarkMode_Explorer")
        Dim cb As ComboBox = TryCast(c, ComboBox)
        If cb IsNot Nothing Then ApplyComboBoxPartsTheme(cb, True)
    End Sub

    Private Sub TextBoxColors_Changed(sender As Object, e As EventArgs)
        If Not _isDark Then Return
        Dim tb As TextBoxBase = TryCast(sender, TextBoxBase)
        If tb Is Nothing Then Return
        If tb.BackColor <> DarkInput Then tb.BackColor = DarkInput
        If tb.ForeColor <> DarkInputText Then tb.ForeColor = DarkInputText
    End Sub

    ' Применяем тёмную тему к вложенным окнам ComboBox (edit и list)
    Private Sub ApplyComboBoxPartsTheme(cb As ComboBox, isDark As Boolean)
        If Not cb.IsHandleCreated Then Return

        Dim info As New COMBOBOXINFO()
        info.cbSize = Marshal.SizeOf(info)
        SendMessage(cb.Handle, CB_GETCOMBOBOXINFO, IntPtr.Zero, info)

        Dim theme As String = If(isDark, "DarkMode_Explorer", "Explorer")

        If info.hwndEdit <> IntPtr.Zero Then
            Try
                SetWindowTheme(info.hwndEdit, theme, Nothing)
            Catch
            End Try
        End If

        If info.hwndList <> IntPtr.Zero Then
            Try
                SetWindowTheme(info.hwndList, theme, Nothing)
            Catch
            End Try
        End If
    End Sub

    ' Обработчик открытия выпадающего списка - применяем тёмную тему к списку
    Private Sub ComboBoxDropDown(sender As Object, e As EventArgs)
        Dim cb As ComboBox = DirectCast(sender, ComboBox)
        If Not cb.IsHandleCreated Then Return

        Dim info As New COMBOBOXINFO()
        info.cbSize = Marshal.SizeOf(info)
        SendMessage(cb.Handle, CB_GETCOMBOBOXINFO, IntPtr.Zero, info)

        If info.hwndList <> IntPtr.Zero Then
            SetThemeSafe(info.hwndList, If(_isDark, "DarkMode_Explorer", "Explorer"))
        End If
    End Sub

    ' ==================== ЦВЕТА И СВОЙСТВА КОНТРОЛОВ ====================

    ' Запоминаем исходные свойства до первого перекрашивания
    Private Sub SaveState(ctrl As Control)
        Dim st As ControlState = Nothing
        If ControlStates.TryGetValue(ctrl, st) Then Return
        st = New ControlState With {.BackColor = ctrl.BackColor, .ForeColor = ctrl.ForeColor}
        Dim btn As Button = TryCast(ctrl, Button)
        If btn IsNot Nothing Then
            st.FlatStyle = btn.FlatStyle
            st.UseVisualStyleBackColor = btn.UseVisualStyleBackColor
        End If
        Dim chk As CheckBox = TryCast(ctrl, CheckBox)
        If chk IsNot Nothing Then st.FlatStyle = chk.FlatStyle
        Dim lb As ListBox = TryCast(ctrl, ListBox)
        If lb IsNot Nothing Then
            st.BorderStyle = lb.BorderStyle
            st.DrawMode = lb.DrawMode
            st.ItemHeight = lb.ItemHeight
        End If
        ' Сохраняем исходные цвета ссылок (только для LinkLabel)
        Dim ll As LinkLabel = TryCast(ctrl, LinkLabel)
        If ll IsNot Nothing Then
            st.LinkColor = ll.LinkColor
            st.ActiveLinkColor = ll.ActiveLinkColor
            st.VisitedLinkColor = ll.VisitedLinkColor
        End If
        ControlStates.Add(ctrl, st)
    End Sub

    ' Возвращаем исходные свойства (светлая тема = как задумано в дизайнере)
    Private Sub RestoreState(ctrl As Control)
        Dim st As ControlState = Nothing
        If Not ControlStates.TryGetValue(ctrl, st) Then Return
        Dim lb As ListBox = TryCast(ctrl, ListBox)
        If lb IsNot Nothing Then
            lb.DrawMode = st.DrawMode
            lb.BorderStyle = st.BorderStyle
            If st.ItemHeight > 0 Then lb.ItemHeight = st.ItemHeight
        End If
        ctrl.BackColor = st.BackColor
        ctrl.ForeColor = st.ForeColor
        Dim btn As Button = TryCast(ctrl, Button)
        If btn IsNot Nothing Then
            btn.FlatStyle = st.FlatStyle
            btn.UseVisualStyleBackColor = st.UseVisualStyleBackColor
        End If
        Dim chk As CheckBox = TryCast(ctrl, CheckBox)
        If chk IsNot Nothing Then chk.FlatStyle = st.FlatStyle
        ' Возвращаем исходные цвета ссылок (если они были сохранены)
        Dim ll As LinkLabel = TryCast(ctrl, LinkLabel)
        If ll IsNot Nothing Then
            If Not st.LinkColor.IsEmpty Then ll.LinkColor = st.LinkColor
            If Not st.ActiveLinkColor.IsEmpty Then ll.ActiveLinkColor = st.ActiveLinkColor
            If Not st.VisitedLinkColor.IsEmpty Then ll.VisitedLinkColor = st.VisitedLinkColor
        End If
    End Sub

    ' Форма сама рисует пункты этого контрола? (метод <Имя>_DrawItem в классе формы)
    Private Function HasOwnDrawHandler(ctrl As Control) As Boolean
        Try
            If String.IsNullOrEmpty(ctrl.Name) Then Return False
            Dim f As Form = ctrl.FindForm()
            If f Is Nothing Then Return False
            Dim m As MethodInfo = f.GetType().GetMethod(ctrl.Name & "_DrawItem",
                BindingFlags.Instance Or BindingFlags.Public Or BindingFlags.NonPublic)
            Return m IsNot Nothing
        Catch
            Return False
        End Try
    End Function

    ' OwnerDraw ComboBox без собственной отрисовки в форме рисуем сами (в обеих темах)
    Private Sub ApplyComboBoxDraw(cb As ComboBox)
        RemoveHandler cb.DrawItem, ComboBoxDrawItemHandler
        If cb.DrawMode = DrawMode.OwnerDrawFixed AndAlso Not HasOwnDrawHandler(cb) Then
            AddHandler cb.DrawItem, ComboBoxDrawItemHandler
        End If
    End Sub

    Private Sub ApplyColorsToControl(ctrl As Control, isDark As Boolean)
        If isDark Then
            ' === ТЕМНАЯ ТЕМА ===


            If TypeOf ctrl Is TextBoxBase Then
                ctrl.BackColor = DarkInput
                ctrl.ForeColor = DarkInputText

            ElseIf TypeOf ctrl Is ListBox Then
                Dim lb As ListBox = CType(ctrl, ListBox)
                lb.BackColor = DarkInput
                lb.ForeColor = DarkInputText
                lb.BorderStyle = BorderStyle.FixedSingle

                RemoveHandler lb.Paint, ListBoxPaintHandler
                AddHandler lb.Paint, ListBoxPaintHandler

                RemoveHandler lb.DrawItem, ListBoxDrawItemHandler
                RemoveHandler lb.FontChanged, ListBoxFontHandler
                RemoveHandler lb.HandleCreated, ListBoxFontHandler

                If Not HasOwnDrawHandler(lb) Then
                    ' OwnerDraw для кастомного выделения серым
                    lb.DrawMode = DrawMode.OwnerDrawFixed
                    AddHandler lb.DrawItem, ListBoxDrawItemHandler
                    AddHandler lb.FontChanged, ListBoxFontHandler
                    AddHandler lb.HandleCreated, ListBoxFontHandler
                    UpdateListBoxItemHeight(lb)
                End If

            ElseIf TypeOf ctrl Is ComboBox Then
                Dim cb As ComboBox = CType(ctrl, ComboBox)
                cb.BackColor = DarkInput
                cb.ForeColor = DarkInputText
                ApplyComboBoxDraw(cb)

            ElseIf TypeOf ctrl Is Button Then
                Dim btn As Button = CType(ctrl, Button)
                btn.FlatStyle = FlatStyle.Flat
                btn.BackColor = DarkControl
                btn.ForeColor = Color.White
                btn.FlatAppearance.BorderColor = DarkBorder
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 80)

            ElseIf TypeOf ctrl Is CheckBox Then
                Dim chk As CheckBox = CType(ctrl, CheckBox)
                chk.BackColor = DarkBack
                chk.ForeColor = DarkText
                chk.FlatStyle = FlatStyle.Flat

            ElseIf TypeOf ctrl Is GroupBox Then
                Dim gb As GroupBox = CType(ctrl, GroupBox)
                gb.BackColor = DarkBack
                gb.ForeColor = DarkText
                RemoveHandler gb.Paint, GroupBoxPaintHandler
                AddHandler gb.Paint, GroupBoxPaintHandler

            ElseIf TypeOf ctrl Is LinkLabel Then
                ' LinkLabel наследуется от Label — ветка должна быть ПЕРЕД Label
                Dim ll As LinkLabel = CType(ctrl, LinkLabel)
                SaveState(ll)
                ll.BackColor = Color.Transparent
                ll.ForeColor = DarkText
                ll.LinkColor = DarkLink
                ll.ActiveLinkColor = DarkLinkActive
                ll.VisitedLinkColor = DarkLinkVisited
            ElseIf TypeOf ctrl Is Label Then
                ctrl.BackColor = Color.Transparent
                ctrl.ForeColor = DarkText
            Else
                ctrl.BackColor = DarkBack
                ctrl.ForeColor = DarkText
            End If

        Else
            ' === СВЕТЛАЯ ТЕМА: снимаем наши обработчики и возвращаем исходные свойства ===
            If TypeOf ctrl Is ListBox Then
                Dim lb As ListBox = CType(ctrl, ListBox)
                RemoveHandler lb.Paint, ListBoxPaintHandler
                RemoveHandler lb.DrawItem, ListBoxDrawItemHandler
                RemoveHandler lb.FontChanged, ListBoxFontHandler
                RemoveHandler lb.HandleCreated, ListBoxFontHandler
            ElseIf TypeOf ctrl Is GroupBox Then
                RemoveHandler ctrl.Paint, GroupBoxPaintHandler
            End If

            RestoreState(ctrl)

            If TypeOf ctrl Is ComboBox Then ApplyComboBoxDraw(CType(ctrl, ComboBox))
            ctrl.Invalidate()
        End If
    End Sub

    ' ==================== РАМКА ComboBox (только дорисовка, без смены стилей) ====================
    Private ReadOnly ComboBorderColor As Color = Color.FromArgb(90, 90, 90)
    Private ReadOnly ComboBorders As New Dictionary(Of ComboBox, List(Of ComboChildBorder))
    Private ComboHandleCreatedHandler As EventHandler
    Private ComboHandleDestroyedHandler As EventHandler

    Private Const WM_NCPAINT As Integer = &H85
    Private Const WM_PAINT As Integer = &HF
    ' толщина нативной рамки вокруг поля ввода (2 = как у списка; если линия съехала на 1 px, поставьте 1)
    Private Const EditFrameThickness As Integer = 2
    ' RDW_INVALIDATE | RDW_FRAME | RDW_UPDATENOW
    Private Const RDW_FRAME_REDRAW As UInteger = &H501UI

    <StructLayout(LayoutKind.Sequential)>
    Private Structure CB_POINT
        Public X As Integer
        Public Y As Integer
    End Structure

    <DllImport("user32.dll")>
    Private Function GetWindowRect(hWnd As IntPtr, ByRef rc As CB_RECT) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Function ClientToScreen(hWnd As IntPtr, ByRef pt As CB_POINT) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Function ScreenToClient(hWnd As IntPtr, ByRef pt As CB_POINT) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Function GetDC(hWnd As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Function GetWindowDC(hWnd As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Function ReleaseDC(hWnd As IntPtr, hDC As IntPtr) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Function RedrawWindow(hWnd As IntPtr, lprcUpdate As IntPtr, hrgnUpdate As IntPtr, flags As UInteger) As Boolean
    End Function

    ' Подкласс вложенных окон ComboBox (edit и list): после стандартной
    ' отрисовки рамки перекрашивает её. Размеры и стили окон не меняются.
    Private NotInheritable Class ComboChildBorder
        Inherits NativeWindow

        Private ReadOnly _hwnd As IntPtr
        Private ReadOnly _editHwnd As IntPtr   ' заполнен только для самого окна ComboBox

        Public Sub New(hwnd As IntPtr, Optional editHwnd As IntPtr = Nothing)
            _hwnd = hwnd
            _editHwnd = editHwnd
            AssignHandle(hwnd)
            RedrawWindow(hwnd, IntPtr.Zero, IntPtr.Zero, RDW_FRAME_REDRAW)
        End Sub

        Public Sub Restore()
            If Handle <> IntPtr.Zero Then ReleaseHandle()
            RedrawWindow(_hwnd, IntPtr.Zero, IntPtr.Zero, RDW_FRAME_REDRAW)
        End Sub

        Protected Overrides Sub WndProc(ByRef m As Message)
            MyBase.WndProc(m)
            If m.Msg = WM_NCPAINT Then
                DrawBorder()
            ElseIf m.Msg = WM_PAINT AndAlso _editHwnd <> IntPtr.Zero Then
                DrawEditFrame()
            End If
        End Sub

        ' Рамка вокруг поля ввода рисуется самим ComboBox в его клиентской области
        Private Sub DrawEditFrame()
            Dim er As CB_RECT
            If Not GetWindowRect(_editHwnd, er) Then Return

            ' если у самого поля есть своя рамка, её уже перекрасил его подкласс
            Dim eo As New CB_POINT()
            ClientToScreen(_editHwnd, eo)
            If eo.X - er.Left > 0 Then Return

            Dim tl As New CB_POINT With {.X = er.Left, .Y = er.Top}
            Dim brp As New CB_POINT With {.X = er.Right, .Y = er.Bottom}
            ScreenToClient(Handle, tl)
            ScreenToClient(Handle, brp)
            Dim x As Integer = tl.X
            Dim y As Integer = tl.Y
            Dim w As Integer = brp.X - tl.X
            Dim h As Integer = brp.Y - tl.Y
            If w <= 0 OrElse h <= 0 Then Return

            Dim dc As IntPtr = GetDC(Handle)
            If dc = IntPtr.Zero Then Return
            Try
                Using g As Graphics = Graphics.FromHdc(dc)
                    ' внутренние пиксели рамки -> цвет поля
                    Using pen As New Pen(DarkInput, 1)
                        For i As Integer = 1 To EditFrameThickness - 1
                            g.DrawRectangle(pen, x - i, y - i, w + 2 * i - 1, h + 2 * i - 1)
                        Next
                    End Using
                    ' внешний пиксель -> серый
                    Using pen As New Pen(ComboBorderColor, 1)
                        Dim f As Integer = EditFrameThickness
                        g.DrawRectangle(pen, x - f, y - f, w + 2 * f - 1, h + 2 * f - 1)
                    End Using
                End Using
            Finally
                ReleaseDC(Handle, dc)
            End Try
        End Sub

        Private Sub DrawBorder()
            Dim rc As CB_RECT
            If Not GetWindowRect(Handle, rc) Then Return
            Dim w As Integer = rc.Right - rc.Left
            Dim h As Integer = rc.Bottom - rc.Top

            ' реальная толщина нативной рамки
            Dim org As New CB_POINT()
            ClientToScreen(Handle, org)
            Dim t As Integer = org.X - rc.Left
            If t < 1 OrElse w < 2 * t + 1 OrElse h < 2 * t + 1 Then Return

            Dim dc As IntPtr = GetWindowDC(Handle)
            If dc = IntPtr.Zero Then Return
            Try
                Using g As Graphics = Graphics.FromHdc(dc)
                    ' внутренние пиксели нативной рамки -> цвет поля
                    Using br As New SolidBrush(DarkInput)
                        g.FillRectangle(br, 0, 0, w, t)
                        g.FillRectangle(br, 0, h - t, w, t)
                        g.FillRectangle(br, 0, 0, t, h)
                        g.FillRectangle(br, w - t, 0, t, h)
                    End Using
                    ' внешний пиксель -> серый
                    Using pen As New Pen(ComboBorderColor, 1)
                        g.DrawRectangle(pen, 0, 0, w - 1, h - 1)
                    End Using
                End Using
            Finally
                ReleaseDC(Handle, dc)
            End Try
        End Sub
    End Class

    Private Sub SetComboBorder(cb As ComboBox, isDark As Boolean)
        If ComboHandleCreatedHandler Is Nothing Then
            ComboHandleCreatedHandler = AddressOf ComboHandleCreated
            ComboHandleDestroyedHandler = AddressOf ComboHandleDestroyed
        End If

        RemoveHandler cb.HandleCreated, ComboHandleCreatedHandler
        RemoveHandler cb.HandleDestroyed, ComboHandleDestroyedHandler

        If isDark Then
            AddHandler cb.HandleCreated, ComboHandleCreatedHandler
            AddHandler cb.HandleDestroyed, ComboHandleDestroyedHandler
            If cb.IsHandleCreated Then AttachComboBorder(cb)
        Else
            DetachComboBorder(cb)
        End If
    End Sub

    Private Sub ComboHandleCreated(sender As Object, e As EventArgs)
        AttachComboBorder(DirectCast(sender, ComboBox))
    End Sub

    Private Sub ComboHandleDestroyed(sender As Object, e As EventArgs)
        ' окна уже уничтожены, восстанавливать нечего
        ComboBorders.Remove(DirectCast(sender, ComboBox))
    End Sub

    Private Sub AttachComboBorder(cb As ComboBox)
        DetachComboBorder(cb)

        Dim info As New COMBOBOXINFO()
        info.cbSize = Marshal.SizeOf(info)
        SendMessage(cb.Handle, CB_GETCOMBOBOXINFO, IntPtr.Zero, info)

        Dim items As New List(Of ComboChildBorder)
        If info.hwndEdit <> IntPtr.Zero Then items.Add(New ComboChildBorder(info.hwndEdit))
        items.Add(New ComboChildBorder(cb.Handle, info.hwndEdit))
        If info.hwndList <> IntPtr.Zero AndAlso info.hwndList <> info.hwndEdit Then
            items.Add(New ComboChildBorder(info.hwndList))
        End If
        ComboBorders(cb) = items
    End Sub

    Private Sub DetachComboBorder(cb As ComboBox)
        Dim items As List(Of ComboChildBorder) = Nothing
        If ComboBorders.TryGetValue(cb, items) Then
            For i As Integer = items.Count - 1 To 0 Step -1
                items(i).Restore()
            Next
            ComboBorders.Remove(cb)
        End If
    End Sub
    ' ============================================================

    ' ==================== КАСТОМНАЯ ОТРИСОВКА ====================

    ' Темная рамка для ListBox (как у TextBox)
    Private Sub ListBoxDarkBorder_Paint(sender As Object, e As PaintEventArgs)
        Dim lb As ListBox = DirectCast(sender, ListBox)
        Dim g = e.Graphics
        Dim rect As New Rectangle(0, 0, lb.Width - 1, lb.Height - 1)
        Using pen As New Pen(DarkBorder, 1)
            g.DrawRectangle(pen, rect)
        End Using
    End Sub

    ' Отрисовка элементов ListBox (серое выделение)
    Private Sub ListBoxDark_DrawItem(sender As Object, e As DrawItemEventArgs)
        If e.Index < 0 Then Return
        Dim lb As ListBox = DirectCast(sender, ListBox)
        DrawListItem(e, lb.GetItemText(lb.Items(e.Index)), lb.Font)
    End Sub

    ' Универсальная отрисовка для OwnerDraw ComboBox (обе темы)
    Private Sub ComboBoxDark_DrawItem(sender As Object, e As DrawItemEventArgs)
        If e.Index < 0 Then Return
        Dim cb As ComboBox = DirectCast(sender, ComboBox)
        DrawListItem(e, cb.GetItemText(cb.Items(e.Index)), cb.Font)
    End Sub

    Private Sub DrawListItem(e As DrawItemEventArgs, itemText As String, itemFont As Font)
        Dim backColor As Color
        Dim textColor As Color
        GetItemColors(e.State, backColor, textColor)

        Using brush As New SolidBrush(backColor)
            e.Graphics.FillRectangle(brush, e.Bounds)
        End Using

        Using brush As New SolidBrush(textColor)
            Dim textBounds As Rectangle = e.Bounds
            textBounds.Inflate(-2, 0)

            Using sf As New StringFormat With {
                .LineAlignment = StringAlignment.Center,
                .Alignment = StringAlignment.Near,
                .Trimming = StringTrimming.EllipsisCharacter,
                .FormatFlags = StringFormatFlags.NoWrap
            }
                e.Graphics.DrawString(itemText, itemFont, brush, textBounds, sf)
            End Using
        End Using

        e.DrawFocusRectangle()
    End Sub

    ' Темная рамка для GroupBox
    Private Sub GroupBoxDark_Paint(sender As Object, e As PaintEventArgs)
        Dim gb As GroupBox = DirectCast(sender, GroupBox)
        Dim g = e.Graphics
        Using brush As New SolidBrush(gb.BackColor)
            g.FillRectangle(brush, 0, 0, gb.ClientSize.Width, gb.ClientSize.Height)
        End Using

        Dim W As Integer = gb.ClientSize.Width
        Dim H As Integer = gb.ClientSize.Height

        Dim textSize As SizeF = g.MeasureString(gb.Text, gb.Font)
        Dim textWidth As Integer = CInt(Math.Ceiling(textSize.Width))
        Dim textHeight As Integer = CInt(Math.Ceiling(textSize.Height))
        Dim textX As Integer = 8
        Dim textY As Integer = 2
        Dim midY As Integer = textY + textHeight \ 2

        g.SmoothingMode = Drawing2D.SmoothingMode.None
        Using pen As New Pen(DarkBorder, 1)
            If textX > 3 Then
                g.DrawLine(pen, 0, midY, textX - 3, midY)
            End If
            g.DrawLine(pen, textX + textWidth + 2, midY, W - 1, midY)
            g.DrawLine(pen, 0, midY, 0, H - 1)
            g.DrawLine(pen, 0, H - 1, W - 1, H - 1)
            g.DrawLine(pen, W - 1, midY, W - 1, H - 1)
        End Using

        Using brush As New SolidBrush(gb.ForeColor)
            g.DrawString(gb.Text, gb.Font, brush, textX, textY)
        End Using
    End Sub

    ' ============================================================================
    Public Sub SetMenuTheme(parent As ToolStrip)
        ApplyToolStrip(parent, True, True)
    End Sub

    Public Sub SetDefaultMenuTheme(parent As ToolStrip)
        ApplyToolStrip(parent, False, True)
    End Sub

    Public Sub SetToolStripTheme(parent As ToolStrip)
        ApplyToolStrip(parent, True, False)
    End Sub

    Public Sub SetDefaultToolStripTheme(parent As ToolStrip)
        ApplyToolStrip(parent, False, False)
    End Sub

    Public Sub SetStatusStripTheme(parent As StatusStrip)
        ApplyStatusStrip(parent, True)
    End Sub

    Public Sub SetDefaultStatusStripTheme(parent As StatusStrip)
        ApplyStatusStrip(parent, False)
    End Sub

    Private Sub ApplyToolStrip(parent As ToolStrip, isDark As Boolean, isMenu As Boolean)
        If TypeOf parent Is ToolStripDropDown Then
            If isDark Then
                parent.Renderer = New DarkRender()
                parent.BackColor = DarkBack
                parent.ForeColor = DarkText
            Else
                parent.Renderer = New WRender()
                parent.BackColor = SystemColors.Menu
                parent.ForeColor = SystemColors.MenuText
            End If
            ApplyMenuItems(parent.Items, isDark)
            Return
        End If

        If isDark Then
            parent.BackColor = DarkMain
            parent.ForeColor = DarkText
        Else
            parent.BackColor = SystemColors.Window
            parent.ForeColor = SystemColors.ControlText
        End If

        For Each item As ToolStripItem In parent.Items
            ApplyItem(item, isDark)
        Next
        ApplyTextBoxes(parent, isDark)
    End Sub

    Private Sub ApplyTextBoxes(parent As ToolStrip, isDark As Boolean)
        For Each item As ToolStripItem In parent.Items
            Dim tb = TryCast(item, ToolStripTextBox)
            If tb IsNot Nothing Then
                tb.BorderStyle = If(isDark, BorderStyle.None, BorderStyle.Fixed3D)
            End If
        Next

        RemoveHandler parent.Paint, AddressOf ToolStripTextBoxBorders
        If isDark Then AddHandler parent.Paint, AddressOf ToolStripTextBoxBorders
        parent.Invalidate()
    End Sub

    Private Sub ToolStripTextBoxBorders(sender As Object, e As PaintEventArgs)
        Dim ts = DirectCast(sender, ToolStrip)
        Using pen As New Pen(Color.FromArgb(90, 90, 90))
            For Each item As ToolStripItem In ts.Items
                If TypeOf item Is ToolStripTextBox AndAlso item.Visible Then
                    Dim r = item.Bounds
                    r.Inflate(1, 1)
                    e.Graphics.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1)
                End If
            Next
        End Using
    End Sub

    Private Sub ApplyStatusStrip(parent As StatusStrip, isDark As Boolean)
        parent.BackColor = If(isDark, DarkBack, SystemColors.Control)
        parent.ForeColor = If(isDark, DarkText, SystemColors.ControlText)

        For Each item As ToolStripItem In parent.Items
            item.BackColor = parent.BackColor
            item.ForeColor = parent.ForeColor

            Dim dd = TryCast(item, ToolStripDropDownItem)
            If dd IsNot Nothing Then
                SetDropDown(dd, isDark, AddressOf StatusStripDropDownOpening)
            End If

            If TypeOf item Is ToolStripStatusLabel Then
                Dim lbl = DirectCast(item, ToolStripStatusLabel)
                lbl.BorderStyle = If(isDark, Border3DStyle.Sunken, Border3DStyle.Flat)
            ElseIf TypeOf item Is ToolStripProgressBar Then
                Dim pb = DirectCast(item, ToolStripProgressBar)
                pb.BackColor = parent.BackColor
                pb.ForeColor = If(isDark, Color.LightGreen, SystemColors.Highlight)
            End If
        Next
    End Sub

    Private Sub ApplyItem(item As ToolStripItem, isDark As Boolean)
        item.BackColor = If(isDark, DarkMain, SystemColors.Window)
        item.ForeColor = If(isDark, DarkText, SystemColors.ControlText)

        Dim menuItem = TryCast(item, ToolStripMenuItem)
        If menuItem IsNot Nothing Then
            SetDropDown(menuItem, isDark, AddressOf MenuDropDownOpening)
        End If

        Dim dd = TryCast(item, ToolStripDropDownItem)
        If dd IsNot Nothing Then
            SetDropDown(dd, isDark, AddressOf ToolStripDropDownOpening)
        End If
    End Sub

    Private Sub SetDropDown(dd As ToolStripDropDownItem, isDark As Boolean, handler As EventHandler)
        If isDark Then
            dd.DropDown.BackColor = DarkBack
            dd.DropDown.ForeColor = DarkText
            RemoveHandler dd.DropDownOpening, handler
            AddHandler dd.DropDownOpening, handler
        Else
            dd.DropDown.BackColor = SystemColors.Menu
            dd.DropDown.ForeColor = SystemColors.MenuText
            RemoveHandler dd.DropDownOpening, handler
        End If
        ApplyDropDownItems(dd, isDark)
    End Sub

    Private Sub ApplyDropDownItems(dd As ToolStripDropDownItem, isDark As Boolean)
        ApplyMenuItems(dd.DropDownItems, isDark)
    End Sub

    Private Sub ApplyMenuItems(items As ToolStripItemCollection, isDark As Boolean)
        For Each subItem As ToolStripItem In items
            subItem.BackColor = If(isDark, DarkBack, SystemColors.Menu)
            subItem.ForeColor = If(isDark, DarkText, SystemColors.MenuText)

            Dim subDD = TryCast(subItem, ToolStripDropDownItem)
            If subDD IsNot Nothing Then
                subDD.DropDown.BackColor = If(isDark, DarkBack, SystemColors.Menu)
                subDD.DropDown.ForeColor = If(isDark, DarkText, SystemColors.MenuText)

                If isDark Then
                    RemoveHandler subDD.DropDownOpening, AddressOf ToolStripDropDownOpening
                    AddHandler subDD.DropDownOpening, AddressOf ToolStripDropDownOpening
                Else
                    RemoveHandler subDD.DropDownOpening, AddressOf ToolStripDropDownOpening
                End If
                ApplyMenuItems(subDD.DropDownItems, isDark)
            End If
        Next
    End Sub

    Private Sub MenuDropDownOpening(sender As Object, e As EventArgs)
        Dim dd = TryCast(sender, ToolStripDropDownItem)
        If dd IsNot Nothing Then ApplyDropDownItems(dd, True)
    End Sub

    Private Sub ToolStripDropDownOpening(sender As Object, e As EventArgs)
        Dim dd = TryCast(sender, ToolStripDropDownItem)
        If dd IsNot Nothing Then ApplyDropDownItems(dd, True)
    End Sub

    Private Sub StatusStripDropDownOpening(sender As Object, e As EventArgs)
        Dim dd = TryCast(sender, ToolStripDropDownItem)
        If dd IsNot Nothing Then ApplyDropDownItems(dd, True)
    End Sub
End Module

Public Class DarkRender
    Inherits ToolStripSystemRenderer

    Private Shared ReadOnly ArrowColorConst As Color = Color.White
    Private Shared ReadOnly PressedColor As Color = Color.FromArgb(70, 70, 70)

    Protected Overrides Sub OnRenderToolStripBorder(e As ToolStripRenderEventArgs)
        Dim rect = e.ToolStrip.ClientRectangle
        Using pen As New Pen(Color.FromArgb(70, 70, 70))
            e.Graphics.DrawLine(pen, rect.Left, rect.Bottom - 1, rect.Right - 1, rect.Bottom - 1)
        End Using
    End Sub

    Protected Overrides Sub OnRenderArrow(e As ToolStripArrowRenderEventArgs)
        e.ArrowColor = ArrowColorConst
        MyBase.OnRenderArrow(e)
    End Sub

    Protected Overrides Sub OnRenderItemCheck(e As ToolStripItemImageRenderEventArgs)
        DrawCheckmark(e.Graphics, e.ImageRectangle, Color.White)
    End Sub

    Protected Overrides Sub OnRenderSplitButtonBackground(e As ToolStripItemRenderEventArgs)
        Dim split = TryCast(e.Item, ToolStripSplitButton)
        If split Is Nothing Then
            MyBase.OnRenderSplitButtonBackground(e)
            Return
        End If

        Dim g = e.Graphics
        Dim rect = split.DropDownButtonBounds
        Dim isPressed = split.DropDownButtonPressed
        Dim backColor = If(isPressed, PressedColor, split.BackColor)

        Using b As New SolidBrush(backColor)
            g.FillRectangle(b, rect)
        End Using

        DrawArrow(g, rect, ArrowColorConst)
    End Sub

    Private Shared Sub DrawCheckmark(g As Graphics, rc As Rectangle, color As Color)
        Dim margin = Math.Max(1, rc.Width \ 4)
        Dim r As New Rectangle(rc.Left + margin, rc.Top + margin,
                               rc.Width - margin * 2, rc.Height - margin * 2)
        Using pen As New Pen(color, 2)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            Dim p1 = New Point(r.Left, r.Top + r.Height \ 2)
            Dim p2 = New Point(r.Left + r.Width \ 3, r.Bottom - r.Height \ 5)
            Dim p3 = New Point(r.Right - r.Width \ 10, r.Top + r.Height \ 5)
            pen.StartCap = Drawing2D.LineCap.Round
            pen.EndCap = Drawing2D.LineCap.Round
            pen.LineJoin = Drawing2D.LineJoin.Round
            g.DrawLine(pen, p1, p2)
            g.DrawLine(pen, p2, p3)
        End Using
    End Sub

    Private Shared Sub DrawArrow(g As Graphics, rect As Rectangle, color As Color)
        Dim mx = rect.Left + rect.Width \ 2
        Dim my = rect.Top + rect.Height \ 2
        Dim pts = {
            New Point(mx - 4, my - 2),
            New Point(mx + 4, my - 2),
            New Point(mx, my + 3)
        }
        Using b As New SolidBrush(color)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.FillPolygon(b, pts)
        End Using
    End Sub

    Protected Overrides Sub OnRenderStatusStripSizingGrip(e As ToolStripRenderEventArgs)
        Dim g = e.Graphics
        Dim rect = e.ToolStrip.ClientRectangle
        g.SmoothingMode = Drawing2D.SmoothingMode.None
        g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
        g.PixelOffsetMode = Drawing2D.PixelOffsetMode.None

        Const cubeSize As Integer = 2
        Const gap As Integer = 2
        Dim stepSize As Integer = cubeSize + gap
        Dim right As Integer = rect.Right - 3
        Dim bottom As Integer = rect.Bottom - 3

        Using brush As New SolidBrush(Color.FromArgb(110, 110, 110))
            g.FillRectangle(brush,
                            right - cubeSize,
                            bottom - stepSize * 2 - cubeSize,
                            cubeSize,
                            cubeSize)
            g.FillRectangle(brush,
                            right - stepSize - cubeSize,
                            bottom - stepSize - cubeSize,
                            cubeSize,
                            cubeSize)
            g.FillRectangle(brush,
                            right - cubeSize,
                            bottom - stepSize - cubeSize,
                            cubeSize,
                            cubeSize)
            g.FillRectangle(brush,
                            right - stepSize * 2 - cubeSize,
                            bottom - cubeSize,
                            cubeSize,
                            cubeSize)
            g.FillRectangle(brush,
                            right - stepSize - cubeSize,
                            bottom - cubeSize,
                            cubeSize,
                            cubeSize)
            g.FillRectangle(brush,
                            right - cubeSize,
                            bottom - cubeSize,
                            cubeSize,
                            cubeSize)
        End Using
    End Sub
End Class

Public Class WRender
    Inherits ToolStripSystemRenderer

    Protected Overrides Sub OnRenderItemCheck(e As ToolStripItemImageRenderEventArgs)
        Dim color As Color = If(e.Item.Selected, Color.White, Color.Black)
        DrawCheckmark(e.Graphics, e.ImageRectangle, color)
    End Sub

    Protected Overrides Sub OnRenderItemText(e As ToolStripItemTextRenderEventArgs)
        Dim menuItem = TryCast(e.Item, ToolStripMenuItem)
        If menuItem IsNot Nothing Then
            Dim isActive As Boolean = menuItem.Selected OrElse menuItem.DropDown.Visible
            e.TextColor = If(isActive,
                             SystemColors.HighlightText,
                             SystemColors.ControlText)
        End If
        MyBase.OnRenderItemText(e)
    End Sub

    Private Shared Sub DrawCheckmark(g As Graphics, rc As Rectangle, color As Color)
        Dim margin = Math.Max(1, rc.Width \ 4)
        Dim r As New Rectangle(rc.Left + margin, rc.Top + margin,
                               rc.Width - margin * 2, rc.Height - margin * 2)
        Using pen As New Pen(color, 2)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            Dim p1 = New Point(r.Left, r.Top + r.Height \ 2)
            Dim p2 = New Point(r.Left + r.Width \ 3, r.Bottom - r.Height \ 5)
            Dim p3 = New Point(r.Right - r.Width \ 10, r.Top + r.Height \ 5)
            pen.StartCap = Drawing2D.LineCap.Round
            pen.EndCap = Drawing2D.LineCap.Round
            pen.LineJoin = Drawing2D.LineJoin.Round
            g.DrawLine(pen, p1, p2)
            g.DrawLine(pen, p2, p3)
        End Using
    End Sub
End Class

