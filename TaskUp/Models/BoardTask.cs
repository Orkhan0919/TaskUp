using System.ComponentModel.DataAnnotations;
using TaskUp.Utilities.Enums;

namespace TaskUp.Models;

public class BoardTask
{
    public int Id { get; set; }
        
    [Required]
    [StringLength(200)]
    public string Title { get; set; }
        
    [StringLength(1000)]
    public string Description { get; set; }
        
    [StringLength(20)]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium; 
        
    public DateTime? DueDate { get; set; }
    public int Order { get; set; }
    public bool IsCompleted { get; set; }
        
    public int ColumnId { get; set; }
    public BoardColumn Column { get; set; }
        
    public DateTime CreatedAt { get; set; } = DateTime.Now;
        
    // Navigation Properties
    public ICollection<TaskAssignee> Assignees { get; set; } = new List<TaskAssignee>();
    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
}
