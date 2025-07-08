using Microsoft.EntityFrameworkCore;
using Service.Models;

namespace Service.DbContext;

public class UserDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
}