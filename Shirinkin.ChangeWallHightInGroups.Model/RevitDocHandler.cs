using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Shirinkin.ChangeWallHightInGroups.Model;

public class RevitDocHandler
{
    public RevitDocHandler(Document document, UIDocument uIDocument)
    {
        Document = document;
        UIDocument = uIDocument;
    }

    public Document Document { get; }
    public UIDocument UIDocument { get; }
}
