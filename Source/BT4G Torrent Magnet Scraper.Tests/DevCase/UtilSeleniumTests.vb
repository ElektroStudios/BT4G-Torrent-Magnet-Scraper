#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports System.IO

Imports BT4GTorrentMagnetScraper.Tests.Infrastructure

Imports DevCase.ThirdParty.Selenium

Imports Microsoft.VisualStudio.TestTools.UnitTesting

#End Region

Namespace BT4GTorrentMagnetScraper.Tests.DevCaseTests

    ''' <summary>
    ''' Tests for the file-system members of the <see cref="UtilSelenium"/> class
    ''' (resolution of the latest cached ChromeDriver and Chrome executables).
    ''' </summary>
    <TestClass>
    Public NotInheritable Class UtilSeleniumTests

#Region " Constants "

        ''' <summary>
        ''' The name of the environment variable read when no Selenium cache directory is specified.
        ''' </summary>
        Private Const SeCachePathVariable As String = "SE_CACHE_PATH"

#End Region

#Region " Fields "

        ''' <summary>
        ''' The value of the <c>SE_CACHE_PATH</c> environment variable before running each test.
        ''' </summary>
        Private originalSeCachePath As String

#End Region

#Region " Test Initialization "

        ''' <summary>
        ''' Saves the value of the <c>SE_CACHE_PATH</c> environment variable.
        ''' </summary>
        <TestInitialize>
        Public Sub TestInitialize()

            Me.originalSeCachePath = Environment.GetEnvironmentVariable(UtilSeleniumTests.SeCachePathVariable, EnvironmentVariableTarget.Process)
        End Sub

        ''' <summary>
        ''' Restores the value of the <c>SE_CACHE_PATH</c> environment variable.
        ''' </summary>
        <TestCleanup>
        Public Sub TestCleanup()

            Environment.SetEnvironmentVariable(UtilSeleniumTests.SeCachePathVariable, Me.originalSeCachePath, EnvironmentVariableTarget.Process)
        End Sub

#End Region

#Region " GetLatestCachedChromeDriverFilePath "

        ''' <summary>
        ''' Verifies that the highest version directory is selected (by version, not alphabetically),
        ''' ignoring directories whose name is not a version.
        ''' </summary>
        <TestMethod>
        Public Sub GetLatestCachedChromeDriverFilePath_ReturnsHighestVersion()

            Using cache As New TemporaryDirectory()
                For Each dirName As String In {"99.0.4844.51", "120.0.6099.9", "120.0.6099.71", "latest", "1000-beta"}
                    cache.CreateSubdirectory("chromedriver", "win64", dirName)
                Next dirName

                Dim result As String = UtilSelenium.GetLatestCachedChromeDriverFilePath(cache.FullPath)

                Assert.AreEqual(cache.Combine("chromedriver", "win64", "120.0.6099.71", "chromedriver.exe"), result)
            End Using
        End Sub

        ''' <summary>
        ''' Verifies that the <c>SE_CACHE_PATH</c> environment variable is used when no path is specified.
        ''' </summary>
        <TestMethod>
        Public Sub GetLatestCachedChromeDriverFilePath_NoPath_UsesEnvironmentVariable()

            Using cache As New TemporaryDirectory()
                cache.CreateSubdirectory("chromedriver", "win64", "131.0.6778.85")
                Environment.SetEnvironmentVariable(UtilSeleniumTests.SeCachePathVariable, cache.FullPath, EnvironmentVariableTarget.Process)

                Dim result As String = UtilSelenium.GetLatestCachedChromeDriverFilePath()

                Assert.AreEqual(cache.Combine("chromedriver", "win64", "131.0.6778.85", "chromedriver.exe"), result)
            End Using
        End Sub

        ''' <summary>
        ''' Verifies that a missing path and a missing environment variable throw <see cref="InvalidOperationException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub GetLatestCachedChromeDriverFilePath_NoPathAndNoVariable_Throws()

            Environment.SetEnvironmentVariable(UtilSeleniumTests.SeCachePathVariable, Nothing, EnvironmentVariableTarget.Process)

            Assert.ThrowsExactly(Of InvalidOperationException)(
                Sub() UtilSelenium.GetLatestCachedChromeDriverFilePath())
        End Sub

        ''' <summary>
        ''' Verifies that a non-existent cache directory throws <see cref="DirectoryNotFoundException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub GetLatestCachedChromeDriverFilePath_MissingCacheDirectory_Throws()

            Using cache As New TemporaryDirectory()
                Dim missingDir As String = cache.Combine("does-not-exist")

                Assert.ThrowsExactly(Of DirectoryNotFoundException)(
                    Sub() UtilSelenium.GetLatestCachedChromeDriverFilePath(missingDir))
            End Using
        End Sub

        ''' <summary>
        ''' Verifies that a cache directory without the "chromedriver\win64" directory throws <see cref="DirectoryNotFoundException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub GetLatestCachedChromeDriverFilePath_MissingBaseDirectory_Throws()

            Using cache As New TemporaryDirectory()
                Assert.ThrowsExactly(Of DirectoryNotFoundException)(
                    Sub() UtilSelenium.GetLatestCachedChromeDriverFilePath(cache.FullPath))
            End Using
        End Sub

        ''' <summary>
        ''' Verifies that a base directory without version directories throws <see cref="InvalidOperationException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub GetLatestCachedChromeDriverFilePath_NoVersionDirectories_Throws()

            Using cache As New TemporaryDirectory()
                cache.CreateSubdirectory("chromedriver", "win64", "not-a-version")

                Assert.ThrowsExactly(Of InvalidOperationException)(
                    Sub() UtilSelenium.GetLatestCachedChromeDriverFilePath(cache.FullPath))
            End Using
        End Sub

#End Region

#Region " GetLatestCachedChromeFilePath "

        ''' <summary>
        ''' Verifies that the highest Chrome version directory is selected.
        ''' </summary>
        <TestMethod>
        Public Sub GetLatestCachedChromeFilePath_ReturnsHighestVersion()

            Using cache As New TemporaryDirectory()
                For Each dirName As String In {"9.9.9.9", "148.0.7778.0", "149.0.7827.155", "149.0.7827.16"}
                    cache.CreateSubdirectory("chrome", "win64", dirName)
                Next dirName

                Dim result As String = UtilSelenium.GetLatestCachedChromeFilePath(cache.FullPath)

                Assert.AreEqual(cache.Combine("chrome", "win64", "149.0.7827.155", "chrome.exe"), result)
            End Using
        End Sub

        ''' <summary>
        ''' Verifies that the ChromeDriver directory is not mistaken for the Chrome directory.
        ''' </summary>
        <TestMethod>
        Public Sub GetLatestCachedChromeFilePath_OnlyChromeDriverCached_Throws()

            Using cache As New TemporaryDirectory()
                cache.CreateSubdirectory("chromedriver", "win64", "149.0.7827.155")

                Assert.ThrowsExactly(Of DirectoryNotFoundException)(
                    Sub() UtilSelenium.GetLatestCachedChromeFilePath(cache.FullPath))
            End Using
        End Sub

        ''' <summary>
        ''' Verifies that a base directory without version directories throws <see cref="InvalidOperationException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub GetLatestCachedChromeFilePath_NoVersionDirectories_Throws()

            Using cache As New TemporaryDirectory()
                cache.CreateSubdirectory("chrome", "win64")

                Assert.ThrowsExactly(Of InvalidOperationException)(
                    Sub() UtilSelenium.GetLatestCachedChromeFilePath(cache.FullPath))
            End Using
        End Sub

#End Region

    End Class

End Namespace
