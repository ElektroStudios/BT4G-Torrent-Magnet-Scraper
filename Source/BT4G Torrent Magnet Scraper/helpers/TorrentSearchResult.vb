#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Diagnostics

#End Region

''' <summary>
''' Represents the essential data extracted from a single item of a BT4G search results page.
''' </summary>
Friend NotInheritable Class TorrentSearchResult

#Region " Properties "

    ''' <summary>
    ''' Gets the name (title) of the torrent.
    ''' </summary>
    Friend ReadOnly Property Name As String

    ''' <summary>
    ''' Gets the relative URL of the torrent's detail page on BT4G (e.g., "/magnet/0123ABCD...").
    ''' </summary>
    Friend ReadOnly Property RelativeHref As String

    ''' <summary>
    ''' Gets the hash portion of <see cref="TorrentSearchResult.RelativeHref"/>,
    ''' that is, the relative URL without the "/magnet/" prefix.
    ''' <para></para>
    ''' This value is used as the unique key stored in the URL history file.
    ''' </summary>
    Friend ReadOnly Property UrlHash As String

#End Region

#Region " Constructors "

    ''' <summary>
    ''' Initializes a new instance of the <see cref="TorrentSearchResult"/> class.
    ''' </summary>
    '''
    ''' <param name="name">
    ''' The name (title) of the torrent.
    ''' </param>
    '''
    ''' <param name="relativeHref">
    ''' The relative URL of the torrent's detail page on BT4G.
    ''' </param>
    '''
    ''' <param name="urlHash">
    ''' The hash portion of <paramref name="relativeHref"/>.
    ''' </param>
    '''
    ''' <exception cref="ArgumentException">
    ''' Thrown when any of the arguments is null, empty or consists only of white-space characters.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Sub New(name As String, relativeHref As String, urlHash As String)

        If String.IsNullOrWhiteSpace(name) Then
            Throw New ArgumentException("Torrent name cannot be null, empty or white-space.", NameOf(name))
        End If

        If String.IsNullOrWhiteSpace(relativeHref) Then
            Throw New ArgumentException("Relative href cannot be null, empty or white-space.", NameOf(relativeHref))
        End If

        If String.IsNullOrWhiteSpace(urlHash) Then
            Throw New ArgumentException("URL hash cannot be null, empty or white-space.", NameOf(urlHash))
        End If

        Me.Name = name
        Me.RelativeHref = relativeHref
        Me.UrlHash = urlHash
    End Sub

#End Region

End Class
