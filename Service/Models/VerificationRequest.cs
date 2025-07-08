namespace Service.Models;

public class VerificationRequest
{
    public Guid UserId { get; set; }
    
    public string DocumentType { get; set; }
    
    public string DocumentData { get; set; }
}