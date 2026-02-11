// Models/BannedUser.cs
using System.ComponentModel.DataAnnotations;

namespace TaskUp.Models;

public class BannedUser
{
    public int Id { get; set; }
    
    [Required]
    public int BoardId { get; set; }
    public Board Board { get; set; }
    
    [Required]
    public string UserId { get; set; }
    public AppUser User { get; set; }
    
    public DateTime BannedAt { get; set; }
    public string BannedBy { get; set; }
    public AppUser BannedByUser { get; set; }
}