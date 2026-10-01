using AiHelper.Models;

namespace AiHelper.Services;

public interface IUserService
{
    /// <summary>
    /// Получаем всех пользователей из БД
    /// </summary>
    /// <returns></returns>
    Task<List<User>> GetAllUsersAsync();

    /// <summary>
    /// Создаём пользователя (по имени ха-ха)
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    Task<User> CreateUserAsync(string username);

    /// <summary>
    /// Получаем последнюю используемую модель
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    /// 
    Task<string?> GetLastModelAsync(Guid userId);

    /// <summary>
    /// Сохраняем выбранную модель в БД
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="modelName"></param>
    /// <returns></returns>
    Task SaveLastModelAsync(Guid userId, string modelName);
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<string?> GetLastUsernameAsync();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    Task SetLastUsernameAsync(string username);
}