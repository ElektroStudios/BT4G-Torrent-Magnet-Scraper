#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports Microsoft.VisualStudio.TestTools.UnitTesting

#End Region

Namespace BT4GTorrentMagnetScraper.Tests

    ''' <summary>
    ''' Tests for the <see cref="TorrentSearchResult"/> class.
    ''' </summary>
    <TestClass>
    Public NotInheritable Class TorrentSearchResultTests

        ''' <summary>
        ''' Verifies that the constructor stores the specified values.
        ''' </summary>
        <TestMethod>
        Public Sub Constructor_StoresValues()

            Dim result As New TorrentSearchResult("Name", "/magnet/ABC", "ABC")

            Assert.AreEqual("Name", result.Name)
            Assert.AreEqual("/magnet/ABC", result.RelativeHref)
            Assert.AreEqual("ABC", result.UrlHash)
        End Sub

        ''' <summary>
        ''' Verifies that empty values throw <see cref="ArgumentException"/>.
        ''' </summary>
        <TestMethod>
        <DataRow("", "/magnet/ABC", "ABC")>
        <DataRow("Name", " ", "ABC")>
        <DataRow("Name", "/magnet/ABC", Nothing)>
        Public Sub Constructor_EmptyValue_Throws(name As String, relativeHref As String, urlHash As String)

            Assert.ThrowsExactly(Of ArgumentException)(
                Sub()
                    Dim unused As New TorrentSearchResult(name, relativeHref, urlHash)
                End Sub)
        End Sub

    End Class

End Namespace
