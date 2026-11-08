#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions

Imports BT4GTorrentMagnetScraper.Tests.Infrastructure

Imports Microsoft.VisualStudio.TestTools.UnitTesting

#End Region

Namespace BT4GTorrentMagnetScraper.Tests

    ''' <summary>
    ''' Tests for the output file functions of <see cref="ScraperHelper"/>.
    ''' </summary>
    <TestClass>
    Public NotInheritable Class OutputTests

#Region " Constants "

        ''' <summary>
        ''' A sample magnet link used in the tests.
        ''' </summary>
        Private Const SampleMagnet As String = "magnet:?xt=urn:btih:0123456789ABCDEF0123456789ABCDEF01234567&dn=Sample"

#End Region

#Region " ToSafeFileName / GetOutputFilePath "

        ''' <summary>
        ''' Verifies that search terms are converted into valid Windows file names.
        ''' </summary>
        <TestMethod>
        <DataRow("John Wick", "John Wick")>
        <DataRow("  John Wick  ", "John Wick")>
        <DataRow("AC/DC", "AC_DC")>
        <DataRow("a<b>c:d""e/f\g|h?i*j", "a_b_c_d_e_f_g_h_i_j")>
        <DataRow("Movie.", "Movie_")>
        <DataRow("Movie. .", "Movie___")>
        <DataRow("Mr. Robot", "Mr. Robot")>
        <DataRow("CON", "_CON")>
        <DataRow("nul", "_nul")>
        <DataRow("Com1.backup", "_Com1.backup")>
        <DataRow("LPT9", "_LPT9")>
        <DataRow("COM10", "COM10")>
        <DataRow("CONSOLE", "CONSOLE")>
        <DataRow("Contact", "Contact")>
        Public Sub ToSafeFileName_ReturnsValidWindowsFileName(searchTerm As String, expected As String)

            Assert.AreEqual(expected, ScraperHelper.ToSafeFileName(searchTerm))
        End Sub

        ''' <summary>
        ''' Verifies that control characters are replaced.
        ''' </summary>
        <TestMethod>
        Public Sub ToSafeFileName_ReplacesControlCharacters()

            Assert.AreEqual("a_b_c", ScraperHelper.ToSafeFileName($"a{vbTab}b{ChrW(1)}c"))
        End Sub

        ''' <summary>
        ''' Verifies that an empty search term throws <see cref="ArgumentException"/>.
        ''' </summary>
        <TestMethod>
        <DataRow("")>
        <DataRow("   ")>
        <DataRow(Nothing)>
        Public Sub ToSafeFileName_Empty_Throws(searchTerm As String)

            Assert.ThrowsExactly(Of ArgumentException)(
                Sub() ScraperHelper.ToSafeFileName(searchTerm))
        End Sub

        ''' <summary>
        ''' Verifies that the output file path is built inside the output directory with a ".txt" extension.
        ''' </summary>
        <TestMethod>
        Public Sub GetOutputFilePath_CombinesDirectoryAndSafeFileName()

            Dim outputDir As String = Path.Combine(Path.GetTempPath(), "output")

            Dim result As String = ScraperHelper.GetOutputFilePath(outputDir, "AC/DC")

            Assert.AreEqual(Path.Combine(outputDir, "AC_DC.txt"), result)
        End Sub

        ''' <summary>
        ''' Verifies that an empty output directory throws <see cref="ArgumentException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub GetOutputFilePath_EmptyDirectory_Throws()

            Assert.ThrowsExactly(Of ArgumentException)(
                Sub() ScraperHelper.GetOutputFilePath("", "x"))
        End Sub

#End Region

#Region " BuildMagnetResultEntry "

        ''' <summary>
        ''' Verifies the layout of an output entry: name line, magnet line with trackers, and an empty separator line.
        ''' </summary>
        <TestMethod>
        Public Sub BuildMagnetResultEntry_HasExpectedLayout()

            Dim entry As String = ScraperHelper.BuildMagnetResultEntry("John Wick", OutputTests.SampleMagnet)

            Dim nl As String = Environment.NewLine
            Assert.AreEqual($"John Wick{nl}{OutputTests.SampleMagnet}{ScraperHelper.Trackers}{nl}{nl}", entry)
        End Sub

        ''' <summary>
        ''' Verifies that the tracker list is well-formed: every tracker is a "&amp;tr=" parameter
        ''' with a URL-encoded UDP or HTTPS announce URL, without duplicates.
        ''' </summary>
        <TestMethod>
        Public Sub Trackers_AreWellFormedAndDistinct()

            Dim trackers As String() = ScraperHelper.Trackers.Split({"&tr="}, StringSplitOptions.RemoveEmptyEntries)

            Assert.IsTrue(ScraperHelper.Trackers.StartsWith("&tr=", StringComparison.Ordinal))
            Assert.AreEqual(21, trackers.Length)
            Assert.AreEqual(trackers.Length, trackers.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            For Each tracker As String In trackers
                Assert.IsTrue(Regex.IsMatch(tracker, "^(udp|https)%3A%2F%2F[^&\s]+%2Fannounce$"), $"Malformed tracker: {tracker}")
            Next tracker
        End Sub

        ''' <summary>
        ''' Verifies the argument validation of <see cref="ScraperHelper.BuildMagnetResultEntry"/>.
        ''' </summary>
        <TestMethod>
        Public Sub BuildMagnetResultEntry_InvalidArguments_Throw()

            Assert.ThrowsExactly(Of ArgumentException)(
                Sub() ScraperHelper.BuildMagnetResultEntry(" ", OutputTests.SampleMagnet))
            Assert.ThrowsExactly(Of ArgumentException)(
                Sub() ScraperHelper.BuildMagnetResultEntry("Name", "https://downloadtorrentfile.com/hash/ABC"))
            Assert.ThrowsExactly(Of ArgumentException)(
                Sub() ScraperHelper.BuildMagnetResultEntry("Name", Nothing))
        End Sub

#End Region

#Region " AppendMagnetResult "

        ''' <summary>
        ''' Verifies that consecutive entries are appended to the same file, that the missing output directory is created,
        ''' and that the UTF-8 BOM is written only once.
        ''' </summary>
        <TestMethod>
        Public Sub AppendMagnetResult_AppendsEntries_WithSingleBom()

            Using tempDir As New TemporaryDirectory()
                Dim outputFilePath As String = tempDir.Combine("output", "John Wick.txt")
                Dim utf8WithBom As New UTF8Encoding(True)

                ScraperHelper.AppendMagnetResult(outputFilePath, "First", OutputTests.SampleMagnet, utf8WithBom)
                ScraperHelper.AppendMagnetResult(outputFilePath, "Second ñ", OutputTests.SampleMagnet, utf8WithBom)

                Dim expected As String =
                    ScraperHelper.BuildMagnetResultEntry("First", OutputTests.SampleMagnet) &
                    ScraperHelper.BuildMagnetResultEntry("Second ñ", OutputTests.SampleMagnet)

                Dim bytes As Byte() = File.ReadAllBytes(outputFilePath)
                Dim bom As Byte() = utf8WithBom.GetPreamble()

                CollectionAssert.AreEqual(bom, bytes.Take(bom.Length).ToArray())
                Assert.AreEqual(expected, utf8WithBom.GetString(bytes, bom.Length, bytes.Length - bom.Length))
            End Using
        End Sub

        ''' <summary>
        ''' Verifies that an invalid magnet link is rejected before creating the output file.
        ''' </summary>
        <TestMethod>
        Public Sub AppendMagnetResult_InvalidMagnet_DoesNotCreateFile()

            Using tempDir As New TemporaryDirectory()
                Dim outputFilePath As String = tempDir.Combine("output", "x.txt")

                Assert.ThrowsExactly(Of ArgumentException)(
                    Sub() ScraperHelper.AppendMagnetResult(outputFilePath, "Name", "not-a-magnet", Encoding.UTF8))

                Assert.IsFalse(File.Exists(outputFilePath))
            End Using
        End Sub

        ''' <summary>
        ''' Verifies the argument validation of <see cref="ScraperHelper.AppendMagnetResult"/>.
        ''' </summary>
        <TestMethod>
        Public Sub AppendMagnetResult_InvalidArguments_Throw()

            Assert.ThrowsExactly(Of ArgumentException)(
                Sub() ScraperHelper.AppendMagnetResult(" ", "Name", OutputTests.SampleMagnet, Encoding.UTF8))
            Assert.ThrowsExactly(Of ArgumentNullException)(
                Sub() ScraperHelper.AppendMagnetResult("x.txt", "Name", OutputTests.SampleMagnet, Nothing))
        End Sub

#End Region

    End Class

End Namespace
