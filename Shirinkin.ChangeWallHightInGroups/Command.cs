using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Shirinkin.ChangeWallHightInGroups.Contracts;
using Shirinkin.ChangeWallHightInGroups.Model;
using Shirinkin.ChangeWallHightInGroups.UI.VM;
using Shirinkin.ChangeWallHightInGroups.UI.xaml;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Shirinkin.ChangeWallHightInGroups;

public class Command : IExternalCommand
{
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    public IntPtr GetHandleWindow(string title) => FindWindow(null!, title);

    private static string _windowTitle;
    private ServiceCollection? _serviceCollection;
    private ServiceProvider? _serviceProvider;

    private static string WindowTitle => _windowTitle ??= $"Изменение высоты стен в группах {typeof(Command).Assembly.GetName().Version}";

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        //checking if it's already running
        (bool flowControl, Result value) = CheckWindowIsAlreadyOpened();
        if (!flowControl)
            return value;

        _serviceCollection = new ServiceCollection();
        //services
        _serviceCollection.AddSingleton<IThreadManager, ThreadManager>(InitThreadManager());
        _serviceCollection.AddSingleton<RevitDocHandler>(new RevitDocHandler(commandData.Application.ActiveUIDocument.Document, commandData.Application.ActiveUIDocument));
        _serviceCollection.AddSingleton<IDetailLinesGetter, DetailLinesGetter>();
        _serviceCollection.AddSingleton<IGroupsFinder, GroupsFinder>();

        //external events
        _serviceCollection.AddSingleton<IMainOperationEvent, MainOperationEvent>();

        //ui
        _serviceCollection.AddSingleton<IMainVM, MainVM>();
        _serviceCollection.AddSingleton<MainWindow>(InitMainWindow());

        //build
        _serviceProvider = _serviceCollection.BuildServiceProvider();

        //register external events
        _serviceProvider.GetRequiredService<IMainOperationEvent>().Register();

        //ui start
        _serviceProvider.GetRequiredService<IThreadManager>()
            .UIDispatcher.InvokeAsync(() => _serviceProvider.GetRequiredService<MainWindow>().Show());
        return Result.Succeeded;
    }

    private static Func<IServiceProvider, MainWindow> InitMainWindow()
    {
        return provider =>
        {
            MainWindow? window = null;
            //create window in ui thread
            var threadManager = provider.GetRequiredService<IThreadManager>();
            threadManager.UIDispatcher.Invoke(() =>
            {
                window = new MainWindow
                {
                    Title = WindowTitle,
                    DataContext = provider.GetRequiredService<IMainVM>()
                };
            });
            return window ?? throw new Exception("Не удалось инициализировать окно");
        };
    }

    private Func<IServiceProvider, ThreadManager> InitThreadManager()
    {
        return provider =>
        {
            var threadManager = new ThreadManager("ChangeWallHightInGroups");
            threadManager.UIDispatcher.UnhandledException += (dispatcher, handledHandler) =>
            {
                var ex = handledHandler.Exception;
                threadManager.UIDispatcher.InvokeShutdown();
                Console.WriteLine($"Fatal exception UIThread {ex}");
                threadManager.RevitDispatcher.Invoke(() =>
                {
                    //сообщение в потоке ревит
                    MessageBox.Show($"Фатальная необработанная ошибка в ходе выполнения UI-потока программы {WindowTitle}\n(╯°□°）╯︵ ┻━┻\nПлагин будет закрыт, но ревит продолжит работу в штатном режиме.\n{ex}", $"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                SynchronizationContext.SetSynchronizationContext(null);
                _serviceProvider?.Dispose();
                handledHandler.Handled = true;
            };
            return threadManager;
        };
    }

    private (bool flowControl, Result value) CheckWindowIsAlreadyOpened()
    {
        try
        {
            IntPtr pluginWindow = GetHandleWindow(WindowTitle);
            if (pluginWindow != IntPtr.Zero)
            {
                HwndSource hwndSource = HwndSource.FromHwnd(pluginWindow);
                if (hwndSource is not null && hwndSource.RootVisual is Window wind)
                {
                    wind!.Dispatcher.InvokeAsync(wind.Activate);
                }
                return (flowControl: false, value: Result.Succeeded);
            }
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Ошибка проверки повторно открытого окна.", ex.Message);
            return (flowControl: false, value: Result.Failed);
        }

        return (flowControl: true, value: default);
    }
}
