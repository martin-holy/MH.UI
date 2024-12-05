using MH.UI.Controls;
using MH.Utils.BaseClasses;
using System;

namespace MH.UI.Dialogs;

public class InputDialog : Dialog {
  private string _message;
  private string? _answer;
  private string? _errorMessage;
  private bool _error;
  private readonly Func<string?, string?> _validator;

  public string Message { get => _message; set { _message = value; OnPropertyChanged(); } }
  public string? Answer { get => _answer; set { _answer = value; OnPropertyChanged(); } }
  public string? ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }
  public bool Error { get => _error; set { _error = value; OnPropertyChanged(); } }

  public InputDialog(string title, string message, string icon, string? answer, Func<string?, string?> validator) : base(title, icon) {
    _message = message;
    _answer = answer;
    _validator = validator;

    Buttons = [
      new(new RelayCommand(_validate, Res.IconCheckMark, "Ok"), true),
      new(CancelCommand, false, true)
    ];
  }

  private void _validate() {
    ErrorMessage = _validator(_answer);
    if (!string.IsNullOrEmpty(_errorMessage)) {
      Error = true;
      return;
    }

    Result = 1;
  }
}