#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports System.IO

#End Region

Namespace BT4GTorrentMagnetScraper.Tests.Infrastructure

    ''' <summary>
    ''' Creates a unique, empty temporary directory that is recursively deleted when disposed.
    ''' </summary>
    Friend NotInheritable Class TemporaryDirectory : Implements IDisposable

#Region " Properties "

        ''' <summary>
        ''' Gets the full path of the temporary directory.
        ''' </summary>
        Friend ReadOnly Property FullPath As String

#End Region

#Region " Fields "

        ''' <summary>
        ''' Flag to detect redundant calls to <see cref="TemporaryDirectory.Dispose()"/>.
        ''' </summary>
        Private isDisposed As Boolean

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="TemporaryDirectory"/> class,
        ''' creating a new unique directory under the user's temporary directory.
        ''' </summary>
        Friend Sub New()

            Me.FullPath = Path.Combine(Path.GetTempPath(), $"BT4G.Tests.{Guid.NewGuid():N}")
            Directory.CreateDirectory(Me.FullPath)
        End Sub

#End Region

#Region " Public Methods "

        ''' <summary>
        ''' Combines the path of the temporary directory with the specified relative path segments.
        ''' </summary>
        '''
        ''' <param name="segments">
        ''' The relative path segments.
        ''' </param>
        '''
        ''' <returns>
        ''' The combined full path.
        ''' </returns>
        Friend Function Combine(ParamArray segments As String()) As String

            If segments Is Nothing Then
                Throw New ArgumentNullException(NameOf(segments))
            End If

            Dim allSegments As New List(Of String) From {Me.FullPath}
            allSegments.AddRange(segments)
            Return Path.Combine(allSegments.ToArray())
        End Function

        ''' <summary>
        ''' Creates the specified sub-directory (and any missing parent) inside the temporary directory.
        ''' </summary>
        '''
        ''' <param name="segments">
        ''' The relative path segments of the sub-directory.
        ''' </param>
        '''
        ''' <returns>
        ''' The full path of the created sub-directory.
        ''' </returns>
        Friend Function CreateSubdirectory(ParamArray segments As String()) As String

            Dim dirPath As String = Me.Combine(segments)
            Directory.CreateDirectory(dirPath)
            Return dirPath
        End Function

        ''' <summary>
        ''' Recursively deletes the temporary directory.
        ''' </summary>
        Public Sub Dispose() Implements IDisposable.Dispose

            If Me.isDisposed Then
                Return
            End If

            If Directory.Exists(Me.FullPath) Then
                Directory.Delete(Me.FullPath, recursive:=True)
            End If

            Me.isDisposed = True
        End Sub

#End Region

    End Class

End Namespace
