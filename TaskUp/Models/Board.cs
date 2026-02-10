using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskUp.Models;

public class Board
{
    public int Id { get; set; }
        
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
        
    [StringLength(500)]
    public string Description { get; set; }
        
    [Required]
    [StringLength(6)]
    public string JoinCode { get; set; }
        
    public bool IsPrivate { get; set; }
    public string Password { get; set; }
        
    public string OwnerId { get; set; }
    public AppUser Owner { get; set; }
        
    public DateTime CreatedAt { get; set; } = DateTime.Now;
        
    public ICollection<BoardColumn> Columns { get; set; } = new List<BoardColumn>();
    public ICollection<BoardMember> Members { get; set; } = new List<BoardMember>();
}