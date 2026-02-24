namespace Blockbuster.API.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string Fullname { get; set; } = string.Empty;
    
    public string Role { get; set; } = string.Empty;
    
    public string ProfilePictureUrl { get; set; } = string.Empty;
    
    public int TotalRatings { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime LastLogin { get; set; }
    
    public bool IsActive { get; set; } = true;
}