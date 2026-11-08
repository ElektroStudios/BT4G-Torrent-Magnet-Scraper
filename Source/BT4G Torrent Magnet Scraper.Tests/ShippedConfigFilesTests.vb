#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.IO
Imports System.Text

Imports Microsoft.VisualStudio.TestTools.UnitTesting

#End Region

Namespace BT4GTorrentMagnetScraper.Tests

    ''' <summary>
    ''' Verifies that the configuration files shipped with the application remain valid
    ''' when parsed with the same rules used by the application.
    ''' </summary>
    <TestClass>
    Public NotInheritable Class ShippedConfigFilesTests

#Region " Private Methods "

        ''' <summary>
        ''' Loads and parses a shipped configuration file copied to the "TestData\config" output directory.
        ''' </summary>
        '''
        ''' <param name="fileName">
        ''' The file name of the configuration file.
        ''' </param>
        '''
        ''' <param name="trimLines">
        ''' Whether to trim the lines (only the additional query parameters file is trimmed by the application).
        ''' </param>
        '''
        ''' <returns>
        ''' The parsed lines.
        ''' </returns>
        Private Shared Function LoadShippedConfig(fileName As String, trimLines As Boolean) As HashSet(Of String)

            Dim filePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "config", fileName)
            If Not File.Exists(filePath) Then
                Throw New FileNotFoundException("Shipped config file was not copied to the test output directory.", filePath)
            End If

            Return ScraperHelper.ParseConfigLines(File.ReadAllLines(filePath, New UTF8Encoding(True)), trimLines)
        End Function

#End Region

#Region " Tests "

        ''' <summary>
        ''' Verifies that the shipped search terms file contains search terms and no comments.
        ''' </summary>
        <TestMethod>
        Public Sub SearchTerms_AreNotEmpty_AndContainNoComments()

            Dim terms As HashSet(Of String) = ShippedConfigFilesTests.LoadShippedConfig("Search Terms.txt", trimLines:=False)

            Assert.IsTrue(terms.Count > 0)
            Assert.IsFalse(terms.Any(Function(t As String) t.TrimStart().StartsWith("#"c)))
            For Each term As String In terms
                ' Every search term must produce a valid output file name.
                Assert.IsFalse(String.IsNullOrWhiteSpace(ScraperHelper.ToSafeFileName(term)))
            Next term
        End Sub

        ''' <summary>
        ''' Verifies that the shipped required keywords file contains keywords and no comments.
        ''' </summary>
        <TestMethod>
        Public Sub RequiredKeywords_AreNotEmpty_AndContainNoComments()

            Dim keywords As HashSet(Of String) = ShippedConfigFilesTests.LoadShippedConfig("Required Keywords.txt", trimLines:=False)

            Assert.IsTrue(keywords.Count > 0)
            Assert.IsFalse(keywords.Any(Function(k As String) k.TrimStart().StartsWith("#"c)))
            Assert.IsTrue(ScraperHelper.ContainsAnyKeyword("John Wick (2014) 2160p", keywords))
        End Sub

        ''' <summary>
        ''' Verifies that the shipped forbidden keywords file preserves the keywords with leading white-space (" sd").
        ''' </summary>
        <TestMethod>
        Public Sub ForbiddenKeywords_PreserveLeadingWhiteSpace()

            Dim keywords As HashSet(Of String) = ShippedConfigFilesTests.LoadShippedConfig("Forbidden Keywords.txt", trimLines:=False)

            Assert.IsTrue(keywords.Count > 0)
            Assert.IsTrue(keywords.Contains(" sd"))
            Assert.IsTrue(ScraperHelper.ContainsAnyKeyword("John Wick (2014) 720p", keywords))
        End Sub

        ''' <summary>
        ''' Verifies that the shipped additional query parameters are trimmed "key=value" pairs.
        ''' </summary>
        <TestMethod>
        Public Sub QueryAdditionalParameters_AreKeyValuePairs()

            Dim parameters As HashSet(Of String) = ShippedConfigFilesTests.LoadShippedConfig("Query Additional Parameters.txt", trimLines:=True)

            Assert.IsTrue(parameters.Count > 0)
            For Each parameter As String In parameters
                Assert.AreEqual(parameter.Trim(), parameter)
                Assert.IsTrue(parameter.IndexOf("="c) > 0, $"Not a key=value pair: {parameter}")
            Next parameter
        End Sub

#End Region

    End Class

End Namespace
