using System.ComponentModel.DataAnnotations;

namespace TaskUp.Models;

public class Column
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!; 

    public int Order { get; set; } 

    public int BoardId { get; set; }
    public virtual Board Board { get; set; } = null!;

    public virtual ICollection<BoardTask> Tasks { get; set; } = new List<BoardTask>();
}