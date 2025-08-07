using System.Windows.Threading;

namespace Shirinkin.ChangeWallHightInGroups.Contracts;
public interface IThreadManager : IDisposable
{
    Dispatcher UIDispatcher { get; }
    Dispatcher RevitDispatcher { get; }
}
