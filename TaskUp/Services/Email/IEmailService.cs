namespace TaskUp.Services.Email;

public interface IEmailService
{
    Task SendConfirmationEmailAsync(string email, string token, string userName);
    Task SendPasswordResetEmailAsync(string email, string token, string userName);
    Task SendWelcomeEmailAsync(string email, string userName);
    
    Task SendInvitationEmailAsync(string email, string inviterName, string boardName, string joinCode);
    
    Task SendTaskAssignmentEmailAsync(string email, string assignerName, string taskTitle, string boardName);
    Task SendMentionNotificationEmailAsync(string email, string mentionerName, string taskTitle, string comment);
}