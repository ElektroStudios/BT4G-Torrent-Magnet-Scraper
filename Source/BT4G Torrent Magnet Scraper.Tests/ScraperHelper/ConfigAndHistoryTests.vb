#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports System.IO
Imports System.Text

Imports BT4GTorrentMagnetScraper.Tests.Infrastructure

Imports Microsoft.VisualStudio.TestTools.UnitTesting

#End Region

Namespace BT4GTorrentMagnetScraper.Tests

    ''' <summary>
    ''' Tests for the config and URL history functions of <see cref="ScraperHelper"/>.
    ''' </summary>
    <TestClass>
    Public NotInheritable Class ConfigAndHistoryTests

#Region " ParseConfigLines "

        ''' <summary>
        ''' Verifies that empty lines, white-space lines and comment lines are skipped.
        ''' </summary>
        <TestMethod>
        Public Sub ParseConfigLines_SkipsEmptyWhiteSpaceAndCommentLines()

            Dim lines As String() = {"# header", "", "   ", "John Wick", "    # indented comment", vbTab, "John Wick 2"}

            Dim result As HashSet(Of String) = ScraperHelper.ParseConfigLines(lines, trimLines:=False)

            CollectionAssert.AreEquivalent({"John Wick", "John Wick 2"}, result.ToArray())
        End Sub

        ''' <summary>
        ''' Verifies that lines are kept verbatim when <c>trimLines</c> is False,
        ''' because keywords such as " sd" rely on their leading white-space.
        ''' </summary>
        <TestMethod>
        Public Sub ParseConfigLines_WithoutTrimming_PreservesSurroundingWhiteSpace()

            Dim lines As String() = {" sd", "+sd ", "720p"}

            Dim result As HashSet(Of String) = ScraperHelper.ParseConfigLines(lines, trimLines:=False)

            CollectionAssert.AreEquivalent({" sd", "+sd ", "720p"}, result.ToArray())
        End Sub

        ''' <summary>
        ''' Verifies that lines are trimmed when <c>trimLines</c> is True.
        ''' </summary>
        <TestMethod>
        Public Sub ParseConfigLines_WithTrimming_TrimsLines()

            Dim lines As String() = {"  category=movie ", vbTab & "orderby=seeders"}

            Dim result As HashSet(Of String) = ScraperHelper.ParseConfigLines(lines, trimLines:=True)

            CollectionAssert.AreEquivalent({"category=movie", "orderby=seeders"}, result.ToArray())
        End Sub

        ''' <summary>
        ''' Verifies that duplicated lines are returned only once, and that the comparison is case-sensitive.
        ''' </summary>
        <TestMethod>
        Public Sub ParseConfigLines_RemovesExactDuplicates_ButIsCaseSensitive()

            Dim lines As String() = {"1080p", "1080p", "1080P"}

            Dim result As HashSet(Of String) = ScraperHelper.ParseConfigLines(lines, trimLines:=False)

            Assert.AreEqual(2, result.Count)
            Assert.IsTrue(result.Contains("1080p"))
            Assert.IsTrue(result.Contains("1080P"))
        End Sub

        ''' <summary>
        ''' Verifies that an input with only comments returns an empty set.
        ''' </summary>
        <TestMethod>
        Public Sub ParseConfigLines_OnlyComments_ReturnsEmptySet()

            Dim result As HashSet(Of String) = ScraperHelper.ParseConfigLines({"# a", "# b"}, trimLines:=True)

            Assert.AreEqual(0, result.Count)
        End Sub

        ''' <summary>
        ''' Verifies that a null input throws <see cref="ArgumentNullException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub ParseConfigLines_Null_Throws()

            Assert.ThrowsExactly(Of ArgumentNullException)(
                Sub() ScraperHelper.ParseConfigLines(Nothing, trimLines:=False))
        End Sub

#End Region

#Region " ParseHistoryLines "

        ''' <summary>
        ''' Verifies that history lines are trimmed and comments/blank lines are skipped.
        ''' </summary>
        <TestMethod>
        Public Sub ParseHistoryLines_TrimsAndSkipsCommentsAndBlankLines()

            Dim lines As String() = {"# history", "  ABC123  ", "", "DEF456", Nothing}

            Dim result As HashSet(Of String) = ScraperHelper.ParseHistoryLines(lines)

            CollectionAssert.AreEquivalent({"ABC123", "DEF456"}, result.ToArray())
        End Sub

        ''' <summary>
        ''' Verifies that the returned history set is case-insensitive.
        ''' </summary>
        <TestMethod>
        Public Sub ParseHistoryLines_ReturnsCaseInsensitiveSet()

            Dim result As HashSet(Of String) = ScraperHelper.ParseHistoryLines({"abcdef", "ABCDEF"})

            Assert.AreEqual(1, result.Count)
            Assert.IsTrue(result.Contains("AbCdEf"))
        End Sub

        ''' <summary>
        ''' Verifies that a null input throws <see cref="ArgumentNullException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub ParseHistoryLines_Null_Throws()

            Assert.ThrowsExactly(Of ArgumentNullException)(
                Sub() ScraperHelper.ParseHistoryLines(Nothing))
        End Sub

#End Region

#Region " LoadUrlsHistory / AppendUrlToHistory "

        ''' <summary>
        ''' Verifies that a missing history file returns an empty, case-insensitive set.
        ''' </summary>
        <TestMethod>
        Public Sub LoadUrlsHistory_MissingFile_ReturnsEmptyCaseInsensitiveSet()

            Using tempDir As New TemporaryDirectory()
                Dim result As HashSet(Of String) = ScraperHelper.LoadUrlsHistory(tempDir.Combine("missing.txt"))

                Assert.AreEqual(0, result.Count)
                result.Add("abc")
                Assert.IsTrue(result.Contains("ABC"))
            End Using
        End Sub

        ''' <summary>
        ''' Verifies that an existing history file (UTF-8 with BOM) is loaded correctly.
        ''' </summary>
        <TestMethod>
        Public Sub LoadUrlsHistory_ExistingFile_LoadsHashes()

            Using tempDir As New TemporaryDirectory()
                Dim historyFilePath As String = tempDir.Combine("history.txt")
                File.WriteAllText(historyFilePath, $"AAA{Environment.NewLine}# comment{Environment.NewLine}{Environment.NewLine}BBB{Environment.NewLine}", New UTF8Encoding(True))

                Dim result As HashSet(Of String) = ScraperHelper.LoadUrlsHistory(historyFilePath)

                CollectionAssert.AreEquivalent({"AAA", "BBB"}, result.ToArray())
            End Using
        End Sub

        ''' <summary>
        ''' Verifies that an empty history file path throws <see cref="ArgumentException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub LoadUrlsHistory_EmptyPath_Throws()

            Assert.ThrowsExactly(Of ArgumentException)(
                Sub() ScraperHelper.LoadUrlsHistory(" "))
        End Sub

        ''' <summary>
        ''' Verifies that a new hash is added both to the set and to the history file,
        ''' creating the missing parent directory.
        ''' </summary>
        <TestMethod>
        Public Sub AppendUrlToHistory_NewHash_AddsToSetAndFile()

            Using tempDir As New TemporaryDirectory()
                Dim historyFilePath As String = tempDir.Combine("cache", "BT4G History.txt")
                Dim history As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

                Dim added As Boolean = ScraperHelper.AppendUrlToHistory(historyFilePath, history, "ABC123", New UTF8Encoding(True))

                Assert.IsTrue(added)
                Assert.IsTrue(history.Contains("ABC123"))
                Assert.IsTrue(File.Exists(historyFilePath))
                CollectionAssert.AreEqual({"ABC123"}, File.ReadAllLines(historyFilePath, Encoding.UTF8))
            End Using
        End Sub

        ''' <summary>
        ''' Verifies that an already present hash (case-insensitive) is not written again to the history file.
        ''' </summary>
        <TestMethod>
        Public Sub AppendUrlToHistory_DuplicateHash_IsNotWrittenTwice()

            Using tempDir As New TemporaryDirectory()
                Dim historyFilePath As String = tempDir.Combine("history.txt")
                Dim history As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                Dim utf8WithBom As New UTF8Encoding(True)

                Dim first As Boolean = ScraperHelper.AppendUrlToHistory(historyFilePath, history, "abc123", utf8WithBom)
                Dim second As Boolean = ScraperHelper.AppendUrlToHistory(historyFilePath, history, "ABC123", utf8WithBom)
                Dim third As Boolean = ScraperHelper.AppendUrlToHistory(historyFilePath, history, "DEF456", utf8WithBom)

                Assert.IsTrue(first)
                Assert.IsFalse(second)
                Assert.IsTrue(third)
                CollectionAssert.AreEqual({"abc123", "DEF456"}, File.ReadAllLines(historyFilePath, Encoding.UTF8))
            End Using
        End Sub

        ''' <summary>
        ''' Verifies that the appended history can be loaded back (round-trip).
        ''' </summary>
        <TestMethod>
        Public Sub AppendUrlToHistory_ThenLoadUrlsHistory_RoundTrips()

            Using tempDir As New TemporaryDirectory()
                Dim historyFilePath As String = tempDir.Combine("history.txt")
                Dim history As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                For Each hash As String In {"H1", "H2", "H3"}
                    ScraperHelper.AppendUrlToHistory(historyFilePath, history, hash, New UTF8Encoding(True))
                Next hash

                Dim loaded As HashSet(Of String) = ScraperHelper.LoadUrlsHistory(historyFilePath)

                Assert.IsTrue(loaded.SetEquals(history))
            End Using
        End Sub

        ''' <summary>
        ''' Verifies the argument validation of <see cref="ScraperHelper.AppendUrlToHistory"/>.
        ''' </summary>
        <TestMethod>
        Public Sub AppendUrlToHistory_InvalidArguments_Throw()

            Dim history As New HashSet(Of String)()
            Dim utf8 As Encoding = Encoding.UTF8

            Assert.ThrowsExactly(Of ArgumentException)(
                Sub() ScraperHelper.AppendUrlToHistory("", history, "hash", utf8))
            Assert.ThrowsExactly(Of ArgumentNullException)(
                Sub() ScraperHelper.AppendUrlToHistory("file.txt", Nothing, "hash", utf8))
            Assert.ThrowsExactly(Of ArgumentException)(
                Sub() ScraperHelper.AppendUrlToHistory("file.txt", history, " ", utf8))
            Assert.ThrowsExactly(Of ArgumentNullException)(
                Sub() ScraperHelper.AppendUrlToHistory("file.txt", history, "hash", Nothing))
            Assert.AreEqual(0, history.Count)
        End Sub

#End Region

    End Class

End Namespace
