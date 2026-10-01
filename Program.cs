using AiHelper.Components;
using AiHelper.Data;
using AiHelper.Services;
using AiHelper.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AppContext = AiHelper.Components.AppContext;

namespace AiHelper;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Устанавливаем кодировку UTF8 для отображения emoji в консоли
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        try
        {
            // 1. Читаем конфигурацию
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // 2. Настраиваем DI-контейнер
            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(configuration);

            services.AddDbContext<AiHelperDbContext>(options =>
                options.UseNpgsql(connectionString, o => o.UseVector()));

            services.AddSingleton<OllamaService>();
            services.AddSingleton<UserService>();
            services.AddSingleton<ChatHistoryService>();

            services.AddSingleton<IAppContext, AppContext>();
            services.AddTransient<MainMenuPage>();

            var serviceProvider = services.BuildServiceProvider();

            // 3. 🛑 СТРОГАЯ ПРОВЕРКА ИНФРАСТРУКТУРЫ (Fail Fast)
            var dbContext = serviceProvider.GetRequiredService<AiHelperDbContext>();
            await InfrastructureValidator.ValidateAsync(dbContext);

            // 4. Запускаем главный компонент приложения
            var appContext = serviceProvider.GetRequiredService<IAppContext>();
            await appContext.ActivateComponentAsync<MainMenuPage>();
        }
        catch (Exception ex)
        {
            ConsoleUi.ShowError(ex.Message);
            Console.ReadLine();
        }
    }
}