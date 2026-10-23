namespace SudokuCoach.WinUI;

/// <summary>Windows entry point; hands off to the shared MAUI app.</summary>
public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
