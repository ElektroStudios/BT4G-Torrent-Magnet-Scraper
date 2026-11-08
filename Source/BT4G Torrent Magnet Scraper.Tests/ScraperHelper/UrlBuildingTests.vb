#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports Microsoft.VisualStudio.TestTools.UnitTesting

#End Region

Namespace BT4GTorrentMagnetScraper.Tests

    ''' <summary>
    ''' Tests for the URL building functions of <see cref="ScraperHelper"/>.
    ''' </summary>
    <TestClass>
    Public NotInheritable Class UrlBuildingTests

#Region " Constants "

        ''' <summary>
        ''' The base URL of the BT4G website used in the tests.
        ''' </summary>
        Private Const BaseUrl As String = "https://bt4gprx.com"

#End Region

#Region " BuildSearchPageUrl "

        ''' <summary>
        ''' Verifies the URL of the first page without additional parameters.
        ''' </summary>
        <TestMethod>
        Public Sub BuildSearchPageUrl_FirstPage_NoParameters()

            Dim url As String = ScraperHelper.BuildSearchPageUrl(UrlBuildingTests.BaseUrl, "John Wick", Array.Empty(Of String)(), 1)

            Assert.AreEqual("https://bt4gprx.com/search?q=John%20Wick", url)
        End Sub

        ''' <summary>
        ''' Verifies that the additional parameters are appended in order, and that the
        ''' page parameter is not appended for the first page.
        ''' </summary>
        <TestMethod>
        Public Sub BuildSearchPageUrl_FirstPage_WithParameters()

            Dim url As String = ScraperHelper.BuildSearchPageUrl(UrlBuildingTests.BaseUrl, "John Wick", {"category=movie", "orderby=seeders"}, 1)

            Assert.AreEqual("https://bt4gprx.com/search?q=John%20Wick&category=movie&orderby=seeders", url)
        End Sub

        ''' <summary>
        ''' Verifies that the page parameter is appended after the additional parameters for pages greater than 1.
        ''' </summary>
        <TestMethod>
        Public Sub BuildSearchPageUrl_NextPage_AppendsPageParameterLast()

            Dim url As String = ScraperHelper.BuildSearchPageUrl(UrlBuildingTests.BaseUrl, "John Wick", {"orderby=seeders"}, 3)

            Assert.AreEqual("https://bt4gprx.com/search?q=John%20Wick&orderby=seeders&p=3", url)
        End Sub

        ''' <summary>
        ''' Verifies that large page numbers are not formatted with thousands separators.
        ''' </summary>
        <TestMethod>
        Public Sub BuildSearchPageUrl_LargePageNumber_HasNoThousandsSeparator()

            Dim url As String = ScraperHelper.BuildSearchPageUrl(UrlBuildingTests.BaseUrl, "x", Array.Empty(Of String)(), 12345)

            Assert.AreEqual("https://bt4gprx.com/search?q=x&p=12345", url)
        End Sub

        ''' <summary>
        ''' Verifies that the search term is URL-encoded.
        ''' </summary>
        <TestMethod>
        Public Sub BuildSearchPageUrl_EncodesSearchTerm()

            Dim url As String = ScraperHelper.BuildSearchPageUrl(UrlBuildingTests.BaseUrl, "AC/DC & Co #1", Array.Empty(Of String)(), 1)

            Assert.AreEqual("https://bt4gprx.com/search?q=AC%2FDC%20%26%20Co%20%231", url)
        End Sub

        ''' <summary>
        ''' Verifies that a trailing slash in the base URL does not produce a double slash.
        ''' </summary>
        <TestMethod>
        Public Sub BuildSearchPageUrl_BaseUrlWithTrailingSlash_HasNoDoubleSlash()

            Dim url As String = ScraperHelper.BuildSearchPageUrl("https://bt4gprx.com/", "x", Array.Empty(Of String)(), 1)

            Assert.AreEqual("https://bt4gprx.com/search?q=x", url)
        End Sub

        ''' <summary>
        ''' Verifies the argument validation of <see cref="ScraperHelper.BuildSearchPageUrl"/>.
        ''' </summary>
        <TestMethod>
        Public Sub BuildSearchPageUrl_InvalidArguments_Throw()

            Dim noParams As String() = Array.Empty(Of String)()

            Assert.ThrowsExactly(Of ArgumentException)(
                Sub() ScraperHelper.BuildSearchPageUrl("", "x", noParams, 1))
            Assert.ThrowsExactly(Of ArgumentException)(
                Sub() ScraperHelper.BuildSearchPageUrl(UrlBuildingTests.BaseUrl, " ", noParams, 1))
            Assert.ThrowsExactly(Of ArgumentNullException)(
                Sub() ScraperHelper.BuildSearchPageUrl(UrlBuildingTests.BaseUrl, "x", Nothing, 1))
            Assert.ThrowsExactly(Of ArgumentOutOfRangeException)(
                Sub() ScraperHelper.BuildSearchPageUrl(UrlBuildingTests.BaseUrl, "x", noParams, 0))
        End Sub

#End Region

#Region " CombineUrl "

        ''' <summary>
        ''' Verifies that the URL segments are joined with a single forward slash
        ''' (the previous implementation used a backslash).
        ''' </summary>
        <TestMethod>
        <DataRow("https://downloadtorrentfile.com/hash", "ABCD", "https://downloadtorrentfile.com/hash/ABCD")>
        <DataRow("https://downloadtorrentfile.com/hash/", "ABCD", "https://downloadtorrentfile.com/hash/ABCD")>
        <DataRow("https://downloadtorrentfile.com/hash", "/ABCD", "https://downloadtorrentfile.com/hash/ABCD")>
        <DataRow("https://downloadtorrentfile.com/hash\", "\ABCD", "https://downloadtorrentfile.com/hash/ABCD")>
        Public Sub CombineUrl_JoinsWithSingleForwardSlash(baseUrl As String, segment As String, expected As String)

            Assert.AreEqual(expected, ScraperHelper.CombineUrl(baseUrl, segment))
        End Sub

        ''' <summary>
        ''' Verifies the argument validation of <see cref="ScraperHelper.CombineUrl"/>.
        ''' </summary>
        <TestMethod>
        Public Sub CombineUrl_InvalidArguments_Throw()

            Assert.ThrowsExactly(Of ArgumentException)(
                Sub() ScraperHelper.CombineUrl(Nothing, "a"))
            Assert.ThrowsExactly(Of ArgumentException)(
                Sub() ScraperHelper.CombineUrl("https://a.com", ""))
        End Sub

#End Region

#Region " NormalizeMagnetPageHref "

        ''' <summary>
        ''' Verifies the normalization of the Magnet button href.
        ''' </summary>
        <TestMethod>
        <DataRow("//downloadtorrentfile.com/hash/ABCD", "https://downloadtorrentfile.com/hash/ABCD")>
        <DataRow("https://downloadtorrentfile.com/hash/ABCD", "https://downloadtorrentfile.com/hash/ABCD")>
        <DataRow("  https://downloadtorrentfile.com/hash/ABCD  ", "https://downloadtorrentfile.com/hash/ABCD")>
        <DataRow("//downloadtorrentfile.com/hash/ABCD?a=1&amp;b=2", "https://downloadtorrentfile.com/hash/ABCD?a=1&b=2")>
        Public Sub NormalizeMagnetPageHref_ReturnsAbsoluteUrl(href As String, expected As String)

            Assert.AreEqual(expected, ScraperHelper.NormalizeMagnetPageHref(href))
        End Sub

        ''' <summary>
        ''' Verifies that an empty href throws <see cref="InvalidOperationException"/>.
        ''' </summary>
        <TestMethod>
        <DataRow("")>
        <DataRow("   ")>
        <DataRow(Nothing)>
        Public Sub NormalizeMagnetPageHref_Empty_Throws(href As String)

            Assert.ThrowsExactly(Of InvalidOperationException)(
                Sub() ScraperHelper.NormalizeMagnetPageHref(href))
        End Sub

#End Region

    End Class

End Namespace
