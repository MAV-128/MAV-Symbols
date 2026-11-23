' MAV Symbols - Текстовый редактор
' Copyright © 2026 Andrey Morozov (MAV-128)
' Лицензия: GNU GPL v3.0


Option Explicit On
Option Strict On
Imports System.ComponentModel
Imports System.Diagnostics

Public Class FAbout
    Private Const LicenseUrl As String = "https://www.gnu.org/licenses/gpl-3.0.html"
    Private Const RepoUrl As String = "https://github.com/MAV-128"

    ' ==================== ЛОКАЛИЗАЦИЯ ====================
    Public Sub ApplyLanguage()
        Me.Text = MAVLang.GetText("FAbout_Title")
        LLabelAuthor.Text = MAVLang.GetText("FAbout_LLabelAuthor")
        LLabelLicense.Text = MAVLang.GetText("FAbout_LLabelLicense")
        LLabelSource.Text = MAVLang.GetText("FAbout_LLabelSource")
        LLabelTech.Text = MAVLang.GetText("FAbout_LLabelTech")
        LLicenseLink.Text = MAVLang.GetText("FAbout_LLicenseLink")
        LGitHubLink.Text = MAVLang.GetText("FAbout_LGitHubLink")
        BOk.Text = MAVLang.GetText("FAbout_BOk")
    End Sub
    ' =====================================================

    Private Sub FAbout_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' применяем текущий язык
        ApplyLanguage()

        Dim info = My.Application.Info

        ' Название программы (НЕ переводим — это имя продукта)
        If String.IsNullOrWhiteSpace(info.Title) Then
            LName.Text = "MAV Symbols"
        Else
            LName.Text = info.Title
        End If

        ' Версия (переводим только префикс "Версия")
        LVersion.Text = MAVLang.GetText("FAbout_VersionPrefix") & info.Version.ToString()

        LDes.Text = MAVLang.GetText("FAbout_DefaultDescription")
    End Sub



    Private Sub OpenUrl(url As String)
        Try
            Process.Start(New ProcessStartInfo(url) With {.UseShellExecute = True})
        Catch ex As Exception
            MessageBox.Show(
                MAVLang.GetText("FAbout_ErrorOpenUrl") & Environment.NewLine & ex.Message,
                MAVLang.GetText("FAbout_ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub LLicenseLink_LinkClicked_1(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LLicenseLink.LinkClicked
        OpenUrl(LicenseUrl)
    End Sub

    Private Sub LGitHubLink_LinkClicked_1(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LGitHubLink.LinkClicked
        OpenUrl(RepoUrl)
    End Sub
End Class