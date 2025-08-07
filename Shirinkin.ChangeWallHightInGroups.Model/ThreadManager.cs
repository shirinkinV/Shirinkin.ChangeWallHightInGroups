using Shirinkin.ChangeWallHightInGroups.Contracts;
using System.Windows.Threading;

namespace Shirinkin.ChangeWallHightInGroups.Model;

public class ThreadManager : IThreadManager
{
    private Dispatcher? _uiDispatcher;
    private readonly Dispatcher _revitDispatcher;

    public Dispatcher UIDispatcher
    {
        get
        {
            _uiDispatcher ??= Dispatcher.FromThread(UIThread);
            while (_uiDispatcher is null || _uiDispatcher.Thread is null)
            {
                _uiDispatcher = Dispatcher.FromThread(UIThread);
                Thread.Sleep(100);//ждем, пока поток UIThread запустится
            }
            return _uiDispatcher;
        }
    }

    public Dispatcher RevitDispatcher => _revitDispatcher;

    public string NameOfPlugin { get; }
    public Thread UIThread { get; }

    public ThreadManager(string nameOfPlugin)
    {
        NameOfPlugin = nameOfPlugin;
        _revitDispatcher = Dispatcher.CurrentDispatcher;
        var syncContext = SynchronizationContext.Current;
        //создаём UI поток и присваиваем диспетчер
        UIThread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(syncContext);
            Dispatcher.Run();
        });
        UIThread.SetApartmentState(ApartmentState.STA);
        UIThread.Name = $"{NameOfPlugin} main UI thread";
        UIThread.Start();
        while (UIDispatcher is null)
        {
            //ждём запуска ui потока с диспетчером
            Thread.Sleep(100);
        }
    }

    public void Dispose()
    {
        try
        {
            if (
                UIDispatcher is not null &&
                UIDispatcher.Thread.IsAlive &&
                UIDispatcher.Thread.ThreadState == ThreadState.Running
                )
                UIDispatcher.InvokeShutdown();
        }
        catch { }
    }
}
