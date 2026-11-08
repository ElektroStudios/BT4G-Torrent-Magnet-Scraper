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
Imports System.Text
Imports System.Threading

Imports DevCase.ThirdParty.Selenium
Imports DevCase.ThirdParty.Selenium.Extensions.ChromeDriverExtensions
Imports DevCase.ThirdParty.Selenium.Extensions.IWebDriverExtensions

Imports HtmlAgilityPack

Imports OpenQA.Selenium
Imports OpenQA.Selenium.Chrome
Imports OpenQA.Selenium.Support.UI

Imports HtmlDocument = HtmlAgilityPack.HtmlDocument


#End Region

Module Program

#Region " Fields "

    ''' <summary>
    ''' The list of search terms to query on BT4G website.
    ''' </summary>
    Private searchTerms As HashSet(Of String)

    ''' <summary>
    ''' The list of keywords used to filter results. 
    ''' <para></para>
    ''' If any of these keywords are present in the torrent name, it will be processed, 
    ''' otherwise, it will be skipped.
    ''' </summary>
    Private requiredKeywords As HashSet(Of String)

    ''' <summary>
    ''' The list of keywords used to filter out unwanted results. 
    ''' <para></para>
    ''' If any of these keywords are present in the torrent name, it will be skipped.
    ''' </summary>
    Private forbiddenKeywords As HashSet(Of String)

    ''' <summary>
    ''' The list of additional parameters to append to the search URL when querying the BT4G website. 
    ''' <para></para>
    ''' For example, "orderby=seeders" to sort results by number of seeders.
    ''' </summary>
    Private queryAdditionalParameters As HashSet(Of String)

    ''' <summary>
    ''' In-memory cache of magnet page URLs that have already been processed.
    ''' <para></para>
    ''' Used to prevent duplicate processing within the current session.
    ''' </summary>
    Private processedUrlHashes As HashSet(Of String)

    ''' <summary>
    ''' The total number of wallpapers successfully saved during the current execution.
    ''' </summary>
    Private totalSavedMagnetCount As Integer

    ''' <summary>
    ''' The total number of wallpapers skipped due already downloaded or forbidden keyword matches.
    ''' </summary>
    Private totalSkippedMagnetCount As Integer

    ''' <summary>
    ''' The total number of wallpaper downloads failed.
    ''' </summary>
    Private totalFailedMagnetCount As Integer

    ''' <summary>
    ''' The encoding used for console output, 
    ''' and reading and writing CS/VB files.
    ''' <para></para>
    ''' It is set to UTF-8 with BOM (Byte Order Mark).
    ''' </summary>
    Private ReadOnly TextEncoding As New UTF8Encoding(True)

    ''' <summary>
    ''' The <see cref="CultureInfo"/> instance representing the "en-US" culture.
    ''' </summary>
    Private ReadOnly CultureInfoEnUs As New CultureInfo("en-US")

#End Region

#Region " Entry Point "

    ''' <summary>
    ''' The main entry point of the application.
    ''' </summary>
    <DebuggerStepperBoundary>
    Public Sub Main(args As String())

        Thread.CurrentThread.CurrentCulture = Program.CultureInfoEnUs
        Thread.CurrentThread.CurrentUICulture = Program.CultureInfoEnUs

        Console.OutputEncoding = Program.TextEncoding
        Console.BackgroundColor = ConsoleColor.Black
        Console.ForegroundColor = ConsoleColor.White

        Dim consoleTitle As String = AppGlobals.AppTitleAndVersion
#If DEBUG Then
        Console.Title = consoleTitle
#End If
        Program.WriteColoredLine($" {consoleTitle}", ConsoleColor.Cyan)
        Program.WriteColoredLine($" {AppGlobals.GitHUB_Repository_URL}", ConsoleColor.Cyan)
        Console.WriteLine("╭──────────────────────────────────────────────────────────────────────────────────╮")
        Console.WriteLine("│ Purpose:                                                                         │")
        Console.WriteLine("│   This application serves as an automated web scraper designed to extract        │")
        Console.WriteLine("│   magnet links from the https://bt4gprx.com/ search engine.                      │")
        Console.WriteLine("│                                                                                  │")
        Console.WriteLine("│   It programmatically queries the search engine, parses the HTML responses,      │")
        Console.WriteLine("│   and saves the magnet links into the following directory:                       │")
        Console.WriteLine("│                                                                                  │")
        Console.WriteLine("│     - <PROGRAM DIRECTORY>\Output\                                                │")
        Console.WriteLine("│                                                                                  │")
        Console.WriteLine("│ Filtering System:                                                                │")
        Console.WriteLine("│   The scraper uses three text files to control the search and filter results:    │")
        Console.WriteLine("│                                                                                  │")
        Console.WriteLine("│   - <PROGRAM DIRECTORY>\Config\Search Terms.txt                                  │")
        Console.WriteLine("│     Defines the terms to search on the search engine.                            │")
        Console.WriteLine("│                                                                                  │")
        Console.WriteLine("│   - <PROGRAM DIRECTORY>\Config\Required Keywords.txt                             │")
        Console.WriteLine("│     Magnet names must match at least one keyword to be kept.                     │")
        Console.WriteLine("│                                                                                  │")
        Console.WriteLine("│   - <PROGRAM DIRECTORY>\Config\Forbidden Keywords.txt                            │")
        Console.WriteLine("│     Magnet names matching any of these will be discarded.                        │")
        Console.WriteLine("│                                                                                  │")
        Console.WriteLine("│ [!] Disclaimer:                                                                  │")
        Console.WriteLine("│   This program is shared 'as-is', without any warranty; Use it at your own risk. │")
        Console.WriteLine("│   I'm not responsible for the content downloaded via scraped links.              │")
        Console.WriteLine("╰──────────────────────────────────────────────────────────────────────────────────╯")
        Console.WriteLine()

        Program.WriteColoredLine("Loading user configuration and cached items...", Console.ForegroundColor)
        Program.LoadConfigFiles()
        Program.WriteColoredLine($"  Loaded {Program.searchTerms.Count:N0} search terms from config file.", ConsoleColor.Green)
        Program.WriteColoredLine($"  Loaded {Program.requiredKeywords.Count:N0} required keywords from config file.", ConsoleColor.Green)
        Program.WriteColoredLine($"  Loaded {Program.forbiddenKeywords.Count:N0} forbidden keywords from config file.", ConsoleColor.Green)
        Program.WriteColoredLine($"  Loaded {Program.queryAdditionalParameters.Count:N0} custom query parameters from config file.", ConsoleColor.Green)

        Program.processedUrlHashes = ScraperHelper.LoadUrlsHistory(AppGlobals.HistoryFilePath)
        If Program.processedUrlHashes.Count > 0 Then
            Program.WriteColoredLine($"  Loaded {Program.processedUrlHashes.Count:N0} URL history hashes from history file.", ConsoleColor.Green)
        Else
            Program.WriteColoredLine($"  Loaded {Program.processedUrlHashes.Count:N0} URL history hashes from history file.", ConsoleColor.Green)
        End If
        Console.WriteLine()

#If DEBUG Then
            Program.WriteColoredLine("Press 'Y' key to start fetching magnet links, or 'Escape' key to exit...", ConsoleColor.Yellow)
            Program.WriteColoredLine("[!] This message only appears in DEBUG mode to prevent accidental execution.", ConsoleColor.Yellow)
            Do
                Dim keyInfo As ConsoleKeyInfo = Console.ReadKey(intercept:=True)
                If keyInfo.Key = ConsoleKey.Y Then
                    Exit Do
                ElseIf keyInfo.Key = ConsoleKey.Escape Then
                    Environment.Exit(0)
                End If
            Loop
            Console.WriteLine()
#End If

        Program.WriteColoredLine("Initializing Selenium...", ConsoleColor.Cyan)
        Dim seleniumEnvResult As SeleniumEnvironmentInitializationResult =
            UtilSelenium.InitializeSeleniumEnvironmentForChrome(".\cache", forceBrowserDownload:=True)

        Program.WriteColoredLine($"Chrome driver executable : {seleniumEnvResult.DriverFilePath?.Replace(My.Application.Info.DirectoryPath, ".")}", ConsoleColor.DarkCyan)
        Program.WriteColoredLine($"Chrome browser executable: {seleniumEnvResult.BrowserFilePath?.Replace(My.Application.Info.DirectoryPath, ".")}", ConsoleColor.DarkCyan)
        Program.WriteColoredLine("Selenium initialized successfully.", ConsoleColor.Green)
        Console.WriteLine()

        Dim driver As ChromeDriver = Nothing
        Program.WriteColoredLine("Launching Chrome browser...", ConsoleColor.Cyan)
        Try
            Using consoleTerminationWatcher As New ConsoleTerminationWatcher(Sub() UtilSelenium.KillDriverAndChildBrowsers("chromedriver")),
                  driverService As ChromeDriverService = Nothing

                driver = UtilSelenium.CreateOptimizedChromeDriver(driverService,
                                                                  driverFilePath:=seleniumEnvResult.DriverFilePath,
                                                                  driverLogFilePath:=Nothing,
                                                                  chromeFilePath:=seleniumEnvResult.BrowserFilePath,
                                                                  userDataDir:=AppGlobals.ChromeUserCachePath,
                                                                  profileName:="Profile1",
                                                                  headless:=False, hideNonHeadlessWindow:=False)

                Program.WriteColoredLine("Chrome browser launched successfully.", ConsoleColor.Green)
                Console.WriteLine()

                Program.CloudflareSessionSetup(driver)

                For Each searchTerm As String In Program.searchTerms

                    Program.WriteColoredLine($"--- Querying search term: '{searchTerm}' ---", ConsoleColor.White)
                    Console.WriteLine()
                    Program.ProcessSearchTerm(driver, searchTerm)
                    Console.WriteLine()
                Next searchTerm
            End Using

        Catch ex As Exception
            Console.WriteLine()
            Dim errMsg As String = If(ex.InnerException IsNot Nothing, ex.InnerException.Message, ex.Message)
            Program.ExitWithMessage($"FATAL ERROR 0x{ex.HResult:X8}: {errMsg}", exitCode:=ex.HResult, ConsoleColor.Red)

        Finally
            If driver IsNot Nothing Then
                Try
                    driver.Quit()
                    Program.WriteColoredLine("Chrome web-browser closed.", ConsoleColor.Green)
                Catch ex As Exception
                    Program.WriteColoredLine($"[WARN] Could not close Chrome web-browser: {ex.Message}", ConsoleColor.Yellow)
                End Try
            End If

        End Try

        Dim exitCode As Integer = If(Program.totalFailedMagnetCount = 0, 0, 1)
        Dim exitColor As ConsoleColor = If(Program.totalFailedMagnetCount = 0, ConsoleColor.Green, ConsoleColor.Yellow)
        Console.WriteLine()
        Program.ExitWithMessage($"All search terms have been processed. Magnet links saved: {Program.totalSavedMagnetCount:N0}, Skipped: {Program.totalSkippedMagnetCount:N0}, Failed: {Program.totalFailedMagnetCount:N0}.", exitCode, exitColor)
    End Sub

#End Region

#Region " Private Methods "

    ''' <summary>
    ''' Loads the search terms, required keywords, forbidden keywords and additional query parameters 
    ''' from the config text files into the corresponding HashSet fields.
    ''' <para></para>
    ''' Exits with error if the search terms file or the required keywords file is missing or empty.
    ''' <para></para>
    ''' The forbidden keywords file and the additional query parameters file are optional:
    ''' if missing, a warning is shown and an empty list is used.
    ''' </summary>
    Private Sub LoadConfigFiles()

        If Not File.Exists(AppGlobals.SearchTermsFilePath) Then
            Program.ExitWithMessage($"[ERROR] Search terms file not found: {AppGlobals.SearchTermsFilePath}", 1, ConsoleColor.Red)
        End If

        If Not File.Exists(AppGlobals.RequiredKeywordsFilePath) Then
            Program.ExitWithMessage($"[ERROR] Required keywords file not found: {AppGlobals.RequiredKeywordsFilePath}", 1, ConsoleColor.Red)
        End If

        Program.searchTerms =
            ScraperHelper.ParseConfigLines(File.ReadAllLines(AppGlobals.SearchTermsFilePath, Program.TextEncoding), trimLines:=False)

        Program.requiredKeywords =
            ScraperHelper.ParseConfigLines(File.ReadAllLines(AppGlobals.RequiredKeywordsFilePath, Program.TextEncoding), trimLines:=False)

        Dim forbiddenKeywordsFileExists As Boolean = File.Exists(AppGlobals.ForbiddenKeywordsFilePath)
        If forbiddenKeywordsFileExists Then
            Program.forbiddenKeywords =
                ScraperHelper.ParseConfigLines(File.ReadAllLines(AppGlobals.ForbiddenKeywordsFilePath, Program.TextEncoding), trimLines:=False)
        Else
            Program.WriteColoredLine($"[WARN] Forbidden keywords file not found: {AppGlobals.ForbiddenKeywordsFilePath}", ConsoleColor.Yellow)
            Program.forbiddenKeywords = New HashSet(Of String)(StringComparer.Ordinal)
        End If

        If File.Exists(AppGlobals.QueryAdditionalParametersFilePath) Then
            Program.queryAdditionalParameters =
                ScraperHelper.ParseConfigLines(File.ReadAllLines(AppGlobals.QueryAdditionalParametersFilePath, Program.TextEncoding), trimLines:=True)
        Else
            Program.WriteColoredLine($"[WARN] Query additional parameters file not found: {AppGlobals.QueryAdditionalParametersFilePath}", ConsoleColor.Yellow)
            Program.queryAdditionalParameters = New HashSet(Of String)(StringComparer.Ordinal)
        End If

        If Program.searchTerms.Count = 0 Then
            Program.ExitWithMessage($"[ERROR] No search terms found in file: {AppGlobals.SearchTermsFilePath}", 2, ConsoleColor.Red)
        End If

        If Program.requiredKeywords.Count = 0 Then
            Program.ExitWithMessage($"[ERROR] No required keywords found in file: {AppGlobals.RequiredKeywordsFilePath}", 2, ConsoleColor.Red)
        End If

        If forbiddenKeywordsFileExists AndAlso Program.forbiddenKeywords.Count = 0 Then
            Program.WriteColoredLine($"[WARN] No forbidden keywords found in file: {AppGlobals.ForbiddenKeywordsFilePath}", ConsoleColor.Yellow)
        End If
    End Sub

    ''' <summary>
    ''' Adds the given URL hash to the in-memory history and appends it to the history text file, 
    ''' only if it was not already present.
    ''' </summary>
    ''' 
    ''' <param name="urlHash">
    ''' The hash portion of the relative URL of the torrent's detail page on BT4G. Not the magnet link or any other URL.
    ''' </param>
    Private Sub AddUrlToHistory(urlHash As String)

        ScraperHelper.AppendUrlToHistory(AppGlobals.HistoryFilePath, Program.processedUrlHashes, urlHash, Program.TextEncoding)
    End Sub

    ''' <summary>
    ''' Navigates to predefined test URLs in order to initialize and persist
    ''' Cloudflare clearance/cookie sessions for the current ChromeDriver instance.
    ''' </summary>
    ''' 
    ''' <param name="driver">
    ''' The <see cref="ChromeDriver"/> instance used to perform navigation
    ''' and complete Cloudflare browser verification challenges.
    ''' </param>
    Private Sub CloudflareSessionSetup(driver As ChromeDriver)

        Program.WriteColoredLine("Navigating to BT4G test URL to initialize Cloudflare cookie session...", ConsoleColor.Cyan)
        SeleniumHelper.NavigateTo(driver, AppGlobals.Bt4gTestUrl)

        If driver.IsCloudflareChallengeRequired() Then
            driver.WaitToCompleteCloudflareChallenge(timeoutSeconds:=CInt(TimeSpan.FromMinutes(30).TotalSeconds))
        End If
        Program.WriteColoredLine("Completed Cloudflare challenge for BT4G test URL.", ConsoleColor.Green)
        Console.WriteLine()

        Program.WriteColoredLine("Navigating To downloadtorrentfile random URL to initialize Cloudflare cookie session...", ConsoleColor.Cyan)
        Dim randomHexString As String = ScraperHelper.GenerateRandomHexString(40)
        SeleniumHelper.NavigateTo(driver, ScraperHelper.CombineUrl(AppGlobals.DownloadTorrentFilegTestUrl, randomHexString))
        If driver.IsCloudflareChallengeRequired() Then
            driver.WaitToCompleteCloudflareChallenge(timeoutSeconds:=CInt(TimeSpan.FromMinutes(30).TotalSeconds))
        End If
        Program.WriteColoredLine("Completed Cloudflare challenge for downloadtorrentfile test URL.", ConsoleColor.Green)
        Console.WriteLine()
    End Sub

    ''' <summary>
    ''' Processes a single search term by navigating through the paginated search results, 
    ''' extracting torrent details, and saving magnet links for valid torrents.
    ''' </summary>
    ''' 
    ''' <param name="driver">
    ''' The Selenium ChromeDriver instance used for navigating web pages and interacting with the DOM.
    ''' </param>
    ''' 
    ''' <param name="searchTerm">
    ''' The search term to query on the bt4g website. This term will be URL-encoded and used to construct the search URL.
    ''' </param>
    Private Sub ProcessSearchTerm(driver As ChromeDriver,
                                  searchTerm As String)

        Dim outputFilePath As String = ScraperHelper.GetOutputFilePath(AppGlobals.OutputDirPath, searchTerm)

        Dim pageNumber As Integer = 1

        Dim totalItems As Integer = 0
        Dim currentItemIndex As Integer = 0

        Do
            Dim pageUrl As String =
                ScraperHelper.BuildSearchPageUrl(AppGlobals.BaseBt4gUrl, searchTerm, Program.queryAdditionalParameters, pageNumber)

            Program.WriteColoredLine($"  [PAGE {pageNumber:N0}] Navigating to {pageUrl}...", ConsoleColor.Cyan)
            SeleniumHelper.NavigateTo(driver, pageUrl)

            If driver.IsCloudflareChallengeRequired() Then
                Program.CloudflareSessionSetup(driver)
                SeleniumHelper.NavigateTo(driver, pageUrl)
            End If
            driver.WaitForPageReady()

            If Not driver.Url.Equals(pageUrl, StringComparison.InvariantCultureIgnoreCase) Then
                Throw New InvalidOperationException($"Current browser URL differs from expected URL ({driver.Url} -> {pageUrl})")
            End If
            Program.WriteColoredLine($"  [PAGE {pageNumber:N0}] Page loaded.", ConsoleColor.Cyan)

            ' Wait for result items OR the "Found N items" badge to appear.
            ' This is the definitive proof that CF is resolved and the real page has rendered.
            If Not Program.WaitForSearchResults(driver) Then
                Dim src As String = driver.PageSource
                If src.IndexOf("notion-list-item", StringComparison.OrdinalIgnoreCase) < 0 Then
                    Program.WriteColoredLine($"  [PAGE {pageNumber:N0}] No result-items found. Stopping pagination.", ConsoleColor.Yellow)
                    Exit Do
                End If
            End If

            Dim pageSource As String = driver.PageSource
            Dim doc As New HtmlDocument()
            doc.LoadHtml(pageSource)

            If totalItems = 0 Then
                Dim countNode As HtmlNode = ScraperHelper.FindTotalItemsNode(doc)
                If countNode IsNot Nothing Then
                    ' Verify it is a valid integer (digits only) that fits in an Int32.
                    If ScraperHelper.TryParseTotalItems(countNode.InnerText, totalItems) Then
                        Program.WriteColoredLine($"  [PAGE {pageNumber:N0}] Found {totalItems:N0} total items for search term: '{searchTerm}'", ConsoleColor.Blue)
                    End If
                Else
                    Program.WriteColoredLine("The total items element was not found on the page.", ConsoleColor.DarkRed)
                End If
            End If

            Dim resultNodes As List(Of HtmlNode) = ScraperHelper.GetResultTitleNodes(doc)

            If resultNodes.Count = 0 Then
                Program.WriteColoredLine($"  [PAGE {pageNumber:N0}] Zero items in this page after HTML parse. Stopping.", ConsoleColor.White)
                Program.WriteColoredLine($"  [PAGE {pageNumber:N0}] Waiting {AppGlobals.ThrottlePageMs \ 1000} seconds to continue...", ConsoleColor.DarkGray)
                Thread.Sleep(AppGlobals.ThrottlePageMs)
                Exit Do
            End If

            Program.WriteColoredLine($"  [PAGE {pageNumber:N0}] Found {resultNodes.Count:N0} items in this page.", ConsoleColor.Blue)
            Program.WriteColoredLine($"  [PAGE {pageNumber:N0}] Waiting {AppGlobals.ThrottlePageMs \ 1000} seconds to continue...", ConsoleColor.DarkGray)
            Thread.Sleep(AppGlobals.ThrottlePageMs)
            For Each node As HtmlNode In resultNodes
                currentItemIndex += 1
                Try
                    Program.ProcessResultNode(driver, node, outputFilePath, currentItemIndex, totalItems)
                Catch itemEx As Exception
                    Dim currentItemIndexPadded As String = ScraperHelper.FormatItemIndex(currentItemIndex, totalItems, Program.CultureInfoEnUs)

                    Program.WriteColoredLine($"  [{currentItemIndexPadded} / {totalItems:N0}] [X ITEM NODE ERROR            ] {itemEx.GetType().Name}: {itemEx.Message}", ConsoleColor.Red)
                End Try
            Next
            Console.WriteLine()

#If DEBUG Then
            Thread.CurrentThread.Join(0) ' Prevents ContextSwitchDeadlock on long-running iterations.
#End If
            pageNumber += 1
        Loop
    End Sub

    ''' <summary>
    ''' Waits until either a search result item (div.result-item) is present in the DOM 
    ''' or the "Found N items" badge (p.lead span.badge) is present, 
    ''' indicating that the search results have loaded.
    ''' </summary>
    ''' 
    ''' <param name="driver">
    ''' The Selenium ChromeDriver instance used to check for the presence of search result items or the badge in the DOM.
    ''' </param>
    ''' 
    ''' <returns>
    ''' Returns True if either a search result item or the "Found N items" badge is found within the timeout period; 
    ''' otherwise, returns False.
    ''' </returns>
    Private Function WaitForSearchResults(driver As ChromeDriver) As Boolean

        Dim wait As New WebDriverWait(driver, TimeSpan.FromSeconds(60))
        wait.IgnoreExceptionTypes(GetType(NoSuchElementException))

        Try
            wait.Until(
                Function(d As IWebDriver) As Boolean
                    Try
                        Dim hasItem As Boolean = d.FindElements(By.CssSelector("div.result-item")).Count > 0
                        Dim hasBadge As Boolean = d.FindElements(By.CssSelector("p.lead span.badge")).Count > 0
                        Dim hasBadge2 As Boolean = d.FindElements(By.CssSelector(".notion-result-info span.badge")).Count > 0
                        Return hasItem OrElse hasBadge OrElse hasBadge2
                    Catch ex As Exception
                        Return False
                    End Try
                End Function)
            Return True

        Catch ex As WebDriverTimeoutException
            Return False

        End Try
    End Function

    ''' <summary>
    ''' Processes a single search result item (div.result-item) from the search results page:
    ''' <para></para>
    ''' 1) Extract torrent name and detail page URL from the result node.
    ''' <para></para>
    ''' 2) Apply gates: check if URL is in history, if name contains forbidden keywords, if name contains required keywords.
    ''' <para></para>
    ''' 3) Navigate to the torrent detail page.
    ''' <para></para>
    ''' 4) Check if the torrent is removed due to DMCA takedown.
    ''' <para></para>
    ''' 5) If not removed, find the Magnet Link button and navigate to its href (downloadtorrentfile.com).
    ''' <para></para>
    ''' 6) Wait for the magnet URL to be populated in 'id="open"' and extract it.
    ''' <para></para>
    ''' 7) Append the torrent name and magnet URL to the output file.
    ''' </summary>
    ''' 
    ''' <param name="driver">
    ''' The Selenium ChromeDriver instance used for navigating to the torrent detail page, 
    ''' extracting the page source, and navigating to the magnet link page.
    ''' </param>
    ''' 
    ''' <param name="node">
    ''' The HtmlNode representing a single search result item (div.result-item) from the search results page. 
    ''' <para></para>
    ''' This node is expected to contain the torrent name, detail page URL, and other relevant information needed for processing.
    ''' </param>
    ''' 
    ''' <param name="outputFilePath">
    ''' The file path of the output text file where the torrent name and magnet URL 
    ''' should be appended if the torrent passes all checks and is processed successfully.
    ''' </param>
    Private Sub ProcessResultNode(driver As ChromeDriver, node As HtmlNode, outputFilePath As String,
                                  currentItemIndex As Integer, totalItems As Integer)

        Dim currentItemIndexPadded As String = ScraperHelper.FormatItemIndex(currentItemIndex, totalItems, Program.CultureInfoEnUs)

        ' ── Extract name + detail URL from the result node ────────────────
        Dim searchResult As TorrentSearchResult = ScraperHelper.ExtractTorrentSearchResult(node)
        Dim torrentName As String = searchResult.Name
        Dim relHref As String = searchResult.RelativeHref
        Dim urlHash As String = searchResult.UrlHash

        Dim shortTorrentName As String = ScraperHelper.ShortenTorrentName(torrentName)

        ' ── Gates ─────────────────────────────────────────────────────────
        If Program.processedUrlHashes.Contains(urlHash) Then
            Program.WriteColoredLine($"  [{currentItemIndexPadded} / {totalItems:N0}] [* MAGNET ALREADY IN HISTORY  ] {shortTorrentName}", ConsoleColor.DarkGray)
            Program.totalSkippedMagnetCount += 1
            Return
        End If

        If ScraperHelper.ContainsAnyKeyword(torrentName, Program.forbiddenKeywords) Then
            Program.WriteColoredLine($"  [{currentItemIndexPadded} / {totalItems:N0}] [X FORBIDDEN MATCH FOUND      ] {shortTorrentName}", ConsoleColor.DarkYellow)
            Program.totalSkippedMagnetCount += 1
            Return
        End If

        If Not ScraperHelper.ContainsAnyKeyword(torrentName, Program.requiredKeywords) Then
            Program.WriteColoredLine($"  [{currentItemIndexPadded} / {totalItems:N0}] [? NO REQUIRED KEYWORD FOUND  ] {shortTorrentName}", ConsoleColor.White)
            Program.totalSkippedMagnetCount += 1
            Return
        End If

        Program.WriteColoredLine($"  [{currentItemIndexPadded} / {totalItems:N0}] [> PROCESSING MAGNET PAGE     ] {shortTorrentName}", ConsoleColor.DarkGreen)

        ' ── Navigate to detail page ─────────────────────────
        Dim detailUrl As String = $"{AppGlobals.BaseBt4gUrl}{relHref}"
        SeleniumHelper.NavigateTo(driver, detailUrl)
        Dim detailSource As String = driver.PageSource

        ' ── DMCA check ─────────────────────────────────────────────────────
        If ScraperHelper.IsDmcaRemoved(detailSource) Then
            Program.WriteColoredLine($"  [{currentItemIndexPadded} / {totalItems:N0}] [! DMCA TAKEDOWN NOTICE       ] {shortTorrentName}", ConsoleColor.Yellow)
            Program.AddUrlToHistory(urlHash)
            Program.totalSkippedMagnetCount += 1
            Return
        End If

        ' ── Find the Magnet Link button href ──────────────────────────────
        Dim detailDoc As New HtmlDocument()
        detailDoc.LoadHtml(detailSource)

        Dim magnetPageHref As String = Nothing
        If Not ScraperHelper.TryGetMagnetPageHref(detailDoc, magnetPageHref) Then
            Program.WriteColoredLine($"  [{currentItemIndexPadded} / {totalItems:N0}] [X NO BUTTON FOUND            ] No Magnet button found: {shortTorrentName}", ConsoleColor.Red)
            Program.AddUrlToHistory(urlHash)
            Program.totalFailedMagnetCount += 1
            Return
        End If

        ' ── Navigate to downloadtorrentfile.com (Block HTML 3) ─────────────
        SeleniumHelper.NavigateTo(driver, magnetPageHref)

        ' ── Wait for JS to populate <a id="open"> with the magnet URL ──────
        Dim magnetUrl As String = Program.WaitForMagnetHref(driver)

        ScraperHelper.AppendMagnetResult(outputFilePath, torrentName, magnetUrl, Program.TextEncoding)
        Program.AddUrlToHistory(urlHash)
        Program.totalSavedMagnetCount += 1

        'Console.WriteLine($"  [- WAIT       ] Waiting {AppGlobals.ThrottleItemMs \ 1000} seconds before continuing...")
        Thread.Sleep(AppGlobals.ThrottleItemMs)
    End Sub

    ''' <summary>
    ''' Waits for the 'id="open"' element to be present in the DOM and have its "href" attribute populated with a magnet URL. 
    ''' This is used after navigating to the downloadtorrentfile.com page, where JavaScript is expected to populate the magnet link asynchronously.
    ''' </summary>
    ''' 
    ''' <param name="driver">
    ''' The Selenium ChromeDriver instance currently on the downloadtorrentfile.com page,
    ''' used to wait for the a#open element and extract its href attribute once it is populated.
    ''' </param>
    ''' 
    ''' <returns>
    ''' Returns the magnet URL extracted from the href attribute of the 'id="open"' element once it is present and populated.
    ''' </returns>
    Private Function WaitForMagnetHref(driver As ChromeDriver) As String

        Dim wait As New WebDriverWait(driver, TimeSpan.FromSeconds(60))
        wait.IgnoreExceptionTypes(
            GetType(NoSuchElementException),
            GetType(StaleElementReferenceException))

        Dim openLink As IWebElement = wait.Until(
            Function(d As IWebDriver) As IWebElement
                Dim el As IWebElement
                Try
                    el = d.FindElement(By.Id("open"))
                Catch ex As NoSuchElementException
                    Return Nothing
                End Try

                Dim hrefVal As String = el.GetAttribute("href")
                Return If(ScraperHelper.IsMagnetUri(hrefVal), el, Nothing)
            End Function)

        Dim magnetUrl As String = openLink.GetAttribute("href")
        If Not ScraperHelper.IsMagnetUri(magnetUrl) Then
            Throw New InvalidOperationException("#open href is not a magnet link after successful wait.")
        End If

        Return magnetUrl
    End Function

    ''' <summary>
    ''' Writes a message to the console in a specified foreground color, 
    ''' then resets the color back to the original.
    ''' </summary>
    ''' 
    ''' <param name="message">
    ''' The message to display. If empty or null, no message is displayed.
    ''' </param>
    ''' 
    ''' <param name="foreColor">
    ''' The console foreground color to use when displaying the message. 
    ''' <para></para>
    ''' After writing the message, the console color is reset to its original value.
    ''' </param>
    <DebuggerStepThrough>
    Private Sub WriteColoredLine(message As String, foreColor As ConsoleColor)

        Dim originalForeColor As ConsoleColor = Console.ForegroundColor
        Console.ForegroundColor = foreColor
        Console.WriteLine(message)
        Console.ForegroundColor = originalForeColor
    End Sub

    ''' <summary>
    ''' Displays a message to the console and exits the application with the specified exit code.
    ''' </summary>
    ''' 
    ''' <param name="message">
    ''' The message to display before exiting. If empty or null, no message is displayed.
    ''' </param>
    ''' 
    ''' <param name="exitCode">
    ''' The exit code to return to the operating system. Typically 0 for success, non-zero for errors.
    ''' </param>
    ''' 
    ''' <param name="foreColor">
    ''' The console foreground color to use when displaying the message. 
    ''' <para></para>
    ''' After writing the message, the console color is reset to its original value.
    ''' </param>
    <DebuggerStepThrough>
    Private Sub ExitWithMessage(message As String, exitCode As Integer, foreColor As ConsoleColor)

        If Not String.IsNullOrEmpty(message) Then
            Program.WriteColoredLine(message, foreColor)
            Console.WriteLine()
        End If

        Console.WriteLine($"Exiting application with exit code: {exitCode} (0x{exitCode:X8}) ...")
        Environment.Exit(exitCode)
    End Sub

#End Region

End Module
