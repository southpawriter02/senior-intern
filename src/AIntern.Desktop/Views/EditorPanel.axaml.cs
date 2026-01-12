using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Search;
using AIntern.Core.Models;
using AIntern.Desktop.Services;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

public partial class EditorPanel : UserControl
{
    private EditorPanelViewModel? _viewModel;
    private SyntaxHighlightingService? _syntaxService;
    private IDisposable? _settingsBinding;

    public EditorPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from old ViewModel
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.UndoRequested -= OnUndoRequested;
            _viewModel.RedoRequested -= OnRedoRequested;
            _viewModel.FindRequested -= OnFindRequested;
            _viewModel.ReplaceRequested -= OnReplaceRequested;
            _viewModel.GoToLineRequested -= OnGoToLineRequested;
        }

        _viewModel = DataContext as EditorPanelViewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.UndoRequested += OnUndoRequested;
            _viewModel.RedoRequested += OnRedoRequested;
            _viewModel.FindRequested += OnFindRequested;
            _viewModel.ReplaceRequested += OnReplaceRequested;
            _viewModel.GoToLineRequested += OnGoToLineRequested;

            UpdateEditorForActiveTab();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorPanelViewModel.ActiveTab))
        {
            UpdateEditorForActiveTab();
        }
    }

    private void UpdateEditorForActiveTab()
    {
        var activeTab = _viewModel?.ActiveTab;

        if (activeTab == null)
        {
            Editor.Document = null;
            return;
        }

        Editor.Document = activeTab.Document;
        Editor.IsReadOnly = activeTab.IsReadOnly;

        if (_syntaxService != null)
        {
            _syntaxService.SetLanguage(Editor, activeTab.Language);
        }

        Editor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
        Editor.TextArea.SelectionChanged += OnSelectionChanged;

        UpdateCaretPosition();
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e) => UpdateCaretPosition();
    private void OnSelectionChanged(object? sender, EventArgs e) => UpdateCaretPosition();

    private void UpdateCaretPosition()
    {
        if (_viewModel?.ActiveTab == null) return;

        var caret = Editor.TextArea.Caret;
        var selection = Editor.TextArea.Selection;
        var selectionLength = selection.IsEmpty ? 0 : Math.Abs(selection.Length);

        _viewModel.ActiveTab.UpdateCaretPosition(caret.Line, caret.Column, selectionLength);
    }

    /// <summary>
    /// Initializes the editor panel with syntax and settings services.
    /// </summary>
    public void Initialize(SyntaxHighlightingService syntaxService, AppSettings settings)
    {
        _syntaxService = syntaxService;

        EditorConfiguration.ApplySettings(Editor, settings);
        _settingsBinding?.Dispose();
        _settingsBinding = EditorConfiguration.BindToSettings(Editor, settings);
        _syntaxService.ApplyHighlighting(Editor, null);
    }

    // Event handlers
    private void OnUndoRequested(object? sender, EventArgs e) => Editor.Undo();
    private void OnRedoRequested(object? sender, EventArgs e) => Editor.Redo();

    private void OnFindRequested(object? sender, EventArgs e)
    {
        var searchPanel = SearchPanel.Install(Editor);
        searchPanel.Open();
    }

    private void OnReplaceRequested(object? sender, EventArgs e)
    {
        var searchPanel = SearchPanel.Install(Editor);
        searchPanel.Open();
        searchPanel.IsReplaceMode = true;
    }

    private void OnGoToLineRequested(object? sender, int lineNumber)
    {
        if (_viewModel?.ActiveTab == null) return;

        var offset = _viewModel.ActiveTab.GetOffsetForLine(lineNumber);
        Editor.TextArea.Caret.Offset = offset;
        Editor.TextArea.Caret.BringCaretToView();
        Editor.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.S:
                    _viewModel?.SaveCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.W:
                    if (_viewModel?.ActiveTab != null)
                        _viewModel.CloseTabCommand.Execute(_viewModel.ActiveTab);
                    e.Handled = true;
                    break;
                case Key.G:
                    _viewModel?.GoToLineCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.F:
                    _viewModel?.FindCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.H:
                    _viewModel?.ReplaceCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
        else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.Tab)
        {
            _viewModel?.NextTabCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.Tab)
        {
            _viewModel?.PreviousTabCommand.Execute(null);
            e.Handled = true;
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        _settingsBinding?.Dispose();
        _syntaxService?.RemoveHighlighting(Editor);

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.UndoRequested -= OnUndoRequested;
            _viewModel.RedoRequested -= OnRedoRequested;
            _viewModel.FindRequested -= OnFindRequested;
            _viewModel.ReplaceRequested -= OnReplaceRequested;
            _viewModel.GoToLineRequested -= OnGoToLineRequested;
        }
    }
}
