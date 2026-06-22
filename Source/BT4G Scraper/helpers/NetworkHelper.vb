#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Diagnostics
Imports System.Net
Imports System.Net.Http
Imports System.Net.NetworkInformation
Imports System.Threading.Tasks

#End Region

''' <summary>
''' Provides helper members related to network operations.
''' </summary>
Public Module NetworkHelper

#Region " Properties "

    ''' <summary>
    ''' Gets a value indicating whether at least one network adapter in the current computer is capable of connecting to Internet.
    ''' </summary>
    '''
    ''' <value>
    ''' <see langword="True"/> if at least one network adapter in the current computer is capable of connecting to Internet; 
    ''' otherwise, <see langword="False"/>.
    ''' </value>
    Public ReadOnly Property IsNetworkAvailable As Boolean
        <DebuggerStepThrough>
        Get
            Return NetworkHelper.Internal_IsNetworkAvailable() AndAlso
                   NetworkHelper.NetworkAdapterCanReachConnectivityTestEndpoints(timeout:=5000)
        End Get
    End Property

#End Region

#Region " Public Methods "

    ''' <summary>
    ''' Attempts to connect to a predefined list of well-known connectivity test endpoints 
    ''' (Google, Microsoft, Apple, Cloudflare, etc.) to determine whether the current 
    ''' network adapter can successfully reach at least one of them.
    ''' </summary>
    ''' 
    ''' <param name="timeout">
    ''' The maximum time, in milliseconds, to wait before the http request times out.
    ''' <para></para>
    ''' The default value is <c>5000</c> (5 seconds).
    ''' </param>
    ''' 
    ''' <returns>
    ''' <see langword="True"/> if at least one endpoint responds successfully; otherwise, <see langword="False"/>.
    ''' </returns>
    <DebuggerStepThrough>
    Public Function NetworkAdapterCanReachConnectivityTestEndpoints(Optional timeout As Integer = 3000) As Boolean

        Dim endpoints As String() = {
            "http://clients3.google.com/generate_204",
            "http://www.google.com/gen_204",
            "http://connectivitycheck.gstatic.com/generate_204",
            "http://www.msftconnecttest.com/connecttest.txt",
            "http://www.msftncsi.com/ncsi.txt",
            "http://www.apple.com/library/test/success.html",
            "http://captive.apple.com/hotspot-detect.html",
            "http://1.1.1.1/cdn-cgi/trace",
            "http://cloudflare.com/cdn-cgi/trace",
            "http://checkip.amazonaws.com",
            "http://detectportal.firefox.com/success.txt"
        }

        For Each url As String In endpoints

            Try
                Using client As New HttpClient()
                    client.Timeout = TimeSpan.FromMilliseconds(timeout)

                    Using response As HttpResponseMessage = client.GetAsync(url).GetAwaiter().GetResult()
                        If response.IsSuccessStatusCode Then
                            Return True
                        End If
                    End Using
                End Using
            Catch
                ' Ignore and continue with the next endpoint.
            End Try
        Next

        Return False
    End Function

    '''' <summary>
    '''' Determines whether the specified URL exists.
    '''' </summary>
    '''' 
    '''' <param name="url">
    '''' The target <see cref="Uri"/> to check.
    '''' </param>
    '''' 
    '''' <returns>
    '''' <see langword="True"/> if the specified URL exists; otherwise, <c>False</c>.
    '''' </returns>
    '<DebuggerStepThrough>
    'Public Function UrlExists(ByVal url As Uri) As Boolean
    '    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 Or SecurityProtocolType.Tls13

    '    Dim request As HttpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
    '    request.Method = "HEAD"
    '    request.Timeout = 10000

    '    Try
    '        Using response As HttpWebResponse = CType(request.GetResponse(), HttpWebResponse)
    '            Return response.StatusCode <> HttpStatusCode.NotFound
    '        End Using
    '    Catch ex As WebException
    '        If ex.Response IsNot Nothing Then
    '            Dim errorResponse As HttpWebResponse = CType(ex.Response, HttpWebResponse)
    '            If errorResponse.StatusCode = HttpStatusCode.NotFound Then
    '                Return False
    '            End If
    '        End If

    '        Return False
    '    End Try
    'End Function

    ''' <summary>
    ''' Asynchronously sends a GET request to the specified url and returns the http status code of the server response.
    ''' </summary>
    '''
    ''' <param name="url">
    ''' The url.
    ''' </param>
    '''
    ''' <param name="useragent">
    ''' A custom user-agent to use for the http request.
    ''' </param>
    '''
    ''' <param name="cookieContainer">
    ''' A custom <see cref="System.Net.CookieContainer"/> object containing 
    ''' the <see cref="Cookie"/> instances to be used for the http request.
    ''' </param>
    '''
    ''' <returns>
    ''' The resulting http status code.
    ''' </returns>
    Public Async Function GetUrlStatusCodeAsync(url As Uri,
                                       Optional useragent As String = Nothing,
                                       Optional cookieContainer As CookieContainer = Nothing) As Task(Of HttpStatusCode)

        Using httpClientHandler As New HttpClientHandler()

            If cookieContainer IsNot Nothing Then
                httpClientHandler.CookieContainer = cookieContainer
                httpClientHandler.UseCookies = True
            End If

            httpClientHandler.AllowAutoRedirect = True
            httpClientHandler.SslProtocols = System.Security.Authentication.SslProtocols.None
            httpClientHandler.MaxAutomaticRedirections = 5
            httpClientHandler.UseDefaultCredentials = True

            ' Disable SSL Certificate Validation to avoid this "AuthenticationException" exception message:
            ' "The remote certificate is invalid according to the validation procedure."
            '
            ' This exception is caused either because the server is not signed by the certification authorities,
            ' or you have inputted the wrong hostname to connect.
            ' The name of the certificate needs to match the hostname you use to contact the server.
            '
            ' https://www.conradakunga.com/blog/disable-ssl-certificate-validation-in-net/
            httpClientHandler.ServerCertificateCustomValidationCallback = Function(message, cert, chain, sslPolicyErrors) True

            Using client As New HttpClient(httpClientHandler) ' With {.BaseAddress = New Uri(endPoint)}
                If Not String.IsNullOrEmpty(useragent) Then
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(useragent)
                End If
                Using checkingResponse As HttpResponseMessage = Await client.GetAsync(url)
                    Return checkingResponse.StatusCode
                End Using
            End Using
        End Using
    End Function

#End Region

#Region " Private Methods "

    ''' <summary>
    ''' Evaluate the online network adapters to determine if at least one of them is capable of connecting to Internet.
    ''' </summary>
    '''
    ''' <returns>
    ''' <see langword="True"/> if at least one of the current network adapters is capable of connecting to Internet; 
    ''' otherwise, <see langword="False"/>.
    ''' </returns>
    <DebuggerStepThrough>
    Private Function Internal_IsNetworkAvailable() As Boolean

        If NetworkInterface.GetIsNetworkAvailable() Then
            Dim interfaces As NetworkInterface() = NetworkInterface.GetAllNetworkInterfaces()

            For Each ni As NetworkInterface In interfaces
                If ni.OperationalStatus = OperationalStatus.Up Then
                    If (ni.NetworkInterfaceType <> NetworkInterfaceType.Tunnel) AndAlso
                           (ni.NetworkInterfaceType <> NetworkInterfaceType.Loopback) Then

                        Dim statistics As IPv4InterfaceStatistics = ni.GetIPv4Statistics()
                        If (statistics.BytesReceived > 0) AndAlso (statistics.BytesSent > 0) Then
                            Return True
                        End If
                    End If
                End If
            Next ni
        End If

        Return False
    End Function

#End Region

End Module
