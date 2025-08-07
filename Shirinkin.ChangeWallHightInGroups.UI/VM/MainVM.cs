using Shirinkin.ChangeWallHightInGroups.Contracts;
using Shirinkin.ChangeWallHightInGroups.UI.VM;
using Shirinkin.ChangeWallHightInGroups.UI.xaml;
using System.Windows;
using System.Windows.Input;

namespace Shirinkin.ChangeWallHightInGroups.UI.VM;

public class MainVM : ObservableObject, IMainVM
{
    /// <summary>
    /// Пустой конструктор для дизайнерского режима xaml
    /// </summary>
    public MainVM() { }

    public MainVM(
        IDetailLinesGetter detailLinesGetter,
        IGroupsFinder groupsFinder,
        IMainOperationEvent mainOperationEvent)
    {
        _detailLinesGetter = detailLinesGetter;
        _groupsFinder = groupsFinder;
        _mainOperationEvent = mainOperationEvent;
        _mainOperationEvent.Success += _mainOperationEvent_Success;
        _mainOperationEvent.Error += _mainOperationEvent_Error;
    }

    private void _mainOperationEvent_Error(Exception ex)
    {
        ResultText = ex.ToString();
        _mainWindow.Activate();
        OperationDone = true;
        CommandManager.InvalidateRequerySuggested();
        Notify(nameof(OperationIsRunning));
    }

    private void _mainOperationEvent_Success()
    {
        ResultText = "Успешно";
        _mainWindow.Activate();
        OperationDone = true;
        CommandManager.InvalidateRequerySuggested();
        Notify(nameof(OperationIsRunning));
    }

    private RelayCommand _selectLinesOnViewCommand;
    private readonly IDetailLinesGetter _detailLinesGetter;
    private readonly IGroupsFinder _groupsFinder;
    private readonly IMainOperationEvent _mainOperationEvent;
    private List<DetailLineDTO>? _selectedLines;
    private MainWindow _mainWindow;
    private List<GroupTypeDTO>? _posibleGroupTypes;
    private List<int> _groupsIDInCircuit;
    private GroupTypeDTO? _selectedGroupType;
    private string _newNameOfGroupType;
    private RelayCommand executeOperationCommand;
    private string _resultText;

    public List<DetailLineDTO>? SelectedLines { get => _selectedLines; set => Set(ref _selectedLines, value); }
    public List<int> GroupsIDInCircuit { get => _groupsIDInCircuit; set => Set(ref _groupsIDInCircuit, value); }

    internal void ContentOnScreen(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        SelectedLines = _detailLinesGetter.GetAllLines();
        GroupsIDInCircuit = _groupsFinder.FindGroupsIDInCircuit(SelectedLines);
        PosibleGroupTypes = _groupsFinder.GetGroupTypesOfGroups(GroupsIDInCircuit);
        SelectedGroupType = PosibleGroupTypes?.FirstOrDefault();
    }

    public ICommand SelectLinesOnViewCommand => _selectLinesOnViewCommand ??= new RelayCommand(PerformSelection, () => _mainOperationEvent.IsRunning == false && OperationDone == false);


    public void PerformSelection()
    {
        SelectedLines = _detailLinesGetter.SelectDetailLinesOnView();
        GroupsIDInCircuit = _groupsFinder.FindGroupsIDInCircuit(SelectedLines);
        PosibleGroupTypes = _groupsFinder.GetGroupTypesOfGroups(GroupsIDInCircuit);
        SelectedGroupType = PosibleGroupTypes?.FirstOrDefault();
        _mainWindow.Activate();
    }

    public List<GroupTypeDTO>? PosibleGroupTypes { get => _posibleGroupTypes; set => Set(ref _posibleGroupTypes, value); }

    public GroupTypeDTO? SelectedGroupType
    {
        get => _selectedGroupType;
        set
        {
            if (Set(ref _selectedGroupType, value))
                NewNameOfGroupType = (SelectedGroupType?.Name ?? "") + "_(+300)";
        }
    }

    public string NewNameOfGroupType { get => _newNameOfGroupType; set => Set(ref _newNameOfGroupType, value); }

    public ICommand ExecuteOperationCommand => executeOperationCommand ??= new RelayCommand(PerformExecuteMain, () => _mainOperationEvent.IsRunning == false && OperationDone == false);

    private void PerformExecuteMain()
    {
        if (SelectedGroupType is null)
            return;
        _mainOperationEvent.Run(GroupsIDInCircuit, SelectedGroupType.ID, NewNameOfGroupType);
        Thread.Sleep(100);//на всякий случай подождём когда запустится
        CommandManager.InvalidateRequerySuggested();//обновляем состояние кнопок, чтобы стали неактивными
        Notify(nameof(OperationIsRunning));
    }

    public bool OperationIsRunning => _mainOperationEvent.IsRunning;

    public string ResultText { get => _resultText; private set => Set(ref _resultText, value); }

    public bool OperationDone { get; private set; }
}
