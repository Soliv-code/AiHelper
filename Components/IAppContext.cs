namespace AiHelper.Components;

public interface IAppContext
{
    T GetService<T>() where T : notnull;
    Task ActivateComponentAsync<T>() where T : IComponent;
}

public interface IComponent
{
    Task RunAsync();
}