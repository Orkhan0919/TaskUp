using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskUp.Models;

public class Board
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    
    [Required]
    public string JoinCode { get; set; } = null!; // 6 haneli eşsiz kod
    
    public bool IsPrivate { get; set; } // Şifreli mi?
    public string? Password { get; set; } // Dashboard şifresi
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Panoyu kuran kişi
    public string OwnerId { get; set; } = null!;
    [ForeignKey("OwnerId")]
    public virtual AppUser Owner { get; set; } = null!;

    // Panodaki görevler
    public virtual ICollection<BoardTask> Tasks { get; set; } = new List<BoardTask>();

    // Panoya kodla katılan misafirler (Ara tablo üzerinden)
    public virtual ICollection<BoardMember> Members { get; set; } = new List<BoardMember>();
}