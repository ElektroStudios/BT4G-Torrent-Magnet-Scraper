
#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.ComponentModel
Imports System.IO
Imports System.Management
Imports System.Text

Imports OpenQA.Selenium
Imports OpenQA.Selenium.Chrome

Imports System.Text.RegularExpressions

Imports System.Reflection

Imports System.Diagnostics
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports System.Linq

#End Region

#Region " Selenium Util "

' ReSharper disable once CheckNamespace

Namespace DevCase.ThirdParty.Selenium

    ''' <summary>
    ''' Contains Selenium related utilities.
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
    <ImmutableObject(True)>
    Public NotInheritable Class UtilSelenium

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="UtilSelenium"/> class from being created.
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
        <DebuggerNonUserCode>
        Private Sub New()
        End Sub

#End Region

#Region " Public Methods "

        ''' <summary>
        ''' Initializes Selenium for the current process by configuring a 
        ''' local cache directory path through the <c>"SE_CACHE_PATH"</c> environment variable, 
        ''' and optionally forcing the download of latest Chrome and ChromeDriver executables into the specified cache directory.
        ''' <para></para>
        ''' This method must be called before any Selenium usage in the current process 
        ''' to ensure that Selenium Manager can locate the ChromeDriver and Chrome browser executables.
        ''' </summary>
        ''' 
        ''' <example>
        ''' This example shows how to initialize the environment:
        ''' <code language="VB">
        ''' Dim cacheDirPath As String = Path.Combine(AppContext.BaseDirectory, "Cache\Selenium")
        ''' Dim forceBrowserDownload As Boolean = True
        ''' Dim result As SeleniumEnvironmentInitializationResult = 
        '''     InitializeSeleniumEnvironmentForChrome(cacheDirPath, forceBrowserDownload)
        ''' 
        ''' Console.WriteLine($"Selenium Manager exit code: {result.SeleniumManagerExitCode}")
        ''' Console.WriteLine($"Driver located at: {result.DriverFilePath}")
        ''' Console.WriteLine($"Browser binary located at: {result.BrowserFilePath}")
        ''' </code>
        ''' </example>
        ''' 
        ''' <param name="cacheDirPath">
        ''' The directory path where Chrome and ChromeDriver will be stored. For example, <c>".\cache\Selenium"</c>
        ''' </param>
        ''' 
        ''' <param name="forceBrowserDownload">
        ''' A <see cref="Boolean"/> value indicating whether to force the download of latest Chrome 
        ''' binaries in the directory specified in <paramref name="cacheDirPath"/> parameter.
        ''' </param>
        ''' 
        ''' <param name="seleniumManagerFilePath">
        ''' Optional. Full path to <c>selenium-manager.exe</c> file.
        ''' <para></para>
        ''' If not specified, the default runtime path inside the current application directory is used: 
        ''' <c>".\runtimes\win\native\selenium-manager.exe"</c>
        ''' </param>
        ''' 
        ''' <returns>
        ''' A <see cref="SeleniumEnvironmentInitializationResult"/> object containing the Selenium Manager process exit code, 
        ''' resolved Selenium driver file path, and resolved browser binary file path.
        ''' </returns>
        ''' 
        ''' <exception cref="FileNotFoundException">
        ''' Thrown when the Selenium Manager file path cannot be resolved, 
        ''' or if the Selenium Manager process execution resolves driver or browser file paths that do not exist.
        ''' </exception>
        ''' 
        ''' <exception cref="TimeoutException">
        ''' Thrown when the execution of Selenium Manager process exceeds the allowed time limit (10 minutes).
        ''' </exception>
        ''' 
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when Selenium Manager execution completes successfully but the expected output information 
        ''' (driver and browser paths) cannot be determined or validated.
        ''' </exception>
        <DebuggerStepThrough>
        Public Shared Function InitializeSeleniumEnvironmentForChrome(cacheDirPath As String,
                                                                     forceBrowserDownload As Boolean,
                                                            Optional seleniumManagerFilePath As String = Nothing) As SeleniumEnvironmentInitializationResult

            If String.IsNullOrEmpty(seleniumManagerFilePath) Then
#If NETCOREAPP Then
                seleniumManagerFilePath = Path.Combine(AppContext.BaseDirectory, "runtimes\win\native\selenium-manager.exe")
#Else
                seleniumManagerFilePath = Path.Combine(My.Application.Info.DirectoryPath, "runtimes\win\native\selenium-manager.exe")
#End If
                If Not File.Exists(seleniumManagerFilePath) Then
                    Throw New FileNotFoundException("selenium-manager.exe not found.", seleniumManagerFilePath)
                End If
            End If

            ' Set env var for this process (it MUST be done before any Selenium usage).
            Environment.SetEnvironmentVariable("SE_CACHE_PATH", cacheDirPath, EnvironmentVariableTarget.Process)

            Dim argumentsList As New List(Of String) From {
                If(forceBrowserDownload, "--force-browser-download", String.Empty),
                "--browser chrome",
                "--driver chromedriver",
                $"--cache-path ""{cacheDirPath}"""
            }
            Dim arguments As String =
                argumentsList.Where(Function(arg) Not String.IsNullOrWhiteSpace(arg)).Aggregate(Function(acc, arg) $"{acc} {arg}")

            Dim outputBuilder As New StringBuilder()
            Dim errorBuilder As New StringBuilder()

            Using p As New Process
                With p.StartInfo
                    .FileName = seleniumManagerFilePath
                    .Arguments = arguments
                    .UseShellExecute = False
                    .RedirectStandardOutput = True
                    .RedirectStandardError = True
                    .CreateNoWindow = True
                    .StandardOutputEncoding = Encoding.UTF8
                    .StandardErrorEncoding = Encoding.UTF8
                    .WindowStyle = ProcessWindowStyle.Hidden
                End With

                AddHandler p.OutputDataReceived,
                    Sub(sender As Object, e As DataReceivedEventArgs)
                        If Not String.IsNullOrWhiteSpace(e.Data) Then
                            outputBuilder.AppendLine(e.Data)
                        End If
                    End Sub

                AddHandler p.ErrorDataReceived,
                    Sub(sender As Object, e As DataReceivedEventArgs)
                        If Not String.IsNullOrWhiteSpace(e.Data) Then
                            errorBuilder.AppendLine(e.Data)
                        End If
                    End Sub

                p.Start()
                p.BeginOutputReadLine()
                p.BeginErrorReadLine()

                Dim exited As Boolean = p.WaitForExit(CInt(TimeSpan.FromMinutes(10).TotalMilliseconds))
                If Not exited Then
                    Try
                        p.Kill()
                    Catch
                    End Try

                    Throw New TimeoutException("selenium-manager.exe execution timed out.")
                End If

                ' Ensure async buffers are fully flushed.
                p.WaitForExit()

                Dim combinedOutput As String = $"{outputBuilder}{Environment.NewLine}{errorBuilder}"
                ' Example:
                ' [INFO] Driver path: C:\...\chromedriver.exe
                ' [INFO] Browser path: C:\...\chrome.exe

                Dim driverMatch As Match =
                    Regex.Match(combinedOutput, "Driver path:\s*(.+?chromedriver\.exe)", RegexOptions.IgnoreCase Or RegexOptions.Multiline)

                Dim browserMatch As Match =
                    Regex.Match(combinedOutput, "Browser path:\s*(.+?chrome\.exe)", RegexOptions.IgnoreCase Or RegexOptions.Multiline)

                Dim result As New SeleniumEnvironmentInitializationResult(
                    p.ExitCode, combinedOutput,
                    If(driverMatch.Success, driverMatch.Groups(1).Value.Trim(), String.Empty),
                    If(browserMatch.Success, browserMatch.Groups(1).Value.Trim(), String.Empty)
                )

                ' Extra validation for successful execution.
                If p.ExitCode = 0 Then
                    If String.IsNullOrWhiteSpace(result.DriverFilePath) Then
                        Throw New InvalidOperationException(
                            $"Could not determine ChromeDriver path from Selenium Manager output.{Environment.NewLine}{Environment.NewLine}" &
                            $"Output:{Environment.NewLine}{combinedOutput}")
                    End If

                    If String.IsNullOrWhiteSpace(result.BrowserFilePath) Then
                        Throw New InvalidOperationException(
                            $"Could not determine Chrome browser path from Selenium Manager output.{Environment.NewLine}{Environment.NewLine}" &
                            $"Output:{Environment.NewLine}{combinedOutput}")
                    End If

                    If Not File.Exists(result.DriverFilePath) Then
                        Throw New FileNotFoundException(
                            "Resolved ChromeDriver.exe file was not found.", result.DriverFilePath)
                    End If

                    If Not File.Exists(result.BrowserFilePath) Then
                        Throw New FileNotFoundException(
                            "Resolved Chrome.exe file was not found.", result.BrowserFilePath)
                    End If
                End If

                Return result
            End Using

        End Function

        ''' <summary>
        ''' Asynchronously initializes Selenium for the current process by configuring a 
        ''' local cache directory path through the <c>"SE_CACHE_PATH"</c> environment variable, 
        ''' and optionally forcing the download of latest Chrome and ChromeDriver executables into the specified cache directory.
        ''' <para></para>
        ''' This method must be called before any Selenium usage in the current process 
        ''' to ensure that Selenium Manager can locate the ChromeDriver and Chrome browser executables.
        ''' </summary>
        ''' 
        ''' <example>
        ''' This example shows how to initialize the environment:
        ''' <code language="VB">
        ''' Dim cacheDirPath As String = Path.Combine(AppContext.BaseDirectory, "Cache\Selenium")
        ''' Dim forceBrowserDownload As Boolean = True
        ''' Dim result As SeleniumEnvironmentInitializationResult = 
        '''     Await InitializeSeleniumEnvironmentForChromeAsync(cacheDirPath, forceBrowserDownload)
        ''' 
        ''' Console.WriteLine($"Selenium Manager exit code: {result.SeleniumManagerExitCode}")
        ''' Console.WriteLine($"Driver located at: {result.DriverFilePath}")
        ''' Console.WriteLine($"Browser binary located at: {result.BrowserFilePath}")
        ''' </code>
        ''' </example>
        ''' 
        ''' <param name="cacheDirPath">
        ''' The directory path where Chrome and ChromeDriver will be stored. For example, <c>".\cache\Selenium"</c>
        ''' </param>
        ''' 
        ''' <param name="forceBrowserDownload">
        ''' A <see cref="Boolean"/> value indicating whether to force the download of latest Chrome 
        ''' binaries in the directory specified in <paramref name="cacheDirPath"/> parameter.
        ''' </param>
        ''' 
        ''' <param name="seleniumManagerFilePath">
        ''' Optional. Full path to <c>selenium-manager.exe</c> file.
        ''' <para></para>
        ''' If not specified, the default runtime path inside the current application directory is used: 
        ''' <c>".\runtimes\win\native\selenium-manager.exe"</c>
        ''' </param>
        ''' 
        ''' <returns>
        ''' A <see cref="Task(Of SeleniumEnvironmentInitializationResult)"/> representing the asynchronous operation,
        ''' containing the Selenium Manager process exit code, resolved Selenium driver file path, and resolved browser binary file path.
        ''' </returns>
        ''' 
        ''' <exception cref="FileNotFoundException">
        ''' Thrown when the Selenium Manager file path cannot be resolved, 
        ''' or if the Selenium Manager process execution resolves driver or browser file paths that do not exist.
        ''' </exception>
        ''' 
        ''' <exception cref="TimeoutException">
        ''' Thrown when the execution of Selenium Manager process exceeds the allowed time limit (10 minutes).
        ''' </exception>
        ''' 
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when Selenium Manager execution completes successfully but the expected output information 
        ''' (driver and browser paths) cannot be determined or validated.
        ''' </exception>
        <DebuggerStepThrough>
        Public Shared Async Function InitializeSeleniumEnvironmentForChromeAsync(cacheDirPath As String,
                                                                                 forceBrowserDownload As Boolean,
                                                                        Optional seleniumManagerFilePath As String = Nothing) As Task(Of SeleniumEnvironmentInitializationResult)

            Return Await Task.Run(
                Function() As SeleniumEnvironmentInitializationResult
                    Return InitializeSeleniumEnvironmentForChrome(cacheDirPath, forceBrowserDownload, seleniumManagerFilePath)
                End Function
            ).ConfigureAwait(continueOnCapturedContext:=False)

        End Function

        ''' <summary>
        ''' Resolves the full file path of the most recent (latest) cached Chrome browser binary version 
        ''' available in the specified Selenium cache directory path.
        ''' </summary>
        ''' 
        ''' <example> This is a code example.
        ''' <code language="VB">
        ''' Dim seleniumCacheDirPath As String = Nothing ' Or set to a specific path if desired. For example, ".\Cache\Selenium".
        ''' Dim latestChromeDriverFilePath As String = GetLatestCachedChromeDriverFilePath(seleniumCacheDirPath)
        ''' Console.WriteLine($"Latest ChromeDriver.exe File Path: {latestChromeDriverFilePath}")
        ''' </code>
        ''' </example>
        ''' 
        ''' <param name="seleniumCacheDirPath">
        ''' Optional. The Selenium cache directory path. For example, ".\Cache\Selenium".
        ''' <para></para>
        ''' This is the base directory where Selenium Manager stores downloaded browser binaries and drivers.
        ''' <para></para>
        ''' If this value is not set, the function will attempt to read the cache path from 
        ''' the <c>SE_CACHE_PATH</c> environment variable for the current process.
        ''' </param>
        ''' 
        ''' <returns>
        ''' A <see cref="String"/> containing the full file path to the latest Chrome version directory.
        ''' </returns>
        ''' 
        ''' <exception cref="DirectoryNotFoundException">
        ''' Thrown when the Selenium cache directory path does not exist, 
        ''' or when the base directory for Chrome versioned directories is not found.
        ''' </exception>
        ''' 
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when the Selenium cache directory path is not provided and <c>SE_CACHE_PATH</c> environment variable is not set, 
        ''' or when no valid versioned Chrome directories are found in the expected location.
        ''' </exception>
        <DebuggerStepThrough>
        Public Shared Function GetLatestCachedChromeDriverFilePath(Optional seleniumCacheDirPath As String = Nothing) As String

            If String.IsNullOrWhiteSpace(seleniumCacheDirPath) Then
                seleniumCacheDirPath = Environment.GetEnvironmentVariable("SE_CACHE_PATH", EnvironmentVariableTarget.Process)

                If String.IsNullOrWhiteSpace(seleniumCacheDirPath) Then
                    Throw New InvalidOperationException("Selenium cache directory path is not provided and SE_CACHE_PATH environment variable is not set.")
                End If

                If Not Directory.Exists(seleniumCacheDirPath) Then
                    Throw New DirectoryNotFoundException($"Selenium cache directory specified in SE_CACHE_PATH does not exist: {seleniumCacheDirPath}")
                End If

            ElseIf Not String.IsNullOrWhiteSpace(seleniumCacheDirPath) AndAlso Not Directory.Exists(seleniumCacheDirPath) Then
                Throw New DirectoryNotFoundException($"The provided Selenium cache directory path does not exist: {seleniumCacheDirPath}")
            End If

            Dim baseDirPath As String = Path.Combine(seleniumCacheDirPath, "chromedriver", "win64")
            If Not Directory.Exists(baseDirPath) Then
                Throw New DirectoryNotFoundException($"Base directory for ChromeDrover versioned directories does not exist: {baseDirPath}")
            End If

            Dim subDirs As String() = Directory.GetDirectories(baseDirPath, "*.*", SearchOption.TopDirectoryOnly)

            Dim latestVersionedDir As String =
            subDirs.Select(Function(dir As String) New With {.Path = dir, .FolderName = Path.GetFileName(dir)}).
                    Where(Function(x)
                              Dim ver As Version = Nothing
                              Return Version.TryParse(x.FolderName, ver)
                          End Function).
                        OrderByDescending(Function(x) New Version(x.FolderName)).
                        Select(Function(x) x.Path).
                        FirstOrDefault()

            If String.IsNullOrEmpty(latestVersionedDir) Then
                Throw New InvalidOperationException($"No valid versioned ChromeDriver directories were found in: ""{baseDirPath}""")
            End If

            Return Path.Combine(latestVersionedDir, "chromedriver.exe")
        End Function

        ''' <summary>
        ''' Resolves the full file path of the most recent (latest) cached Chrome browser binary version 
        ''' available in the specified Selenium cache directory path.
        ''' </summary>
        ''' 
        ''' <example> This is a code example.
        ''' <code language="VB">
        ''' Dim seleniumCacheDirPath As String = Nothing ' Or set to a specific path if desired. For example, ".\Cache\Selenium".
        ''' Dim latestChromeFilePath As String = GetLatestCachedChromeFilePath(seleniumCacheDirPath)
        ''' Console.WriteLine($"Latest Chrome.exe File Path: {latestChromeFilePath}")
        ''' </code>
        ''' </example>
        ''' 
        ''' <param name="seleniumCacheDirPath">
        ''' Optional. The Selenium cache directory path. For example, ".\Cache\Selenium".
        ''' <para></para>
        ''' This is the base directory where Selenium Manager stores downloaded browser binaries and drivers.
        ''' <para></para>
        ''' If this value is not set, the function will attempt to read the cache path from 
        ''' the <c>SE_CACHE_PATH</c> environment variable for the current process.
        ''' </param>
        ''' 
        ''' <returns>
        ''' A <see cref="String"/> containing the full file path to the latest Chrome version directory.
        ''' </returns>
        ''' 
        ''' <exception cref="DirectoryNotFoundException">
        ''' Thrown when the Selenium cache directory path does not exist, 
        ''' or when the base directory for Chrome versioned directories is not found.
        ''' </exception>
        ''' 
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when the Selenium cache directory path is not provided and <c>SE_CACHE_PATH</c> environment variable is not set, 
        ''' or when no valid versioned Chrome directories are found in the expected location.
        ''' </exception>
        <DebuggerStepThrough>
        Public Shared Function GetLatestCachedChromeFilePath(Optional seleniumCacheDirPath As String = Nothing) As String

            If String.IsNullOrWhiteSpace(seleniumCacheDirPath) Then
                seleniumCacheDirPath = Environment.GetEnvironmentVariable("SE_CACHE_PATH", EnvironmentVariableTarget.Process)

                If String.IsNullOrWhiteSpace(seleniumCacheDirPath) Then
                    Throw New InvalidOperationException("Selenium cache directory path is not provided and SE_CACHE_PATH environment variable is not set.")
                End If

                If Not Directory.Exists(seleniumCacheDirPath) Then
                    Throw New DirectoryNotFoundException($"Selenium cache directory specified in SE_CACHE_PATH does not exist: {seleniumCacheDirPath}")
                End If

            ElseIf Not String.IsNullOrWhiteSpace(seleniumCacheDirPath) AndAlso Not Directory.Exists(seleniumCacheDirPath) Then
                Throw New DirectoryNotFoundException($"The provided Selenium cache directory path does not exist: {seleniumCacheDirPath}")
            End If

            Dim baseDirPath As String = Path.Combine(seleniumCacheDirPath, "chrome", "win64")
            If Not Directory.Exists(baseDirPath) Then
                Throw New DirectoryNotFoundException($"Base directory for Chrome versioned directories does not exist: {baseDirPath}")
            End If

            Dim subDirs As String() = Directory.GetDirectories(baseDirPath, "*.*", SearchOption.TopDirectoryOnly)

            Dim latestVersionedDir As String =
            subDirs.Select(Function(dir As String) New With {.Path = dir, .FolderName = Path.GetFileName(dir)}).
                    Where(Function(x)
                              Dim ver As Version = Nothing
                              Return Version.TryParse(x.FolderName, ver)
                          End Function).
                        OrderByDescending(Function(x) New Version(x.FolderName)).
                        Select(Function(x) x.Path).
                        FirstOrDefault()

            If String.IsNullOrEmpty(latestVersionedDir) Then
                Throw New InvalidOperationException($"No valid versioned Chrome directories were found in: ""{baseDirPath}""")
            End If

            Return Path.Combine(latestVersionedDir, "chrome.exe")
        End Function

        ''' <summary>
        ''' Initializes and returns a <see cref="ChromeDriver"/> instance 
        ''' with a preconfigured set of options optimized for browser automation, 
        ''' and optionally hiding the Chrome window if not running in headless mode and specified by the caller.
        ''' </summary>
        ''' 
        ''' <example> This is a code example.
        ''' <code language="VB">
        ''' Dim driverService As ChromeDriverService = Nothing
        ''' Dim headless As Boolean = False
        ''' Dim hideNonHeadlessWindow As Boolean = False
        ''' Dim driverFilePath As String = Nothing    ' Optional, as the function automatically resolves the latest cached ChromeDriver.exe file path.
        ''' Dim driverLogFilePath As String = Nothing ' Optional, as the function automatically resolves a default log file path if no specified.
        ''' Dim chromeFilePath As String = Nothing    ' Optional, as the function automatically resolves the latest cached Chrome.exe file path.
        ''' Dim userDataDir As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache\Chrome User Data")
        ''' Dim profileName As String = "Automation Profile"
        ''' Dim additionalArguments As String() = {"--window-position=0,0"}
        ''' 
        ''' Try
        '''     Using driver As ChromeDriver = CreateOptimizedChromeDriver(
        '''         driverService,
        '''         driverFilePath:=driverFilePath,
        '''         driverLogFilePath:=driverLogFilePath,
        '''         chromeFilePath:=chromeFilePath,
        '''         userDataDir:=userDataDir,
        '''         profileName:=profileName,
        '''         headless:=headless,
        '''         hideNonHeadlessWindow:=hideNonHeadlessWindow,
        '''         additionalArguments)
        ''' 
        '''         driver.Navigate().GoToUrl("https://www.google.com/")
        '''         Console.WriteLine($"Page Title: {driver.Title}")
        '''     End Using
        ''' Finally
        '''     driverService?.Dispose()
        ''' End Try
        ''' </code>
        ''' </example>
        ''' 
        ''' <param name="refDriverService">
        ''' When this function returns, receives the <see cref="ChromeDriverService"/> instance created.
        ''' </param>
        ''' 
        ''' <param name="driverFilePath">
        ''' File path to ChromeDriver.exe; For example, ".\chromedriver.exe"
        ''' <para></para>
        ''' This value can be null, in which case the function will attempt to resolve the latest cached ChromeDriver.exe file path 
        ''' from the Selenium cache directory (as configured in <c>SE_CACHE_PATH</c> environment variable) 
        ''' using the <see cref="UtilSelenium.GetLatestCachedChromeDriverFilePath"/> function.
        ''' </param>
        ''' 
        ''' <param name="driverLogFilePath">
        ''' Full path to the log file where ChromeDriver logs will be written. For example, ".\chromedriver.log"
        ''' <para></para>
        ''' This value can be null, in which case a default log file path is used in the same directory as the 
        ''' <paramref name="userDataDir"/> dierctory path, with the name "chromedriver.log".
        ''' </param>
        ''' 
        ''' <param name="chromeFilePath">
        ''' File path to Chrome.exe; For example, "C:\Program Files\Google Chrome\chrome.exe"
        ''' <para></para>
        ''' This value can be null, in which case the function will attempt to resolve the latest cached Chrome.exe file path 
        ''' from the Selenium cache directory (as configured in <c>SE_CACHE_PATH</c> environment variable) 
        ''' using the <see cref="UtilSelenium.GetLatestCachedChromeFilePath"/> function.
        ''' </param>
        ''' 
        ''' <param name="userDataDir">
        ''' The directory path for Chrome's user data. For example, ".\Cache\Chrome User Data"
        ''' <para></para>
        ''' This directory is used to store user-specific settings and data.
        ''' <para></para>
        ''' This value can be null, in which case the function will attempt to resolve the 
        ''' Selenium cache directory (as configured in <c>SE_CACHE_PATH</c> environment variable) 
        ''' and create a subdirectory inside it for Chrome user data, with the name "chrome user data". 
        ''' If the Selenium cache directory cannot be resolved, the function will create the 
        ''' user data directory inside the current application base directory, with the name "chrome user data".
        ''' </param>
        ''' 
        ''' <param name="profileName">
        ''' The name of the Chrome profile directory to use within the <paramref name="userDataDir"/>. 
        ''' For example, "Default" or "Profile 1".
        ''' <para></para>
        ''' This value can be null, in which case "Default" profile name is assumed.
        ''' </param>
        ''' 
        ''' <param name="headless">
        ''' If <see langword="True"/>, launches Chrome in headless mode.
        ''' </param>
        ''' 
        ''' <param name="hideNonHeadlessWindow">
        ''' If <see langword="True"/> and <paramref name="headless"/> is set to <see langword="False"/>, 
        ''' attempts to hide the Chrome window and taskbar icon.
        ''' <para></para>
        ''' Has no effect if <paramref name="headless"/> is set to <see langword="True"/>, 
        ''' since Chrome windows are not shown in headless mode.
        ''' </param>
        ''' 
        ''' <param name="additionalArguments">
        ''' Optional. Additional arguments to add to the underlying <see cref="ChromeOptions"/> object 
        ''' through its <see cref="ChromeOptions.AddArguments"/> method. 
        ''' <para></para>
        ''' For example, when not running in headless mode you can use 
        ''' <c>"--window-position=-32000,0"</c> to set the Chrome window off-screen, 
        ''' or <c>"--window-size=1200,800"</c> to set the initial Chrome window size.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The resulting <see cref="ChromeDriver"/> instance.
        ''' </returns>
        <DebuggerStepThrough>
        Public Shared Function CreateOptimizedChromeDriver(ByRef refDriverService As ChromeDriverService,
                                                                 driverFilePath As String, driverLogFilePath As String,
                                                                 chromeFilePath As String, userDataDir As String, profileName As String,
                                                                 headless As Boolean, hideNonHeadlessWindow As Boolean,
                                                      ParamArray additionalArguments As String()) As ChromeDriver

            If String.IsNullOrWhiteSpace(driverFilePath) Then
                Try
                    driverFilePath = UtilSelenium.GetLatestCachedChromeDriverFilePath()
                Catch ex As Exception
                    Throw New InvalidOperationException($"Failed to resolve ChromeDriver.exe file path from Selenium cache. See inner exception for details.", ex)
                End Try
            ElseIf Not String.IsNullOrWhiteSpace(driverFilePath) AndAlso Not File.Exists(driverFilePath) Then
                Throw New FileNotFoundException($"ChromeDriver.exe not found at the specified path: {driverFilePath}", driverFilePath)
            End If
            Dim driverDirPath As String = Path.GetDirectoryName(driverFilePath)
            Dim driveFileName As String = Path.GetFileName(driverFilePath)

            If String.IsNullOrWhiteSpace(chromeFilePath) Then
                Try
                    chromeFilePath = UtilSelenium.GetLatestCachedChromeFilePath()
                Catch ex As Exception
                    Throw New InvalidOperationException($"Failed to resolve Chrome.exe file path from Selenium cache. See inner exception for details.", ex)
                End Try
            ElseIf Not String.IsNullOrWhiteSpace(chromeFilePath) AndAlso Not File.Exists(chromeFilePath) Then
                Throw New FileNotFoundException($"Chrome.exe not found at the specified path: {chromeFilePath}", chromeFilePath)
            End If

            If String.IsNullOrWhiteSpace(userDataDir) Then
                Dim seCachePathEnvVar As String = Environment.GetEnvironmentVariable("SE_CACHE_PATH", EnvironmentVariableTarget.Process)

                If Not String.IsNullOrWhiteSpace(seCachePathEnvVar) Then
                    Dim ass As Assembly = Assembly.GetEntryAssembly()
                    Dim assName As String = ass.GetName().Name
                    userDataDir = Path.Combine(seCachePathEnvVar, $"chrome user data - {assName}")
                Else
                    userDataDir = Path.Combine(AppContext.BaseDirectory, "chrome user data")
                End If
            End If
            userDataDir = Path.GetFullPath(userDataDir)

            If String.IsNullOrWhiteSpace(profileName) Then
                profileName = "Default"
            End If

            If String.IsNullOrWhiteSpace(driverLogFilePath) Then
                driverLogFilePath = Path.Combine(userDataDir, $"{profileName}\chromedriver.log")
            End If

            Dim options As New ChromeOptions() With {
                .AcceptInsecureCertificates = True,
                .EnableDownloads = False,
                .LeaveBrowserRunning = False,
                .UnhandledPromptBehavior = UnhandledPromptBehavior.Ignore,
                .UseStrictFileInteractability = True,
                .BinaryLocation = chromeFilePath
            }

            options.AddAdditionalOption("useAutomationExtension", False)
            options.AddExcludedArgument("enable-automation")

            ' Add a set of default arguments optimized for browser automation.
            Dim currentArgs As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
                "--allow-insecure-localhost",
                "--disable-background-networking",
                "--disable-backgrounding-occluded-windows",
                "--disable-blink-features=AutomationControlled",
                "--disable-default-apps",
                "--disable-dev-shm-usage",
                "--disable-features=Translate,TranslateUI",
                "--disable-hang-monitor",
                "--disable-infobars", ' Suppress the "Chrome for Testing {version} is only for automated testing." bar
                "--disable-notifications",
                "--disable-popup-blocking",
                "--disable-prompt-on-repost",
                "--disable-sync",
                "--ignore-certificate-errors",
                "--ignore-ssl-errors",
                "--lang=en-US",
                "--no-first-run",
                "--no-sandbox",
                "--noerrdialogs",
                "--remote-debugging-port=0", ' Or "--remote-debugging-pipe"
                "--test-type",
                $"--user-data-dir={userDataDir}",    ' Do NOT use double quotes around the path, as Chrome does not recognize it.
                $"--profile-directory={profileName}" ' Do NOT use double quotes around the name, as Chrome does not recognize it.
            }

            ' Add additional specific optimized arguments if headless mode is specified by the caller.
            If headless Then
                Dim headlessAdditionalArgs As String() = {
                    "--headless=new",
                    "--start-maximized",
                    "--disable-site-isolation-trials",
                    "--disable-web-security",
                    "--disable-gpu"
                }

                For Each arg As String In headlessAdditionalArgs
                    currentArgs.Add(arg)
                Next arg
            End If

            ' Add additional arguments specified by the caller, avoiding duplicates with the current arguments.
            If additionalArguments IsNot Nothing AndAlso additionalArguments.Length > 0 Then

                Dim filteredAdditionalArgs As IEnumerable(Of String) =
                    From arg As String In additionalArguments
                    Where Not String.IsNullOrWhiteSpace(arg)
                    Select arg.Trim()

                For Each arg As String In additionalArguments
                    currentArgs.Add(arg)
                Next arg
            End If

            ' Add the final set of arguments to ChromeOptions.
            options.AddArguments(currentArgs)

            ' Initialize ChromeDriverService with the specified driver file path and log file path, and configure its options.
            refDriverService = ChromeDriverService.CreateDefaultService(driverDirPath, driveFileName)
            With refDriverService
                .DisableBuildCheck = False
                .EnableAppendLog = False
                .EnableVerboseLogging = False
                .HideCommandPromptWindow = True
                .LogLevel = Chromium.ChromiumDriverLogLevel.Info
                .LogPath = driverLogFilePath
                .ReadableTimestamp = True
                .SuppressInitialDiagnosticInformation = False ' Note: If True, it hangs ChromeDriver initialization.
            End With

            ' chromedriver.exe sometimes may cause an error if log file does not exists.
            If Not File.Exists(driverLogFilePath) Then
                Try
                    Directory.CreateDirectory(Path.GetDirectoryName(driverLogFilePath))
                Catch
                End Try
                Try
                    File.WriteAllText(driverLogFilePath, String.Empty)
                Catch
                End Try
            End If

            Dim driver As New ChromeDriver(refDriverService, options)

            ' ----------------------------------------------------------------------------------------------------------------------------------
            ' 'hideNonHeadlessWindow' FUNCTIONALITY IS NOT  IMPLEMENTED IN THIS PROJECT DUE ADDITIONAL REQUIREMENTS FROM ELEKTROSTUDIOS' DEVCASE
            ' ----------------------------------------------------------------------------------------------------------------------------------
            '
            ' Hide the Chrome.exe window / taskbar icon if not running in headless mode and specified by the caller.
            If Not headless AndAlso hideNonHeadlessWindow Then
                'Dim chromeChilds As List(Of Process) = UtilSelenium.GetChildProcesses(refDriverService.ProcessId)
                'For Each chromeChild As Process In chromeChilds
                '    Dim hwnd As IntPtr = chromeChild.MainWindowHandle
                '    If hwnd <> IntPtr.Zero Then
                '        NativeMethods.ShowWindow(hwnd, NativeWindowState.Hide)
                '    End If
                'Next

                Throw New NotImplementedException("'hideNonHeadlessWindow' FUNCTIONALITY IS NOT IMPLEMENTED IN THIS PROJECT DUE ADDITIONAL REQUIREMENTS FROM ELEKTROSTUDIOS' DEVCASE.")
            End If

            Return driver
        End Function

        ''' <summary>
        ''' Forcefully terminates all instances of a specific Selenium driver process (e.g., "chromedriver") 
        ''' that were spawned as child processes of the current application.
        ''' </summary>
        ''' 
        ''' <remarks>
        ''' This method is intended as a cleanup safeguard to prevent orphaned driver and child browser processes
        ''' when the application shuts down unexpectedly or fails to properly dispose Selenium sessions.
        ''' <para></para>
        ''' It ensures that any child processes created during automated browser sessions are fully terminated, 
        ''' avoiding background instances that may continue running after the current process exits.
        ''' </remarks>
        ''' 
        ''' <param name="driverName">
        ''' The name of the driver executable to terminate, without the extension. 
        ''' For example, <c>"chromedriver"</c>.
        ''' </param>
        <DebuggerStepThrough>
        Public Shared Sub KillDriverAndChildBrowsers(driverName As String)

#If NETCOREAPP Then
            ArgumentNullException.ThrowIfNullOrWhiteSpace(driverName, NameOf(driverName))

            Dim processId As Integer = Environment.ProcessId
#Else
            If String.IsNullOrWhiteSpace(driverName) Then
                Throw New ArgumentNullException(NameOf(driverName))
            End If

            Dim processId As Integer = Process.GetCurrentProcess().Id
#End If
            Dim childProcesses As List(Of Process) = UtilSelenium.GetChildProcesses(processId)

            Dim childDriverProcesses As IEnumerable(Of Process) =
                From p As Process In childProcesses
                Where Not p?.HasExited AndAlso p?.ProcessName.Equals(driverName, StringComparison.OrdinalIgnoreCase)

            For Each p As Process In childDriverProcesses

#If NETCOREAPP Then
                p.Kill(entireProcessTree:=True)
#Else
                Using taskkillProcess As New Process()
                    taskkillProcess.StartInfo.FileName = "taskkill"
                    taskkillProcess.StartInfo.Arguments = $"/PID {p.Id} /T /F"
                    taskkillProcess.StartInfo.UseShellExecute = False
                    taskkillProcess.StartInfo.CreateNoWindow = True

                    taskkillProcess.Start()
                End Using
#End If
            Next

        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Retrieves a list of child processes for the specified parent process ID.
        ''' </summary>
        ''' 
        ''' <param name="parentProcessId">
        ''' The ID of the parent process whose child processes should be retrieved.
        ''' </param>
        ''' 
        ''' <returns>
        ''' A <see cref="List(Of Process)"/> containing all child processes of the specified parent process.
        ''' </returns>
        <DebuggerStepThrough>
        Private Shared Function GetChildProcesses(parentProcessId As Integer) As List(Of Process)

            Dim children As New List(Of Process)()

            Dim scope As New ManagementScope("root\CIMV2")
            Dim query As New ObjectQuery($"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId={parentProcessId}")
            Dim options As New System.Management.EnumerationOptions() With {
                .EnsureLocatable = False,
                .ReturnImmediately = True,
                .Rewindable = False,
                .Timeout = TimeSpan.FromSeconds(5)
            }

            scope.Connect()

            Using searcher As New ManagementObjectSearcher(scope, query, options)

                For Each proc As ManagementObject In searcher.Get()

                    Dim pid As Integer = Convert.ToInt32(proc("ProcessId"))
                    Try
                        Dim childProc As Process = Process.GetProcessById(pid)
                        children.Add(childProc)
                    Catch ' Ignore. Process may no longer exists.
                    End Try
                Next
            End Using

            Return children
        End Function

#End Region

    End Class

End Namespace

#End Region
