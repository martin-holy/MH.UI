using MH.Utils;
using System;
using System.ComponentModel;

namespace MH.UI.Binding;

public sealed class ViewBinder<TSource, TProp, TValue> : IDisposable
  where TSource : class, INotifyPropertyChanged {

  private readonly Action<EventHandler<TValue>>? _subscribe;
  private readonly Action<EventHandler<TValue>>? _unsubscribe;
  private readonly Action<TValue> _setValue;
  private readonly EventHandler<TValue>? _viewChangedHandler;

  private IDisposable? _vmSubscription;
  private Action<TValue>? _vmSetter;
  private bool _updating;
  private bool _disposed;

  public ViewBinder(
    TSource source,
    string propertyName,
    Func<TSource, TProp> getter,
    Action<TSource, TProp> setter,
    Action<TValue> setValue,
    Action<EventHandler<TValue>> subscribe,
    Action<EventHandler<TValue>> unsubscribe) {

    _subscribe = subscribe;
    _unsubscribe = unsubscribe;
    _setValue = setValue;
    _viewChangedHandler = _onViewChanged;
    _subscribe(_viewChangedHandler);
    _bind(source, propertyName, getter, setter);
  }

  public ViewBinder(
    TSource source,
    string propertyName,
    Func<TSource, TProp> getter,
    Action<TValue> setValue) {

    _setValue = setValue;
    _bind(source, propertyName, getter, null);
  }

  private void _onViewChanged(object? sender, TValue newValue) {
    if (!_updating) _vmSetter?.Invoke(newValue);
  }

  private void _bind(TSource source, string propertyName, Func<TSource, TProp> getter, Action<TSource, TProp>? setter) {
    _vmSubscription?.Dispose();
    _vmSetter = null;

    // VM → View
    _vmSubscription = source.Bind(propertyName, getter, v => {
      _updating = true;
      try {
        _setValue((TValue)Convert.ChangeType(v, typeof(TValue))!);
      }
      finally { _updating = false; }
    });

    // View → VM
    if (setter != null) {
      _vmSetter = v => {
        if (!_updating)
          setter(source, (TProp)Convert.ChangeType(v, typeof(TProp))!);
      };
    }
  }

  public void Dispose() {
    if (_disposed) return;
    _disposed = true;

    _vmSubscription?.Dispose();

    if (_unsubscribe != null && _viewChangedHandler != null)
      _unsubscribe(_viewChangedHandler);
  }
}