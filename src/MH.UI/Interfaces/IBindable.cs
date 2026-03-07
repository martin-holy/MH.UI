namespace MH.UI.Interfaces;

public interface IBindable<in T> : IUnbindable {
  void Bind(T item);

  void Rebind(T item) {
    Unbind();
    Bind(item);
  }
}