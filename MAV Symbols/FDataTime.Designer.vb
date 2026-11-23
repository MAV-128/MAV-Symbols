<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FDataTime
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
        Me.CBFormat = New System.Windows.Forms.ListBox()
        Me.BCancel = New System.Windows.Forms.Button()
        Me.BOk = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(9, 9)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(52, 13)
        Me.Label1.TabIndex = 13
        Me.Label1.Text = "Формат:"
        '
        'CBFormat
        '
        Me.CBFormat.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(204, Byte))
        Me.CBFormat.FormattingEnabled = True
        Me.CBFormat.ItemHeight = 16
        Me.CBFormat.Location = New System.Drawing.Point(12, 25)
        Me.CBFormat.Name = "CBFormat"
        Me.CBFormat.Size = New System.Drawing.Size(367, 164)
        Me.CBFormat.TabIndex = 1
        '
        'BCancel
        '
        Me.BCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.BCancel.Location = New System.Drawing.Point(279, 195)
        Me.BCancel.Name = "BCancel"
        Me.BCancel.Size = New System.Drawing.Size(100, 32)
        Me.BCancel.TabIndex = 10
        Me.BCancel.Text = "Отмена"
        Me.BCancel.UseVisualStyleBackColor = True
        '
        'BOk
        '
        Me.BOk.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.BOk.Location = New System.Drawing.Point(173, 195)
        Me.BOk.Name = "BOk"
        Me.BOk.Size = New System.Drawing.Size(100, 32)
        Me.BOk.TabIndex = 1
        Me.BOk.Text = "&ОК"
        Me.BOk.UseVisualStyleBackColor = True
        '
        'FDataTime
        '
        Me.AcceptButton = Me.BOk
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.BCancel
        Me.ClientSize = New System.Drawing.Size(390, 237)
        Me.Controls.Add(Me.BOk)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.CBFormat)
        Me.Controls.Add(Me.BCancel)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FDataTime"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Время и дата"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents CBFormat As System.Windows.Forms.ListBox
    Friend WithEvents BCancel As System.Windows.Forms.Button
    Friend WithEvents BOk As System.Windows.Forms.Button
End Class
