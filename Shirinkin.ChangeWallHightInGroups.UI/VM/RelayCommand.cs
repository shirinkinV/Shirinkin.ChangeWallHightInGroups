using System.Windows.Input;

namespace Shirinkin.ChangeWallHightInGroups.UI.VM;

public class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Action execute = execute;
    private readonly Func<bool>? canExecute = canExecute;

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    /// <inheritdoc/>
    public bool CanExecute(object? parameter) => canExecute is null || canExecute();

    /// <inheritdoc/>
    public void Execute(object? parameter) => execute();

    /// <summary>
    /// Неявный оператор преобразования из делегата Action
    /// </summary>
    /// <param name="action"></param>
    public static implicit operator RelayCommand(Action action) => new(action);
}