<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FOpenCODE
    Inherits System.Windows.Forms.Form

    'Форма переопределяет dispose для очистки списка компонентов.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Является обязательной для конструктора форм Windows Forms
    Private components As System.ComponentModel.IContainer

    'Примечание: следующая процедура является обязательной для конструктора форм Windows Forms
    'Для ее изменения используйте конструктор форм Windows Form.  
    'Не изменяйте ее в редакторе исходного кода.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.BOk = New System.Windows.Forms.Button()
        Me.BCancel = New System.Windows.Forms.Button()
        Me.CBEnc = New System.Windows.Forms.ListBox()
        Me.TextP = New System.Windows.Forms.TextBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.LDoc = New System.Windows.Forms.Label()
        Me.LAd = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'BOk
        '
        Me.BOk.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.BOk.Location = New System.Drawing.Point(556, 295)
        Me.BOk.Name = "BOk"
        Me.BOk.Size = New System.Drawing.Size(100, 32)
        Me.BOk.TabIndex = 2
        Me.BOk.Text = "&ОК"
        Me.BOk.UseVisualStyleBackColor = True
        '
        'BCancel
        '
        Me.BCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.BCancel.Location = New System.Drawing.Point(662, 295)
        Me.BCancel.Name = "BCancel"
        Me.BCancel.Size = New System.Drawing.Size(100, 32)
        Me.BCancel.TabIndex = 3
        Me.BCancel.Text = "Отмена"
        Me.BCancel.UseVisualStyleBackColor = True
        '
        'CBEnc
        '
        Me.CBEnc.FormattingEnabled = True
        Me.CBEnc.Location = New System.Drawing.Point(15, 77)
        Me.CBEnc.Name = "CBEnc"
        Me.CBEnc.Size = New System.Drawing.Size(295, 212)
        Me.CBEnc.TabIndex = 4
        '
        'TextP
        '
        Me.TextP.BackColor = System.Drawing.SystemColors.Window
        Me.TextP.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.TextP.Location = New System.Drawing.Point(316, 77)
        Me.TextP.Multiline = True
        Me.TextP.Name = "TextP"
        Me.TextP.ReadOnly = True
        Me.TextP.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.TextP.Size = New System.Drawing.Size(446, 212)
        Me.TextP.TabIndex = 5
        Me.TextP.WordWrap = False
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(12, 61)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(65, 13)
        Me.Label1.TabIndex = 6
        Me.Label1.Text = "Кодировка:"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(313, 61)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(185, 13)
        Me.Label2.TabIndex = 7
        Me.Label2.Text = "Пример (первые 300 строк файла):"
        '
        'LDoc
        '
        Me.LDoc.AutoSize = True
        Me.LDoc.Location = New System.Drawing.Point(12, 31)
        Me.LDoc.Name = "LDoc"
        Me.LDoc.Size = New System.Drawing.Size(61, 13)
        Me.LDoc.TabIndex = 9
        Me.LDoc.Text = "Документ:"
        '
        'LAd
        '
        Me.LAd.Location = New System.Drawing.Point(12, 9)
        Me.LAd.Name = "LAd"
        Me.LAd.Size = New System.Drawing.Size(750, 22)
        Me.LAd.TabIndex = 10
        Me.LAd.Text = "Адрес:"
        '
        'FOpenCODE
        '
        Me.AcceptButton = Me.BOk
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.BCancel
        Me.ClientSize = New System.Drawing.Size(774, 340)
        Me.Controls.Add(Me.LAd)
        Me.Controls.Add(Me.LDoc)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.TextP)
        Me.Controls.Add(Me.CBEnc)
        Me.Controls.Add(Me.BCancel)
        Me.Controls.Add(Me.BOk)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FOpenCODE"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Кодировка"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents BOk As System.Windows.Forms.Button
    Friend WithEvents BCancel As System.Windows.Forms.Button
    Friend WithEvents CBEnc As System.Windows.Forms.ListBox
    Friend WithEvents TextP As System.Windows.Forms.TextBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents LDoc As System.Windows.Forms.Label
    Friend WithEvents LAd As Label
End Class
