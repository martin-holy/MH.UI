namespace MH.UI.Controls;

public class RangeSelector : ValueSelectorBase {
  double _startValue;
  double _endValue = 100;

  public double StartValue {
    get => _startValue;
    set {
      var snapped = Snap(value);

      if (snapped > EndValue)
        snapped = EndValue;

      _setIfVary(ref _startValue, snapped);
    }
  }

  public double EndValue {
    get => _endValue;
    set {
      var snapped = Snap(value);

      if (snapped < StartValue)
        snapped = StartValue;

      _setIfVary(ref _endValue, snapped);
    }
  }

  protected override void OnRangeChanged() {
    StartValue = _startValue;
    EndValue = _endValue;
  }
}