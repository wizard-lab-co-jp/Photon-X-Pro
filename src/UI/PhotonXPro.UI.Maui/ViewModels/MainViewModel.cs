using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PhotonXPro.Core.Orchestration;

namespace PhotonXPro.UI.Maui.ViewModels;

public class PageViewModel : INotifyPropertyChanged
{
    private string _pageLabel = "";
    public string PageLabel { get => _pageLabel; set => SetProperty(ref _pageLabel, value); }

    private string _pageSize = "A4 (210x297mm)";
    public string PageSize { get => _pageSize; set => SetProperty(ref _pageSize, value); }

    private ImageSource? _thumbnail;
    public ImageSource? Thumbnail { get => _thumbnail; set => SetProperty(ref _thumbnail, value); }

    public ICommand DeleteCommand { get; }

    public PageViewModel(Action<PageViewModel> onDelete)
    {
        DeleteCommand = new Command(() => onDelete(this));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value)) return false;
        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class DocumentViewModel : INotifyPropertyChanged
{
    public string FileName { get; }
    public string FullPath { get; }
    public ObservableCollection<PageViewModel> Pages { get; } = new();

    public DocumentViewModel(string path, Action<PageViewModel> onDeletePage)
    {
        FullPath = path;
        FileName = Path.GetFileName(path);
        
        if (PdfParser.openPdf(path))
        {
            int count = PdfParser.getPageCount();
            for (int i = 0; i < count; i++)
            {
                var page = new PageViewModel(onDeletePage)
                {
                    PageLabel = $"Page {i + 1}",
                    Thumbnail = ImageSource.FromFile("dotnet_bot.png") // Placeholder
                };
                Pages.Add(page);
                
                // Kick off thumbnail generation in background
                _ = Task.Run(async () => {
                    var rgba = PdfRenderer.renderPage(i, 200, 282); // Small thumbnail
                    if (rgba != null)
                    {
                        var png = ImageEncoder.encodeToPng(rgba.Value, 200, 282);
                        if (png != null)
                        {
                            var stream = new MemoryStream(png.Value);
                            page.Thumbnail = ImageSource.FromStream(() => stream);
                        }
                    }
                });
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class MainViewModel : INotifyPropertyChanged
{
    public ObservableCollection<DocumentViewModel> Documents { get; } = new();
    
    private DocumentViewModel? _selectedDocument;
    public DocumentViewModel? SelectedDocument 
    { 
        get => _selectedDocument; 
        set {
            if (SetProperty(ref _selectedDocument, value))
            {
                OnPropertyChanged(nameof(Pages));
                OnPropertyChanged(nameof(LoadedFileName));
                OnPropertyChanged(nameof(IsDocumentLoaded));
            }
        }
    }

    public ObservableCollection<PageViewModel>? Pages => SelectedDocument?.Pages;
    public string LoadedFileName => SelectedDocument?.FileName ?? "No Document Loaded";
    public bool IsDocumentLoaded => SelectedDocument != null;

    private ImageSource? _currentPageImage;
    public ImageSource? CurrentPageImage { get => _currentPageImage; set => SetProperty(ref _currentPageImage, value); }

    private int _currentPageIndex = 0;
    public int CurrentPageIndex 
    { 
        get => _currentPageIndex; 
        set {
            if (SetProperty(ref _currentPageIndex, value))
            {
                OnPropertyChanged(nameof(PageIndicator));
                _ = RenderCurrentPageAsync();
            }
        }
    }

    public string PageIndicator => $"Page {(SelectedDocument != null ? CurrentPageIndex + 1 : 0)} / {(SelectedDocument?.Pages.Count ?? 0)}";

    private string _statusText = "Ready";
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    private bool _isLeftPanelOpen = true;
    public bool IsLeftPanelOpen { get => _isLeftPanelOpen; set => SetProperty(ref _isLeftPanelOpen, value); }

    private bool _isRightPanelOpen = true;
    public bool IsRightPanelOpen { get => _isRightPanelOpen; set => SetProperty(ref _isRightPanelOpen, value); }

    private string _cursorPos = "X: 0, Y: 0";
    public string CursorPos { get => _cursorPos; set => SetProperty(ref _cursorPos, value); }

    private string _renderMode = "Hardware (AVX-512)";
    public string RenderMode { get => _renderMode; set => SetProperty(ref _renderMode, value); }

    private int _drawCalls = 0;
    public int DrawCalls { get => _drawCalls; set => SetProperty(ref _drawCalls, value); }

    public ICommand SelectDocumentCommand { get; }
    public ICommand OpenFileCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand PlaceStampCommand { get; }
    public ICommand CloseDocumentCommand { get; }
    public ICommand ToggleLeftPanelCommand { get; }
    public ICommand ToggleRightPanelCommand { get; }
    public ICommand CombineFilesCommand { get; }
    public ICommand ToggleStampModeCommand { get; }
    public ICommand ItemDraggedCommand { get; }
    public ICommand ItemDroppedCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PrevPageCommand { get; }
    public ICommand SelectPageCommand { get; }

    private PageViewModel? _draggedItem;

    public MainViewModel()
    {
        SelectDocumentCommand = new Command<DocumentViewModel>((doc) => SelectedDocument = doc);
        OpenFileCommand = new Command(async () => await ExecuteOpenFile());
        SaveCommand = new Command(async () => await ExecuteSave());
        PlaceStampCommand = new Command<Point>(async (pos) => {
            if (SelectedDocument == null) return;
            StatusText = $"Placing stamp at {pos.X}, {pos.Y}...";
            
            try {
                // 1. Render current page to RGBA
                var pageRgba = await Task.Run(() => PdfRenderer.renderPage(CurrentPageIndex, 1200, 1697));
                if (pageRgba == null) return;

                // 2. Load hanko image (fixed size for now 200x200)
                var hankoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images", "hanko.png");
                // Note: BaseDirectory might be different in MAUI Windows, but let's assume path for now
                var hankoRgba = await Task.Run(() => PdfRenderer.loadImage(hankoPath, 200, 200));
                
                if (hankoRgba != null) {
                    // 3. Blend (Real implementation would handle positioning, but for now we blend)
                    var blended = await Task.Run(() => PdfRenderer.blendHanko(pageRgba.Value, hankoRgba.Value, 1200, 1697));
                    
                    // 4. Update UI
                    CurrentPageImage = ImageSource.FromStream(() => new MemoryStream(RgbaToPng(blended, 1200, 1697)));
                    StatusText = "Stamp placed successfully.";
                }
            } catch (Exception ex) {
                StatusText = $"Stamp error: {ex.Message}";
            }
        });
        CloseDocumentCommand = new Command<DocumentViewModel>((doc) => {
            Documents.Remove(doc);
            if (SelectedDocument == doc) SelectedDocument = Documents.FirstOrDefault();
        });
        ToggleLeftPanelCommand = new Command(() => IsLeftPanelOpen = !IsLeftPanelOpen);
        ToggleRightPanelCommand = new Command(() => IsRightPanelOpen = !IsRightPanelOpen);
        CombineFilesCommand = new Command(async () => await ExecuteCombineFiles());
        
        ItemDraggedCommand = new Command<PageViewModel>((item) => _draggedItem = item);
        ItemDroppedCommand = new Command<PageViewModel>((targetItem) => {
            if (_draggedItem != null && targetItem != null && _draggedItem != targetItem && Pages != null)
            {
                int oldIndex = Pages.IndexOf(_draggedItem);
                int newIndex = Pages.IndexOf(targetItem);
                if (oldIndex != -1 && newIndex != -1)
                {
                    Pages.Move(oldIndex, newIndex);
                    StatusText = $"Moved {_draggedItem.PageLabel} to pos {newIndex + 1}";
                }
            }
            _draggedItem = null;
        });

        ToggleStampModeCommand = new Command(() => StatusText = "Stamp Mode Toggled");

        NextPageCommand = new Command(() => {
            if (SelectedDocument != null && CurrentPageIndex < SelectedDocument.Pages.Count - 1)
                CurrentPageIndex++;
        });
        PrevPageCommand = new Command(() => {
            if (CurrentPageIndex > 0)
                CurrentPageIndex--;
        });
        SelectPageCommand = new Command<int>((index) => CurrentPageIndex = index);
    }

    private async Task ExecuteSave()
    {
        if (SelectedDocument == null) return;
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions {
                PickerTitle = "Save PDF As",
                FileTypes = FilePickerFileType.Pdf
            });

            if (result != null)
            {
                StatusText = "Saving...";
                bool success = await Task.Run(() => PdfWriter.saveWithDeletion(result.FullPath, Array.Empty<int>()));
                if (success) StatusText = "File saved successfully.";
                else StatusText = "Save failed.";
            }
        }
        catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
    }

    private async Task ExecuteOpenFile()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions {
                PickerTitle = "Select PDF",
                FileTypes = FilePickerFileType.Pdf
            });

            if (result != null)
            {
                var doc = new DocumentViewModel(result.FullPath, (p) => Pages?.Remove(p));
                Documents.Add(doc);
                SelectedDocument = doc;
                StatusText = $"Opened {doc.FileName}";
            }
        }
        catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
    }

    private async Task ExecuteCombineFiles()
    {
        try
        {
            var results = await FilePicker.Default.PickMultipleAsync(new PickOptions {
                PickerTitle = "Select PDFs to combine",
                FileTypes = FilePickerFileType.Pdf
            });

            if (results != null && results.Any())
            {
                StatusText = "Combining files...";
                var paths = results.Select(r => r.FullPath).ToList();
                var output = Path.Combine(FileSystem.CacheDirectory, "combined_output.pdf");
                PdfWorkflow.mergeFilesEnumerable(paths, output);
                
                var doc = new DocumentViewModel(output, (p) => Pages?.Remove(p));
                Documents.Add(doc);
                SelectedDocument = doc;
                StatusText = "Files combined and loaded.";
            }
        }
        catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
    }

    private async Task RenderCurrentPageAsync()
    {
        if (SelectedDocument == null) return;

        try
        {
            StatusText = $"Rendering page {CurrentPageIndex + 1}...";
            
            // Render at high resolution for the main view
            var rgba = await Task.Run(() => PdfRenderer.renderPage(CurrentPageIndex, 1200, 1697));
            
            if (rgba != null)
            {
                var bytes = rgba.Value;
                CurrentPageImage = ImageSource.FromStream(() => new MemoryStream(RgbaToPng(bytes, 1200, 1697)));
                StatusText = "Page rendered.";
            }
            else
            {
                StatusText = "Failed to render page.";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Render error: {ex.Message}";
        }
    }

    private byte[] RgbaToPng(byte[] rgba, int width, int height)
    {
        var png = ImageEncoder.encodeToPng(rgba, width, height);
        if (png != null)
        {
            return png.Value;
        }
        return Array.Empty<byte>();
    }

    public void UpdateHardwareInfo()
    {
        try
        {
            var version = EngineInfo.getVersion();
            if (version != -1) StatusText = $"Engine Ready (v{version})";
        }
        catch { }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value)) return false;
        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
