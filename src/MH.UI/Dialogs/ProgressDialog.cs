using MH.UI.Controls;
using MH.Utils.BaseClasses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MH.UI.Dialogs;

public class ProgressDialog<T> : Dialog {
  private readonly T[] _items;
  private readonly bool _autoClose;
  private int _progressMax;
  private int _progressValue;
  private int _progressIndex;
  private string? _progressText;
  private readonly IProgress<(int, string, object?)> _progress;

  public int ProgressMax { get => _progressMax; set { _progressMax = value; OnPropertyChanged(); } }
  public int ProgressValue { get => _progressValue; set { _progressValue = value; OnPropertyChanged(); } }
  public string? ProgressText { get => _progressText; set { _progressText = value; OnPropertyChanged(); } }
  public bool RunSync { get; set; }
  public AsyncRelayCommand ActionCommand { get; }

  public ProgressDialog(string title, string icon, T[] items, string? actionIcon = null, string? actionText = null, bool autoClose = true) : base(title, icon) {
    ActionCommand = new(_doAction, _canAction, actionIcon, actionText);
    _items = items;
    _autoClose = autoClose;
    _progressMax = _items.Length;
    _progressValue = _progressMax;
    _progress = new Progress<(int, string, object?)>(x => {
      ProgressValue = x.Item1;
      ProgressText = x.Item2;
      _customProgress(x.Item3);
    });

    Buttons = actionIcon == null && actionText == null
      ? [new(CloseCommand, true, true)]
      : [new(ActionCommand, true), new(CloseCommand, false, true)];
  }

  protected void _autoRun() {
    if (ActionCommand.CanExecute(null))
      ActionCommand.Execute(null);
  }

  protected void _reportProgress(string msg) =>
    _reportProgress(msg, null);

  protected virtual void _reportProgress(string msg, object? args) =>
    _progress.Report((++_progressIndex, msg, args));

  protected virtual void _customProgress(object? args) { }

  protected override Task _onResultChanged(int result) {
    if (result == 0) ActionCommand.CancelCommand.Execute(null);
    return Task.CompletedTask;
  }

  protected virtual bool _canAction() => true;

  protected virtual bool _doBefore() => true;

  protected virtual Task _do(T item, CancellationToken token) => Task.CompletedTask;

  protected virtual async Task _do(T[] items, CancellationToken token) {
    foreach (var item in _items) {
      if (token.IsCancellationRequested) break;
      await _do(item, token).ConfigureAwait(false);
    }
  }

  protected virtual void _doAfter() { }

  private async Task _doAction(CancellationToken token) {
    try {
      if (!_doBefore()) return;

      _progressIndex = 0;

      if (RunSync)
        await _do(_items, token);
      else
        await Task.Run(async () => { await _do(_items, token).ConfigureAwait(false); }, token);
    }
    finally {
      _doAfter();
      if (_autoClose) Result = 1;
    }
  }
}