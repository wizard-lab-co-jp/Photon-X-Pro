using Microsoft.Extensions.DependencyInjection;

namespace PhotonXPro.UI.Maui;

public partial class App : Application
{
    public static string[] InitialFilePaths { get; private set; } = Array.Empty<string>();

	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Handle command line arguments (e.g. from context menu)
		var args = Environment.GetCommandLineArgs();
		if (args.Length > 1)
		{
			// The first arg is the EXE path, subsequent args are file paths
			InitialFilePaths = args.Skip(1).ToArray();
		}

		return new Window(new DashboardPage());
	}
}