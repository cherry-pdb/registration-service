using Service.Models;

namespace Service.Interfaces;

public interface IUserRepository
{
    Task AddUserAsync(User user);

    Task<User?> FindUserAsync(string id);

    Task UpdateUserVerifyingAsync(string id);
}