using agents_tools.controllers;
using agents_tools.views;
using Microsoft.Extensions.DependencyInjection;

namespace agents_tools;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var services = new ServiceCollection();
        ConfigureServices(services);

        using var provider = services.BuildServiceProvider();
        var mainForm = provider.GetRequiredService<Main>();
        Application.Run(mainForm);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<AssetCatalogService>();
        services.AddSingleton<InstallService>();
        services.AddSingleton<McpServerProcessService>();
        services.AddSingleton<MainCoordinator>();
        services.AddTransient<Main>();
    }
}
