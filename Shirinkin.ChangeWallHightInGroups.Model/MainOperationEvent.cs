using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Shirinkin.ChangeWallHightInGroups.Contracts;

namespace Shirinkin.ChangeWallHightInGroups.Model;

public class MainOperationEvent : IMainOperationEvent, IExternalEventHandler
{
    static string eventAndTransactionName = "Изменение высоты стен в группах";

    private readonly IThreadManager _threadManager;
    private ExternalEvent? _externalEvent;
    private List<int> _groupIDs;
    private int _groupTypeID;
    private string _newGroupTypeName;
    private bool _isRunning;

    public MainOperationEvent(IThreadManager threadManager)
    {
        _threadManager = threadManager;
    }

    public bool IsRunning => _isRunning;

    public event Action Success;
    public event Action<Exception> Error;

    public void Execute(UIApplication app)
    {
        Transaction? t = null;
        try
        {
            _isRunning = true;
            t = new Transaction(app.ActiveUIDocument.Document, eventAndTransactionName);
            t.Start();
            ExecuteOperationBehindTransaction(app.ActiveUIDocument.Document);
            t.Commit();
            _isRunning = false;
            _threadManager.UIDispatcher.InvokeAsync(() => Success?.Invoke());
        }
        catch (Exception ex)
        {
            if (t is not null && t.GetStatus() == TransactionStatus.Started)
                t.RollBack();
            _isRunning = false;
            _threadManager.UIDispatcher.InvokeAsync(() => Error?.Invoke(ex));
        }
        finally
        {
            t?.Dispose();
        }
    }

    private void ExecuteOperationBehindTransaction(Document document)
    {
        var groupsSource =
            _groupIDs
            .Select(x => (Group)document.GetElement(new ElementId(x)))
            .Where(g => g.GroupType.Id.IntegerValue == _groupTypeID)
            .ToList();
        //1. Первую группу разгруппировываем, изменяем стены
        //2. Сгруппировываем все разгруппированные элементы обратно, создавая новый тип группы с указанным именем
        //3. У второй группы меняем тип группы на новый, запоминая при этом преобразование координат, которое применилось к элементам
        //4. Восстанавливаем правильные координаты второй группы
        //5. Проделываем п. 3-4 для всех остальных групп, используя преобразование координат, которое уже получили

        //1.
        var elements = groupsSource[0].UngroupMembers().Select(id => document.GetElement(id)).ToList();
        var walls = elements.Where(x => x is Wall);
        foreach (var wall in walls)
        {
            var topOffsetParam = wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);
            topOffsetParam.Set(-200.0 * 0.00328084);
        }
        document.Regenerate();//чтобы не появлялось уведомление, что группа была изменена вне режима редактирования групп
        //(ревиту кажется, что мы редактируем группу, а не создаём новую)

        //2.
        var newGroup = document.Create.NewGroup(elements.Select(x => x.Id).ToList());
        newGroup.GroupType.Name = _newGroupTypeName;
        var newGroupType = newGroup.GroupType;

        //3.
        if (groupsSource.Count == 1)
            return;
        var secondGroup = groupsSource[1];
        //обращение к свойству group.Location.Rotation выдаёт ошибку, поэтому информацию о расположении придется вытягивать из элементов
        var secondGroup_firstWallBefore = (Wall)secondGroup.GetMemberIds().Select(x => document.GetElement(x)).First(x => x is Wall);
        var secondGroup_curveBefore = ((LocationCurve)secondGroup_firstWallBefore.Location).Curve;
        var secondGroup_pointBefore = secondGroup_curveBefore.Evaluate(0, true);
        var tangentBefore = secondGroup_curveBefore.ComputeDerivatives(0, true).BasisX.Normalize();
        secondGroup.GroupType = newGroupType;
        //надеемся на то, что ревит сохраняет порядок элементов в группе
        var secondGroup_firstWallAfter = (Wall)secondGroup.GetMemberIds().Select(x => document.GetElement(x)).First(x => x is Wall);
        var secondGroup_curveAfter = ((LocationCurve)secondGroup_firstWallAfter.Location).Curve;
        var tangentAfter = secondGroup_curveAfter.ComputeDerivatives(0, true).BasisX.Normalize();
        //4.
        //угол между двумя единичными векторами считаем по их векторному и скалярному произведению
        var angleBetweenTwoTangents = Math.Atan2(tangentBefore.X * tangentAfter.Y - tangentBefore.Y * tangentAfter.X,tangentAfter.X*tangentBefore.X+tangentAfter.Y*tangentBefore.Y);
        var secondGroup_locationPoint = ((LocationPoint)secondGroup.Location).Point;
        secondGroup.Location.Rotate(Line.CreateBound(secondGroup_locationPoint, secondGroup_locationPoint + new XYZ(0, 0, 1)), -angleBetweenTwoTangents);
        secondGroup_curveAfter = ((LocationCurve)secondGroup_firstWallAfter.Location).Curve;
        //после доворота получаем координаты точек, чтобы увидеть разницу переноса
        var secondGroup_pointAfter = secondGroup_curveAfter.Evaluate(0, true);
        var secondGroup_translateDifference = secondGroup_pointAfter - secondGroup_pointBefore;
        secondGroup.Location.Move(-secondGroup_translateDifference);
        //5.
        foreach (var group in groupsSource.Skip(2))
        {
            var firstWallBefore = (Wall)group.GetMemberIds().Select(x => document.GetElement(x)).First(x => x is Wall);
            var curveBefore = ((LocationCurve)firstWallBefore.Location).Curve;
            var pointBefore = curveBefore.Evaluate(0, true);
            group.GroupType = newGroupType;
            var firstWallAfter = (Wall)group.GetMemberIds().Select(x => document.GetElement(x)).First(x => x is Wall);
            var locationPoint = ((LocationPoint)group.Location).Point;
            group.Location.Rotate(Line.CreateBound(locationPoint, locationPoint + new XYZ(0, 0, 1)), -angleBetweenTwoTangents);
            var curveAfter = ((LocationCurve)firstWallAfter.Location).Curve;
            //смещение придется каждый раз считать заново, потому что мы не знаем, как была группа повернута до преобразования
            var pointAfter = curveAfter.Evaluate(0, true);
            var translateDifference = pointAfter - pointBefore;
            group.Location.Move(-translateDifference);
        }
    }

    public string GetName() => eventAndTransactionName;

    public void Register()
    {
        _externalEvent = ExternalEvent.Create(this);
    }

    public void Run(List<int> groupIDs, int groupTypeID, string newGroupTypeName)
    {
        _groupIDs = groupIDs;
        _groupTypeID = groupTypeID;
        _newGroupTypeName = newGroupTypeName;
        _externalEvent?.Raise();
    }
}
