using System.ComponentModel.DataAnnotations;
using TaskUp.Utilities.Enums;

namespace TaskUp.ViewModels;

public class AddTaskViewModel
{
    
    [Required]
    public int ColumnId { get; set; }
        
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; }
        
    [StringLength(1000)]
    public string Description { get; set; }
        
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;        
    public DateTime? DueDate { get; set; }
}