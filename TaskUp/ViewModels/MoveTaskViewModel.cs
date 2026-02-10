using System.ComponentModel.DataAnnotations;

namespace TaskUp.ViewModels;

public class MoveTaskViewModel
{
        [Required]
        public int TaskId { get; set; }
        
        [Required]
        public int ColumnId { get; set; }
        
        public int BoardId { get; set; } // Optional, for verification
    }