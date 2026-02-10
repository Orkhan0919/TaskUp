namespace TaskUp.Models;

public class BoardMember
{
    public int BoardId { get; set; }
    public Board Board { get; set; }
        
    public string UserId { get; set; }
    public AppUser User { get; set; }
        
    public DateTime JoinedAt { get; set; } = DateTime.Now;
        
    public string Role { get; set; } = "Member"; // Owner, Admin, Member
}