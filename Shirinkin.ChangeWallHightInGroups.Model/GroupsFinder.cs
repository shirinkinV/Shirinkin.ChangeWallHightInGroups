using Autodesk.Revit.DB;
using Shirinkin.ChangeWallHightInGroups.Contracts;
using System.Linq;

namespace Shirinkin.ChangeWallHightInGroups.Model;
public class GroupsFinder : IGroupsFinder
{
    private readonly Document _doc;

    public GroupsFinder(RevitDocHandler revitDocHandler)
    {
        _doc = revitDocHandler.Document;
    }

    public List<int> FindGroupsIDInCircuit(List<DetailLineDTO> lines)
    {
        //для всех групп проверяем, лежит ли их начало внутри контура lines

        var allGroupsIdAndTheirPoints =
            new FilteredElementCollector(_doc)
                .WhereElementIsNotElementType()
                .OfClass(typeof(Group))
                .Cast<Group>()
                .Select(group => (Id: group.Id.IntegerValue, Point: new Point2D(((LocationPoint)group.Location).Point.X, ((LocationPoint)group.Location).Point.Y)))
                .ToList();
        return allGroupsIdAndTheirPoints
            .Where(x => IsPointInsideCircuit(x.Point, lines))
            .Select(x => x.Id)
            .ToList();
    }

    private bool IsPointInsideCircuit(Point2D point, List<DetailLineDTO> lines)
    {
        //алгоритм пересечения луча, отправленного из точки, с контуром
        //четное количество пересечений - точка не лежит в контуре
        //нечётное - точка в контуре
        //случайное направление луча
        var random = new Random();
        var rayUPerp = random.NextDouble();
        var rayVPerp = random.NextDouble();
        var rayPerpendicular = new Point2D(rayUPerp, rayVPerp).GetNormalized();
        var u0 = point.X;
        var v0 = point.Y;
        //уравнение прямой для проверки близости отрезков
        var a = rayPerpendicular.X;
        var b = rayPerpendicular.Y;
        var c = -a * u0 - b * v0;
        var rayDir = new Point2D(-b, a);
        //все отрезки на пересечение с лучом проверять не надо
        //проверяем только те отрезки, которые подозреваем в пересечении с лучом, а это такие отрезки,
        //центры которых находятся на расстоянии от луча меньше, чем половина их длины
        var segmentsNeedCheck = lines
            .Where(x => DistanceFromPointToLineIfABNormalized(a, b, c, x.Center.X, x.Center.Y) <= x.Length * 0.5)
            //теперь убираем параллельные лучу отрезки
            .Select(x =>
            {
                (DetailLineDTO source, Point2D ab) result = (x, x.End - x.Start);
                return result;
            })
            .Where(x => Math.Abs(Vector2DCross(x.ab, rayDir)) > 1e-10)
            //угол между векторами больше ~1e-10 рад, то есть выкидываем отрезки, параллельные лучу
            //TODO задать здесь точность
            .ToList();
        var cd = rayDir;
        var cdu = cd.X;
        var cdv = cd.Y;
        var segmentsHasIntersectionAndItsIntersectios = segmentsNeedCheck
            .Select(x =>
            {
                //ищем пересечение прямых отрезка и луча, и смотрим лежит ли оно в отрезке
                var ab = x.ab;
                var ac = point - x.source.Start;
                var acu = ac.X;
                var acv = ac.Y;
                var abu = ab.X;
                var abv = ab.Y;
                var f = abv * cd.X - cd.Y * abu;
                var t = (cdu * acv - cdv * acu) / f;
                var s = Math.Abs(cdu) > 1e-5 ? (x.source.Start.X + ab.X * t - point.X) / cdu : (x.source.Start.Y + ab.Y * t - point.Y) / cdv;
                (bool IsIntersect, double intersectionParameter, DetailLineDTO? source) result;
                //сложное условие, во первых луч должен пересекать отрезок одним своим концом, во вторых точка пересечения должна лежать на самом отрезке, а не просто на его линии
                if (t > 1e-5 && t < 1 + 1e-5 && s > 0
                )//TODO задать точность
                    result = (true, t, x.source);
                else
                    result = (false, 0, null);
                return result;
            })
            .Where(x => x.IsIntersect && Math.Abs(x.intersectionParameter) > 1e-5)//отбрасываем отрезки, которые мы пересекли в начале, чтобы избежать дублирования в точках пересечений
            .ToList();

        return segmentsHasIntersectionAndItsIntersectios.Count % 2 == 1;
    }

    /// <summary>
    /// Векторное произведение двухмерных векторов. Является длиной вектора в 3D пространстве, который является произведением их дополнений до 3D
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private double Vector2DCross(Point2D v1, Point2D v2)
    {
        return v1.X * v2.Y - v1.Y * v2.X;
    }

    /// <summary>
    /// Нахождение расстояния от точки (u,v) до прямой, заданной уравнением ax+by+c=0. При условии, что a*a + b*b = 1
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="u"></param>
    /// <param name="v"></param>
    /// <returns></returns>
    private double DistanceFromPointToLineIfABNormalized(double a, double b, double c, double u, double v)
        => Math.Abs(a * u + b * v + c);

    public List<GroupTypeDTO> GetGroupTypesOfGroups(List<int> groupsID)
    {
        var groups = groupsID.Select(x => (Group)_doc.GetElement(new ElementId(x)));
        var groupTypes = groups
            .Select(x => x.GroupType)
            .Distinct(new GroupTypeComparer())
            .Select(gt => new GroupTypeDTO() { ID = gt.Id.IntegerValue, Name = gt.Name })
            .ToList();
        return groupTypes;
    }
}
