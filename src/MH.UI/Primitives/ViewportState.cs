namespace MH.UI.Primitives;

public readonly struct ViewportState {
  public float ScaleX { get; }
  public float ScaleY { get; }
  public float TranslateX { get; }
  public float TranslateY { get; }

  public double ContentWidth { get; }
  public double ContentHeight { get; }

  public double HostWidth { get; }
  public double HostHeight { get; }

  public bool HasHostArea =>
    HostWidth > 0 && HostHeight > 0;

  public bool HasContentArea =>
    ContentWidth > 0 && ContentHeight > 0;

  public bool HasArea =>
    HasHostArea && HasContentArea;

  public ViewportState(float scaleX, float scaleY, float translateX, float translateY,
    double contentWidth, double contentHeight, double hostWidth, double hostHeight) {

    ScaleX = scaleX;
    ScaleY = scaleY;
    TranslateX = translateX;
    TranslateY = translateY;
    ContentWidth = contentWidth;
    ContentHeight = contentHeight;
    HostWidth = hostWidth;
    HostHeight = hostHeight;
  }
}