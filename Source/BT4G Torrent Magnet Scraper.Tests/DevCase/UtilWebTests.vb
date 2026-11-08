#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports DevCase.Core.Networking.Common

Imports Microsoft.VisualStudio.TestTools.UnitTesting

#End Region

Namespace BT4GTorrentMagnetScraper.Tests.DevCaseTests

    ''' <summary>
    ''' Tests for the <see cref="UtilWeb"/> class.
    ''' </summary>
    <TestClass>
    Public NotInheritable Class UtilWebTests

        ''' <summary>
        ''' Verifies that the known Cloudflare challenge indicators are detected (case-insensitive).
        ''' </summary>
        <TestMethod>
        <DataRow("<html><head><title>Just a moment...</title></head></html>")>
        <DataRow("<script src=""/cdn-cgi/challenge-platform/h/b/orchestrate/chl_page/v1""></script>")>
        <DataRow("<script>window._cf_chl_opt = {cvId: '3'};</script>")>
        <DataRow("<div id=""challenge-error-text"">Enable JavaScript and cookies to continue</div>")>
        <DataRow("<SCRIPT>WINDOW._CF_CHL_OPT = {};</SCRIPT>")>
        Public Sub IsCloudflareChallengeRequired_ChallengePage_ReturnsTrue(pageSource As String)

            Assert.IsTrue(UtilWeb.IsCloudflareChallengeRequired(pageSource, pageTitle:="Some title"))
        End Sub

        ''' <summary>
        ''' Verifies that the "Just a moment..." page title alone is enough to detect the challenge.
        ''' </summary>
        <TestMethod>
        Public Sub IsCloudflareChallengeRequired_ChallengeTitle_ReturnsTrue()

            Assert.IsTrue(UtilWeb.IsCloudflareChallengeRequired("<html><body></body></html>", pageTitle:="just a moment..."))
        End Sub

        ''' <summary>
        ''' Verifies that a regular page is not detected as a challenge.
        ''' </summary>
        <TestMethod>
        Public Sub IsCloudflareChallengeRequired_RegularPage_ReturnsFalse()

            Dim pageSource As String = "<html><head><title>John Wick - BT4G</title></head><body>Powered by Cloudflare</body></html>"

            Assert.IsFalse(UtilWeb.IsCloudflareChallengeRequired(pageSource, pageTitle:="John Wick - BT4G"))
        End Sub

        ''' <summary>
        ''' Verifies that an empty page source (no page loaded) is not detected as a challenge.
        ''' </summary>
        <TestMethod>
        <DataRow("")>
        <DataRow("   ")>
        <DataRow(Nothing)>
        Public Sub IsCloudflareChallengeRequired_EmptyPageSource_ReturnsFalse(pageSource As String)

            Assert.IsFalse(UtilWeb.IsCloudflareChallengeRequired(pageSource, pageTitle:=Nothing))
        End Sub

    End Class

End Namespace
