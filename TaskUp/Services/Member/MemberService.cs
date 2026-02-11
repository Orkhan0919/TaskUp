// Services/Member/MemberService.cs
using Microsoft.EntityFrameworkCore;
using TaskUp.Data;
using TaskUp.Models;  // ✅ TEK SATIR!
// using TaskUp.Models;  // ❌ SİL!

namespace TaskUp.Services.Member;

public class MemberService : IMemberService
{
    private readonly AppDbContext _context;
    private readonly ILogger<MemberService> _logger;

    public MemberService(AppDbContext context, ILogger<MemberService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> IsMemberAsync(int boardId, string userId)
    {
        return await _context.BoardMembers
            .AnyAsync(m => m.BoardId == boardId && m.UserId == userId);
    }

    public async Task<bool> IsOwnerAsync(int boardId, string userId)
    {
        var board = await _context.Boards
            .Select(b => new { b.Id, b.OwnerId })
            .FirstOrDefaultAsync(b => b.Id == boardId);
            
        return board?.OwnerId == userId;
    }

    public async Task<bool> IsBannedAsync(int boardId, string userId)
    {
        return await _context.BannedUsers
            .AnyAsync(b => b.BoardId == boardId && b.UserId == userId);
    }

    public async Task<BoardMember> AddMemberAsync(int boardId, string userId, string role)
    {
        var member = new BoardMember
        {
            BoardId = boardId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };

        _context.BoardMembers.Add(member);
        await _context.SaveChangesAsync();
        
        return member;
    }

    public async Task RemoveMemberAsync(int boardId, string userId)
    {
        var member = await _context.BoardMembers
            .FirstOrDefaultAsync(m => m.BoardId == boardId && m.UserId == userId);
            
        if (member != null)
        {
            _context.BoardMembers.Remove(member);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<BoardMember>> GetMembersAsync(int boardId)
    {
        return await _context.BoardMembers
            .Include(m => m.User)
            .Where(m => m.BoardId == boardId)
            .ToListAsync();
    }

    public async Task<BoardMember> GetMemberAsync(int boardId, string userId)
    {
        return await _context.BoardMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.BoardId == boardId && m.UserId == userId);
    }

    public async Task UpdateMemberRoleAsync(int boardId, string userId, string role)
    {
        var member = await _context.BoardMembers
            .FirstOrDefaultAsync(m => m.BoardId == boardId && m.UserId == userId);
            
        if (member != null)
        {
            member.Role = role;
            await _context.SaveChangesAsync();
        }
    }

    // ============= KICK/BAN İŞLEMLERİ =============
    public async Task KickMemberAsync(int boardId, string userId, string kickedBy)
    {
        await RemoveMemberAsync(boardId, userId);
        
        _logger.LogInformation("User {UserId} kicked from board {BoardId} by {KickedBy}", 
            userId, boardId, kickedBy);
    }

    public async Task BanMemberAsync(int boardId, string userId, string bannedBy)
    {
        await RemoveMemberAsync(boardId, userId);
        
        var bannedUser = new BannedUser
        {
            BoardId = boardId,
            UserId = userId,
            BannedAt = DateTime.UtcNow,
            BannedBy = bannedBy
        };

        _context.BannedUsers.Add(bannedUser);
        await _context.SaveChangesAsync();
        
        _logger.LogWarning("User {UserId} BANNED from board {BoardId} by {BannedBy}", 
            userId, boardId, bannedBy);
    }

    public async Task UnbanMemberAsync(int boardId, string userId)
    {
        var bannedUser = await _context.BannedUsers
            .FirstOrDefaultAsync(b => b.BoardId == boardId && b.UserId == userId);
            
        if (bannedUser != null)
        {
            _context.BannedUsers.Remove(bannedUser);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("User {UserId} unbanned from board {BoardId}", 
                userId, boardId);
        }
    }

    public async Task<List<BannedUser>> GetBannedUsersAsync(int boardId)
    {
        return await _context.BannedUsers
            .Include(b => b.User)
            .Include(b => b.BannedByUser)
            .Where(b => b.BoardId == boardId)
            .ToListAsync();
    }
}