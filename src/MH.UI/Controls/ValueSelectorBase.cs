using MH.Utils.BaseClasses;
using System;

namespace MH.UI.Controls;

public abstract class ValueSelectorBase : ObservableObject {
  double _minimum;
  double _maximum = 100;
  double _tickFrequency = 1;

  public double Minimum {
    get => _minimum;
    set {
      if (_setIfVary(ref _minimum, value)) {
        if (_maximum < _minimum)
          _maximum = _minimum;

        OnRangeChanged();
        OnPropertyChanged();
      }
    }
  }

  public double Maximum {
    get => _maximum;
    set {
      var coerced = Math.Max(value, Minimum);

      if (_setIfVary(ref _maximum, coerced))
        OnRangeChanged();
    }
  }

  public double TickFrequency {
    get => _tickFrequency;
    set {
      var coerced = value <= 0 ? 1 : value;

      if (_setIfVary(ref _tickFrequency, coerced))
        OnRangeChanged();
    }
  }

  protected virtual void OnRangeChanged() { }

  protected double Clamp(double value) {
    if (value < Minimum) return Minimum;
    if (value > Maximum) return Maximum;
    return value;
  }

  protected double Snap(double value) {
    value = Clamp(value);

    var steps = Math.Round((value - Minimum) / TickFrequency);
    return Minimum + steps * TickFrequency;
  }
}