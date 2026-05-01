using PhotonXPro.Core.Orchestration;

namespace PhotonXPro.UI.Maui
{
	public partial class DashboardPage : ContentPage
	{
		private ViewModels.MainViewModel _viewModel;

		public DashboardPage()
		{
			InitializeComponent();
			_viewModel = new ViewModels.MainViewModel();
			BindingContext = _viewModel;
			_viewModel.UpdateHardwareInfo();

			if (App.InitialFilePaths.Length > 0)
			{
				foreach (var path in App.InitialFilePaths)
				{
					var doc = new ViewModels.DocumentViewModel(path, (p) => _viewModel.Pages?.Remove(p));
					_viewModel.Documents.Add(doc);
					if (_viewModel.SelectedDocument == null) _viewModel.SelectedDocument = doc;
				}
				_viewModel.StatusText = $"Loaded {App.InitialFilePaths.Length} file(s)";
			}
		}

		private void OnDocumentTapped(object sender, TappedEventArgs e)
		{
			var position = e.GetPosition((View)sender);
			if (position.HasValue && _viewModel.PlaceStampCommand.CanExecute(position.Value))
			{
				_viewModel.PlaceStampCommand.Execute(position.Value);
			}
		}

		private void OnPointerMoved(object sender, PointerEventArgs e)
		{
			var position = e.GetPosition((View)sender);
			if (position.HasValue)
			{
				_viewModel.CursorPos = $"X: {Math.Round(position.Value.X)}, Y: {Math.Round(position.Value.Y)}";
			}
		}
	}
}
