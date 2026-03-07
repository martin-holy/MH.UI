namespace MH.UI.Interfaces;

public interface IBindable<in T> : IUnbindable {
  void Bind(T item);
}