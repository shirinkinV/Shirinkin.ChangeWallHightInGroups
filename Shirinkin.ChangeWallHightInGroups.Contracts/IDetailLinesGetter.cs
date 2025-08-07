namespace Shirinkin.ChangeWallHightInGroups.Contracts;
public interface IDetailLinesGetter
{
    List<DetailLineDTO> GetAllLines();
    List<DetailLineDTO> SelectDetailLinesOnView();
}
