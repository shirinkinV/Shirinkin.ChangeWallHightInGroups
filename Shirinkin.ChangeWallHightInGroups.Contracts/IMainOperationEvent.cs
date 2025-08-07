namespace Shirinkin.ChangeWallHightInGroups.Contracts;
public interface IMainOperationEvent
{
    void Register();
    void Run(List<int> groupIDs, int groupTypeID, string newGroupTypeName);
    bool IsRunning { get; }
    event Action Success;
    event Action<Exception> Error;
}
