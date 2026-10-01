using Microsoft.Extensions.DependencyInjection;

namespace AiHelper.Components;

public class AppContext : IAppContext
{
    private readonly IServiceProvider _serviceProvider;

    public AppContext(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public T GetService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();

    public async Task ActivateComponentAsync<T>() where T : IComponent
    {
        var component = _serviceProvider.GetRequiredService<T>();
        await component.RunAsync();
    }
}