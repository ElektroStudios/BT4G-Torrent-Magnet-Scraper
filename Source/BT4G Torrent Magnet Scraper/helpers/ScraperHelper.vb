#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions

Imports HtmlAgilityPack

Imports HtmlDocument = HtmlAgilityPack.HtmlDocument

#End Region

''' <summary>
''' Provides the pure (browser-independent) scraping logic of this application:
''' config and history parsing, URL building, HTML parsing of BT4G pages,
''' keyword filtering and formatting of the magnet output entries.
''' <para></para>
''' This logic is kept apart from <see cref="Program"/> so it can be unit-tested
''' without requiring a Selenium <c>ChromeDriver</c> instance or network access.
''' </summary>
Friend Module ScraperHelper

#Region " Constants "

    ''' <summary>
    ''' The relative URL prefix of the torrent detail pages on BT4G.
    ''' </summary>
    Friend Const MagnetDetailPathPrefix As String = "/magnet/"

    ''' <summary>
    ''' The URI scheme prefix of a magnet link.
    ''' </summary>
    Friend Const MagnetUriPrefix As String = "magnet:"

    ''' <summary>
    ''' The maximum length of a torrent name when displayed in the console.
    ''' <para></para>
    ''' Longer names are truncated and suffixed with an ellipsis character.
    ''' </summary>
    Friend Const MaxDisplayedTorrentNameLength As Integer = 100

    ''' <summary>
    ''' The XPath expression to find the "Found N items" badge in a BT4G search results page.
    ''' </summary>
    Friend Const TotalItemsBadgeXPath As String =
        "//p[contains(@class, 'notion-result-info')]/span[contains(@class, 'badge')][1]"

    ''' <summary>
    ''' The XPath expression to find the title node of every result item in a BT4G search results page.
    ''' </summary>
    Friend Const ResultTitleNodesXPath As String =
        "//div[contains(@class,'notion-list-item-title')]"

    ''' <summary>
    ''' The XPath expression to find the torrent detail link inside a result title node.
    ''' </summary>
    Friend Const ResultLinkXPath As String =
        ".//a[contains(@href, '/magnet/')]"

    ''' <summary>
    ''' The XPath expression to find the preferred "Magnet Link" button in a BT4G torrent detail page.
    ''' </summary>
    Friend Const MagnetButtonXPath As String =
        "//a[contains(@href,'downloadtorrentfile.com') and contains(.,'Magnet')]"

    ''' <summary>
    ''' The fallback XPath expression to find any downloadtorrentfile.com link in a BT4G torrent detail page.
    ''' </summary>
    Friend Const MagnetButtonFallbackXPath As String =
        "//a[contains(@href,'downloadtorrentfile.com')]"

    ''' <summary>
    ''' The list of public trackers appended to every magnet link written to the output files.
    ''' </summary>
    Friend Const Trackers As String =
        "&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce" &
        "&tr=udp%3A%2F%2Fopen.demonii.com%3A1337%2Fannounce" &
        "&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce" &
        "&tr=udp%3A%2F%2Fwepzone.net%3A6969%2Fannounce" &
        "&tr=udp%3A%2F%2Fudp.tracker.projectk.org%3A23333%2Fannounce" &
        "&tr=udp%3A%2F%2Ftracker.torrent.eu.org%3A451%2Fannounce" &
        "&tr=udp%3A%2F%2Ftracker.theoks.net%3A6969%2Fannounce" &
        "&tr=udp%3A%2F%2Ftracker.srv00.com%3A6969%2Fannounce" &
        "&tr=udp%3A%2F%2Ftracker.qu.ax%3A6969%2Fannounce" &
        "&tr=udp%3A%2F%2Ftracker.plx.im%3A6969%2Fannounce" &
        "&tr=udp%3A%2F%2Ftracker.gmi.gd%3A6969%2Fannounce" &
        "&tr=udp%3A%2F%2Ftracker.fnix.net%3A6969%2Fannounce" &
        "&tr=udp%3A%2F%2Ftracker.dler.org%3A6969%2Fannounce" &
        "&tr=udp%3A%2F%2Ftracker.004430.xyz%3A1337%2Fannounce" &
        "&tr=udp%3A%2F%2Ftracker-udp.gbitt.info%3A80%2Fannounce" &
        "&tr=udp%3A%2F%2Ftorrents.tmtime.dev%3A6969%2Fannounce" &
        "&tr=udp%3A%2F%2Ft.overflow.biz%3A6969%2Fannounce" &
        "&tr=udp%3A%2F%2Fmartin-gebhardt.eu%3A25%2Fannounce" &
        "&tr=udp%3A%2F%2Fipv4announce.sktorrent.eu%3A6969%2Fannounce" &
        "&tr=udp%3A%2F%2Fevan.im%3A6969%2Fannounce" &
        "&tr=https%3A%2F%2Ftracker.bt4g.com%3A443%2Fannounce"

#End Region

#Region " Fields "

    ''' <summary>
    ''' The characters that are not allowed in a Windows file name.
    ''' <para></para>
    ''' This set is fixed (instead of using <see cref="Path.GetInvalidFileNameChars()"/>)
    ''' to guarantee a deterministic result regardless of the host platform.
    ''' </summary>
    Private ReadOnly InvalidWindowsFileNameChars As HashSet(Of Char) = ScraperHelper.BuildInvalidWindowsFileNameChars()

    ''' <summary>
    ''' The reserved MS-DOS device names that cannot be used as a Windows file name (with or without extension).
    ''' </summary>
    Private ReadOnly ReservedWindowsDeviceNames As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    }

    ''' <summary>
    ''' The regular expression that validates the text of the "Found N items" badge.
    ''' </summary>
    Private ReadOnly DigitsOnlyRegex As New Regex("^\d+$", RegexOptions.CultureInvariant)

#End Region

#Region " Config And History "

    ''' <summary>
    ''' Parses the lines of a user configuration text file (search terms, keywords or query parameters).
    ''' <para></para>
    ''' Empty lines, white-space lines and comment lines (starting with '#', ignoring leading white-spaces) are skipped.
    ''' </summary>
    '''
    ''' <param name="lines">
    ''' The lines of the configuration file.
    ''' </param>
    '''
    ''' <param name="trimLines">
    ''' If <see langword="True"/>, leading and trailing white-spaces are removed from every returned line;
    ''' otherwise, the lines are returned verbatim.
    ''' </param>
    '''
    ''' <returns>
    ''' A case-sensitive <see cref="HashSet(Of String)"/> with the distinct, non-comment lines.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="lines"/> is <see langword="Nothing"/>.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function ParseConfigLines(lines As IEnumerable(Of String), trimLines As Boolean) As HashSet(Of String)

        If lines Is Nothing Then
            Throw New ArgumentNullException(NameOf(lines))
        End If

        Dim result As New HashSet(Of String)(StringComparer.Ordinal)
        For Each line As String In lines
            If String.IsNullOrWhiteSpace(line) Then
                Continue For
            End If

            Dim trimmed As String = line.Trim()
            If trimmed.StartsWith("#"c) Then
                Continue For
            End If

            result.Add(If(trimLines, trimmed, line))
        Next line

        Return result
    End Function

    ''' <summary>
    ''' Parses the lines of the URL history file.
    ''' <para></para>
    ''' Every line is trimmed; empty lines and comment lines (starting with '#') are skipped.
    ''' </summary>
    '''
    ''' <param name="lines">
    ''' The lines of the history file.
    ''' </param>
    '''
    ''' <returns>
    ''' A case-insensitive <see cref="HashSet(Of String)"/> with the distinct URL hashes.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="lines"/> is <see langword="Nothing"/>.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function ParseHistoryLines(lines As IEnumerable(Of String)) As HashSet(Of String)

        If lines Is Nothing Then
            Throw New ArgumentNullException(NameOf(lines))
        End If

        Dim result As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each line As String In lines
            If line Is Nothing Then
                Continue For
            End If

            Dim trimmed As String = line.Trim()
            If trimmed.Length <> 0 AndAlso Not trimmed.StartsWith("#"c) Then
                result.Add(trimmed)
            End If
        Next line

        Return result
    End Function

    ''' <summary>
    ''' Loads the URL history file into a case-insensitive <see cref="HashSet(Of String)"/>.
    ''' </summary>
    '''
    ''' <param name="historyFilePath">
    ''' The full path of the history file.
    ''' </param>
    '''
    ''' <returns>
    ''' The URL hashes contained in the history file,
    ''' or an empty set if the history file does not exist yet.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentException">
    ''' Thrown when <paramref name="historyFilePath"/> is null, empty or white-space.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function LoadUrlsHistory(historyFilePath As String) As HashSet(Of String)

        If String.IsNullOrWhiteSpace(historyFilePath) Then
            Throw New ArgumentException("History file path cannot be null, empty or white-space.", NameOf(historyFilePath))
        End If

        If Not File.Exists(historyFilePath) Then
            Return New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        End If

        Return ScraperHelper.ParseHistoryLines(File.ReadAllLines(historyFilePath, Encoding.UTF8))
    End Function

    ''' <summary>
    ''' Adds the specified URL hash to the in-memory history set and, only if it was not already present,
    ''' appends it as a new line to the history file (creating the file and its directory if needed).
    ''' </summary>
    '''
    ''' <param name="historyFilePath">
    ''' The full path of the history file.
    ''' </param>
    '''
    ''' <param name="history">
    ''' The in-memory history set.
    ''' </param>
    '''
    ''' <param name="urlHash">
    ''' The URL hash to add.
    ''' </param>
    '''
    ''' <param name="encoding">
    ''' The text encoding used to write the history file.
    ''' </param>
    '''
    ''' <returns>
    ''' <see langword="True"/> if the URL hash was added;
    ''' <see langword="False"/> if it was already present in the history.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentException">
    ''' Thrown when <paramref name="historyFilePath"/> or <paramref name="urlHash"/> is null, empty or white-space.
    ''' </exception>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="history"/> or <paramref name="encoding"/> is <see langword="Nothing"/>.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function AppendUrlToHistory(historyFilePath As String, history As HashSet(Of String),
                                       urlHash As String, encoding As Encoding) As Boolean

        If String.IsNullOrWhiteSpace(historyFilePath) Then
            Throw New ArgumentException("History file path cannot be null, empty or white-space.", NameOf(historyFilePath))
        End If

        If history Is Nothing Then
            Throw New ArgumentNullException(NameOf(history))
        End If

        If String.IsNullOrWhiteSpace(urlHash) Then
            Throw New ArgumentException("URL hash cannot be null, empty or white-space.", NameOf(urlHash))
        End If

        If encoding Is Nothing Then
            Throw New ArgumentNullException(NameOf(encoding))
        End If

        If Not history.Add(urlHash) Then
            Return False
        End If

        ScraperHelper.EnsureParentDirectoryExists(historyFilePath)
        File.AppendAllText(historyFilePath, $"{urlHash}{Environment.NewLine}", encoding)
        Return True
    End Function

#End Region

#Region " Filtering "

    ''' <summary>
    ''' Determines whether the specified text contains any of the specified keywords, using a case-insensitive comparison.
    ''' </summary>
    '''
    ''' <param name="text">
    ''' The text to check for keywords.
    ''' </param>
    '''
    ''' <param name="keywords">
    ''' The keywords to look for in the text.
    ''' </param>
    '''
    ''' <returns>
    ''' <see langword="True"/> if any of the keywords is present in the text; otherwise, <see langword="False"/>.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="text"/> or <paramref name="keywords"/> is <see langword="Nothing"/>.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function ContainsAnyKeyword(text As String, keywords As IEnumerable(Of String)) As Boolean

        If text Is Nothing Then
            Throw New ArgumentNullException(NameOf(text))
        End If

        If keywords Is Nothing Then
            Throw New ArgumentNullException(NameOf(keywords))
        End If

        For Each kw As String In keywords
            If Not String.IsNullOrEmpty(kw) AndAlso
               text.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0 Then
                Return True
            End If
        Next kw

        Return False
    End Function

    ''' <summary>
    ''' Determines whether the specified BT4G detail page source indicates that the torrent
    ''' has been removed due to a DMCA takedown.
    ''' </summary>
    '''
    ''' <param name="pageSource">
    ''' The HTML source of the torrent detail page.
    ''' </param>
    '''
    ''' <returns>
    ''' <see langword="True"/> if the torrent has been removed due to a DMCA takedown; otherwise, <see langword="False"/>.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="pageSource"/> is <see langword="Nothing"/>.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function IsDmcaRemoved(pageSource As String) As Boolean

        If pageSource Is Nothing Then
            Throw New ArgumentNullException(NameOf(pageSource))
        End If

        Return pageSource.IndexOf("content has been deleted", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
               (
                pageSource.IndexOf("request", StringComparison.OrdinalIgnoreCase) >= 0 AndAlso
                pageSource.IndexOf("DMCA", StringComparison.OrdinalIgnoreCase) >= 0
               )
    End Function

    ''' <summary>
    ''' Determines whether the specified value is a magnet link (i.e., it starts with "magnet:", case-insensitive).
    ''' </summary>
    '''
    ''' <param name="value">
    ''' The value to check. Can be <see langword="Nothing"/>.
    ''' </param>
    '''
    ''' <returns>
    ''' <see langword="True"/> if <paramref name="value"/> is a magnet link; otherwise, <see langword="False"/>.
    ''' </returns>
    <DebuggerStepThrough>
    Friend Function IsMagnetUri(value As String) As Boolean

        Return value IsNot Nothing AndAlso
               value.StartsWith(ScraperHelper.MagnetUriPrefix, StringComparison.OrdinalIgnoreCase)
    End Function

#End Region

#Region " URL Building "

    ''' <summary>
    ''' Builds the URL of a BT4G search results page.
    ''' </summary>
    '''
    ''' <param name="baseUrl">
    ''' The base URL of the BT4G website (e.g., "https://bt4gprx.com").
    ''' </param>
    '''
    ''' <param name="searchTerm">
    ''' The search term to query. It is URL-encoded by this function.
    ''' </param>
    '''
    ''' <param name="additionalParameters">
    ''' Additional raw query parameters (e.g., "orderby=seeders") to append to the URL. Can be empty.
    ''' </param>
    '''
    ''' <param name="pageNumber">
    ''' The 1-based number of the results page. The "p" parameter is only appended for pages greater than 1.
    ''' </param>
    '''
    ''' <returns>
    ''' The URL of the search results page.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentException">
    ''' Thrown when <paramref name="baseUrl"/> or <paramref name="searchTerm"/> is null, empty or white-space.
    ''' </exception>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="additionalParameters"/> is <see langword="Nothing"/>.
    ''' </exception>
    '''
    ''' <exception cref="ArgumentOutOfRangeException">
    ''' Thrown when <paramref name="pageNumber"/> is less than 1.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function BuildSearchPageUrl(baseUrl As String, searchTerm As String,
                                       additionalParameters As IEnumerable(Of String),
                                       pageNumber As Integer) As String

        If String.IsNullOrWhiteSpace(baseUrl) Then
            Throw New ArgumentException("Base URL cannot be null, empty or white-space.", NameOf(baseUrl))
        End If

        If String.IsNullOrWhiteSpace(searchTerm) Then
            Throw New ArgumentException("Search term cannot be null, empty or white-space.", NameOf(searchTerm))
        End If

        If additionalParameters Is Nothing Then
            Throw New ArgumentNullException(NameOf(additionalParameters))
        End If

        If pageNumber < 1 Then
            Throw New ArgumentOutOfRangeException(NameOf(pageNumber), pageNumber, "Page number must be greater than zero.")
        End If

        Dim encodedTerm As String = Uri.EscapeDataString(searchTerm)

        Dim parameters As String() = additionalParameters.ToArray()
        Dim additionalParametersFormatted As String =
            If(parameters.Length > 0, $"&{String.Join("&", parameters)}", String.Empty)

        Dim url As String = $"{baseUrl.TrimEnd("/"c)}/search?q={encodedTerm}{additionalParametersFormatted}"
        If pageNumber > 1 Then
            url = $"{url}&p={pageNumber.ToString(CultureInfo.InvariantCulture)}"
        End If

        Return url
    End Function

    ''' <summary>
    ''' Builds a URL by joining a base URL and a path segment with a single forward slash.
    ''' </summary>
    '''
    ''' <param name="baseUrl">
    ''' The base URL (e.g., "https://downloadtorrentfile.com/hash").
    ''' </param>
    '''
    ''' <param name="segment">
    ''' The path segment to append.
    ''' </param>
    '''
    ''' <returns>
    ''' The combined URL.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentException">
    ''' Thrown when <paramref name="baseUrl"/> or <paramref name="segment"/> is null, empty or white-space.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function CombineUrl(baseUrl As String, segment As String) As String

        If String.IsNullOrWhiteSpace(baseUrl) Then
            Throw New ArgumentException("Base URL cannot be null, empty or white-space.", NameOf(baseUrl))
        End If

        If String.IsNullOrWhiteSpace(segment) Then
            Throw New ArgumentException("URL segment cannot be null, empty or white-space.", NameOf(segment))
        End If

        Return $"{baseUrl.TrimEnd("/"c, "\"c)}/{segment.TrimStart("/"c, "\"c)}"
    End Function

    ''' <summary>
    ''' Normalizes the href of the "Magnet Link" button found in a BT4G detail page,
    ''' converting protocol-relative URLs (starting with "//") to absolute HTTPS URLs.
    ''' </summary>
    '''
    ''' <param name="href">
    ''' The raw href attribute value.
    ''' </param>
    '''
    ''' <returns>
    ''' The normalized absolute URL.
    ''' </returns>
    '''
    ''' <exception cref="InvalidOperationException">
    ''' Thrown when <paramref name="href"/> is null, empty or white-space.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function NormalizeMagnetPageHref(href As String) As String

        If String.IsNullOrWhiteSpace(href) Then
            Throw New InvalidOperationException("The Magnet button href is empty.")
        End If

        Dim trimmed As String = HtmlEntity.DeEntitize(href).Trim()
        If trimmed.StartsWith("//", StringComparison.Ordinal) Then
            Return $"https:{trimmed}"
        End If

        Return trimmed
    End Function

#End Region

#Region " HTML Parsing "

    ''' <summary>
    ''' Finds the "Found N items" badge node in a BT4G search results page.
    ''' </summary>
    '''
    ''' <param name="doc">
    ''' The parsed search results page.
    ''' </param>
    '''
    ''' <returns>
    ''' The badge node, or <see langword="Nothing"/> if the page does not contain it.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="doc"/> is <see langword="Nothing"/>.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function FindTotalItemsNode(doc As HtmlDocument) As HtmlNode

        If doc Is Nothing Then
            Throw New ArgumentNullException(NameOf(doc))
        End If

        Return doc.DocumentNode.SelectSingleNode(ScraperHelper.TotalItemsBadgeXPath)
    End Function

    ''' <summary>
    ''' Tries to parse the text of the "Found N items" badge as a non-negative integer.
    ''' </summary>
    '''
    ''' <param name="text">
    ''' The inner text of the badge node. Leading and trailing white-spaces are ignored.
    ''' </param>
    '''
    ''' <param name="result">
    ''' When this function returns <see langword="True"/>, receives the parsed number of items; otherwise, zero.
    ''' </param>
    '''
    ''' <returns>
    ''' <see langword="True"/> if the text is a valid number that fits in an <see cref="Integer"/>;
    ''' otherwise, <see langword="False"/>.
    ''' </returns>
    <DebuggerStepThrough>
    Friend Function TryParseTotalItems(text As String, ByRef result As Integer) As Boolean

        result = 0
        If text Is Nothing Then
            Return False
        End If

        Dim trimmed As String = text.Trim()
        If Not ScraperHelper.DigitsOnlyRegex.IsMatch(trimmed) Then
            Return False
        End If

        Return Integer.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, result)
    End Function

    ''' <summary>
    ''' Gets the title node of every result item in a BT4G search results page.
    ''' </summary>
    '''
    ''' <param name="doc">
    ''' The parsed search results page.
    ''' </param>
    '''
    ''' <returns>
    ''' The list of result title nodes. The list is empty if the page has no results.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="doc"/> is <see langword="Nothing"/>.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function GetResultTitleNodes(doc As HtmlDocument) As List(Of HtmlNode)

        If doc Is Nothing Then
            Throw New ArgumentNullException(NameOf(doc))
        End If

        ' SelectNodes returns Nothing (not an empty collection) when there are no matches.
        Dim nodes As HtmlNodeCollection = doc.DocumentNode.SelectNodes(ScraperHelper.ResultTitleNodesXPath)
        If nodes Is Nothing Then
            Return New List(Of HtmlNode)()
        End If

        Return nodes.ToList()
    End Function

    ''' <summary>
    ''' Extracts the torrent name, relative detail URL and URL hash from a single result title node.
    ''' </summary>
    '''
    ''' <param name="node">
    ''' The result title node (div.notion-list-item-title).
    ''' </param>
    '''
    ''' <returns>
    ''' The extracted <see cref="TorrentSearchResult"/>.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="node"/> is <see langword="Nothing"/>.
    ''' </exception>
    '''
    ''' <exception cref="InvalidOperationException">
    ''' Thrown when the node does not contain a "/magnet/" link,
    ''' or when the torrent name, the href or the URL hash is empty.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function ExtractTorrentSearchResult(node As HtmlNode) As TorrentSearchResult

        If node Is Nothing Then
            Throw New ArgumentNullException(NameOf(node))
        End If

        Dim linkNode As HtmlNode = node.SelectSingleNode(ScraperHelper.ResultLinkXPath)
        If linkNode Is Nothing Then
            Throw New InvalidOperationException("Could not find any <a> link containing href='/magnet/' inside the current result node.")
        End If

        ' Attribute values are returned raw by HtmlAgilityPack (e.g., "&amp;"), so they must be de-entitized.
        Dim torrentName As String = HtmlEntity.DeEntitize(linkNode.GetAttributeValue("title", String.Empty)).Trim()
        If torrentName.Length = 0 Then
            torrentName = HtmlEntity.DeEntitize(linkNode.InnerText).Trim()
        End If
        If torrentName.Length = 0 Then
            Throw New InvalidOperationException("Empty torrent name in notion-list-item-title node.")
        End If

        Dim relHref As String = HtmlEntity.DeEntitize(linkNode.GetAttributeValue("href", String.Empty)).Trim()
        If relHref.Length = 0 Then
            Throw New InvalidOperationException($"Empty href in notion-list-item-title node for: {torrentName}")
        End If

        Dim urlHash As String = relHref.Replace(ScraperHelper.MagnetDetailPathPrefix, String.Empty)
        If urlHash.Trim().Length = 0 Then
            Throw New InvalidOperationException($"Empty URL hash in href '{relHref}' for: {torrentName}")
        End If

        Return New TorrentSearchResult(torrentName, relHref, urlHash)
    End Function

    ''' <summary>
    ''' Tries to find the href of the "Magnet Link" button (pointing to downloadtorrentfile.com) in a BT4G torrent detail page.
    ''' </summary>
    '''
    ''' <param name="doc">
    ''' The parsed torrent detail page.
    ''' </param>
    '''
    ''' <param name="result">
    ''' When this function returns <see langword="True"/>, receives the normalized absolute URL of the button;
    ''' otherwise, <see langword="Nothing"/>.
    ''' </param>
    '''
    ''' <returns>
    ''' <see langword="True"/> if the button was found; otherwise, <see langword="False"/>.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="doc"/> is <see langword="Nothing"/>.
    ''' </exception>
    '''
    ''' <exception cref="InvalidOperationException">
    ''' Thrown when the button was found but its href attribute is empty.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function TryGetMagnetPageHref(doc As HtmlDocument, ByRef result As String) As Boolean

        If doc Is Nothing Then
            Throw New ArgumentNullException(NameOf(doc))
        End If

        result = Nothing
        Dim magnetBtnNode As HtmlNode =
            If(doc.DocumentNode.SelectSingleNode(ScraperHelper.MagnetButtonXPath),
               doc.DocumentNode.SelectSingleNode(ScraperHelper.MagnetButtonFallbackXPath))

        If magnetBtnNode Is Nothing Then
            Return False
        End If

        result = ScraperHelper.NormalizeMagnetPageHref(magnetBtnNode.GetAttributeValue("href", String.Empty))
        Return True
    End Function

#End Region

#Region " Output "

    ''' <summary>
    ''' Converts the specified search term into a valid Windows file name (without extension).
    ''' <para></para>
    ''' Invalid file name characters are replaced with '_', trailing dots and spaces are replaced with '_',
    ''' and reserved MS-DOS device names (CON, NUL, COM1, etc.) are prefixed with '_'.
    ''' </summary>
    '''
    ''' <param name="searchTerm">
    ''' The search term.
    ''' </param>
    '''
    ''' <returns>
    ''' A valid Windows file name.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentException">
    ''' Thrown when <paramref name="searchTerm"/> is null, empty or white-space.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function ToSafeFileName(searchTerm As String) As String

        If String.IsNullOrWhiteSpace(searchTerm) Then
            Throw New ArgumentException("Search term cannot be null, empty or white-space.", NameOf(searchTerm))
        End If

        Dim chars As Char() = searchTerm.Trim().ToCharArray()
        For i As Integer = 0 To chars.Length - 1
            If ScraperHelper.InvalidWindowsFileNameChars.Contains(chars(i)) Then
                chars(i) = "_"c
            End If
        Next i

        ' Windows silently strips trailing dots and spaces from file names.
        For i As Integer = chars.Length - 1 To 0 Step -1
            If chars(i) <> "."c AndAlso chars(i) <> " "c Then
                Exit For
            End If
            chars(i) = "_"c
        Next i

        Dim fileName As String = New String(chars)

        Dim dotIndex As Integer = fileName.IndexOf("."c)
        Dim baseName As String = If(dotIndex >= 0, fileName.Substring(0, dotIndex), fileName).TrimEnd()
        If ScraperHelper.ReservedWindowsDeviceNames.Contains(baseName) Then
            fileName = $"_{fileName}"
        End If

        Return fileName
    End Function

    ''' <summary>
    ''' Builds the full path of the output text file for the specified search term.
    ''' </summary>
    '''
    ''' <param name="outputDirPath">
    ''' The output directory path.
    ''' </param>
    '''
    ''' <param name="searchTerm">
    ''' The search term.
    ''' </param>
    '''
    ''' <returns>
    ''' The full path of the output text file.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentException">
    ''' Thrown when <paramref name="outputDirPath"/> or <paramref name="searchTerm"/> is null, empty or white-space.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function GetOutputFilePath(outputDirPath As String, searchTerm As String) As String

        If String.IsNullOrWhiteSpace(outputDirPath) Then
            Throw New ArgumentException("Output directory path cannot be null, empty or white-space.", NameOf(outputDirPath))
        End If

        Return Path.Combine(outputDirPath, $"{ScraperHelper.ToSafeFileName(searchTerm)}.txt")
    End Function

    ''' <summary>
    ''' Builds the text entry written to the output file for a single magnet link:
    ''' the torrent name, a new line, the magnet link with the <see cref="ScraperHelper.Trackers"/> appended,
    ''' and an empty separator line.
    ''' </summary>
    '''
    ''' <param name="torrentName">
    ''' The torrent name.
    ''' </param>
    '''
    ''' <param name="magnetUrl">
    ''' The magnet link.
    ''' </param>
    '''
    ''' <returns>
    ''' The output entry text.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentException">
    ''' Thrown when <paramref name="torrentName"/> is null, empty or white-space,
    ''' or when <paramref name="magnetUrl"/> is not a magnet link.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function BuildMagnetResultEntry(torrentName As String, magnetUrl As String) As String

        If String.IsNullOrWhiteSpace(torrentName) Then
            Throw New ArgumentException("Torrent name cannot be null, empty or white-space.", NameOf(torrentName))
        End If

        If Not ScraperHelper.IsMagnetUri(magnetUrl) Then
            Throw New ArgumentException($"The value is not a magnet link: '{magnetUrl}'", NameOf(magnetUrl))
        End If

        Dim nl As String = Environment.NewLine
        Return $"{torrentName}{nl}{magnetUrl}{ScraperHelper.Trackers}{nl}{nl}"
    End Function

    ''' <summary>
    ''' Appends the output entry of a magnet link to the specified output file
    ''' (creating the file and its directory if needed).
    ''' </summary>
    '''
    ''' <param name="outputFilePath">
    ''' The full path of the output file.
    ''' </param>
    '''
    ''' <param name="torrentName">
    ''' The torrent name.
    ''' </param>
    '''
    ''' <param name="magnetUrl">
    ''' The magnet link.
    ''' </param>
    '''
    ''' <param name="encoding">
    ''' The text encoding used to write the output file.
    ''' </param>
    '''
    ''' <exception cref="ArgumentException">
    ''' Thrown when <paramref name="outputFilePath"/> is null, empty or white-space.
    ''' </exception>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="encoding"/> is <see langword="Nothing"/>.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Sub AppendMagnetResult(outputFilePath As String, torrentName As String, magnetUrl As String, encoding As Encoding)

        If String.IsNullOrWhiteSpace(outputFilePath) Then
            Throw New ArgumentException("Output file path cannot be null, empty or white-space.", NameOf(outputFilePath))
        End If

        If encoding Is Nothing Then
            Throw New ArgumentNullException(NameOf(encoding))
        End If

        Dim entry As String = ScraperHelper.BuildMagnetResultEntry(torrentName, magnetUrl)

        ScraperHelper.EnsureParentDirectoryExists(outputFilePath)
        File.AppendAllText(outputFilePath, entry, encoding)
    End Sub

#End Region

#Region " Formatting "

    ''' <summary>
    ''' Truncates the specified torrent name to <see cref="ScraperHelper.MaxDisplayedTorrentNameLength"/> characters
    ''' (the last one being an ellipsis character) for console display.
    ''' </summary>
    '''
    ''' <param name="torrentName">
    ''' The torrent name.
    ''' </param>
    '''
    ''' <returns>
    ''' The original torrent name if it does not exceed the maximum length; otherwise, the truncated name.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="torrentName"/> is <see langword="Nothing"/>.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function ShortenTorrentName(torrentName As String) As String

        If torrentName Is Nothing Then
            Throw New ArgumentNullException(NameOf(torrentName))
        End If

        Dim maxLength As Integer = ScraperHelper.MaxDisplayedTorrentNameLength
        Return If(torrentName.Length > maxLength,
                  $"{torrentName.Substring(0, maxLength - 1)}…",
                  torrentName)
    End Function

    ''' <summary>
    ''' Formats the 1-based index of the current result item, left-padded with spaces
    ''' to the width of the formatted total items count, so the console output stays aligned.
    ''' </summary>
    '''
    ''' <param name="currentItemIndex">
    ''' The 1-based index of the current item.
    ''' </param>
    '''
    ''' <param name="totalItems">
    ''' The total number of items.
    ''' </param>
    '''
    ''' <param name="provider">
    ''' The format provider used to format both numbers (with the "N0" format).
    ''' </param>
    '''
    ''' <returns>
    ''' The padded item index.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="provider"/> is <see langword="Nothing"/>.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function FormatItemIndex(currentItemIndex As Integer, totalItems As Integer, provider As IFormatProvider) As String

        If provider Is Nothing Then
            Throw New ArgumentNullException(NameOf(provider))
        End If

        Dim padding As Integer = totalItems.ToString("N0", provider).Length
        Return currentItemIndex.ToString("N0", provider).PadLeft(padding, " "c)
    End Function

    ''' <summary>
    ''' Generates a cryptographically random uppercase hexadecimal string of the specified length.
    ''' </summary>
    '''
    ''' <param name="length">
    ''' The length of the string. It must be a positive even number.
    ''' </param>
    '''
    ''' <returns>
    ''' The random hexadecimal string.
    ''' </returns>
    '''
    ''' <exception cref="ArgumentOutOfRangeException">
    ''' Thrown when <paramref name="length"/> is less than or equal to zero.
    ''' </exception>
    '''
    ''' <exception cref="ArgumentException">
    ''' Thrown when <paramref name="length"/> is not an even number.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function GenerateRandomHexString(length As Integer) As String

        If length <= 0 Then
            Throw New ArgumentOutOfRangeException(NameOf(length))
        End If

        If (length Mod 2) <> 0 Then
            Throw New ArgumentException("HEX string length must be even.", NameOf(length))
        End If

        Dim byteCount As Integer = length \ 2
        Dim randomBytes(byteCount - 1) As Byte

        Using rng As RandomNumberGenerator = RandomNumberGenerator.Create()
            rng.GetBytes(randomBytes)
        End Using

        Dim builder As New StringBuilder(length)
        For Each currentByte As Byte In randomBytes
            builder.Append(currentByte.ToString("X2", CultureInfo.InvariantCulture))
        Next currentByte

        Return builder.ToString()
    End Function

#End Region

#Region " Private Methods "

    ''' <summary>
    ''' Creates the parent directory of the specified file path if it does not exist.
    ''' </summary>
    '''
    ''' <param name="filePath">
    ''' The full file path.
    ''' </param>
    '''
    ''' <exception cref="InvalidOperationException">
    ''' Thrown when the parent directory of <paramref name="filePath"/> cannot be determined.
    ''' </exception>
    <DebuggerStepThrough>
    Private Sub EnsureParentDirectoryExists(filePath As String)

        Dim dirPath As String = Path.GetDirectoryName(Path.GetFullPath(filePath))
        If String.IsNullOrEmpty(dirPath) Then
            Throw New InvalidOperationException($"Cannot determine the parent directory of file path: {filePath}")
        End If

        If Not Directory.Exists(dirPath) Then
            Directory.CreateDirectory(dirPath)
        End If
    End Sub

    ''' <summary>
    ''' Builds the set of characters that are not allowed in a Windows file name:
    ''' the control characters (0-31) and <c>&lt; &gt; : " / \ | ? *</c>.
    ''' </summary>
    '''
    ''' <returns>
    ''' The set of invalid Windows file name characters.
    ''' </returns>
    <DebuggerStepThrough>
    Private Function BuildInvalidWindowsFileNameChars() As HashSet(Of Char)

        Dim result As New HashSet(Of Char)("<>:""/\|?*".ToCharArray())
        For code As Integer = 0 To 31
            result.Add(ChrW(code))
        Next code

        Return result
    End Function

#End Region

End Module
