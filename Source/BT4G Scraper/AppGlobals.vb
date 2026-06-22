#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.IO

#End Region

''' <summary>
''' Provides global constants, read-only fields and properties for this application.
''' </summary>
Public Module AppGlobals

    ''' <summary>
    ''' The title of the application.
    ''' </summary>
    Friend ReadOnly AppTitleAndVersion As String =
        $"{My.Application.Info.Title} {My.Application.Info.Version.ToString(fieldCount:=3)} — by ElektroStudios"

    ''' <summary>
    ''' The file path where to save the search history, i.e., the list of torrent names that have already been processed.
    ''' </summary>
    Friend ReadOnly HistoryFilePath As String =
        Path.Combine(My.Application.Info.DirectoryPath, "cache\BT4G History.txt")

    ''' <summary>
    ''' Directory path where to save the Chrome cache data.
    ''' </summary>
    Friend ReadOnly ChromeUserCachePath As String =
        Path.Combine(My.Application.Info.DirectoryPath, "cache\chrome user data")

    ''' <summary>
    ''' The file path from which to load the user-configured search terms list.
    ''' </summary>
    Friend ReadOnly SearchTermsFilePath As String =
        Path.Combine(My.Application.Info.DirectoryPath, "config\search terms.txt")

    ''' <summary>
    ''' The file path from which to load the user-configured required keywords list.
    ''' </summary>
    Friend ReadOnly RequiredKeywordsFilePath As String =
        Path.Combine(My.Application.Info.DirectoryPath, "config\required keywords.txt")

    ''' <summary>
    ''' The file path from which to load the user-configured forbidden keywords list.
    ''' </summary>
    Friend ReadOnly ForbiddenKeywordsFilePath As String =
        Path.Combine(My.Application.Info.DirectoryPath, "config\forbidden keywords.txt")

    ''' <summary>
    ''' The file path from which to load the user-configured additional query parameters list.
    ''' </summary>
    Friend ReadOnly QueryAdditionalParametersFilePath As String =
        Path.Combine(My.Application.Info.DirectoryPath, "config\query additional parameters.txt")

    ''' <summary>
    ''' The base directory where to save the output files containing the magnet URLs.
    ''' </summary>
    Friend ReadOnly OutputDirPath As String =
        Path.Combine(My.Application.Info.DirectoryPath, "output")

    ''' <summary>
    ''' The base URL of the BT4G website.
    ''' </summary>
    Friend Const BaseBt4gUrl As String = "https://bt4gprx.com"

    ''' <summary>
    ''' The URL of the BT4G website to test Cloudflare challenge.
    ''' </summary>
    Friend Const Bt4gTestUrl As String = "https://bt4gprx.com/search?q=Test"

    ''' <summary>
    ''' The URL of the downloadtorrentfile website to test Cloudflare challenge.
    ''' </summary>
    Friend Const DownloadTorrentFilegTestUrl As String =
        "https://downloadtorrentfile.com/hash"

    ''' <summary>
    ''' Milliseconds to sleep between two consecutive processing items.
    ''' </summary>
    Friend Const ThrottleItemMs As Integer = 100 ' milliseconds

    ''' <summary>
    ''' Milliseconds to sleep before requesting the next search page.
    ''' </summary>
    Friend Const ThrottlePageMs As Integer = 3000 ' milliseconds

End Module
