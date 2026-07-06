#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Net
Imports System.Net.Http

Imports System.Diagnostics.CodeAnalysis
Imports System.Diagnostics

#End Region

#Region " Util Web "

' ReSharper disable once CheckNamespace

Namespace DevCase.Core.Networking.Common

    ''' <summary>
    ''' Contains web related utilities (http, html, url, mime, etc).
    ''' </summary>
    <SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification:="Required to migrate this code to .NET Core")>
    Public NotInheritable Class UtilWeb

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="UtilWeb"/> class from being created.
        ''' </summary>
        <DebuggerNonUserCode>
        Private Sub New()
        End Sub

#End Region

#Region " Public Methods "

        ''' <summary>
        ''' Determines whether the provided HTML source-code indicates that a Cloudflare challenge is required to access the page.
        ''' </summary>
        ''' 
        ''' <param name="pageSource">
        ''' The raw HTML source code.
        ''' </param>
        ''' 
        ''' <param name="pageTitle">
        ''' Optional. The title of the web page.
        ''' </param>
        ''' 
        ''' <see langword="True"/> if a Cloudflare challenge is required to load the web page; 
        ''' otherwise, <see langword="False"/>.
        <DebuggerStepThrough>
        Public Shared Function IsCloudflareChallengeRequired(pageSource As String, pageTitle As String) As Boolean

            If String.IsNullOrWhiteSpace(pageSource) Then
                Return False
            End If

            Dim challengeIndicators As String() = {
                "challenge-error-text",
                "/cdn-cgi/challenge-platform",
                "window._cf_chl_opt",
                "<title>Just a moment...</title>"
            }

            For Each indicator As String In challengeIndicators
#If NETCOREAPP Then
                If pageSource.Contains(indicator, StringComparison.OrdinalIgnoreCase) Then
                    Return True
                End If
#Else
                If pageSource.IndexOf(indicator, StringComparison.OrdinalIgnoreCase) >= 0 Then
                    Return True
                End If
#End If
            Next

            Return String.Equals(pageTitle, "Just a moment...", StringComparison.OrdinalIgnoreCase)
        End Function

#Region " NOT USED IN THIS PROJECT "

        '''' <summary>
        '''' Sends an HTTP request to the specified URL to determine whether 
        '''' a Cloudflare challenge is required to load the web page that points to.
        '''' </summary>
        '''' 
        '''' <param name="url">
        '''' The URL to check.
        '''' </param>
        '''' 
        '''' <returns>
        '''' <see langword="True"/> if a Cloudflare challenge is required to load the web page; 
        '''' otherwise, <see langword="False"/>.
        '''' </returns>
        '<DebuggerStepThrough>
        'Public Shared Function IsCloudflareChallengeRequired(url As String) As Boolean
        '
        '    Using handler As New HttpClientHandler() With {
        '            .AllowAutoRedirect = True,
        '            .AutomaticDecompression = DecompressionMethods.GZip Or DecompressionMethods.Deflate
        '        }

        '        Using client As New HttpClient(handler)
        '            Dim resp As HttpResponseMessage = client.GetAsync(url).ConfigureAwait(False).GetAwaiter().GetResult()
        '            Dim pageSource As String = resp.Content.ReadAsStringAsync().ConfigureAwait(False).GetAwaiter().GetResult()

        '            Return (resp.StatusCode <> HttpStatusCode.OK) AndAlso
        '                UtilWeb.IsCloudflareChallengeRequired(pageSource, pageTitle:=Nothing)
        '        End Using
        '    End Using
        'End Function

        '''' <summary>
        '''' Sends an HTTP request to the specified <see cref="Uri"/> to determine whether 
        '''' a Cloudflare challenge is required to load the web page that points to.
        '''' </summary>
        '''' 
        '''' <param name="uri">
        '''' The <see cref="Uri"/> to check.
        '''' </param>
        '''' 
        '''' <returns>
        '''' <see langword="True"/> if a Cloudflare challenge is required to load the web page; 
        '''' otherwise, <see langword="False"/>.
        '''' </returns>
        '<DebuggerStepThrough>
        'Public Shared Function IsCloudflareChallengeRequired(uri As Uri) As Boolean
        '
        '    Return UtilWeb.IsCloudflareChallengeRequired(uri.ToString())
        'End Function

#End Region

#End Region

    End Class

End Namespace

#End Region
