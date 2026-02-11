using TaskUp.Utilities.Enums;

namespace TaskUp.ViewModels
{
    public class DashboardAccessVm
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool EnablePassword { get; set; }
        public string? DashboardPassword { get; set; }
        public string? AccessCode { get; set; }
        public string? Password { get; set; }
        public bool RequiresPassword { get; set; }
        public bool ShowDemoInfo { get; set; } = true;
        
        public BoardType BoardType { get; set; } = BoardType.Team; 
        
        public string? InviteCode { get; set; }
        
        public UserRole InvitedRole { get; set; } = UserRole.TeamMember;
    }
}