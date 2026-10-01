using AiHelper.Data;
using AiHelper.Models;
using Microsoft.EntityFrameworkCore;

namespace AiHelper.Services;

public class UserService(AiHelperDbContext dbContext) : IUserService
{
    private readonly AiHelperDbContext _dbContext = dbContext;
    
    public async Task<List<User>> GetAllUsersAsync()
        => await _dbContext.Users.OrderBy(u => u.Username).ToListAsync();
   
    public async Task<User> CreateUserAsync(string username)
    {
        var cleanUsername = username.Trim();

        // 1. Обязательно добавляем await!
        var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == cleanUsername);

        // 2. Если пользователь уже есть, просто возвращаем его (элегантный фоллбек)
        if (existingUser is not null) 
            return existingUser;

        // 3. Если нет, создаем нового
        var user = new User { Username = username };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }
  
    public async Task<string?> GetLastModelAsync(Guid userId)
    {
        var pref = await _dbContext.UserPreferences.FindAsync(userId);
        return pref?.LastModelName;
    }

    public async Task SaveLastModelAsync(Guid userId, string modelName)
    {
        var pref = await _dbContext.UserPreferences.FindAsync(userId);
        if (pref == null)
        {
            pref = new UserPreference { UserId = userId, LastModelName = modelName };
            _dbContext.UserPreferences.Add(pref);
        }
        else
        {
            pref.LastModelName = modelName;
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task<string?> GetLastUsernameAsync()
    {
        var state = await _dbContext.AppStates.FindAsync("last_username");
        return state?.Value;
    }

    public async Task SetLastUsernameAsync(string username)
    {
        var state = await _dbContext.AppStates.FindAsync("last_username");
        if (state == null)
        {
            state = new AppState { Key = "last_username", Value = username };
            _dbContext.AppStates.Add(state);
        }
        else
        {
            state.Value = username;
        }
        await _dbContext.SaveChangesAsync();
    }
}
