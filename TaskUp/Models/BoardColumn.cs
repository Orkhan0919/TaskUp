using System.ComponentModel.DataAnnotations;

namespace TaskUp.Models;

public class BoardColumn
{
    
    public int Id { get; set; }
        
    [Required]
    [StringLength(50)]
    public string Name { get; set; }
        
    public int Order { get; set; }
        
    public int BoardId { get; set; }
    public Board Board { get; set; }
        
    
    public ICollection<BoardTask> Tasks { get; set; } = new List<BoardTask>();
}