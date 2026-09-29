using AiHelper.Data;
using AiHelper.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

namespace AiHelper.Services;

public class UserService(AiHelperDbContext dbContext) : IUserService
{
    private readonly AiHelperDbContext _dbContext = dbContext;
    public async Task<List<User>> GetAllUsersAsync()
        => await _dbContext.Users.OrderBy(u => u.Username).ToListAsync();
    public async Task<User?> CreateUserAsync(string username)
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
}
