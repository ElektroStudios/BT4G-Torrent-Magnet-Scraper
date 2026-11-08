#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic

Imports Microsoft.VisualStudio.TestTools.UnitTesting

#End Region

Namespace BT4GTorrentMagnetScraper.Tests

    ''' <summary>
    ''' Tests for the keyword filtering and page classification functions of <see cref="ScraperHelper"/>.
    ''' </summary>
    <TestClass>
    Public NotInheritable Class FilteringTests

#Region " ContainsAnyKeyword "

        ''' <summary>
        ''' Verifies that keyword matching is case-insensitive and works on substrings.
        ''' </summary>
        <TestMethod>
        <DataRow("John.Wick.2014.2160p.UHD", True)>
        <DataRow("john wick 4K HDR", True)>
        <DataRow("John Wick 1080P", True)>
        <DataRow("John Wick 720p", False)>
        <DataRow("", False)>
        Public Sub ContainsAnyKeyword_MatchesCaseInsensitiveSubstrings(text As String, expected As Boolean)

            Dim keywords As New HashSet(Of String) From {"2160p", "4k", "1080p"}

            Assert.AreEqual(expected, ScraperHelper.ContainsAnyKeyword(text, keywords))
        End Sub

        ''' <summary>
        ''' Verifies that keywords with a leading white-space (such as " sd" in the shipped forbidden keywords file)
        ''' only match when preceded by a white-space.
        ''' </summary>
        <TestMethod>
        Public Sub ContainsAnyKeyword_LeadingWhiteSpaceKeyword_RequiresWhiteSpace()

            Dim keywords As New HashSet(Of String) From {" sd"}

            Assert.IsTrue(ScraperHelper.ContainsAnyKeyword("Movie SD x264", keywords))
            Assert.IsFalse(ScraperHelper.ContainsAnyKeyword("Movie.SDR.x264", keywords))
        End Sub

        ''' <summary>
        ''' Verifies that an empty keyword set never matches.
        ''' </summary>
        <TestMethod>
        Public Sub ContainsAnyKeyword_EmptyKeywords_ReturnsFalse()

            Assert.IsFalse(ScraperHelper.ContainsAnyKeyword("anything", New HashSet(Of String)()))
        End Sub

        ''' <summary>
        ''' Verifies that an empty keyword is ignored instead of matching every text.
        ''' </summary>
        <TestMethod>
        Public Sub ContainsAnyKeyword_EmptyKeyword_IsIgnored()

            Assert.IsFalse(ScraperHelper.ContainsAnyKeyword("anything", {String.Empty}))
        End Sub

        ''' <summary>
        ''' Verifies that null arguments throw <see cref="ArgumentNullException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub ContainsAnyKeyword_NullArguments_Throw()

            Assert.ThrowsExactly(Of ArgumentNullException)(
                Sub() ScraperHelper.ContainsAnyKeyword(Nothing, {"a"}))
            Assert.ThrowsExactly(Of ArgumentNullException)(
                Sub() ScraperHelper.ContainsAnyKeyword("a", Nothing))
        End Sub

#End Region

#Region " IsDmcaRemoved "

        ''' <summary>
        ''' Verifies the detection of DMCA takedown pages.
        ''' </summary>
        <TestMethod>
        <DataRow("<div>This content has been deleted.</div>", True)>
        <DataRow("<div>CONTENT HAS BEEN DELETED</div>", True)>
        <DataRow("<div>Removed due to a DMCA request.</div>", True)>
        <DataRow("<div>dmca takedown REQUEST received</div>", True)>
        <DataRow("<div>DMCA</div>", False)>
        <DataRow("<div>Send a request</div>", False)>
        <DataRow("<div>Magnet Link</div>", False)>
        <DataRow("", False)>
        Public Sub IsDmcaRemoved_DetectsTakedownPages(pageSource As String, expected As Boolean)

            Assert.AreEqual(expected, ScraperHelper.IsDmcaRemoved(pageSource))
        End Sub

        ''' <summary>
        ''' Verifies that a null page source throws <see cref="ArgumentNullException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub IsDmcaRemoved_Null_Throws()

            Assert.ThrowsExactly(Of ArgumentNullException)(
                Sub() ScraperHelper.IsDmcaRemoved(Nothing))
        End Sub

#End Region

#Region " IsMagnetUri "

        ''' <summary>
        ''' Verifies the detection of magnet links.
        ''' </summary>
        <TestMethod>
        <DataRow("magnet:?xt=urn:btih:0123ABCD", True)>
        <DataRow("MAGNET:?xt=urn:btih:0123ABCD", True)>
        <DataRow("https://downloadtorrentfile.com/hash/0123ABCD", False)>
        <DataRow(" magnet:?xt=urn:btih:0123ABCD", False)>
        <DataRow("", False)>
        <DataRow(Nothing, False)>
        Public Sub IsMagnetUri_DetectsMagnetLinks(value As String, expected As Boolean)

            Assert.AreEqual(expected, ScraperHelper.IsMagnetUri(value))
        End Sub

#End Region

    End Class

End Namespace
