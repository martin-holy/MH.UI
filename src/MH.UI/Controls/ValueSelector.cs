namespace MH.UI.Controls;

public class ValueSelector : ValueSelectorBase {
  double _value;

  public double Value {
    get => _value;
    set => _setIfVary(ref _value, Snap(value));
  }

  protected override void OnRangeChanged() {
    Value = _value;
  }
}