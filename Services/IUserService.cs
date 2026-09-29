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
}