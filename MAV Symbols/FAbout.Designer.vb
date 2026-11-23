<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FAbout
    Inherits System.Windows.Forms.Form

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

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FAbout))
        Me.SIcon = New System.Windows.Forms.PictureBox()
        Me.LName = New System.Windows.Forms.Label()
        Me.LVersion = New System.Windows.Forms.Label()
        Me.LDes = New System.Windows.Forms.Label()
        Me.LLabelAuthor = New System.Windows.Forms.Label()
        Me.LAuthorValue = New System.Windows.Forms.Label()
        Me.TextBox1 = New System.Windows.Forms.TextBox()
        Me.LLabelLicense = New System.Windows.Forms.Label()
        Me.LLicenseValue = New System.Windows.Forms.Label()
        Me.LLicenseLink = New System.Windows.Forms.LinkLabel()
        Me.LLabelSource = New System.Windows.Forms.Label()
        Me.LGitHubLink = New System.Windows.Forms.LinkLabel()
        Me.LLabelTech = New System.Windows.Forms.Label()
        Me.LTechValue = New System.Windows.Forms.Label()
        Me.BOk = New System.Windows.Forms.Button()
        CType(Me.SIcon, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'SIcon
        '
        Me.SIcon.Image = CType(resources.GetObject("SIcon.Image"), System.Drawing.Image)
        Me.SIcon.Location = New System.Drawing.Point(14, 17)
        Me.SIcon.Margin = New System.Windows.Forms.Padding(0, 0, 16, 0)
        Me.SIcon.Name = "SIcon"
        Me.SIcon.Size = New System.Drawing.Size(64, 64)
        Me.SIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.SIcon.TabIndex = 19
        Me.SIcon.TabStop = False
        '
        'LName
        '
        Me.LName.AutoSize = True
        Me.LName.Font = New System.Drawing.Font("Microsoft Sans Serif", 15.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(204, Byte))
        Me.LName.ForeColor = System.Drawing.Color.FromArgb(CType(CType(32, Byte), Integer), CType(CType(32, Byte), Integer), CType(CType(32, Byte), Integer))
        Me.LName.Location = New System.Drawing.Point(94, 17)
        Me.LName.Margin = New System.Windows.Forms.Padding(0)
        Me.LName.Name = "LName"
        Me.LName.Size = New System.Drawing.Size(157, 25)
        Me.LName.TabIndex = 20
        Me.LName.Text = "MAV Symbols"
        '
        'LVersion
        '
        Me.LVersion.AutoSize = True
        Me.LVersion.ForeColor = System.Drawing.Color.FromArgb(CType(CType(110, Byte), Integer), CType(CType(110, Byte), Integer), CType(CType(110, Byte), Integer))
        Me.LVersion.Location = New System.Drawing.Point(96, 44)
        Me.LVersion.Margin = New System.Windows.Forms.Padding(0, 2, 0, 2)
        Me.LVersion.Name = "LVersion"
        Me.LVersion.Size = New System.Drawing.Size(71, 13)
        Me.LVersion.TabIndex = 21
        Me.LVersion.Text = "Версия 1.0.0"
        '
        'LDes
        '
        Me.LDes.AutoSize = True
        Me.LDes.ForeColor = System.Drawing.Color.FromArgb(CType(CType(110, Byte), Integer), CType(CType(110, Byte), Integer), CType(CType(110, Byte), Integer))
        Me.LDes.Location = New System.Drawing.Point(96, 59)
        Me.LDes.Margin = New System.Windows.Forms.Padding(0)
        Me.LDes.MaximumSize = New System.Drawing.Size(320, 0)
        Me.LDes.Name = "LDes"
        Me.LDes.Size = New System.Drawing.Size(113, 13)
        Me.LDes.TabIndex = 22
        Me.LDes.Text = "Текстовый редактор"
        '
        'LLabelAuthor
        '
        Me.LLabelAuthor.AutoSize = True
        Me.LLabelAuthor.ForeColor = System.Drawing.Color.FromArgb(CType(CType(110, Byte), Integer), CType(CType(110, Byte), Integer), CType(CType(110, Byte), Integer))
        Me.LLabelAuthor.Location = New System.Drawing.Point(14, 104)
        Me.LLabelAuthor.Margin = New System.Windows.Forms.Padding(0, 4, 16, 4)
        Me.LLabelAuthor.Name = "LLabelAuthor"
        Me.LLabelAuthor.Size = New System.Drawing.Size(75, 13)
        Me.LLabelAuthor.TabIndex = 23
        Me.LLabelAuthor.Text = "Разработчик:"
        '
        'LAuthorValue
        '
        Me.LAuthorValue.AutoSize = True
        Me.LAuthorValue.Location = New System.Drawing.Point(96, 104)
        Me.LAuthorValue.Margin = New System.Windows.Forms.Padding(0, 4, 0, 4)
        Me.LAuthorValue.Name = "LAuthorValue"
        Me.LAuthorValue.Size = New System.Drawing.Size(176, 13)
        Me.LAuthorValue.TabIndex = 24
        Me.LAuthorValue.Text = "© 2026 Andrey Morozov (MAV-128)"
        '
        'TextBox1
        '
        Me.TextBox1.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.TextBox1.Location = New System.Drawing.Point(102, 124)
        Me.TextBox1.Name = "TextBox1"
        Me.TextBox1.ReadOnly = True
        Me.TextBox1.Size = New System.Drawing.Size(170, 13)
        Me.TextBox1.TabIndex = 33
        Me.TextBox1.TabStop = False
        Me.TextBox1.Text = "mav-128@outlook.com"
        '
        'LLabelLicense
        '
        Me.LLabelLicense.AutoSize = True
        Me.LLabelLicense.ForeColor = System.Drawing.Color.FromArgb(CType(CType(110, Byte), Integer), CType(CType(110, Byte), Integer), CType(CType(110, Byte), Integer))
        Me.LLabelLicense.Location = New System.Drawing.Point(14, 161)
        Me.LLabelLicense.Margin = New System.Windows.Forms.Padding(0, 4, 16, 4)
        Me.LLabelLicense.Name = "LLabelLicense"
        Me.LLabelLicense.Size = New System.Drawing.Size(60, 13)
        Me.LLabelLicense.TabIndex = 25
        Me.LLabelLicense.Text = "Лицензия:"
        '
        'LLicenseValue
        '
        Me.LLicenseValue.AutoSize = True
        Me.LLicenseValue.Location = New System.Drawing.Point(96, 161)
        Me.LLicenseValue.Margin = New System.Windows.Forms.Padding(0, 4, 0, 4)
        Me.LLicenseValue.Name = "LLicenseValue"
        Me.LLicenseValue.Size = New System.Drawing.Size(167, 13)
        Me.LLicenseValue.TabIndex = 26
        Me.LLicenseValue.Text = "GNU General Public License v3.0"
        '
        'LLicenseLink
        '
        Me.LLicenseLink.AutoSize = True
        Me.LLicenseLink.Location = New System.Drawing.Point(96, 182)
        Me.LLicenseLink.Margin = New System.Windows.Forms.Padding(0, 4, 0, 4)
        Me.LLicenseLink.Name = "LLicenseLink"
        Me.LLicenseLink.Size = New System.Drawing.Size(133, 13)
        Me.LLicenseLink.TabIndex = 27
        Me.LLicenseLink.TabStop = True
        Me.LLicenseLink.Text = "Открыть текст лицензии"
        '
        'LLabelSource
        '
        Me.LLabelSource.AutoSize = True
        Me.LLabelSource.ForeColor = System.Drawing.Color.FromArgb(CType(CType(110, Byte), Integer), CType(CType(110, Byte), Integer), CType(CType(110, Byte), Integer))
        Me.LLabelSource.Location = New System.Drawing.Point(14, 228)
        Me.LLabelSource.Margin = New System.Windows.Forms.Padding(0, 4, 16, 4)
        Me.LLabelSource.Name = "LLabelSource"
        Me.LLabelSource.Size = New System.Drawing.Size(82, 13)
        Me.LLabelSource.TabIndex = 28
        Me.LLabelSource.Text = "Исходный код:"
        '
        'LGitHubLink
        '
        Me.LGitHubLink.AutoSize = True
        Me.LGitHubLink.Location = New System.Drawing.Point(96, 228)
        Me.LGitHubLink.Margin = New System.Windows.Forms.Padding(0, 4, 0, 4)
        Me.LGitHubLink.Name = "LGitHubLink"
        Me.LGitHubLink.Size = New System.Drawing.Size(152, 13)
        Me.LGitHubLink.TabIndex = 29
        Me.LGitHubLink.TabStop = True
        Me.LGitHubLink.Text = "GitHub репозиторий проекта"
        '
        'LLabelTech
        '
        Me.LLabelTech.AutoSize = True
        Me.LLabelTech.ForeColor = System.Drawing.Color.FromArgb(CType(CType(110, Byte), Integer), CType(CType(110, Byte), Integer), CType(CType(110, Byte), Integer))
        Me.LLabelTech.Location = New System.Drawing.Point(14, 249)
        Me.LLabelTech.Margin = New System.Windows.Forms.Padding(0, 4, 16, 4)
        Me.LLabelTech.Name = "LLabelTech"
        Me.LLabelTech.Size = New System.Drawing.Size(41, 13)
        Me.LLabelTech.TabIndex = 30
        Me.LLabelTech.Text = "Среда:"
        '
        'LTechValue
        '
        Me.LTechValue.AutoSize = True
        Me.LTechValue.Location = New System.Drawing.Point(96, 249)
        Me.LTechValue.Margin = New System.Windows.Forms.Padding(0, 4, 0, 4)
        Me.LTechValue.Name = "LTechValue"
        Me.LTechValue.Size = New System.Drawing.Size(105, 39)
        Me.LTechValue.TabIndex = 31
        Me.LTechValue.Text = "Visual Basic .NET" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & ".NET Framework 4.8" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Windows Forms"
        '
        'BOk
        '
        Me.BOk.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.BOk.Location = New System.Drawing.Point(200, 347)
        Me.BOk.Name = "BOk"
        Me.BOk.Size = New System.Drawing.Size(100, 32)
        Me.BOk.TabIndex = 34
        Me.BOk.Text = "ОК"
        Me.BOk.UseVisualStyleBackColor = True
        '
        'FAbout
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AutoSize = True
        Me.BackColor = System.Drawing.SystemColors.Control
        Me.ClientSize = New System.Drawing.Size(312, 391)
        Me.Controls.Add(Me.BOk)
        Me.Controls.Add(Me.SIcon)
        Me.Controls.Add(Me.LName)
        Me.Controls.Add(Me.LVersion)
        Me.Controls.Add(Me.LDes)
        Me.Controls.Add(Me.LLabelAuthor)
        Me.Controls.Add(Me.LAuthorValue)
        Me.Controls.Add(Me.TextBox1)
        Me.Controls.Add(Me.LLabelLicense)
        Me.Controls.Add(Me.LLicenseValue)
        Me.Controls.Add(Me.LLicenseLink)
        Me.Controls.Add(Me.LLabelSource)
        Me.Controls.Add(Me.LGitHubLink)
        Me.Controls.Add(Me.LLabelTech)
        Me.Controls.Add(Me.LTechValue)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FAbout"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "О программе"
        CType(Me.SIcon, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents SIcon As PictureBox
    Friend WithEvents LName As Label
    Friend WithEvents LVersion As Label
    Friend WithEvents LDes As Label
    Friend WithEvents LLabelAuthor As Label
    Friend WithEvents LAuthorValue As Label
    Friend WithEvents TextBox1 As TextBox
    Friend WithEvents LLabelLicense As Label
    Friend WithEvents LLicenseValue As Label
    Friend WithEvents LLicenseLink As LinkLabel
    Friend WithEvents LLabelSource As Label
    Friend WithEvents LGitHubLink As LinkLabel
    Friend WithEvents LLabelTech As Label
    Friend WithEvents LTechValue As Label
    Friend WithEvents BOk As Button
End Class