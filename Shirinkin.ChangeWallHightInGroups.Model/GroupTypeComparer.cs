using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shirinkin.ChangeWallHightInGroups.Model;
internal class GroupTypeComparer : IEqualityComparer<GroupType>
{
    public bool Equals(GroupType x, GroupType y)
    {
        return x.Id == y.Id;
    }

    public int GetHashCode(GroupType obj)
    {
        return obj.Id.GetHashCode();
    }
}
