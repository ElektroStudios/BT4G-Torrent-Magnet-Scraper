#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Diagnostics
Imports OpenQA.Selenium

Imports DevCase.ThirdParty.Selenium.Extensions.IWebDriverExtensions

#End Region

''' <summary>
''' Provides helper members related to selenium / selenium-manager.exe operations.
''' </summary>
Friend Module SeleniumHelper

#Region " Public Methods "

    ''' <summary>
    ''' Instructs the specified <see cref="IWebDriver"/> to navigate to the given URL.
    ''' </summary>
    ''' 
    ''' <param name="driver">
    ''' The <see cref="IWebDriver"/> instance.
    ''' </param>
    ''' 
    ''' <param name="url">
    ''' The URL to navigate to.
    ''' </param>
    <DebuggerStepThrough>
    Public Sub NavigateTo(driver As IWebDriver, url As String)

        If Not NetworkHelper.IsNetworkAvailable Then
            Throw New Exception("Network adapter is not available.")
        End If

        Dim dateBeforeNav As Date = Date.UtcNow
        Dim previousTimeout As TimeSpan = driver.Manage().Timeouts().PageLoad
        Try
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30)
            driver.Navigate().GoToUrl(url)

        Catch ex As WebDriverTimeoutException
            Throw New Exception($"Operation has timed out trying to load URL: {url}")

        Finally
            driver.Manage().Timeouts().PageLoad = previousTimeout
            driver.ThrowIfAnyErrorStatusCode(dateBeforeNav)

        End Try
    End Sub

#End Region

End Module
