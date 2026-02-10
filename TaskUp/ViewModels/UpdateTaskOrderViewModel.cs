using System.ComponentModel.DataAnnotations;
using TaskUp.Models;

namespace TaskUp.ViewModels;

public class UpdateTaskOrderViewModel
{
    [Required]
    public int ColumnId { get; set; }
        
    public List<TaskOrder> TaskOrders { get; set; }
}