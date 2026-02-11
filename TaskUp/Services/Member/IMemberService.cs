using TaskUp.Models;

namespace TaskUp.Services.Member;

public interface IMemberService
{
    Task<bool> IsMemberAsync(int boardId, string userId);
    Task<bool> IsOwnerAsync(int boardId, string userId);
    Task<bool> IsBannedAsync(int boardId, string userId);
    Task<BoardMember> AddMemberAsync(int boardId, string userId, string role);
    Task RemoveMemberAsync(int boardId, string userId);
    Task<List<BoardMember>> GetMembersAsync(int boardId);
    Task<BoardMember> GetMemberAsync(int boardId, string userId);
    Task UpdateMemberRoleAsync(int boardId, string userId, string role);
    
    Task KickMemberAsync(int boardId, string userId, string kickedBy);
    Task BanMemberAsync(int boardId, string userId, string bannedBy);
    Task UnbanMemberAsync(int boardId, string userId);
    Task<List<BannedUser>> GetBannedUsersAsync(int boardId);
}