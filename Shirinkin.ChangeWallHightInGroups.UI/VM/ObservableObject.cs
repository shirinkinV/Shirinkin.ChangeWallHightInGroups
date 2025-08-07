using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shirinkin.ChangeWallHightInGroups.UI.VM;

public class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Метод, который надо записывать в сеттерах свойств, работаюхих в связке с полями таким образом: <para></para>
    /// set => if(Set(ref fieldName, value)) DoCallbackForThatProperty();
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field">ссылка на изменяемое поле, которое спрятано в свойстве</param>
    /// <param name="value">новое значение свойства</param>
    /// <param name="propertyName">имя свойства, которое подхватывается автоматически, если метод вызван в сеттере свойства</param>
    /// <returns></returns>
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    /// <summary>
    /// Метод принудительного вызова уведомления для какого либо свойства
    /// </summary>
    /// <param name="propertyName">имя свойства, которое подхватывается автоматически, если метод вызван в сеттере свойства</param>
    public void Notify([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}