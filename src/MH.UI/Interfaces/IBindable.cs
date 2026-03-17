namespace MH.UI.Interfaces;

public interface IBindable<T> : IUnbindable {
  T? DataContext { get; }

  void Bind(T item);

  void Rebind(T item) {
    Unbind();
    Bind(item);
  }
}