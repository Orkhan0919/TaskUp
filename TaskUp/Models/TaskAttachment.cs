using System.ComponentModel.DataAnnotations;

namespace TaskUp.Models;

public class TaskAttachment
{
    
    public int Id { get; set; }
        
    [Required]
    [StringLength(255)]
    public string FileName { get; set; }
        
    [StringLength(50)]
    public string FileType { get; set; }
        
    public long FileSize { get; set; }
    public string FilePath { get; set; }
        
    public int TaskId { get; set; }
    public BoardTask Task { get; set; }
        
    public string UserId { get; set; }
    public AppUser User { get; set; }
        
    public DateTime UploadedAt { get; set; } = DateTime.Now;
}