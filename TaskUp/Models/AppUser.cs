using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TaskUp.Models
{
    public class AppUser : IdentityUser
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
        public string Surname { get; set; } = null!;

        public string FullName => $"{Name} {Surname}";
        
        public string DisplayName { get; set; } = string.Empty; 
        public string AvatarUrl { get; set; } = string.Empty;   
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;  
        
        public ICollection<TaskAssignee> AssignedTasks { get; set; } = new List<TaskAssignee>();
        public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
        public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
        
        public virtual ICollection<Board> OwnedBoards { get; set; } = new List<Board>();
        public virtual ICollection<BoardMember> JoinedBoards { get; set; } = new List<BoardMember>();
    }
}