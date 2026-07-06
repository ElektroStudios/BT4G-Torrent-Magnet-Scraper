
#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Runtime.CompilerServices
Imports System.Threading

Imports OpenQA.Selenium
Imports OpenQA.Selenium.Chrome

Imports SeleniumCookie = OpenQA.Selenium.Cookie

#End Region

#Region " ChromeDriver Extensions "

' ReSharper disable once CheckNamespace

Namespace DevCase.ThirdParty.Selenium.Extensions.ChromeDriverExtensions

    ''' <summary>
    ''' Provides extension methods to use with <see cref="ChromeDriver"/>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' Note: Some functionalities of this assembly may require to install one or all of the listed NuGet packages:
    ''' <para></para>
    ''' <see href="https://www.nuget.org/packages/Selenium.Support">Selenium.Support by Selenium Committers</see>
    ''' <para></para>
    ''' <see href="https://www.nuget.org/packages/Selenium.WebDriver">Selenium.WebDriver by Selenium Committers</see>
    ''' <para></para>
    ''' </remarks>
    <HideModuleName>
    Public Module ChromeDriverExtensions

#Region " Public Extension Methods "

        ''' <summary>
        ''' Waits for a Cloudflare challenge to complete in the current browser, 
        ''' by detecting and validating the <c>cf_clearance</c> cookie.
        ''' </summary>
        ''' 
        ''' <param name="driver">
        ''' The <see cref="ChromeDriver"/> instance.
        ''' </param>
        ''' 
        ''' <param name="timeoutSeconds">
        ''' Optional. The maximum time in seconds to wait for the Cloudflare challenge to complete.
        ''' <para></para>
        ''' Default value is 30 seconds.
        ''' <para></para>
        ''' If the condition is not met within this time, a <see cref="WebDriverTimeoutException"/> is thrown.
        ''' </param>
        <Extension>
        <DebuggerStepThrough>
        <EditorBrowsable(EditorBrowsableState.Always)>
        Public Sub WaitToCompleteCloudflareChallenge(driver As ChromeDriver,
                                                     Optional timeoutSeconds As Integer = 30)

            Dim result As String = driver.ExecuteScript("return navigator.userAgent;").ToString()
#If Not NETCOREAPP Then
            Dim isHeadlessChrome As Boolean = result.IndexOf("HeadlessChrome", StringComparison.OrdinalIgnoreCase) >= 0
#Else
            Dim isHeadlessChrome As Boolean = result.Contains("HeadlessChrome", StringComparison.OrdinalIgnoreCase)
#End If
            If isHeadlessChrome Then
                Throw New NotSupportedException("Headless mode in Chrome driver is not supported to complete the Cloudflare challenge. Please don't use headless mode.")
            End If

            ' cf_clearance:
            '   Clearance Cookie stores the proof of challenge passed.
            '   It is used to no longer issue a challenge if present. It is required to reach an origin server.
            ' https://developers.cloudflare.com/fundamentals/reference/policies-compliances/cloudflare-cookies/#additional-cookies-used-by-the-challenge-platform

            Dim conditionFunction As Func(Of Boolean) =
                Function()
                    Dim cfCookie As SeleniumCookie = driver.Manage().Cookies.GetCookieNamed("cf_clearance")
                    Return cfCookie IsNot Nothing AndAlso
                           Not String.IsNullOrWhiteSpace(cfCookie.Value) AndAlso
                           (cfCookie.Expiry.HasValue AndAlso Date.Now.AddSeconds(timeoutSeconds) < cfCookie.Expiry.Value) AndAlso
                           Not driver.PageSource.Contains("challenges.cloudflare")
                End Function

            Dim startTime As Date = Date.Now
            While Not conditionFunction.Invoke()
                If (Date.Now - startTime).TotalSeconds >= timeoutSeconds Then
                    Throw New WebDriverTimeoutException("Timeout has been reached while waiting to complete the Cloudflare challenge.")
                End If

                Thread.CurrentThread.Join(0)
                Thread.Sleep(3000)
            End While

        End Sub

#End Region

    End Module

End Namespace

#End Region
