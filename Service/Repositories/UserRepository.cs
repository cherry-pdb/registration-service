using Npgsql;
using Service.Interfaces;
using Service.Models;

namespace Service.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string? _connectionString;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(IConfiguration configuration, ILogger<UserRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _logger = logger;
    }
    
    public async Task AddUserAsync(User user)
    {
        const string query = "INSERT INTO users (id, name, password, is_verified) VALUES (@id, @name, @password, @is_verified)";
        
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        
        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", user.Id);
        command.Parameters.AddWithValue("name", user.Name);
        command.Parameters.AddWithValue("password", user.Password);
        command.Parameters.AddWithValue("is_verified", user.IsVerified);
        
        await command.ExecuteNonQueryAsync();
    }

    public async Task<User?> FindUserAsync(string id)
    {
        User? user = null;
        const string query = "SELECT * FROM users WHERE id = @id";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            user = new User
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Password = reader.GetString(2),
                IsVerified = reader.GetBoolean(3)
            };
        }
        
        return user;
    }

    public async Task UpdateUserVerifyingAsync(string id)
    {
        const string query = "UPDATE users SET is_verified = TRUE WHERE id = @id";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", id);

        await command.ExecuteNonQueryAsync();
    }
}
