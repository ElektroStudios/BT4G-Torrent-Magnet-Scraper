#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Globalization
Imports System.Text.RegularExpressions

Imports Microsoft.VisualStudio.TestTools.UnitTesting

#End Region

Namespace BT4GTorrentMagnetScraper.Tests

    ''' <summary>
    ''' Tests for the formatting functions of <see cref="ScraperHelper"/>.
    ''' </summary>
    <TestClass>
    Public NotInheritable Class FormattingTests

#Region " Fields "

        ''' <summary>
        ''' The "en-US" culture, the same one forced by the application at startup.
        ''' </summary>
        Private Shared ReadOnly EnUs As New CultureInfo("en-US")

#End Region

#Region " ShortenTorrentName "

        ''' <summary>
        ''' Verifies that names up to the maximum length are returned unchanged.
        ''' </summary>
        <TestMethod>
        <DataRow(0)>
        <DataRow(1)>
        <DataRow(99)>
        <DataRow(100)>
        Public Sub ShortenTorrentName_UpToMaxLength_ReturnsSameName(length As Integer)

            Dim name As New String("x"c, length)

            Assert.AreEqual(name, ScraperHelper.ShortenTorrentName(name))
        End Sub

        ''' <summary>
        ''' Verifies that longer names are truncated to the maximum length, ending with an ellipsis.
        ''' </summary>
        <TestMethod>
        <DataRow(101)>
        <DataRow(500)>
        Public Sub ShortenTorrentName_LongerThanMaxLength_IsTruncated(length As Integer)

            Dim name As String = String.Concat(Enumerable.Range(0, length).Select(Function(i As Integer) CStr(i Mod 10)))

            Dim result As String = ScraperHelper.ShortenTorrentName(name)

            Assert.AreEqual(ScraperHelper.MaxDisplayedTorrentNameLength, result.Length)
            Assert.IsTrue(result.EndsWith("…", StringComparison.Ordinal))
            Assert.AreEqual(name.Substring(0, ScraperHelper.MaxDisplayedTorrentNameLength - 1), result.Substring(0, result.Length - 1))
        End Sub

        ''' <summary>
        ''' Verifies that a null name throws <see cref="ArgumentNullException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub ShortenTorrentName_Null_Throws()

            Assert.ThrowsExactly(Of ArgumentNullException)(
                Sub() ScraperHelper.ShortenTorrentName(Nothing))
        End Sub

#End Region

#Region " FormatItemIndex "

        ''' <summary>
        ''' Verifies that the item index is padded to the width of the formatted total
        ''' (including thousands separators, which were previously not taken into account).
        ''' </summary>
        <TestMethod>
        <DataRow(1, 9, "1")>
        <DataRow(5, 99, " 5")>
        <DataRow(5, 1234, "    5")>
        <DataRow(999, 1234, "  999")>
        <DataRow(1234, 1234, "1,234")>
        <DataRow(12, 9, "12")>
        <DataRow(7, 0, "7")>
        Public Sub FormatItemIndex_PadsToFormattedTotalWidth(currentItemIndex As Integer, totalItems As Integer, expected As String)

            Assert.AreEqual(expected, ScraperHelper.FormatItemIndex(currentItemIndex, totalItems, FormattingTests.EnUs))
        End Sub

        ''' <summary>
        ''' Verifies that the index and the total always have the same width when the index does not exceed the total.
        ''' </summary>
        <TestMethod>
        Public Sub FormatItemIndex_IndexAndTotal_HaveSameWidth()

            Const total As Integer = 12345
            Dim totalWidth As Integer = total.ToString("N0", FormattingTests.EnUs).Length

            For Each index As Integer In {1, 10, 100, 1000, 10000, total}
                Assert.AreEqual(totalWidth, ScraperHelper.FormatItemIndex(index, total, FormattingTests.EnUs).Length, $"Index: {index}")
            Next index
        End Sub

        ''' <summary>
        ''' Verifies that a null format provider throws <see cref="ArgumentNullException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub FormatItemIndex_NullProvider_Throws()

            Assert.ThrowsExactly(Of ArgumentNullException)(
                Sub() ScraperHelper.FormatItemIndex(1, 1, Nothing))
        End Sub

#End Region

#Region " GenerateRandomHexString "

        ''' <summary>
        ''' Verifies that the generated string has the requested length and only contains uppercase hexadecimal digits.
        ''' </summary>
        <TestMethod>
        <DataRow(2)>
        <DataRow(40)>
        <DataRow(64)>
        Public Sub GenerateRandomHexString_ReturnsUppercaseHex(length As Integer)

            Dim result As String = ScraperHelper.GenerateRandomHexString(length)

            Assert.AreEqual(length, result.Length)
            Assert.IsTrue(Regex.IsMatch(result, "^[0-9A-F]+$"), result)
        End Sub

        ''' <summary>
        ''' Verifies that consecutive calls return different values.
        ''' </summary>
        <TestMethod>
        Public Sub GenerateRandomHexString_ReturnsDifferentValues()

            Dim values As New HashSet(Of String)()
            For i As Integer = 1 To 20
                values.Add(ScraperHelper.GenerateRandomHexString(40))
            Next i

            Assert.AreEqual(20, values.Count)
        End Sub

        ''' <summary>
        ''' Verifies that a non-positive length throws <see cref="ArgumentOutOfRangeException"/>.
        ''' </summary>
        <TestMethod>
        <DataRow(0)>
        <DataRow(-2)>
        Public Sub GenerateRandomHexString_NonPositiveLength_Throws(length As Integer)

            Assert.ThrowsExactly(Of ArgumentOutOfRangeException)(
                Sub() ScraperHelper.GenerateRandomHexString(length))
        End Sub

        ''' <summary>
        ''' Verifies that an odd length throws <see cref="ArgumentException"/>.
        ''' </summary>
        <TestMethod>
        Public Sub GenerateRandomHexString_OddLength_Throws()

            Assert.ThrowsExactly(Of ArgumentException)(
                Sub() ScraperHelper.GenerateRandomHexString(3))
        End Sub

#End Region

    End Class

End Namespace
