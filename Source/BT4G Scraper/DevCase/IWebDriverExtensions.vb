
#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Threading

Imports OpenQA.Selenium
Imports OpenQA.Selenium.Support.UI

Imports SeleniumLogEntry = OpenQA.Selenium.LogEntry

Imports DevCase.Core.Networking.Common

#End Region

#Region " IWebDriver Extensions "

' ReSharper disable once CheckNamespace

Namespace DevCase.ThirdParty.Selenium.Extensions.IWebDriverExtensions

    ''' <summary>
    ''' Provides extension methods to use with <see cref="IWebDriver"/>.
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
    Public Module IWebDriverExtensions

#Region " Public Extension Methods "

        '''' <summary>
        '''' Retrieves the plain text content of the <c>body</c> element from the current page.
        '''' </summary>
        ''''
        '''' <remarks>
        '''' Note: Some functionalities of this assembly may require to install one or all of the listed NuGet packages:
        '''' <para></para>
        '''' <see href="https://www.nuget.org/packages/Selenium.Support">Selenium.Support by Selenium Committers</see>
        '''' <para></para>
        '''' <see href="https://www.nuget.org/packages/Selenium.WebDriver">Selenium.WebDriver by Selenium Committers</see>
        '''' <para></para>
        '''' </remarks>
        ''''
        '''' <param name="drv">
        '''' The source <see cref="IWebDriver"/>.
        '''' </param>
        ''''
        '''' <returns>
        '''' The plain text content of the <c>body</c> element from the current page.
        '''' </returns>
        '<DebuggerStepThrough>
        '<Extension>
        '<EditorBrowsable(EditorBrowsableState.Always)>
        'Public Function GetBodyText(drv As IWebDriver) As String

        '    Return drv.FindElement(By.TagName("body")).Text
        'End Function

        ''' <summary>
        ''' Waits for the current web page in the specified <see cref="IWebDriver"/> instance 
        ''' to report a ready state of <c>"complete"</c>. And optionally, it can also 
        ''' wait for any pending dynamic updates in the DOM to complete after the page 
        ''' has reported a ready state of <c>"complete"</c>.
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
        '''
        ''' <param name="driver">
        ''' The <see cref="IWebDriver"/> instance.
        ''' </param>
        ''' 
        ''' <param name="afterPageReadyDelay">
        ''' Optional. A <see cref="TimeSpan"/> representing the delay to wait 
        ''' <b>after</b> the web page reports a ready state of <c>"complete"</c>, 
        ''' before the method returns.
        ''' <para></para>
        ''' This can be useful to allow background scripts, animations, or 
        ''' asynchronous content to finish initializing after the document is loaded.
        ''' <para></para>
        ''' Default value is null.
        ''' </param>
        ''' 
        ''' <param name="waitForDomIdle">
        ''' Optional. When set to <see langword="True"/>, the method starts waiting for any pending dynamic updates in the DOM 
        ''' to complete after the page has reported a ready state of <c>"complete"</c>.
        ''' <para></para>
        ''' Default value is <see langword="False"/>.
        ''' <para></para>
        ''' ⚠️ Do not set this parameter to <see langword="True"/> for web pages with continuously changing DOM elements 
        ''' (e.g., pages with animations, snow effects, or real-time updates).
        ''' </param>
        ''' 
        ''' <param name="timeoutSeconds">
        ''' Optional. The maximum time in seconds to wait for the page to report a ready state of <c>"complete"</c>, 
        ''' and for any pending dynamic updates in the DOM to complete if 
        ''' <paramref name="waitForDomIdle"/> is set to <see langword="True"/>.
        ''' <para></para>
        ''' Default value is 30 seconds.
        ''' <para></para>
        ''' If the condition is not met within this time, a <see cref="WebDriverTimeoutException"/> is thrown.
        ''' </param>
        ''' 
        ''' <param name="throwOnTimeout">
        ''' Optional. When set to <see langword="True"/>, 
        ''' a <see cref="WebDriverTimeoutException"/> will be thrown if the time specified in 
        ''' <paramref name="timeoutSeconds"/> parameter reaches while waiting the
        ''' web page to report a ready state of <c>"complete"</c>,
        ''' or while waiting for any pending dynamic updates in the DOM to complete after the 
        ''' page has reported a ready state of <c>"complete"</c>.
        ''' <para></para>
        ''' Default value is <see langword="True"/>.
        ''' </param>
        <DebuggerStepThrough>
        <Extension>
        <EditorBrowsable(EditorBrowsableState.Always)>
        Public Sub WaitForPageReady(driver As IWebDriver,
                           Optional afterPageReadyDelay As TimeSpan = Nothing,
                           Optional waitForDomIdle As Boolean = False,
                           Optional timeoutSeconds As Integer = 30,
                           Optional throwOnTimeout As Boolean = True)

            If timeoutSeconds <= 0 Then
                Throw New ArgumentException("Timeout must be a value greater than zero.", NameOf(timeoutSeconds))
            End If

            Dim js As IJavaScriptExecutor = TryCast(driver, IJavaScriptExecutor)
            If js Is Nothing Then
                Throw New ArgumentException("Driver must support javascript execution", NameOf(driver))
            End If

            Dim wait As New WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds)) With {
                .PollingInterval = TimeSpan.FromMilliseconds(500)
            }

            Dim startTime As Date = Date.Now
            Dim domLength As Integer

            wait.Until(
                Function(d As IWebDriver)
                    Try
                        Dim readyState As String = js.ExecuteScript("if (document.readyState) return document.readyState;").ToString()
                        If readyState.Equals("complete", StringComparison.OrdinalIgnoreCase) Then
                            domLength = d.PageSource.Length
                            Return True
                        Else
                            Return False
                        End If

                    Catch ex As WebDriverTimeoutException
                        If throwOnTimeout Then
                            Throw
                        End If
                        Return True

                    Catch ex As InvalidOperationException ' Window is no longer available
#If Not NETCOREAPP Then
                        Return ex.Message.IndexOf("unable to get browser", StringComparison.OrdinalIgnoreCase) >= 0
#Else
                        Return ex.Message.Contains("unable to get browser", StringComparison.OrdinalIgnoreCase)
#End If

                    Catch ex As WebDriverException ' Browser is no longer available
#If Not NETCOREAPP Then
                        Return ex.Message.IndexOf("unable to connect", StringComparison.OrdinalIgnoreCase) >= 0
#Else
                        Return ex.Message.Contains("unable to connect", StringComparison.OrdinalIgnoreCase)
#End If

                    Catch ex As Exception
                        Return True

                    End Try
                End Function)

            If afterPageReadyDelay <> Nothing Then
                Thread.Sleep(afterPageReadyDelay)
            End If

            ' Even when "document.readyState()" returns "complete", web pages can continue to modify
            ' the DOM dynamically after the initial load. This can occur due to asynchronous scripts,
            ' client-side rendering frameworks (such as React, Angular, or Vue), AJAX/fetch requests
            ' that inject additional content and rewrites portions of the page, etc.
            '
            ' As a result, the page source may still change for a short period of time even though the 
            ' web browser reports that the document has finished loading.
            '
            ' This check ensures that the HTML content remains stable (IDLE) before exiting.
            If waitForDomIdle Then

                Dim newDomLength As Integer = driver.PageSource.Length
                If newDomLength <> domLength Then

                    Dim elapsedTime As TimeSpan = Date.Now - startTime
                    timeoutSeconds -= CInt(elapsedTime.TotalSeconds)
                    If timeoutSeconds <= 0 Then
                        timeoutSeconds = 1
                    End If

                    Dim domWait As New WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds)) With {
                        .PollingInterval = TimeSpan.FromSeconds(1)
                    }

                    domWait.Until(Function(d As IWebDriver)
                                      Try
                                          Dim length As Integer = d.PageSource.Length
                                          Dim idle As Boolean = (length = newDomLength)
                                          newDomLength = length
                                          Return idle

                                      Catch ex As WebDriverTimeoutException
                                          If throwOnTimeout Then
                                              Throw
                                          End If
                                          Return True

                                      Catch ex As InvalidOperationException ' Window is no longer available
#If Not NETCOREAPP Then
                                          Return ex.Message.IndexOf("unable to get browser", StringComparison.OrdinalIgnoreCase) >= 0
#Else
                                          Return ex.Message.Contains("unable to get browser", StringComparison.OrdinalIgnoreCase)
#End If

                                      Catch ex As WebDriverException ' Browser is no longer available
#If Not NETCOREAPP Then
                                          Return ex.Message.IndexOf("unable to connect", StringComparison.OrdinalIgnoreCase) >= 0
#Else
                                          Return ex.Message.Contains("unable to connect", StringComparison.OrdinalIgnoreCase)
#End If

                                      Catch ex As Exception
                                          Return True

                                      End Try
                                  End Function)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Analyzes the browser log entries since the specified date 
        ''' to find any entry containing an HTTP status code error, 
        ''' throwing an <see cref="Exception"/> with the corresponding log entry message if found.
        ''' <para></para>
        ''' It also analyzes the current page source, applying special handling for Cloudflare-protected pages.
        ''' <para></para>
        ''' This method helps determine whether the currently loaded page returned an HTTP error status code.
        ''' </summary>
        ''' 
        ''' <param name="driver">
        ''' The <see cref="IWebDriver"/> instance pointing to the current page to be checked.
        ''' </param>
        ''' 
        ''' <param name="afterDate">
        ''' Only browser log entries with a <see cref="SeleniumLogEntry.Timestamp"/> greater than 
        ''' or equal to this date are analyzed.
        ''' <para></para>
        ''' This allows filtering out logs from previous navigations or operations.
        ''' </param>
        <Extension>
        <DebuggerStepThrough>
        <EditorBrowsable(EditorBrowsableState.Always)>
        Public Sub ThrowIfAnyErrorStatusCode(driver As IWebDriver, afterDate As Date)

            Dim logs As ReadOnlyCollection(Of SeleniumLogEntry) = driver.Manage().Logs.GetLog(LogType.Browser)
            For Each log As SeleniumLogEntry In logs.Where(
                Function(x) x.Timestamp >= afterDate AndAlso
                            x.Message.IndexOf("status of", StringComparison.OrdinalIgnoreCase) >= 0 AndAlso
                            x.Message.IndexOf("/api/", StringComparison.OrdinalIgnoreCase) = -1 AndAlso
                            Not IWebDriverExtensions.IsResourceUrlLogEntryMessage(x.Message))

                Select Case log.Level
                    Case LogLevel.Severe
                        Dim msg As String = log.Message
                        If driver.PageSource.IndexOf("cloudflare", StringComparison.OrdinalIgnoreCase) >= 0 Then
                            ' The first time navigating to a Cloudflare-protected page,
                            ' it may return a status code of 403 (Forbidden).
                            ' This happens before the Cloudflare challenge is fully completed once.
                            ' Ignore it, as we have special handling for Cloudflare-protected pages below. 👇
                            Exit Select
                        End If

                        Throw New Exception(msg)
                End Select
            Next log

            If driver.PageSource.IndexOf("cloudflare", StringComparison.OrdinalIgnoreCase) >= 0 AndAlso (
                driver.PageSource.IndexOf(".error-footer", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                driver.PageSource.IndexOf("Web server is down", StringComparison.OrdinalIgnoreCase) >= 0
            ) Then

                Try
                    Dim titleElement As IWebElement = driver.FindElement(By.TagName("title"))
                    Dim titleText As String = titleElement.GetAttribute("textContent")
                    Throw New Exception(titleText)

                Catch ex As Exception
                    Throw
                End Try
            End If

        End Sub

        ''' <summary>
        ''' Determines whether the current web page loaded in the specified <see cref="IWebDriver"/> is protected by a Cloudflare challenge, 
        ''' so a navigation block or anti-bot challenge is currently being displayed instead of the expected content.
        ''' </summary>
        ''' 
        ''' <param name="driver">
        ''' The <see cref="IWebDriver"/> instance.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if a Cloudflare challenge is required to load the web page; 
        ''' otherwise, <see langword="False"/>.
        ''' </returns>
        <Extension>
        <DebuggerStepThrough>
        Public Function IsCloudflareChallengeRequired(driver As IWebDriver) As Boolean

#If NETCOREAPP Then
            ArgumentNullException.ThrowIfNull(driver)
#Else
            If driver Is Nothing Then
                Throw New ArgumentNullException(NameOf(driver))
            End If
#End If
            Dim pageSource As String = driver.PageSource
            Dim pageTitle As String = driver.Title

            Return UtilWeb.IsCloudflareChallengeRequired(pageSource, pageTitle)
        End Function

        ''' <summary>
        ''' Injects Javascript into the current DOM to draw an animated overlay box on top of the 
        ''' specified <see cref="IWebElement"/> to visually highlight it, optionally showing a 
        ''' centered text label and an arrow pointing toward the element from a specified side.
        ''' <para></para>
        ''' This is ideal for visually guiding users' attention toward specific page elements during 
        ''' demos, tutorials, or debugging sessions.
        ''' </summary>
        ''' 
        ''' <example> This is a code example.
        ''' <code language="VB">
        ''' Dim elemment As IWebElement = driver.FindElement(By.Id("submit-btn"))
        ''' HighlightElement(driver, elemment,
        '''                  labelText:="Click here",
        '''                  labelForeColor:="red",
        '''                  showArrow:=True,
        '''                  arrowSize:=24,
        '''                  arrowAlignment:=ContentAlignment.MiddleLeft,
        '''                  arrowColor:="red",
        '''                  borderColor:="red",
        '''                  fillColor:="white",
        '''                  fillOpacity:=0.4,
        '''                  durationMs:=3000)
        ''' </code>
        ''' </example>
        ''' 
        ''' <param name="driver">
        ''' Active <see cref="IWebDriver"/> instance. Must implement
        ''' <see cref="IJavaScriptExecutor"/>; otherwise an <see cref="InvalidCastException"/> will be thrown.
        ''' </param>
        ''' 
        ''' <param name="element">
        ''' The DOM element to highlight. Its position is determined at runtime
        ''' via <c>getBoundingClientRect()</c>, accounting for current scroll offsets.
        ''' </param>
        ''' 
        ''' <param name="text">
        ''' Optional. The text displayed centered inside the overlay box.
        ''' <para></para>
        ''' Default value is an empty string (no label).
        ''' </param>
        ''' 
        ''' <param name="fontSize">
        ''' Optional. The initial font size for <paramref name="text"/>, in pixels. 
        ''' <para></para>
        ''' Note: The font size auto-shrinks down up to 6px if the text overflows the element's bounds.
        ''' <para></para>
        ''' Default value is <c>14</c>.
        ''' </param>
        ''' 
        ''' <param name="textForeColor">
        ''' Optional. The CSS color value for the label text (e.g. <c>"black"</c>, <c>"#fff"</c>). 
        ''' <para></para>
        ''' Default value is <c>"black"</c>.
        ''' </param>
        ''' 
        ''' <param name="showArrow">
        ''' Optional. A value indicating whether to render an animated arrow pointing
        ''' toward the element from the side set by <paramref name="arrowAlignment"/> parameter.
        ''' </param>
        ''' 
        ''' <param name="arrowAlignment">
        ''' Optional. A <see cref="ContentAlignment"/> value that controls which side of the element
        ''' the arrow appears on and the direction it points.
        ''' <para></para>
        ''' Note: This value has no effect if <paramref name="showArrow"/> is <see langword="False"/>
        ''' <para></para>
        ''' Default value is <see cref="ContentAlignment.MiddleLeft"/>.
        ''' </param>
        ''' 
        ''' <param name="arrowSize">
        ''' Optional. The square size (width x height) of the arrow bounding box, in pixels. 
        ''' <para></para>
        ''' Default value is <c>30</c>.
        ''' </param>
        ''' 
        ''' <param name="arrowColor">
        ''' Optional. The CSS color value for the arrow (e.g. <c>"black"</c>, <c>"#fff"</c>). 
        ''' <para></para>
        ''' Default value is <c>"darkred"</c>.
        ''' </param>
        ''' 
        ''' <param name="borderColor">
        ''' Optional. The CSS color value for the overlay border (e.g. <c>"black"</c>, <c>"#fff"</c>). 
        ''' <para></para>
        ''' Default value is <c>"red"</c>.
        ''' </param>
        ''' 
        ''' <param name="fillColor">
        ''' Optional. The CSS color value to fill the overlay background (e.g. <c>"black"</c>, <c>"#fff"</c>). 
        ''' <para></para>
        ''' Default value is <c>"white"</c>.
        ''' </param>
        ''' 
        ''' <param name="fillOpacity">
        ''' Optional. The opacity of the background overlay, from <c>0.0</c> (transparent) to <c>1.0</c> (fully opaque).
        ''' <para></para>
        ''' Default value is <c>0.4</c>.
        ''' </param>
        ''' 
        ''' <param name="pageDimOpacity">
        ''' Optional. The opacity of the dimmed page area outside the highlighted element, from <c>0.0</c> (transparent) to <c>1.0</c> (fully opaque).
        ''' <para></para>
        ''' Default value is <c>0.60</c>.
        ''' </param>
        ''' 
        ''' <param name="durationMs">
        ''' Optional. The duration before the overlay (and arrow, if shown) are removed from the DOM, in milliseconds.
        ''' <para></para>
        ''' Default value is <see cref="Integer.MaxValue"/>.
        ''' </param>
        <Extension>
        <DebuggerStepThrough>
        Public Function HighlightElement(driver As IWebDriver, element As IWebElement,
                                Optional text As String = "",
                                Optional fontSize As Integer = 14,
                                Optional textForeColor As String = "black",
                                Optional showArrow As Boolean = True,
                                Optional arrowAlignment As ContentAlignment = ContentAlignment.MiddleLeft,
                                Optional arrowSize As Integer = 30,
                                Optional arrowColor As String = "darkred",
                                Optional borderColor As String = "red",
                                Optional fillColor As String = "white",
                                Optional fillOpacity As Double = 0.4,
                                Optional pageDimOpacity As Double = 0.6,
                                Optional durationMs As Integer = Integer.MaxValue) As Boolean

#If Not NETCOREAPP Then
            If driver Is Nothing Then
                Throw New ArgumentNullException(NameOf(driver))
            End If

            If element Is Nothing Then
                Throw New ArgumentNullException(NameOf(element))
            End If
#Else
            ArgumentNullException.ThrowIfNull(driver, NameOf(driver))
            ArgumentNullException.ThrowIfNull(element, NameOf(element))
#End If

            If pageDimOpacity < 0 OrElse pageDimOpacity > 1 Then
                Throw New ArgumentOutOfRangeException(NameOf(pageDimOpacity), "Value must be between 0.0 and 1.0.")
            End If

            ' Avoids passing null to JavaScript.
            If String.IsNullOrEmpty(text) Then
                text = ""
            End If

            Try
                Dim probeHandle As String = driver.CurrentWindowHandle
            Catch ex As Exception
                Throw New InvalidOperationException("WebDriver session is no longer available.", ex)
            End Try

            Try
                Dim probeTag As String = element.TagName
                If Not element.Displayed Then
                    Return False
                End If

            Catch ex As StaleElementReferenceException
                Throw New InvalidOperationException("Target element is stale or detached from the DOM.", ex)

            Catch ex As WebDriverException
                Throw New InvalidOperationException("Target element is not accessible through the WebDriver.", ex)

            End Try

            Dim jsExecutor As IJavaScriptExecutor = TryCast(driver, IJavaScriptExecutor)
            If jsExecutor Is Nothing Then
                Throw New NotSupportedException("The provided IWebDriver does not implement IJavaScriptExecutor.")
            End If

            Dim script As String = "
try {
    return (function(el, 
                     labelText, labelFontSize, labelForeColor, 
                     showArrow, arrowAlignment, arrowSize, arrowColor, 
                     borderColor, fillColor, fillOpacity, 
                     pageDimOpacity, 
                     durationMs) {

    if (!el || !(el instanceof Element) || !el.isConnected) { return 'no-element'; }

    let r  = el.getBoundingClientRect();
    if (!r || (r.width === 0 && r.height === 0)) { return 'zero-rect'; }

    let sx = window.scrollX, sy = window.scrollY;
    let cx = r.left + sx + r.width  / 2;
    let cy = r.top  + sy + r.height / 2;

    // Overlay box — positioned and sized to wrap the element with a small padding
    let overlay = document.createElement('div');
    overlay.style.cssText = [
        'position:absolute',
        'left:'   + (r.left + sx - 5) + 'px',
        'top:'    + (r.top  + sy - 5) + 'px',
        'width:'  + (r.width  + 10)   + 'px',
        'height:' + (r.height + 10)   + 'px',
        'border:3px solid ' + borderColor,
        'border-radius:8px',
        'box-shadow:0 0 20px ' + borderColor,
        'z-index:999998',
        'pointer-events:none',
        'display:flex',
        'align-items:center',
        'justify-content:center',
        'box-sizing:border-box',
        'overflow:hidden'
    ].join(';');
    document.body.appendChild(overlay);

    // Fill div — opacity only affects background, not the border
    let fill = document.createElement('div');
    fill.style.cssText = 'position:absolute;' +
                         'inset:0;' +
                         'background:' + fillColor + ';' +
                         'opacity:' + fillOpacity + ';' +
                         'z-index:0;';
    overlay.appendChild(fill);

    // Page dimmer — darkens and slightly blurs everything except the highlighted element.
    let pageDimContainer = null;
    let pageDimTop = null;
    let pageDimBottom = null;
    let pageDimLeft = null;
    let pageDimRight = null;

    if (pageDimOpacity > 0) {
        pageDimContainer = document.createElement('div');
        pageDimContainer.style.cssText = [
            'position:fixed',
            'inset:0',
            'z-index:999997',
            'pointer-events:none'
        ].join(';');

        function createDimPart() {
            let part = document.createElement('div');
            part.style.cssText = [
                'position:absolute',
                'background:rgba(0,0,0,' + pageDimOpacity + ')',
                'backdrop-filter:blur(2px)',
                '-webkit-backdrop-filter:blur(2px)'
            ].join(';');
            return part;
        }

        pageDimTop = createDimPart();
        pageDimBottom = createDimPart();
        pageDimLeft = createDimPart();
        pageDimRight = createDimPart();

        pageDimContainer.appendChild(pageDimTop);
        pageDimContainer.appendChild(pageDimBottom);
        pageDimContainer.appendChild(pageDimLeft);
        pageDimContainer.appendChild(pageDimRight);

        document.body.appendChild(pageDimContainer);
    }

    // Label — auto-shrinks font size until text fits inside the overlay
    let lbl = null;
    if (labelText) {
        let lbl = document.createElement('div');
        lbl.textContent = labelText;
        lbl.style.cssText = 'position:relative;' +
                            'z-index:1;' +
                            'width:100%;' +
                            'box-sizing:border-box;' +
                            'white-space:normal;' +
                            'word-break:break-word;' +
                            'text-align:center;' +
                            'color:' + labelForeColor + ';' +
                            'font-size:' + labelFontSize + 'px;' +
                            'font-weight:bold;' +
                            'padding:2px 4px;' +
                            'border-radius:4px;';
        overlay.appendChild(lbl);
        let fs = labelFontSize;
        while (fs > 6 && lbl.scrollHeight > overlay.clientHeight) {
            fs--;
            lbl.style.fontSize = fs + 'px';
        }
    }

    // Arrow — SVG polygon rotated via CSS to point toward the element from its position.
    // Two nested divs: arrowWrap handles position + bounce animation,
    // arrowInner handles rotation so both transforms don't conflict.
    let arrowWrap = null;
    if (showArrow) {
        let styleTag = document.createElement('style');
        styleTag.textContent =
            '@keyframes bML {from{transform:translateX( 0px)} to{transform:translateX(-8px)}}' +
            '@keyframes bMR {from{transform:translateX( 0px)} to{transform:translateX( 8px)}}' +
            '@keyframes bTC {from{transform:translateY( 0px)} to{transform:translateY(-8px)}}' +
            '@keyframes bBC {from{transform:translateY( 0px)} to{transform:translateY( 8px)}}' +
            '@keyframes bTL {from{transform:translate(0,0)} to{transform:translate(-6px,-6px)}}' +
            '@keyframes bTR {from{transform:translate(0,0)} to{transform:translate( 6px,-6px)}}' +
            '@keyframes bBL {from{transform:translate(0,0)} to{transform:translate(-6px, 6px)}}' +
            '@keyframes bBR {from{transform:translate(0,0)} to{transform:translate( 6px, 6px)}}';
        document.head.appendChild(styleTag);

        // rot: CSS rotation so the arrow always points TOWARD the element from its side.
        // anim: bounce keyframe name matching the arrow's side.
        // Both are constant for the lifetime of the highlight — assigned once here,
        // never inside updatePosition (which would restart the animation every tick).
        let staticCfg = {
            'MiddleLeft'   : { rot:  '0', anim:'bML' },
            'MiddleRight'  : { rot:'180', anim:'bMR' },
            'TopCenter'    : { rot: '90', anim:'bTC' },
            'BottomCenter' : { rot:'270', anim:'bBC' },
            'TopLeft'      : { rot: '45', anim:'bTL' },
            'TopRight'     : { rot:'135', anim:'bTR' },
            'BottomLeft'   : { rot:'315', anim:'bBL' },
            'BottomRight'  : { rot:'225', anim:'bBR' }
        }[arrowAlignment] || { rot:'0', anim:'bML' };

        arrowWrap = document.createElement('div');
        arrowWrap.style.cssText = 'position:absolute;' +
                                  'width:' + arrowSize + 'px;' +
                                  'height:' + arrowSize + 'px;' +
                                  'z-index:999999;pointer-events:none;';
        arrowWrap.style.animation = staticCfg.anim + ' 0.4s ease-in-out infinite alternate';

        arrowInner = document.createElement('div');
        arrowInner.style.cssText = 'width:' + arrowSize + 'px;' +
                                   'height:' + arrowSize + 'px;' +
                                   'transform:rotate(' + staticCfg.rot + 'deg);' +
                                   'transform-origin:center center;';

        // Arrow shape: rectangular shaft + concave arrowhead.
        arrowInner.innerHTML = '<svg viewBox=\'0 0 48 40\' width=\'' + arrowSize + '\' height=\'' + arrowSize + '\' xmlns=\'http://www.w3.org/2000/svg\'>' +
                               '<polygon points=\'0,16 30,16 24,4 48,20 24,36 30,24 0,24\' fill=\'' + arrowColor + '\'/>' +
                               '</svg>';

        arrowWrap.appendChild(arrowInner);
        document.body.appendChild(arrowWrap);
    }

    function updatePageDim(r) {
        if (!pageDimContainer || !pageDimTop) { return; }

        let pad = 5;
        let vw = window.innerWidth;
        let vh = window.innerHeight;

        let holeLeft = Math.max(0, r.left - pad);
        let holeTop = Math.max(0, r.top - pad);
        let holeRight = Math.min(vw, r.right + pad);
        let holeBottom = Math.min(vh, r.bottom + pad);

        let holeWidth = Math.max(0, holeRight - holeLeft);
        let holeHeight = Math.max(0, holeBottom - holeTop);

        pageDimTop.style.left = '0px';
        pageDimTop.style.top = '0px';
        pageDimTop.style.width = '100%';
        pageDimTop.style.height = holeTop + 'px';

        pageDimBottom.style.left = '0px';
        pageDimBottom.style.top = holeBottom + 'px';
        pageDimBottom.style.width = '100%';
        pageDimBottom.style.height = Math.max(0, vh - holeBottom) + 'px';

        pageDimLeft.style.left = '0px';
        pageDimLeft.style.top = holeTop + 'px';
        pageDimLeft.style.width = holeLeft + 'px';
        pageDimLeft.style.height = holeHeight + 'px';

        pageDimRight.style.left = holeRight + 'px';
        pageDimRight.style.top = holeTop + 'px';
        pageDimRight.style.width = Math.max(0, vw - holeRight) + 'px';
        pageDimRight.style.height = holeHeight + 'px';
    }

    // updatePosition — recalculates overlay and arrow placement on every tick,
    // so the highlight stays glued to the element when the user scrolls,
    // resizes the window, or the page reflows dynamically.
    function updatePosition() {
        try {
            if (!el || !el.isConnected) { return; }

            let r  = el.getBoundingClientRect();
            let sx = window.scrollX, sy = window.scrollY;

            updatePageDim(r);

            overlay.style.left   = (r.left + sx - 5) + 'px';
            overlay.style.top    = (r.top  + sy - 5) + 'px';
            overlay.style.width  = (r.width  + 10) + 'px';
            overlay.style.height = (r.height + 10) + 'px';

            // Auto-shrink the label font size until it fits inside the overlay,
            // recomputed on each tick because the box can be resized by the user.
            if (lbl) {
                let fs = labelFontSize;
                lbl.style.fontSize = fs + 'px';
                while (fs > 6 && lbl.scrollHeight > overlay.clientHeight) {
                    fs--;
                    lbl.style.fontSize = fs + 'px';
                }
            }

            if (arrowWrap && arrowInner) {
                // ax/ay: absolute position of the arrow bounding box, recomputed each tick.
                // d: diagonal inset — compensates the 45° rotation so the visible tip sits
                //    exactly `marginDiagonal` pixels away from the element border, regardless of arrowSize.
                // marginStraight: clearance for MiddleLeft/MiddleRight/TopCenter/BottomCenter.
                // marginDiagonal: clearance for TopLeft/TopRight/BottomLeft/BottomRight.
                let cx             = r.left + sx + r.width  / 2;
                let cy             = r.top  + sy + r.height / 2;
                let d              = arrowSize * (0.5 - Math.SQRT2 / 4);
                let marginStraight = 8;
                let marginDiagonal = 4;
                let cfg = {
                    'MiddleLeft'   : { ax: r.left   + sx - arrowSize - marginStraight,     ay: cy - arrowSize / 2 },
                    'MiddleRight'  : { ax: r.right  + sx + marginStraight,                 ay: cy - arrowSize / 2 },
                    'TopCenter'    : { ax: cx - arrowSize / 2,                             ay: r.top    + sy - arrowSize - marginStraight },
                    'BottomCenter' : { ax: cx - arrowSize / 2,                             ay: r.bottom + sy + marginStraight },
                    'TopLeft'      : { ax: r.left   + sx - arrowSize + d - marginDiagonal, ay: r.top    + sy - arrowSize + d - marginDiagonal },
                    'TopRight'     : { ax: r.right  + sx - d + marginDiagonal,             ay: r.top    + sy - arrowSize + d - marginDiagonal },
                    'BottomLeft'   : { ax: r.left   + sx - arrowSize + d - marginDiagonal, ay: r.bottom + sy - d + marginDiagonal },
                    'BottomRight'  : { ax: r.right  + sx - d + marginDiagonal,             ay: r.bottom + sy - d + marginDiagonal }
                }[arrowAlignment] || {
                    ax: r.left + sx - arrowSize - marginStraight, ay: cy - arrowSize / 2
                };

                arrowWrap.style.left = cfg.ax + 'px';
                arrowWrap.style.top  = cfg.ay + 'px';
                // Note: animation and rotation are set ONCE outside this function —
                // reassigning them each tick would restart the CSS animation and
                // freeze the arrow on its first frame.
            }
        } catch (e) { /* swallow — next tick will retry */ }
    }

    updatePosition();
    let intervalId = setInterval(updatePosition, 100);

    // Cleanup — remove all injected DOM nodes after the specified duration
    setTimeout(function() {
        overlay.remove();
        if (pageDimContainer) pageDimContainer.remove();
        if (arrowWrap) arrowWrap.remove();
        clearInterval(intervalId);
    }, durationMs);

      return 'ok';
})(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4],
   arguments[5], arguments[6], arguments[7], arguments[8], arguments[9],
   arguments[10], arguments[11], arguments[12]);

} catch (e) {
  return 'js-error:' + (e && e.message ? e.message : String(e));
}
"

            Dim result As Object
            Try
                result =
                    jsExecutor.ExecuteScript(script, element,
                                             text, fontSize, textForeColor,
                                             showArrow, arrowAlignment.ToString(), arrowSize, arrowColor,
                                             borderColor, fillColor, fillOpacity.ToString(CultureInfo.InvariantCulture),
                                             pageDimOpacity.ToString(CultureInfo.InvariantCulture),
                                             durationMs)

            Catch ex As StaleElementReferenceException
                Throw New InvalidOperationException("Element became stale during highlight script execution.", ex)

            Catch ex As WebDriverException
                Throw New InvalidOperationException("WebDriver failed to execute the highlight script.", ex)

            End Try

            Dim status As String = If(result?.ToString(), String.Empty)
            Select Case status
                Case "ok"
                    Return True

                Case "no-element", "zero-rect"
                    Return False

                Case Else
                    If status.StartsWith("js-error:", StringComparison.Ordinal) Then
                        Throw New InvalidOperationException($"HighlightElement javascript failed in the browser: {status.Substring(9)}")
                    End If

                    Throw New InvalidOperationException($"HighlightElement javascript returned an unexpected status: '{status}'.")
            End Select

        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Determines whether a given browser log entry message refers to a resource URL
        ''' that points to a file (e.g., CSS, JS, image file) rather than a page/document.
        ''' </summary>
        ''' 
        ''' <param name="msg">
        ''' The raw log entry message containing the URL.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the URL in the message points to a file (has a filename with an extension); 
        ''' otherwise, <see langword="False"/>.
        ''' </returns>
        <DebuggerStepThrough>
        Private Function IsResourceUrlLogEntryMessage(msg As String) As Boolean

            Try
                Dim url As String = msg.Substring(0, msg.IndexOf(" "c))
                If url.Contains("?"c) Then
                    url = url.Substring(0, msg.IndexOf("?"c))
                End If

                Dim uri As New Uri(url)
                Dim filename As String = Path.GetFileName(uri.LocalPath)

                Return Not String.IsNullOrEmpty(filename) AndAlso
                   filename.Contains("."c) AndAlso
                   Not (filename.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) OrElse
                        filename.EndsWith(".html", StringComparison.OrdinalIgnoreCase) OrElse
                        filename.EndsWith(".php", StringComparison.OrdinalIgnoreCase)
                       )
            Catch
                Return False
            End Try
        End Function

#End Region

    End Module

End Namespace

#End Region
