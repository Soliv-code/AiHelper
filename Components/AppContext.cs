using Microsoft.Extensions.DependencyInjection;

namespace AiHelper.Components;

public class AppContext(IServiceProvider _serviceProvider) : IAppContext
{
    public T GetService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();

    public async Task ActivateComponentAsync<T>() where T : IComponent
    {
        var component = _serviceProvider.GetRequiredService<T>();
        await component.RunAsync();
    }
}