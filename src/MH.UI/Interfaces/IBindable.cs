namespace MH.UI.Interfaces;

public interface IBindable<in T> {
  void Bind(T item);
  void Unbind();
}