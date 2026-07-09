using DbManager.Common;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using DbManager.Core.Repositories;
using DbManager.Core.Services;
using DbManager.Wpf.ViewModels;
using MaterialDesignThemes.Wpf;
using MaterialDesignColors;
using System.IO;
using System.Windows;

namespace DbManager.Wpf;

public partial class App : Application
{
    public static DbConnectionManageService ConnectionService { get; private set; } = null!;
    public static IDbTreeNavigateService TreeService { get; private set; } = null!;
    public static IDbConnectionFactory ConnectionFactory { get; private set; } = null!;
    public static DbMetadataServiceFactory MetadataFactory { get; private set; } = null!;
    public static DbExecuteServiceFactory ExecuteFactory { get; private set; } = null!;
    public static SettingsService SettingsServiceInstance { get; private set; } = null!;
    public static AppSettingsModel CurrentSettings { get; set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            Directory.CreateDirectory(AppConst.AppDataDir);
            Directory.CreateDirectory(AppConst.SqlHistoryDir);
            Directory.CreateDirectory(AppConst.ScriptsDir);
            LogHelper.Initialize();

            SettingsServiceInstance = new SettingsService();
            CurrentSettings = SettingsServiceInstance.Settings;
            ApplyTheme(CurrentSettings.UseDarkTheme);

            var repository = new ConnRepository(AppConst.DbFilePath);
            await repository.InitializeDatabaseAsync();

            ConnectionService = new DbConnectionManageService(repository);
            ConnectionFactory = new DbConnectionFactory();
            MetadataFactory = new DbMetadataServiceFactory(ConnectionFactory);
            ExecuteFactory = new DbExecuteServiceFactory(ConnectionFactory);
            TreeService = new DbTreeNavigateService(ConnectionService, MetadataFactory);

            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(ConnectionService, TreeService)
            };
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "应用启动失败");
            MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 释放所有 SSH 隧道，避免残留 SSH 连接与转发端口
        try
        {
            SshTunnelManager.CloseAll();
        }
        catch
        {
            // 退出清理异常忽略
        }
        base.OnExit(e);
    }

    public static void ApplyTheme(bool isDark)
    {
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
        paletteHelper.SetTheme(theme);
    }
}