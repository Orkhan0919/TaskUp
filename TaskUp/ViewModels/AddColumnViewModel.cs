using System.ComponentModel.DataAnnotations;

namespace TaskUp.ViewModels;

public class AddColumnViewModel
{
    
    [Required]
    public int BoardId { get; set; }
        
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; set; }
}