#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports HtmlAgilityPack

Imports HtmlDocument = HtmlAgilityPack.HtmlDocument

#End Region

Namespace BT4GTorrentMagnetScraper.Tests.Infrastructure

    ''' <summary>
    ''' Provides minimal HTML fixtures that replicate the structure of the BT4G pages parsed by the scraper.
    ''' </summary>
    Friend Module HtmlFixtures

#Region " Constants "

        ''' <summary>
        ''' A BT4G search results page with the "Found N items" badge and three result items:
        ''' one with a title attribute, one without it (name taken from the inner text, with HTML entities),
        ''' and one whose title attribute contains HTML entities.
        ''' </summary>
        Friend Const SearchResultsPage As String =
            "<html><head><title>John Wick - BT4G</title></head><body>" &
            "<p class=""notion-result-info"">Found <span class=""badge"">1234</span> items for John Wick " &
            "in <span class=""badge"">0.012</span> seconds</p>" &
            "<div class=""notion-list-item result-item"">" &
            "  <div class=""notion-list-item-title""><h5>" &
            "    <a href=""/magnet/AAAA1111BBBB2222CCCC3333DDDD4444EEEE5555"" title=""John Wick (2014) 2160p UHD"">John Wick (2014) <b>2160p</b> UHD</a>" &
            "  </h5></div>" &
            "</div>" &
            "<div class=""notion-list-item result-item"">" &
            "  <div class=""notion-list-item-title""><h5>" &
            "    <a href=""/magnet/FFFF6666"">  Tom &amp; Jerry 1080p  </a>" &
            "  </h5></div>" &
            "</div>" &
            "<div class=""notion-list-item result-item"">" &
            "  <div class=""notion-list-item-title""><h5>" &
            "    <a href=""/magnet/0123ABCD"" title=""Fast &amp; Furious 4K"">Fast &amp; Furious 4K</a>" &
            "  </h5></div>" &
            "</div>" &
            "</body></html>"

        ''' <summary>
        ''' A BT4G search results page without results and without the "Found N items" badge.
        ''' </summary>
        Friend Const EmptySearchResultsPage As String =
            "<html><head><title>BT4G</title></head><body>" &
            "<p class=""lead"">No results.</p>" &
            "</body></html>"

        ''' <summary>
        ''' A BT4G torrent detail page that contains a generic downloadtorrentfile.com link first,
        ''' and then the preferred "Magnet Link" button (protocol-relative URL).
        ''' </summary>
        Friend Const DetailPageWithMagnetButton As String =
            "<html><body>" &
            "<a href=""https://downloadtorrentfile.com/torrent/0123ABCD"">Download Torrent</a>" &
            "<a class=""btn"" href=""//downloadtorrentfile.com/hash/0123ABCD?name=Fast&amp;x=1"">Magnet Link</a>" &
            "</body></html>"

        ''' <summary>
        ''' A BT4G torrent detail page that only contains a generic downloadtorrentfile.com link (no "Magnet" text).
        ''' </summary>
        Friend Const DetailPageWithFallbackLink As String =
            "<html><body>" &
            "<a href=""https://downloadtorrentfile.com/hash/0123ABCD"">Download</a>" &
            "</body></html>"

        ''' <summary>
        ''' A BT4G torrent detail page without any downloadtorrentfile.com link.
        ''' </summary>
        Friend Const DetailPageWithoutButton As String =
            "<html><body>" &
            "<a href=""https://bt4gprx.com/"">Home</a>" &
            "</body></html>"

#End Region

#Region " Public Methods "

        ''' <summary>
        ''' Parses the specified HTML into a new <see cref="HtmlDocument"/>.
        ''' </summary>
        '''
        ''' <param name="html">
        ''' The HTML to parse.
        ''' </param>
        '''
        ''' <returns>
        ''' The parsed <see cref="HtmlDocument"/>.
        ''' </returns>
        Friend Function Parse(html As String) As HtmlDocument

            Dim doc As New HtmlDocument()
            doc.LoadHtml(html)
            Return doc
        End Function

        ''' <summary>
        ''' Parses the specified HTML fragment and returns its first <c>div.notion-list-item-title</c> node.
        ''' </summary>
        '''
        ''' <param name="innerHtml">
        ''' The inner HTML of the result title node.
        ''' </param>
        '''
        ''' <returns>
        ''' The result title node.
        ''' </returns>
        Friend Function CreateResultTitleNode(innerHtml As String) As HtmlNode

            Dim doc As HtmlDocument = HtmlFixtures.Parse($"<div class=""notion-list-item-title"">{innerHtml}</div>")
            Return doc.DocumentNode.SelectSingleNode("//div[contains(@class,'notion-list-item-title')]")
        End Function

#End Region

    End Module

End Namespace
