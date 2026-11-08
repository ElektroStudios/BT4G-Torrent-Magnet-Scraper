#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports Microsoft.VisualStudio.TestTools.UnitTesting

Imports WebDriverExtensions = DevCase.ThirdParty.Selenium.Extensions.IWebDriverExtensions.IWebDriverExtensions

#End Region

Namespace BT4GTorrentMagnetScraper.Tests.DevCaseTests

    ''' <summary>
    ''' Tests for the browser-independent members of the <c>IWebDriverExtensions</c> module.
    ''' </summary>
    <TestClass>
    Public NotInheritable Class IWebDriverExtensionsTests

        ''' <summary>
        ''' Verifies that Chrome log messages referring to resource files (scripts, styles, images, etc.) are detected,
        ''' so their HTTP errors are ignored by <c>ThrowIfAnyErrorStatusCode</c>.
        ''' </summary>
        <TestMethod>
        <DataRow("https://bt4gprx.com/static/js/app.js - Failed to load resource: the server responded with a status of 404 ()")>
        <DataRow("https://bt4gprx.com/static/css/site.css 0:0 Failed to load resource: the server responded with a status of 403 ()")>
        <DataRow("https://bt4gprx.com/img/logo.png?v=2 - Failed to load resource: the server responded with a status of 404 ()")>
        <DataRow("https://bt4gprx.com/favicon.ico - Failed to load resource: the server responded with a status of 404 ()")>
        Public Sub IsResourceUrlLogEntryMessage_ResourceFile_ReturnsTrue(message As String)

            Assert.IsTrue(WebDriverExtensions.IsResourceUrlLogEntryMessage(message))
        End Sub

        ''' <summary>
        ''' Verifies that Chrome log messages referring to pages/documents are not treated as resources.
        ''' </summary>
        <TestMethod>
        <DataRow("https://bt4gprx.com/search?q=John.Wick - Failed to load resource: the server responded with a status of 500 ()")>
        <DataRow("https://bt4gprx.com/magnet/0123ABCD - Failed to load resource: the server responded with a status of 404 ()")>
        <DataRow("https://bt4gprx.com/index.html - Failed to load resource: the server responded with a status of 404 ()")>
        <DataRow("https://bt4gprx.com/page.htm - Failed to load resource: the server responded with a status of 404 ()")>
        <DataRow("https://bt4gprx.com/search.php?q=x - Failed to load resource: the server responded with a status of 404 ()")>
        <DataRow("https://bt4gprx.com/ - Failed to load resource: the server responded with a status of 503 ()")>
        Public Sub IsResourceUrlLogEntryMessage_Page_ReturnsFalse(message As String)

            Assert.IsFalse(WebDriverExtensions.IsResourceUrlLogEntryMessage(message))
        End Sub

        ''' <summary>
        ''' Verifies that malformed messages are not treated as resources (and do not throw).
        ''' </summary>
        <TestMethod>
        <DataRow("")>
        <DataRow("no-space-in-this-message")>
        <DataRow("not-a-url.js - something")>
        Public Sub IsResourceUrlLogEntryMessage_MalformedMessage_ReturnsFalse(message As String)

            Assert.IsFalse(WebDriverExtensions.IsResourceUrlLogEntryMessage(message))
        End Sub

    End Class

End Namespace
