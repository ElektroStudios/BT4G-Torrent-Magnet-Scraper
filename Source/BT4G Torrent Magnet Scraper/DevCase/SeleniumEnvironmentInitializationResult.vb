
#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " SeleniumEnvironmentInitializationResult "

Namespace DevCase.ThirdParty.Selenium

    ''' <summary>
    ''' Represents the result of Selenium environment initialization process performed by 
    ''' functions like <see cref="UtilSelenium.InitializeSeleniumEnvironmentForChrome"/>  
    ''' and <see cref="UtilSelenium.InitializeSeleniumEnvironmentForChromeAsync"/>,
    ''' containing the Selenium Manager process exit code, resolved Selenium driver file path, 
    ''' and resolved browser binary file path.
    ''' </summary>
    Public NotInheritable Class SeleniumEnvironmentInitializationResult

#Region " Properties "

        ''' <summary>
        ''' Gets the exit code returned by the Selenium Manager process execution.
        ''' <para></para>
        ''' A value of 0 indicates success, while non-zero values indicate an error condition.
        ''' </summary>
        Public ReadOnly Property SeleniumManagerExitCode As Integer

        ''' <summary>
        ''' Gets the console output produced by the Selenium Manager process execution, 
        ''' which may contain informational messages, warnings, or error details related to the environment initialization process.
        ''' </summary>
        Public ReadOnly Property SeleniumManagerConsoleOutput As String

        ''' <summary>
        ''' Gets the full file path of the resolved Selenium driver executable, 
        ''' as determined by Selenium Manager process execution.
        ''' </summary>
        Public ReadOnly Property DriverFilePath As String

        ''' <summary>
        ''' Gets the full file path of the resolved browser executable, 
        ''' as determined by Selenium Manager process execution.
        ''' </summary>
        Public ReadOnly Property BrowserFilePath As String

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="SeleniumEnvironmentInitializationResult"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SeleniumEnvironmentInitializationResult"/> class.
        ''' </summary>
        ''' 
        ''' <param name="seleniumManagerExitCode">
        ''' The Selenium Manager exit code.
        ''' </param>
        ''' 
        ''' <param name="seleniumManagerConsoleOutput">
        ''' The Selenium Manager console output, 
        ''' which may contain informational messages, warnings, 
        ''' or error details related to the environment initialization process.
        ''' </param>
        ''' 
        ''' <param name="driverFilePath">
        ''' The resolved Selenium driver file path.
        ''' </param>
        ''' 
        ''' <param name="browserFilePath">
        ''' The resolved browser file path.
        ''' </param>
        Public Sub New(seleniumManagerExitCode As Integer, seleniumManagerConsoleOutput As String,
                       driverFilePath As String, browserFilePath As String)

            Me.SeleniumManagerExitCode = seleniumManagerExitCode
            Me.SeleniumManagerConsoleOutput = seleniumManagerConsoleOutput

            Me.DriverFilePath = driverFilePath
            Me.BrowserFilePath = browserFilePath
        End Sub

#End Region

    End Class

End Namespace

#End Region
