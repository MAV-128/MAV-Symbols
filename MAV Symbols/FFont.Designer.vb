<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FFont
    Inherits System.Windows.Forms.Form

    'Форма переопределяет dispose для очистки списка компонентов.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.GBSample = New System.Windows.Forms.GroupBox()
        Me.LSample = New System.Windows.Forms.Label()
        Me.LFontName = New System.Windows.Forms.Label()
        Me.CBFontName = New System.Windows.Forms.ComboBox()
        Me.LItBl = New System.Windows.Forms.Label()
        Me.CBBold = New System.Windows.Forms.CheckBox()
        Me.CBItalic = New System.Windows.Forms.CheckBox()
        Me.CBSize = New System.Windows.Forms.ComboBox()
        Me.LSize = New System.Windows.Forms.Label()
        Me.BOk = New System.Windows.Forms.Button()
        Me.BCancel = New System.Windows.Forms.Button()
        Me.GBSample.SuspendLayout()
        Me.SuspendLayout()
        '
        'GBSample
        '
        Me.GBSample.Controls.Add(Me.LSample)
        Me.GBSample.Location = New System.Drawing.Point(11, 190)
        Me.GBSample.Margin = New System.Windows.Forms.Padding(2)
        Me.GBSample.Name = "GBSample"
        Me.GBSample.Padding = New System.Windows.Forms.Padding(2)
        Me.GBSample.Size = New System.Drawing.Size(416, 172)
        Me.GBSample.TabIndex = 2
        Me.GBSample.TabStop = False
        Me.GBSample.Text = "Образец"
        '
        'LSample
        '
        Me.LSample.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(204, Byte))
        Me.LSample.Location = New System.Drawing.Point(4, 15)
        Me.LSample.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.LSample.Name = "LSample"
        Me.LSample.Size = New System.Drawing.Size(406, 148)
        Me.LSample.TabIndex = 0
        Me.LSample.Text = "AaBbYyZz"
        Me.LSample.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'LFontName
        '
        Me.LFontName.AutoSize = True
        Me.LFontName.Location = New System.Drawing.Point(9, 7)
        Me.LFontName.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.LFontName.Name = "LFontName"
        Me.LFontName.Size = New System.Drawing.Size(44, 13)
        Me.LFontName.TabIndex = 1
        Me.LFontName.Text = "Шрифт:"
        '
        'CBFontName
        '
        Me.CBFontName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.Simple
        Me.CBFontName.FormattingEnabled = True
        Me.CBFontName.Location = New System.Drawing.Point(11, 23)
        Me.CBFontName.Margin = New System.Windows.Forms.Padding(2)
        Me.CBFontName.Name = "CBFontName"
        Me.CBFontName.Size = New System.Drawing.Size(197, 145)
        Me.CBFontName.TabIndex = 0
        '
        'LItBl
        '
        Me.LItBl.AutoSize = True
        Me.LItBl.Location = New System.Drawing.Point(218, 7)
        Me.LItBl.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.LItBl.Name = "LItBl"
        Me.LItBl.Size = New System.Drawing.Size(70, 13)
        Me.LItBl.TabIndex = 3
        Me.LItBl.Text = "Начертание:"
        '
        'CBBold
        '
        Me.CBBold.AutoSize = True
        Me.CBBold.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.8!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(204, Byte))
        Me.CBBold.Location = New System.Drawing.Point(220, 32)
        Me.CBBold.Margin = New System.Windows.Forms.Padding(2)
        Me.CBBold.Name = "CBBold"
        Me.CBBold.Size = New System.Drawing.Size(101, 17)
        Me.CBBold.TabIndex = 2
        Me.CBBold.Text = "Полужирный"
        Me.CBBold.UseVisualStyleBackColor = True
        '
        'CBItalic
        '
        Me.CBItalic.AutoSize = True
        Me.CBItalic.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.8!, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, CType(204, Byte))
        Me.CBItalic.Location = New System.Drawing.Point(220, 53)
        Me.CBItalic.Margin = New System.Windows.Forms.Padding(2)
        Me.CBItalic.Name = "CBItalic"
        Me.CBItalic.Size = New System.Drawing.Size(84, 17)
        Me.CBItalic.TabIndex = 3
        Me.CBItalic.Text = "Наклонный"
        Me.CBItalic.UseVisualStyleBackColor = True
        '
        'CBSize
        '
        Me.CBSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.Simple
        Me.CBSize.FormattingEnabled = True
        Me.CBSize.Location = New System.Drawing.Point(364, 23)
        Me.CBSize.Margin = New System.Windows.Forms.Padding(2)
        Me.CBSize.Name = "CBSize"
        Me.CBSize.Size = New System.Drawing.Size(64, 145)
        Me.CBSize.TabIndex = 4
        '
        'LSize
        '
        Me.LSize.AutoSize = True
        Me.LSize.Location = New System.Drawing.Point(362, 7)
        Me.LSize.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.LSize.Name = "LSize"
        Me.LSize.Size = New System.Drawing.Size(49, 13)
        Me.LSize.TabIndex = 7
        Me.LSize.Text = "Размер:"
        '
        'BOk
        '
        Me.BOk.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.BOk.Location = New System.Drawing.Point(328, 367)
        Me.BOk.Margin = New System.Windows.Forms.Padding(2)
        Me.BOk.Name = "BOk"
        Me.BOk.Size = New System.Drawing.Size(100, 32)
        Me.BOk.TabIndex = 6
        Me.BOk.Text = "Применить"
        Me.BOk.UseVisualStyleBackColor = True
        '
        'BCancel
        '
        Me.BCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.BCancel.Location = New System.Drawing.Point(221, 367)
        Me.BCancel.Margin = New System.Windows.Forms.Padding(2)
        Me.BCancel.Name = "BCancel"
        Me.BCancel.Size = New System.Drawing.Size(100, 32)
        Me.BCancel.TabIndex = 5
        Me.BCancel.Text = "Отмена"
        Me.BCancel.UseVisualStyleBackColor = True
        '
        'FFont
        '
        Me.AcceptButton = Me.BOk
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.BCancel
        Me.ClientSize = New System.Drawing.Size(437, 410)
        Me.Controls.Add(Me.BCancel)
        Me.Controls.Add(Me.BOk)
        Me.Controls.Add(Me.LSize)
        Me.Controls.Add(Me.CBSize)
        Me.Controls.Add(Me.CBItalic)
        Me.Controls.Add(Me.CBBold)
        Me.Controls.Add(Me.LItBl)
        Me.Controls.Add(Me.GBSample)
        Me.Controls.Add(Me.LFontName)
        Me.Controls.Add(Me.CBFontName)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.Margin = New System.Windows.Forms.Padding(2)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FFont"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Шрифт"
        Me.GBSample.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents GBSample As GroupBox
    Friend WithEvents LSample As Label
    Friend WithEvents LFontName As Label
    Friend WithEvents CBFontName As ComboBox
    Friend WithEvents LItBl As Label
    Friend WithEvents CBBold As CheckBox
    Friend WithEvents CBItalic As CheckBox
    Friend WithEvents CBSize As ComboBox
    Friend WithEvents LSize As Label
    Friend WithEvents BOk As Button
    Friend WithEvents BCancel As Button
End Class
