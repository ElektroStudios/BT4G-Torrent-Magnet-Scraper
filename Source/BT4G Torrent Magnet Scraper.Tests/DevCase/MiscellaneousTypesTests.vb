#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports DevCase.ThirdParty.Selenium

Imports Microsoft.VisualStudio.TestTools.UnitTesting

#End Region

Namespace BT4GTorrentMagnetScraper.Tests.DevCaseTests

    ''' <summary>
    ''' Tests for the <see cref="SeleniumEnvironmentInitializationResult"/> and <see cref="ConsoleTerminationWatcher"/> classes.
    ''' </summary>
    <TestClass>
    Public NotInheritable Class MiscellaneousTypesTests

#Region " SeleniumEnvironmentInitializationResult "

        ''' <summary>
        ''' Verifies that the constructor stores the specified values.
        ''' </summary>
        <TestMethod>
        Public Sub SeleniumEnvironmentInitializationResult_StoresValues()

            Dim result As New SeleniumEnvironmentInitializationResult(
                seleniumManagerExitCode:=0, seleniumManagerConsoleOutput:="output",
                driverFilePath:="C:\cache\chromedriver.exe", browserFilePath:="C:\cache\chrome.exe")

            Assert.AreEqual(0, result.SeleniumManagerExitCode)
            Assert.AreEqual("output", result.SeleniumManagerConsoleOutput)
            Assert.AreEqual("C:\cache\chromedriver.exe", result.DriverFilePath)
            Assert.AreEqual("C:\cache\chrome.exe", result.BrowserFilePath)
        End Sub

#End Region

#Region " ConsoleTerminationWatcher "

        ''' <summary>
        ''' Verifies that a null cleanup action throws <see cref="ArgumentNullException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub ConsoleTerminationWatcher_NullAction_Throws()

            Assert.ThrowsExactly(Of ArgumentNullException)(
                Sub()
                    Using unused As New ConsoleTerminationWatcher(Nothing)
                    End Using
                End Sub)
        End Sub

        ''' <summary>
        ''' Verifies that the watcher registers and unregisters the console control handler without invoking
        ''' the cleanup action, and that disposing it twice is safe.
        ''' </summary>
        <TestMethod>
        <TestCategory("Win32")>
        Public Sub ConsoleTerminationWatcher_CreateAndDispose_DoesNotInvokeCleanup()

            Dim cleanupInvoked As Boolean = False

            Dim watcher As New ConsoleTerminationWatcher(Sub() cleanupInvoked = True)
            watcher.Dispose()
            watcher.Dispose()

            Assert.IsFalse(cleanupInvoked)
        End Sub

#End Region

    End Class

End Namespace
