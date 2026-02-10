namespace TaskUp.Models;

public class BoardMember
{
    public int Id { get; set; }
    
    public int BoardId { get; set; }
    public virtual Board Board { get; set; } = null!;

    public string UserId { get; set; } = null!;
    public virtual AppUser User { get; set; } = null!;

    public DateTime JoinedAt { get; set; } = DateTime.Now;
}