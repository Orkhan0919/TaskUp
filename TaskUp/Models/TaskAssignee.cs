namespace TaskUp.Models;

public class TaskAssignee
{
    
    public int TaskId { get; set; }
    public BoardTask Task { get; set; }
        
    public string UserId { get; set; }
    public AppUser User { get; set; }
        
    public DateTime AssignedAt { get; set; } = DateTime.Now;
}