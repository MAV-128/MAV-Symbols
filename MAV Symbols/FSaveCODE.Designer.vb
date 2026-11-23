<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FSaveCODE
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
        Me.Label1 = New System.Windows.Forms.Label()
        Me.CBEnc = New System.Windows.Forms.ListBox()
        Me.BCancel = New System.Windows.Forms.Button()
        Me.BOk = New System.Windows.Forms.Button()
        Me.LDoc = New System.Windows.Forms.Label()
        Me.BOMch = New System.Windows.Forms.CheckBox()
        Me.TextP = New System.Windows.Forms.TextBox()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.LInfo = New System.Windows.Forms.Label()
        Me.MLch = New System.Windows.Forms.CheckBox()
        Me.GroupBox1.SuspendLayout()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(12, 31)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(65, 13)
        Me.Label1.TabIndex = 13
        Me.Label1.Text = "Кодировка:"
        '
        'CBEnc
        '
        Me.CBEnc.FormattingEnabled = True
        Me.CBEnc.Location = New System.Drawing.Point(12, 47)
        Me.CBEnc.Name = "CBEnc"
        Me.CBEnc.Size = New System.Drawing.Size(295, 212)
        Me.CBEnc.TabIndex = 11
        '
        'BCancel
        '
        Me.BCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.BCancel.Location = New System.Drawing.Point(659, 302)
        Me.BCancel.Name = "BCancel"
        Me.BCancel.Size = New System.Drawing.Size(100, 32)
        Me.BCancel.TabIndex = 10
        Me.BCancel.Text = "Отмена"
        Me.BCancel.UseVisualStyleBackColor = True
        '
        'BOk
        '
        Me.BOk.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.BOk.Location = New System.Drawing.Point(553, 302)
        Me.BOk.Name = "BOk"
        Me.BOk.Size = New System.Drawing.Size(100, 32)
        Me.BOk.TabIndex = 9
        Me.BOk.Text = "&ОК"
        Me.BOk.UseVisualStyleBackColor = True
        '
        'LDoc
        '
        Me.LDoc.AutoSize = True
        Me.LDoc.Location = New System.Drawing.Point(12, 9)
        Me.LDoc.Name = "LDoc"
        Me.LDoc.Size = New System.Drawing.Size(61, 13)
        Me.LDoc.TabIndex = 16
        Me.LDoc.Text = "Документ:"
        '
        'BOMch
        '
        Me.BOMch.AutoSize = True
        Me.BOMch.Location = New System.Drawing.Point(12, 265)
        Me.BOMch.Name = "BOMch"
        Me.BOMch.Size = New System.Drawing.Size(50, 17)
        Me.BOMch.TabIndex = 17
        Me.BOMch.Text = "BOM"
        Me.BOMch.UseVisualStyleBackColor = True
        '
        'TextP
        '
        Me.TextP.BackColor = System.Drawing.SystemColors.Window
        Me.TextP.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.TextP.Location = New System.Drawing.Point(313, 47)
        Me.TextP.Multiline = True
        Me.TextP.Name = "TextP"
        Me.TextP.ReadOnly = True
        Me.TextP.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.TextP.Size = New System.Drawing.Size(446, 212)
        Me.TextP.TabIndex = 18
        Me.TextP.WordWrap = False
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.LInfo)
        Me.GroupBox1.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(204, Byte))
        Me.GroupBox1.Location = New System.Drawing.Point(12, 340)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(747, 66)
        Me.GroupBox1.TabIndex = 19
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "ВНИМАНИЕ!"
        '
        'LInfo
        '
        Me.LInfo.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(204, Byte))
        Me.LInfo.Location = New System.Drawing.Point(10, 20)
        Me.LInfo.Name = "LInfo"
        Me.LInfo.Size = New System.Drawing.Size(568, 37)
        Me.LInfo.TabIndex = 20
        Me.LInfo.Text = "Символы, отсутствующие в выбранной кодировке, будут заменены на ""?"" при сохранени" &
    "и - это необратимо. Проверьте пример текста перед сохранением!"
        '
        'MLch
        '
        Me.MLch.AutoSize = True
        Me.MLch.Location = New System.Drawing.Point(313, 265)
        Me.MLch.Name = "MLch"
        Me.MLch.Size = New System.Drawing.Size(126, 17)
        Me.MLch.TabIndex = 20
        Me.MLch.Text = "Перенос по словам"
        Me.MLch.UseVisualStyleBackColor = True
        '
        'FSaveCODE
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(768, 417)
        Me.Controls.Add(Me.MLch)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.TextP)
        Me.Controls.Add(Me.BOMch)
        Me.Controls.Add(Me.LDoc)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.CBEnc)
        Me.Controls.Add(Me.BCancel)
        Me.Controls.Add(Me.BOk)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FSaveCODE"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Кодировка"
        Me.GroupBox1.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents CBEnc As System.Windows.Forms.ListBox
    Friend WithEvents BCancel As System.Windows.Forms.Button
    Friend WithEvents BOk As System.Windows.Forms.Button
    Friend WithEvents LDoc As System.Windows.Forms.Label
    Friend WithEvents BOMch As System.Windows.Forms.CheckBox
    Friend WithEvents TextP As TextBox
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents LInfo As Label
    Friend WithEvents MLch As CheckBox
End Class
