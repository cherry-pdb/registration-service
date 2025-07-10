namespace Service.Models;

public class OAuthOptions
{
    public string RedirectUrl { get; set; }
    
    public string ResponseType { get; set; }
    
    public string State { get; set; }
    
    public string GrantType { get; set; }
    
    public List<Client> Clients { get; set; }
}