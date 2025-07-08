namespace Service.Models;

public class User
{
    public Guid Id { get; set; }
    
    public string Name { get; set; }
    
    public bool IsVerified { get; set; }
}