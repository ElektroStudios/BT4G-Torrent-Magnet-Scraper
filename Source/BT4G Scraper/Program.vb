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
        Program.WriteColoredLine(" " & consoleTitle, ConsoleColor.Cyan)
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

        Program.processedUrlHashes = Program.LoadUrlsHistory()
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
    ''' Loads the search terms, required keywords and forbidden keywords from the config text files into the corresponding HashSet fields.
    ''' <para></para>
    ''' Exits with error if any file is missing or empty.
    ''' </summary>
    Private Sub LoadConfigFiles()

        Dim searchTermsFileExists As Boolean = File.Exists(AppGlobals.SearchTermsFilePath)
        Dim requiredKeywordsFileExists As Boolean = File.Exists(AppGlobals.RequiredKeywordsFilePath)
        Dim forbiddenKeywordsFileExists As Boolean = File.Exists(AppGlobals.ForbiddenKeywordsFilePath)

        If Not searchTermsFileExists Then
            Program.ExitWithMessage($"[ERROR] Search terms file not found: {AppGlobals.SearchTermsFilePath}", 1, ConsoleColor.Red)

        ElseIf Not requiredKeywordsFileExists Then
            Program.ExitWithMessage($"[ERROR] Required keywords file not found: {AppGlobals.RequiredKeywordsFilePath}", 1, ConsoleColor.Red)

        ElseIf Not forbiddenKeywordsFileExists Then
            Program.WriteColoredLine($"[WARN] Forbidden keywords file not found: {AppGlobals.ForbiddenKeywordsFilePath}", ConsoleColor.Yellow)

        End If

        Program.searchTerms =
            (From line In File.ReadAllLines(AppGlobals.SearchTermsFilePath, Program.TextEncoding)
             Where Not String.IsNullOrWhiteSpace(line) AndAlso Not line.Trim().StartsWith("#"c)
            ).ToHashSet()

        Program.requiredKeywords =
            (From line In File.ReadAllLines(AppGlobals.RequiredKeywordsFilePath, Program.TextEncoding)
             Where Not String.IsNullOrWhiteSpace(line) AndAlso Not line.Trim().StartsWith("#"c)
            ).ToHashSet()

        Program.forbiddenKeywords =
            (From line As String In File.ReadAllLines(AppGlobals.ForbiddenKeywordsFilePath, Program.TextEncoding)
             Where Not String.IsNullOrWhiteSpace(line) AndAlso Not line.Trim().StartsWith("#"c)
            ).ToHashSet()

        Program.queryAdditionalParameters =
            (From line As String In File.ReadAllLines(AppGlobals.QueryAdditionalParametersFilePath, Program.TextEncoding)
             Let trimmed As String = line.Trim()
             Where Not String.IsNullOrWhiteSpace(trimmed) AndAlso Not trimmed.StartsWith("#"c)
             Select trimmed
            ).ToHashSet()

        If Program.searchTerms Is Nothing OrElse Program.searchTerms.Count = 0 Then
            Program.ExitWithMessage($"[ERROR] No search terms found in file: {AppGlobals.SearchTermsFilePath}", 2, ConsoleColor.Red)

        ElseIf Program.requiredKeywords Is Nothing OrElse Program.RequiredKeywords.Count = 0 Then
            Program.ExitWithMessage($"[ERROR] No required keywords found in file: {AppGlobals.RequiredKeywordsFilePath}", 2, ConsoleColor.Red)

        ElseIf forbiddenKeywordsFileExists AndAlso Program.ForbiddenKeywords Is Nothing OrElse Program.ForbiddenKeywords.Count = 0 Then
            Program.WriteColoredLine($"[WARN] No forbidden keywords found in file: {AppGlobals.ForbiddenKeywordsFilePath}", ConsoleColor.Yellow)

        End If
    End Sub

    ''' <summary>
    ''' Loads the history of already-processed detail page URLs from the history text file into a HashSet for quick lookup.
    ''' </summary>
    ''' 
    ''' <returns>
    ''' Returns a HashSet containing the URLs of detail pages that have already been processed, loaded from the history text file.
    ''' </returns>
    Private Function LoadUrlsHistory() As HashSet(Of String)

        Dim result As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        If Not File.Exists(AppGlobals.HistoryFilePath) Then
            Return result
        End If

        Dim lines As String() = File.ReadAllLines(AppGlobals.HistoryFilePath, Encoding.UTF8)
        For Each line As String In lines
            Dim trimmed As String = line.Trim()
            If Not String.IsNullOrWhiteSpace(trimmed) AndAlso Not trimmed.StartsWith("#"c) Then
                result.Add(trimmed)
            End If
        Next

        Return result
    End Function

    ''' <summary>
    ''' Adds the given URL to the history HashSet and appends it to the history text file if it
    ''' </summary>
    ''' 
    ''' <param name="relHref">
    ''' The relative URL of the torrent's detail page on bt4g. Not the magnet link or any other URL.
    ''' </param>
    Private Sub AddUrlToHistory(relHref As String)

        If Program.processedUrlHashes.Add(relHref) Then
            File.AppendAllText(AppGlobals.HistoryFilePath, relHref & Environment.NewLine, Program.TextEncoding)
        End If
    End Sub

    ''' <summary>
    ''' Appends the given torrent name and magnet URL to the output text file in a structured format, 
    ''' along with a predefined list of trackers.
    ''' </summary>
    ''' 
    ''' <param name="torrentName">
    ''' The name of the torrent to be saved in the output file.
    ''' </param>
    ''' 
    ''' <param name="magnetUrl">
    ''' The magnet URL of the torrent to be saved in the output file.
    ''' </param>
    ''' 
    ''' <param name="outputFilePath">
    ''' The file path of the output text file where the torrent name and magnet URL should be appended.
    ''' </param>
    Private Sub WriteMagnetTorrentResult(torrentName As String, magnetUrl As String, outputFilePath As String)

        Const trackers As String =
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

        Dim line As String = $"{torrentName}{Environment.NewLine}{magnetUrl}{trackers}"

        If Not Directory.Exists(AppGlobals.OutputDirPath) Then
            Directory.CreateDirectory(AppGlobals.OutputDirPath)
        End If
        File.AppendAllText(outputFilePath, line & Environment.NewLine & Environment.NewLine, Program.TextEncoding)
        ' Program.WriteColoredLine($"  [MAGNET SAVED ] {torrentName}",ConsoleColor.Green)
    End Sub

    ''' <summary>
    ''' Checks if the given text contains any of the keywords in the provided set, using case-insensitive comparison.
    ''' </summary>
    ''' 
    ''' <param name="text">
    ''' The text to check for keywords.
    ''' </param>
    ''' 
    ''' <param name="keywords">
    ''' The set of keywords to look for in the text. If any keyword is found, the function returns True.
    ''' </param>
    ''' 
    ''' <returns>
    ''' Returns True if any of the keywords are present in the text; 
    ''' otherwise, returns False.
    ''' </returns>
    Private Function ContainsAnyKeyword(text As String, keywords As HashSet(Of String)) As Boolean

        For Each kw As String In keywords
            If text.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0 Then
                Return True
            End If
        Next kw

        Return False
    End Function

    ''' <summary>
    ''' Determines if the given page source indicates that the torrent has been removed due to a DMCA takedown.
    ''' </summary>
    ''' <param name="pageSource"></param>
    ''' 
    ''' <returns>
    ''' Returns True if the page source indicates that the torrent has been removed due to a DMCA takedown; 
    ''' otherwise, returns False.
    ''' </returns>
    Private Function IsDmcaRemoved(pageSource As String) As Boolean

        Return pageSource.IndexOf("content has been deleted", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
               (
                pageSource.IndexOf("request", StringComparison.OrdinalIgnoreCase) >= 0 AndAlso
                pageSource.IndexOf("DMCA", StringComparison.OrdinalIgnoreCase) >= 0
               )
    End Function

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
            driver.WaitToCompleteCloudflareChallenge(timeoutSeconds:=CInt(TimeSpan.FromMinutes(30).TotalMilliseconds))
        End If
        Program.WriteColoredLine("Completed Cloudflare challenge for BT4G test URL.", ConsoleColor.Green)
        Console.WriteLine()

        Program.WriteColoredLine("Navigating To downloadtorrentfile random URL to initialize Cloudflare cookie session...", ConsoleColor.Cyan)
        Dim randomHexString As String = Program.GenerateRandomHexString(40)
        SeleniumHelper.NavigateTo(driver, $"{AppGlobals.DownloadTorrentFilegTestUrl}\{randomHexString}")
        If driver.IsCloudflareChallengeRequired() Then
            driver.WaitToCompleteCloudflareChallenge(timeoutSeconds:=CInt(TimeSpan.FromMinutes(30).TotalMilliseconds))
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

        Dim outputFilePath As String = $"{AppGlobals.OutputDirPath}\{searchTerm}.txt"

        Dim encodedTerm As String = Uri.EscapeDataString(searchTerm)
        Dim pageNumber As Integer = 1

        Dim totalItems As Integer = 0
        Dim currentItemIndex As Integer = 0

        Dim additionalParametersFormatted As String =
            If(Program.queryAdditionalParameters IsNot Nothing AndAlso Program.queryAdditionalParameters.Count > 0,
                "&" & String.Join("&", Program.queryAdditionalParameters), String.Empty)

        Do
            Dim pageUrl As String =
                If(pageNumber = 1,
                    $"{AppGlobals.BaseBt4gUrl}/search?q={encodedTerm}{additionalParametersFormatted}",
                    $"{AppGlobals.BaseBt4gUrl}/search?q={encodedTerm}{additionalParametersFormatted}&p={pageNumber}")

            Program.WriteColoredLine($"  [PAGE {pageNumber:N0}] Navigating to {pageUrl}...", ConsoleColor.Cyan)
            SeleniumHelper.NavigateTo(driver, pageUrl)

            If driver.IsCloudflareChallengeRequired() Then
                Program.CloudflareSessionSetup(driver)
                SeleniumHelper.NavigateTo(driver, pageUrl)
            End If
            driver.WaitForPageReady()

            If Not driver.Url.Equals(pageUrl, StringComparison.InvariantCultureIgnoreCase) Then
                Throw New Exception("Current browser URL differs from expected URL " & $" ({driver.Url} -> {pageUrl})")
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
                Dim countNode As HtmlNode = doc.DocumentNode.SelectSingleNode("//p[contains(@class, 'notion-result-info')]/span[contains(@class, 'badge')][1]")
                If countNode IsNot Nothing Then
                    Dim nodeText As String = countNode.InnerText.Trim()

                    ' Clean any unexpected character and verify it is a valid integer
                    If Regex.IsMatch(nodeText, "^\d+$") Then
                        totalItems = Convert.ToInt32(nodeText)
                        Program.WriteColoredLine($"  [PAGE {pageNumber:N0}] Found {totalItems:N0} total items for search term: '{searchTerm}'", ConsoleColor.Blue)
                    End If
                Else
                    Program.WriteColoredLine("The total items element was not found on the page.", ConsoleColor.DarkRed)
                End If
            End If

            Dim resultNodes As HtmlNodeCollection =
                doc.DocumentNode.SelectNodes("//div[contains(@class,'notion-list-item-title')]")

            If resultNodes Is Nothing OrElse resultNodes.Count = 0 Then
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
                    Program.ProcessResultNode(driver, node, OutputFilePath, currentItemIndex, totalItems)
                Catch itemEx As Exception
                    Dim currentItemIndexPadding As Integer = totalItems.ToString().Length
                    Dim currentItemIndexPadded As String = $"{currentItemIndex.ToString("N0").PadLeft(currentItemIndexPadding, " "c):N0}"

                    Program.WriteColoredLine($"  [{currentItemIndexPadded:N0} / {totalItems:N0}] [X ITEM NODE ERROR            ] {itemEx.GetType().Name}: {itemEx.Message}", ConsoleColor.Red)
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

        Dim currentItemIndexPadding As Integer = totalItems.ToString().Length
        Dim currentItemIndexPadded As String = $"{currentItemIndex.ToString("N0").PadLeft(currentItemIndexPadding, " "c):N0}"

        ' ── Extract name + detail URL from Block HTML 1 ───────────────────
        Dim linkNode As HtmlNode = node.SelectSingleNode(".//a[contains(@href, '/magnet/')]")
        If linkNode Is Nothing Then
            Throw New InvalidOperationException("Could not find any <a> link containing href='/magnet/' inside the current result node.")
        End If

        Dim torrentName As String = linkNode.GetAttributeValue("title", String.Empty)
        If torrentName.Length = 0 Then
            torrentName = HtmlEntity.DeEntitize(linkNode.InnerText).Trim()
        End If
        If torrentName.Length = 0 Then
            Throw New InvalidOperationException("Empty torrent name in notion-list-item-title node.")
        End If

        Dim relHref As String = linkNode.GetAttributeValue("href", String.Empty)
        If relHref.Length = 0 Then
            Throw New InvalidOperationException($"Empty href in notion-list-item-title node for: {torrentName}")
        End If

        Dim urlHash As String = relHref.Replace("/magnet/", "")

        Dim shortTorrentName As String = If(torrentName.Length > 100, $"{torrentName.Substring(0, 99)}…", torrentName)

        ' ── Gates ─────────────────────────────────────────────────────────
        If Program.processedUrlHashes.Contains(urlHash) Then
            Program.WriteColoredLine($"  [{currentItemIndexPadded:N0} / {totalItems:N0}] [* MAGNET ALREADY IN HISTORY  ] {shortTorrentName}", ConsoleColor.DarkGray)
            Program.totalSkippedMagnetCount += 1
            Return
        End If

        If Program.ContainsAnyKeyword(torrentName, ForbiddenKeywords) Then
            Program.WriteColoredLine($"  [{currentItemIndexPadded:N0} / {totalItems:N0}] [X FORBIDDEN MATCH FOUND      ] {shortTorrentName}", ConsoleColor.DarkYellow)
            Program.totalSkippedMagnetCount += 1
            Return
        End If

        If Not Program.ContainsAnyKeyword(torrentName, RequiredKeywords) Then
            Program.WriteColoredLine($"  [{currentItemIndexPadded:N0} / {totalItems:N0}] [? NO REQUIRED KEYWORD FOUND  ] {shortTorrentName}", ConsoleColor.White)
            Program.totalSkippedMagnetCount += 1
            Return
        End If

        Program.WriteColoredLine($"  [{currentItemIndexPadded:N0} / {totalItems:N0}] [> PROCESSING MAGNET PAGE     ] {shortTorrentName}", ConsoleColor.DarkGreen)

        ' ── Navigate to detail page ─────────────────────────
        Dim detailUrl As String = $"{AppGlobals.BaseBt4gUrl}{relHref}"
        SeleniumHelper.NavigateTo(driver, detailUrl)
        Dim detailSource As String = driver.PageSource

        ' ── DMCA check ─────────────────────────────────────────────────────
        If Program.IsDmcaRemoved(detailSource) Then
            Program.WriteColoredLine($"  [{currentItemIndexPadded:N0} / {totalItems:N0}] [! DMCA TAKEDOWN NOTICE       ] {shortTorrentName}", ConsoleColor.Yellow)
            Program.AddUrlToHistory(urlHash)
            Program.totalSkippedMagnetCount += 1
            Return
        End If

        ' ── Find the Magnet Link button href ──────────────────────────────
        Dim detailDoc As New HtmlDocument()
        detailDoc.LoadHtml(detailSource)

        Dim magnetBtnNode As HtmlNode = If(detailDoc.DocumentNode.SelectSingleNode(
            "//a[contains(@href,'downloadtorrentfile.com') and contains(.,'Magnet')]"), detailDoc.DocumentNode.SelectSingleNode(
                "//a[contains(@href,'downloadtorrentfile.com')]"))

        If magnetBtnNode Is Nothing Then
            Program.WriteColoredLine($"  [{currentItemIndexPadded:N0} / {totalItems:N0}] [X NO BUTTON FOUND            ] No Magnet button found: {shortTorrentName}", ConsoleColor.Red)
            Program.AddUrlToHistory(urlHash)
            Program.totalFailedMagnetCount += 1
            Return
        End If

        Dim magnetPageHref As String = magnetBtnNode.GetAttributeValue("href", String.Empty)

        If magnetPageHref.Length = 0 Then
            Throw New InvalidOperationException($"Magnet button href empty for: {shortTorrentName}")
        End If

        If magnetPageHref.StartsWith("//", StringComparison.Ordinal) Then
            magnetPageHref = $"https:{magnetPageHref}"
        End If

        ' ── Navigate to downloadtorrentfile.com (Block HTML 3) ─────────────
        SeleniumHelper.NavigateTo(driver, magnetPageHref)

        ' ── Wait for JS to populate <a id="open"> with the magnet URL ──────
        Dim magnetUrl As String = Program.WaitForMagnetHref(driver)

        Program.WriteMagnetTorrentResult(torrentName, magnetUrl, outputFilePath)
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
                Return If(hrefVal IsNot Nothing AndAlso hrefVal.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase), el, Nothing)
            End Function)

        Dim magnetUrl As String = openLink.GetAttribute("href")
        If magnetUrl Is Nothing OrElse magnetUrl.Length = 0 Then
            Throw New InvalidOperationException("#open href is null/empty after successful wait.")
        End If

        Return magnetUrl
    End Function

    Private Function GenerateRandomHexString(length As Integer) As String

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
            builder.Append(currentByte.ToString("X2"))
        Next

        Return builder.ToString()
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
