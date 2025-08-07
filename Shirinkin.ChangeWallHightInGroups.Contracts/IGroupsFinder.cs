
namespace Shirinkin.ChangeWallHightInGroups.Contracts;
public interface IGroupsFinder
{
    List<int> FindGroupsIDInCircuit(List<DetailLineDTO> lines);
    List<GroupTypeDTO> GetGroupTypesOfGroups(List<int> groupsID);
}
