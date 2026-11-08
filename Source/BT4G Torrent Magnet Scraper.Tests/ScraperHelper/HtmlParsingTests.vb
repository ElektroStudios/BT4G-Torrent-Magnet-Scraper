#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic

Imports BT4GTorrentMagnetScraper.Tests.Infrastructure

Imports HtmlAgilityPack

Imports Microsoft.VisualStudio.TestTools.UnitTesting

Imports HtmlDocument = HtmlAgilityPack.HtmlDocument

#End Region

Namespace BT4GTorrentMagnetScraper.Tests

    ''' <summary>
    ''' Tests for the HTML parsing functions of <see cref="ScraperHelper"/>.
    ''' </summary>
    <TestClass>
    Public NotInheritable Class HtmlParsingTests

#Region " FindTotalItemsNode / TryParseTotalItems "

        ''' <summary>
        ''' Verifies that the first badge of the result info paragraph is found and parsed.
        ''' </summary>
        <TestMethod>
        Public Sub FindTotalItemsNode_SearchPage_ReturnsFirstBadge()

            Dim doc As HtmlDocument = HtmlFixtures.Parse(HtmlFixtures.SearchResultsPage)

            Dim node As HtmlNode = ScraperHelper.FindTotalItemsNode(doc)
            Dim totalItems As Integer

            Assert.IsNotNull(node)
            Assert.IsTrue(ScraperHelper.TryParseTotalItems(node.InnerText, totalItems))
            Assert.AreEqual(1234, totalItems)
        End Sub

        ''' <summary>
        ''' Verifies that a page without the badge returns <see langword="Nothing"/>.
        ''' </summary>
        <TestMethod>
        Public Sub FindTotalItemsNode_EmptyPage_ReturnsNothing()

            Dim doc As HtmlDocument = HtmlFixtures.Parse(HtmlFixtures.EmptySearchResultsPage)

            Assert.IsNull(ScraperHelper.FindTotalItemsNode(doc))
        End Sub

        ''' <summary>
        ''' Verifies that a null document throws <see cref="ArgumentNullException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub FindTotalItemsNode_Null_Throws()

            Assert.ThrowsExactly(Of ArgumentNullException)(
                Sub() ScraperHelper.FindTotalItemsNode(Nothing))
        End Sub

        ''' <summary>
        ''' Verifies the parsing of valid badge texts.
        ''' </summary>
        <TestMethod>
        <DataRow("0", 0)>
        <DataRow("42", 42)>
        <DataRow("  1234 ", 1234)>
        <DataRow("2147483647", Integer.MaxValue)>
        Public Sub TryParseTotalItems_ValidText_ReturnsTrue(text As String, expected As Integer)

            Dim result As Integer

            Assert.IsTrue(ScraperHelper.TryParseTotalItems(text, result))
            Assert.AreEqual(expected, result)
        End Sub

        ''' <summary>
        ''' Verifies that invalid badge texts are rejected (including values that overflow an <see cref="Integer"/>,
        ''' which previously caused an <see cref="OverflowException"/>).
        ''' </summary>
        <TestMethod>
        <DataRow("")>
        <DataRow("   ")>
        <DataRow("1,234")>
        <DataRow("-5")>
        <DataRow("+5")>
        <DataRow("12a")>
        <DataRow("2147483648")>
        <DataRow("99999999999999999999")>
        <DataRow(Nothing)>
        Public Sub TryParseTotalItems_InvalidText_ReturnsFalse(text As String)

            Dim result As Integer = -1

            Assert.IsFalse(ScraperHelper.TryParseTotalItems(text, result))
            Assert.AreEqual(0, result)
        End Sub

#End Region

#Region " GetResultTitleNodes "

        ''' <summary>
        ''' Verifies that all the result title nodes of a search page are returned.
        ''' </summary>
        <TestMethod>
        Public Sub GetResultTitleNodes_SearchPage_ReturnsAllItems()

            Dim doc As HtmlDocument = HtmlFixtures.Parse(HtmlFixtures.SearchResultsPage)

            Dim nodes As List(Of HtmlNode) = ScraperHelper.GetResultTitleNodes(doc)

            Assert.AreEqual(3, nodes.Count)
        End Sub

        ''' <summary>
        ''' Verifies that a page without results returns an empty list (not <see langword="Nothing"/>).
        ''' </summary>
        <TestMethod>
        Public Sub GetResultTitleNodes_EmptyPage_ReturnsEmptyList()

            Dim doc As HtmlDocument = HtmlFixtures.Parse(HtmlFixtures.EmptySearchResultsPage)

            Dim nodes As List(Of HtmlNode) = ScraperHelper.GetResultTitleNodes(doc)

            Assert.IsNotNull(nodes)
            Assert.AreEqual(0, nodes.Count)
        End Sub

        ''' <summary>
        ''' Verifies that a null document throws <see cref="ArgumentNullException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub GetResultTitleNodes_Null_Throws()

            Assert.ThrowsExactly(Of ArgumentNullException)(
                Sub() ScraperHelper.GetResultTitleNodes(Nothing))
        End Sub

#End Region

#Region " ExtractTorrentSearchResult "

        ''' <summary>
        ''' Verifies the extraction of every item of the search page fixture.
        ''' </summary>
        <TestMethod>
        Public Sub ExtractTorrentSearchResult_SearchPage_ExtractsAllItems()

            Dim doc As HtmlDocument = HtmlFixtures.Parse(HtmlFixtures.SearchResultsPage)
            Dim nodes As List(Of HtmlNode) = ScraperHelper.GetResultTitleNodes(doc)

            Dim first As TorrentSearchResult = ScraperHelper.ExtractTorrentSearchResult(nodes(0))
            Dim second As TorrentSearchResult = ScraperHelper.ExtractTorrentSearchResult(nodes(1))
            Dim third As TorrentSearchResult = ScraperHelper.ExtractTorrentSearchResult(nodes(2))

            ' Name taken from the title attribute.
            Assert.AreEqual("John Wick (2014) 2160p UHD", first.Name)
            Assert.AreEqual("/magnet/AAAA1111BBBB2222CCCC3333DDDD4444EEEE5555", first.RelativeHref)
            Assert.AreEqual("AAAA1111BBBB2222CCCC3333DDDD4444EEEE5555", first.UrlHash)

            ' Name taken from the trimmed and de-entitized inner text.
            Assert.AreEqual("Tom & Jerry 1080p", second.Name)
            Assert.AreEqual("FFFF6666", second.UrlHash)

            ' Name taken from the de-entitized title attribute.
            Assert.AreEqual("Fast & Furious 4K", third.Name)
            Assert.AreEqual("0123ABCD", third.UrlHash)
        End Sub

        ''' <summary>
        ''' Verifies that an empty title attribute falls back to the inner text.
        ''' </summary>
        <TestMethod>
        Public Sub ExtractTorrentSearchResult_EmptyTitleAttribute_FallsBackToInnerText()

            Dim node As HtmlNode = HtmlFixtures.CreateResultTitleNode("<a href=""/magnet/ABC"" title=""  "">Inner Name</a>")

            Dim result As TorrentSearchResult = ScraperHelper.ExtractTorrentSearchResult(node)

            Assert.AreEqual("Inner Name", result.Name)
        End Sub

        ''' <summary>
        ''' Verifies that a node without a "/magnet/" link throws <see cref="InvalidOperationException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub ExtractTorrentSearchResult_NoMagnetLink_Throws()

            Dim node As HtmlNode = HtmlFixtures.CreateResultTitleNode("<a href=""/search?q=x"">Not a result</a>")

            Assert.ThrowsExactly(Of InvalidOperationException)(
                Sub() ScraperHelper.ExtractTorrentSearchResult(node))
        End Sub

        ''' <summary>
        ''' Verifies that a link without name throws <see cref="InvalidOperationException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub ExtractTorrentSearchResult_EmptyName_Throws()

            Dim node As HtmlNode = HtmlFixtures.CreateResultTitleNode("<a href=""/magnet/ABC"">   </a>")

            Assert.ThrowsExactly(Of InvalidOperationException)(
                Sub() ScraperHelper.ExtractTorrentSearchResult(node))
        End Sub

        ''' <summary>
        ''' Verifies that a link without hash ("/magnet/" only) throws <see cref="InvalidOperationException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub ExtractTorrentSearchResult_EmptyHash_Throws()

            Dim node As HtmlNode = HtmlFixtures.CreateResultTitleNode("<a href=""/magnet/"">Name</a>")

            Assert.ThrowsExactly(Of InvalidOperationException)(
                Sub() ScraperHelper.ExtractTorrentSearchResult(node))
        End Sub

        ''' <summary>
        ''' Verifies that a null node throws <see cref="ArgumentNullException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub ExtractTorrentSearchResult_Null_Throws()

            Assert.ThrowsExactly(Of ArgumentNullException)(
                Sub() ScraperHelper.ExtractTorrentSearchResult(Nothing))
        End Sub

#End Region

#Region " TryGetMagnetPageHref "

        ''' <summary>
        ''' Verifies that the "Magnet Link" button is preferred over other downloadtorrentfile.com links,
        ''' and that its href is normalized.
        ''' </summary>
        <TestMethod>
        Public Sub TryGetMagnetPageHref_PrefersMagnetButton()

            Dim doc As HtmlDocument = HtmlFixtures.Parse(HtmlFixtures.DetailPageWithMagnetButton)
            Dim href As String = Nothing

            Assert.IsTrue(ScraperHelper.TryGetMagnetPageHref(doc, href))
            Assert.AreEqual("https://downloadtorrentfile.com/hash/0123ABCD?name=Fast&x=1", href)
        End Sub

        ''' <summary>
        ''' Verifies that any downloadtorrentfile.com link is used when there is no "Magnet" button.
        ''' </summary>
        <TestMethod>
        Public Sub TryGetMagnetPageHref_UsesFallbackLink()

            Dim doc As HtmlDocument = HtmlFixtures.Parse(HtmlFixtures.DetailPageWithFallbackLink)
            Dim href As String = Nothing

            Assert.IsTrue(ScraperHelper.TryGetMagnetPageHref(doc, href))
            Assert.AreEqual("https://downloadtorrentfile.com/hash/0123ABCD", href)
        End Sub

        ''' <summary>
        ''' Verifies that a page without the button returns <see langword="False"/> and a null href.
        ''' </summary>
        <TestMethod>
        Public Sub TryGetMagnetPageHref_NoButton_ReturnsFalse()

            Dim doc As HtmlDocument = HtmlFixtures.Parse(HtmlFixtures.DetailPageWithoutButton)
            Dim href As String = "unchanged"

            Assert.IsFalse(ScraperHelper.TryGetMagnetPageHref(doc, href))
            Assert.IsNull(href)
        End Sub

        ''' <summary>
        ''' Verifies that a null document throws <see cref="ArgumentNullException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub TryGetMagnetPageHref_Null_Throws()

            Dim href As String = Nothing

            Assert.ThrowsExactly(Of ArgumentNullException)(
                Sub() ScraperHelper.TryGetMagnetPageHref(Nothing, href))
        End Sub

#End Region

    End Class

End Namespace
