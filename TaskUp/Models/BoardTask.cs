using System.ComponentModel.DataAnnotations;

namespace TaskUp.Models;

public class BoardTask
{
    public int Id { get; set; }
    
    [Required]
    public string Title { get; set; } = null!;
    public string? Content { get; set; }
    public bool IsDone { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int BoardId { get; set; }
    public virtual Board Board { get; set; } = null!;

    public string? AssignedUserId { get; set; }
    public virtual AppUser? AssignedUser { get; set; }
}