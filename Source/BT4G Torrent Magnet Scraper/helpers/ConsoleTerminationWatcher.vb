#Region " Option Statements "

Option Explicit On
Option Strict On
Option Infer Off

#End Region

#Region " Imports "

Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Runtime.InteropServices

#End Region

''' <summary>
''' Provides a wrapper for the Win32 SetConsoleCtrlHandler function.
''' <para></para>
''' This class ensures that cleanup logic is executed when the console window is closed or interrupted.
''' </summary>
Public Class ConsoleTerminationWatcher : Implements IDisposable

    ''' <summary>
    ''' Adds or removes an application-defined HandlerRoutine function from the list of handler functions for the calling process.
    ''' </summary>
    ''' 
    ''' <param name="handler">
    ''' A pointer to the application-defined HandlerRoutine function to be added or removed.
    ''' </param>
    ''' 
    ''' <param name="add">
    ''' If this parameter is TRUE, the handler is added; if it is FALSE, the handler is removed.
    ''' </param>
    ''' 
    ''' <returns>
    ''' Returns TRUE if the function succeeds; otherwise, FALSE.
    ''' </returns>
    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function SetConsoleCtrlHandler(handler As ConsoleEventDelegate, add As Boolean) As Boolean
    End Function

    ''' <summary>
    ''' An application-defined function used with the SetConsoleCtrlHandler function.
    ''' </summary>
    ''' 
    ''' <param name="eventType">
    ''' The type of control signal received by the handler.
    ''' </param>
    ''' 
    ''' <returns>
    ''' If the function handles the control signal, it should return TRUE. 
    ''' <para></para>
    ''' If it returns FALSE, the next handler function in the list is called.
    ''' </returns>
    Private Delegate Function ConsoleEventDelegate(eventType As ConsoleEventType) As Boolean

    ''' <summary>
    ''' Enumerates the control signal types received by the console control handler.
    ''' </summary>
    Private Enum ConsoleEventType As Integer

        ''' <summary>
        ''' A CTRL+C signal was received.
        ''' </summary>
        CTRL_C_EVENT = 0

        ''' <summary>
        ''' A CTRL+BREAK signal was received.
        ''' </summary>
        CTRL_BREAK_EVENT = 1

        ''' <summary>
        ''' The user closed the console window.
        ''' </summary>
        CTRL_CLOSE_EVENT = 2

        ''' <summary>
        ''' The user is logging off.
        ''' </summary>
        CTRL_LOGOFF_EVENT = 5

        ''' <summary>
        ''' The system is shutting down.
        ''' </summary>
        CTRL_SHUTDOWN_EVENT = 6

    End Enum

    ''' <summary>
    ''' Holds the delegate reference to prevent it from being collected by the Garbage Collector.
    ''' </summary>
    Private ReadOnly _handler As ConsoleEventDelegate

    ''' <summary>
    ''' The action to be executed when a termination event occurs.
    ''' </summary>
    Private ReadOnly _cleanupAction As Action

    ''' <summary>
    ''' Tracks the disposal status of the instance.
    ''' </summary>
    Private _disposedValue As Boolean = False

    ''' <summary>
    ''' Initializes a new instance of the <see cref="ConsoleTerminationWatcher"/> class.
    ''' </summary>
    ''' 
    ''' <param name="cleanupAction">
    ''' The action to execute when the console terminates (e.g., process cleanup).
    ''' </param>
    ''' 
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when cleanupAction is null.
    ''' </exception>
    ''' 
    ''' <exception cref="Win32Exception">
    ''' Thrown when the Win32 API registration fails.
    ''' </exception>
    <DebuggerStepThrough>
    Public Sub New(cleanupAction As Action)

        If cleanupAction Is Nothing Then
            Throw New ArgumentNullException(NameOf(cleanupAction))
        End If

        Me._cleanupAction = cleanupAction
        Me._handler = New ConsoleEventDelegate(AddressOf Me.ConsoleEventCallback)

        If Not SetConsoleCtrlHandler(Me._handler, True) Then
            Dim errorCode As Integer = Marshal.GetLastWin32Error()
            Throw New Win32Exception(errorCode, $"Failed to register console control handler. Win32 Error Code: {errorCode}")
        End If
    End Sub

    ''' <summary>
    ''' The callback method invoked by the Windows Operating System when a console event occurs.
    ''' </summary>
    ''' 
    ''' <param name="eventType">
    ''' The type of console event triggered.
    ''' </param>
    ''' 
    ''' <returns>
    ''' Always returns FALSE to allow the process to terminate normally after cleanup.
    ''' </returns>
    <DebuggerStepThrough>
    Private Function ConsoleEventCallback(eventType As ConsoleEventType) As Boolean

        Select Case eventType
            Case ConsoleEventType.CTRL_C_EVENT,
                 ConsoleEventType.CTRL_BREAK_EVENT,
                 ConsoleEventType.CTRL_CLOSE_EVENT,
                 ConsoleEventType.CTRL_LOGOFF_EVENT,
                 ConsoleEventType.CTRL_SHUTDOWN_EVENT

                ' Trigger the external cleanup logic
                Me._cleanupAction.Invoke()

            Case Else
                ' Ignore other events.
        End Select

        Return False
    End Function

    ''' <summary>
    ''' Releases the resources used by the <see cref="ConsoleTerminationWatcher"/>.
    ''' </summary>
    ''' 
    ''' <param name="disposing">
    ''' True to release both managed and unmanaged resources; False to release only unmanaged resources.
    ''' </param>
    <DebuggerStepThrough>
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not Me._disposedValue Then
            If disposing Then
                ' Free managed objects here if any.
            End If

            ' Unregister the Win32 handler (Unmanaged resource cleanup)
            ' This must happen even if disposing is False (called from Finalizer)
            SetConsoleCtrlHandler(Me._handler, False)

            Me._disposedValue = True
        End If
    End Sub

    ''' <summary>
    ''' Finalizes an instance of the <see cref="ConsoleTerminationWatcher"/> class.
    ''' </summary>
    <DebuggerStepThrough>
    Protected Overrides Sub Finalize()
        ' Ensure the Win32 hook is removed if the programmer forgot to call Dispose
        Me.Dispose(disposing:=False)
        MyBase.Finalize()
    End Sub

    ''' <summary>
    ''' Releases the resources used by the <see cref="ConsoleTerminationWatcher"/>.
    ''' </summary>
    <DebuggerStepThrough>
    Public Sub Dispose() Implements IDisposable.Dispose
        Me.Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub

End Class