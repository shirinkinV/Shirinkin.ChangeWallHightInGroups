using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Shirinkin.ChangeWallHightInGroups.Contracts;

namespace Shirinkin.ChangeWallHightInGroups.Model;
public class DetailLinesGetter : IDetailLinesGetter
{
    private readonly IThreadManager _threadManager;
    private readonly Document _doc;
    private readonly UIDocument _uiDoc;

    public DetailLinesGetter(RevitDocHandler revitDocHandler, IThreadManager threadManager)
    {
        _doc = revitDocHandler.Document;
        _uiDoc = revitDocHandler.UIDocument;
        _threadManager = threadManager;
    }

    public List<DetailLineDTO> GetAllLines()
    {
        var detailLines = new FilteredElementCollector(_doc)
            .WhereElementIsNotElementType()
            .Where(x => x is DetailLine)
            .Cast<DetailLine>()
            .ToList();
        return ConvertDetailLinesToDTO(detailLines);
    }

    private static List<DetailLineDTO> ConvertDetailLinesToDTO(List<DetailLine> detailLines)
    {
        return detailLines.Select(dl =>
        {
            var start = dl.GeometryCurve.Evaluate(0, true);
            var end = dl.GeometryCurve.Evaluate(1, true);
            //нужны только 2d координаты
            return new DetailLineDTO()
            {
                ID = dl.Id.IntegerValue,
                Start = new Point2D(start.X, start.Y),
                End = new Point2D(end.X, end.Y)
            };
        }
        ).ToList();
    }

    public List<DetailLineDTO> SelectDetailLinesOnView()
    {
        return _threadManager.RevitDispatcher.Invoke(() =>
        {
            var selected = _uiDoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, "Выберите линии, ограничивающие группы").Select(x => x.ElementId);
            var selectedLines =
                selected.Select(id => _doc.GetElement(id))
                .Where(x => x is DetailLine)
                .Cast<DetailLine>()
            .ToList();

            return ConvertDetailLinesToDTO(selectedLines);
        });
    }
}
