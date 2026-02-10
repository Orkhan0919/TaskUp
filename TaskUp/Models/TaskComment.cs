using System.ComponentModel.DataAnnotations;

namespace TaskUp.Models;

public class TaskComment
{
    
    public int Id { get; set; }
        
    [Required]
    [StringLength(1000)]
    public string Content { get; set; }
        
    public int TaskId { get; set; }
    public BoardTask Task { get; set; }
        
    public string UserId { get; set; }
    public AppUser User { get; set; }
        
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}