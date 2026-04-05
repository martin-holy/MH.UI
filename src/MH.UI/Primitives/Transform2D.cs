namespace MH.UI.Primitives;

public struct Transform2D {
  public float ScaleX;
  public float ScaleY;
  public float TranslateX;
  public float TranslateY;

  public Transform2D(float scaleX, float scaleY, float translateX, float translateY) {
    ScaleX = scaleX;
    ScaleY = scaleY;
    TranslateX = translateX;
    TranslateY = translateY;
  }
}