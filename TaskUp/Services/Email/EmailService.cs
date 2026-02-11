using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;
using TaskUp.Models.Settings;
using Microsoft.Extensions.Configuration; // IConfiguration için EKLENDİ

namespace TaskUp.Services.Email;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly IWebHostEnvironment _env;
    private readonly string _appUrl; // 🔥 AppUrl EKLENDİ

    public EmailService(
        IOptions<EmailSettings> emailSettings, 
        IWebHostEnvironment env,
        IConfiguration configuration) // 🔥 IConfiguration EKLENDİ
    {
        _emailSettings = emailSettings.Value;
        _env = env;
        _appUrl = configuration["AppUrl"] ?? "https://localhost:7237"; // 🔥 SENİN PORTUN!
        
        Console.WriteLine("========== EMAIL SERVICE BAŞLATILDI ==========");
        Console.WriteLine($"📧 SMTP: {_emailSettings.SmtpServer}");
        Console.WriteLine($"🔌 Port: {_emailSettings.Port}");
        Console.WriteLine($"📋 Username: {_emailSettings.Username}");
        Console.WriteLine($"📨 From: {_emailSettings.FromEmail}");
        Console.WriteLine($"🌐 AppUrl: {_appUrl}");
        Console.WriteLine("===============================================");
    }

    public async Task SendConfirmationEmailAsync(string email, string token, string userName)
    {
        var confirmationLink = $"{_appUrl}/Account/ConfirmEmail?email={email}&token={token}";

        var subject = "Welcome to TaskUp - Confirm Your Email";
        var body = GetConfirmationEmailTemplate(userName, confirmationLink);

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string email, string token, string userName)
    {
        // 🚨 WebUtility.UrlEncode KALDIRILDI
        var resetLink = $"{_appUrl}/Account/ResetPassword?email={email}&token={token}";

        var subject = "TaskUp - Password Reset Request";
        var body = GetPasswordResetEmailTemplate(userName, resetLink);

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string email, string userName)
    {
        var subject = "Welcome to TaskUp - Let's Get Started!";
        var body = GetWelcomeEmailTemplate(userName, _appUrl); // 🔥 _appUrl EKLENDİ

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendInvitationEmailAsync(string email, string inviterName, string boardName, string joinCode)
    {
        var subject = $"{inviterName} invited you to join {boardName} on TaskUp";
        var body = GetInvitationEmailTemplate(inviterName, boardName, joinCode, _appUrl); // 🔥 _appUrl EKLENDİ

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendTaskAssignmentEmailAsync(string email, string assignerName, string taskTitle, string boardName)
    {
        var subject = $"{assignerName} assigned you a task on TaskUp";
        var body = GetTaskAssignmentEmailTemplate(assignerName, taskTitle, boardName, _appUrl); // 🔥 _appUrl EKLENDİ

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendMentionNotificationEmailAsync(string email, string mentionerName, string taskTitle, string comment)
    {
        var subject = $"{mentionerName} mentioned you in a comment";
        var body = GetMentionNotificationEmailTemplate(mentionerName, taskTitle, comment, _appUrl); // 🔥 _appUrl EKLENDİ

        await SendEmailAsync(email, subject, body);
    }

    private async Task SendEmailAsync(string to, string subject, string body)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_emailSettings.FromEmail, "TaskUp"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
            Priority = MailPriority.High
        };
        
        message.To.Add(to);

        using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port)
        {
            Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
            EnableSsl = _emailSettings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000 
        };

        try
        {
            await client.SendMailAsync(message);
            Console.WriteLine($"✅ Email sent successfully to {to} - {subject}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Email sending failed: {ex.Message}");
            throw;
        }
    }

    private string GetConfirmationEmailTemplate(string userName, string confirmationLink)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <style>
                body {{ font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background-color: #f9fafb; margin: 0; padding: 0; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 40px 20px; }}
                .card {{ background-color: #ffffff; border-radius: 16px; padding: 40px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); }}
                .logo {{ width: 48px; height: 48px; background: linear-gradient(135deg, #2563eb, #1e40af); border-radius: 12px; display: flex; align-items: center; justify-content: center; margin-bottom: 24px; }}
                .logo i {{ color: white; font-size: 24px; }}
                h1 {{ color: #0f172a; font-size: 24px; font-weight: 600; margin-bottom: 16px; }}
                p {{ color: #475569; font-size: 16px; line-height: 1.6; margin-bottom: 24px; }}
                .button {{ display: inline-block; background: linear-gradient(135deg, #2563eb, #1e40af); color: white; text-decoration: none; padding: 14px 32px; border-radius: 12px; font-weight: 500; font-size: 16px; margin-bottom: 24px; }}
                .button:hover {{ background: linear-gradient(135deg, #1e40af, #1e3a8a); }}
                .divider {{ border-top: 1px solid #e2e8f0; margin: 32px 0; }}
                .footer {{ color: #64748b; font-size: 14px; text-align: center; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='card'>
                    <div class='logo'>
                        <svg width='24' height='24' viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'>
                            <path d='M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z' fill='white'/>
                        </svg>
                    </div>
                    <h1>Welcome to TaskUp, {userName}! 🎉</h1>
                    <p>Thanks for signing up! We're excited to help you manage your projects more efficiently. Please confirm your email address to get started.</p>
                    <a href='{confirmationLink}' class='button'>Confirm Email Address</a>
                    <p style='color: #64748b; font-size: 14px;'>Or copy this link: <br> <span style='color: #2563eb; word-break: break-all;'>{confirmationLink}</span></p>
                    <div class='divider'></div>
                    <div class='footer'>
                        <p style='margin-bottom: 8px;'>This link will expire in 24 hours.</p>
                        <p style='margin-bottom: 8px;'>If you didn't create an account with TaskUp, please ignore this email.</p>
                        <p style='margin-top: 24px;'>&copy; 2024 TaskUp. All rights reserved.</p>
                    </div>
                </div>
            </div>
        </body>
        </html>";
    }

    private string GetPasswordResetEmailTemplate(string userName, string resetLink)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <style>
                body {{ font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background-color: #f9fafb; margin: 0; padding: 0; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 40px 20px; }}
                .card {{ background-color: #ffffff; border-radius: 16px; padding: 40px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); }}
                .logo {{ width: 48px; height: 48px; background: linear-gradient(135deg, #2563eb, #1e40af); border-radius: 12px; display: flex; align-items: center; justify-content: center; margin-bottom: 24px; }}
                h1 {{ color: #0f172a; font-size: 24px; font-weight: 600; margin-bottom: 16px; }}
                p {{ color: #475569; font-size: 16px; line-height: 1.6; margin-bottom: 24px; }}
                .button {{ display: inline-block; background: linear-gradient(135deg, #2563eb, #1e40af); color: white; text-decoration: none; padding: 14px 32px; border-radius: 12px; font-weight: 500; font-size: 16px; margin-bottom: 24px; }}
                .warning {{ background-color: #fff7ed; border-left: 4px solid #f97316; padding: 16px; border-radius: 8px; margin-bottom: 24px; }}
                .footer {{ color: #64748b; font-size: 14px; text-align: center; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='card'>
                    <div class='logo'>
                        <svg width='24' height='24' viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'>
                            <path d='M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c1.66 0 3 1.34 3 3s-1.34 3-3 3-3-1.34-3-3 1.34-3 3-3zm0 14.2c-2.5 0-4.71-1.28-6-3.22.03-1.99 4-3.08 6-3.08 1.99 0 5.97 1.09 6 3.08-1.29 1.94-3.5 3.22-6 3.22z' fill='white'/>
                        </svg>
                    </div>
                    <h1>Reset Your Password, {userName}</h1>
                    <p>We received a request to reset your TaskUp account password. Click the button below to create a new password.</p>
                    <div class='warning'>
                        <p style='margin: 0; color: #9a3412; font-weight: 500;'>⚠️ This password reset link will expire in 1 hour.</p>
                    </div>
                    <a href='{resetLink}' class='button'>Reset Password</a>
                    <p style='color: #64748b; font-size: 14px;'>If you didn't request this, you can safely ignore this email. Your password won't be changed.</p>
                    <div class='divider'></div>
                    <div class='footer'>
                        <p>&copy; 2024 TaskUp. All rights reserved.</p>
                    </div>
                </div>
            </div>
        </body>
        </html>";
    }

    private string GetWelcomeEmailTemplate(string userName, string appUrl) // 🔥 appUrl parametresi EKLENDİ
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <style>
                body {{ font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background-color: #f9fafb; margin: 0; padding: 0; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 40px 20px; }}
                .card {{ background-color: #ffffff; border-radius: 16px; padding: 40px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); }}
                .logo {{ width: 48px; height: 48px; background: linear-gradient(135deg, #2563eb, #1e40af); border-radius: 12px; display: flex; align-items: center; justify-content: center; margin-bottom: 24px; }}
                h1 {{ color: #0f172a; font-size: 24px; font-weight: 600; margin-bottom: 16px; }}
                p {{ color: #475569; font-size: 16px; line-height: 1.6; margin-bottom: 24px; }}
                .features {{ display: flex; gap: 16px; margin-bottom: 32px; }}
                .feature {{ flex: 1; text-align: center; }}
                .feature-icon {{ width: 48px; height: 48px; background: #eff6ff; border-radius: 12px; display: flex; align-items: center; justify-content: center; margin: 0 auto 12px; color: #2563eb; font-size: 24px; font-weight: bold; }}
                .button {{ display: inline-block; background: linear-gradient(135deg, #2563eb, #1e40af); color: white; text-decoration: none; padding: 14px 32px; border-radius: 12px; font-weight: 500; font-size: 16px; }}
                .footer {{ color: #64748b; font-size: 14px; text-align: center; margin-top: 32px; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='card'>
                    <div class='logo'>
                        <svg width='24' height='24' viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'>
                            <path d='M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z' fill='white'/>
                        </svg>
                    </div>
                    <h1>Welcome to TaskUp, {userName}! 🚀</h1>
                    <p>Your account is now confirmed. You're ready to start managing your projects like a pro.</p>
                    
                    <div style='margin-bottom: 32px;'>
                        <p style='font-weight: 600; color: #0f172a;'>Here's what you can do with TaskUp:</p>
                        <ul style='color: #475569; line-height: 1.8; padding-left: 20px;'>
                            <li>✅ Create unlimited boards for different projects</li>
                            <li>👥 Invite team members and collaborate in real-time</li>
                            <li>📊 Track progress with Kanban boards</li>
                            <li>🔔 Get notifications and mentions</li>
                            <li>📎 Attach files and leave comments</li>
                        </ul>
                    </div>
                    
                    <a href='{appUrl}/Dashboard' class='button'>Go to Your Dashboard →</a>
                    
                    <div class='divider'></div>
                    
                    <div class='footer'>
                        <p>Need help? Check out our <a href='#' style='color: #2563eb; text-decoration: none;'>Help Center</a> or <a href='#' style='color: #2563eb; text-decoration: none;'>contact support</a>.</p>
                        <p style='margin-top: 24px;'>&copy; 2024 TaskUp. All rights reserved.</p>
                    </div>
                </div>
            </div>
        </body>
        </html>";
    }

    private string GetInvitationEmailTemplate(string inviterName, string boardName, string joinCode, string appUrl) // 🔥 appUrl parametresi EKLENDİ
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <style>
                body {{ font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background-color: #f9fafb; margin: 0; padding: 0; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 40px 20px; }}
                .card {{ background-color: #ffffff; border-radius: 16px; padding: 40px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); }}
                .logo {{ width: 48px; height: 48px; background: linear-gradient(135deg, #2563eb, #1e40af); border-radius: 12px; display: flex; align-items: center; justify-content: center; margin-bottom: 24px; }}
                h1 {{ color: #0f172a; font-size: 24px; font-weight: 600; margin-bottom: 16px; }}
                p {{ color: #475569; font-size: 16px; line-height: 1.6; margin-bottom: 24px; }}
                .invite-card {{ background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 24px; margin-bottom: 24px; }}
                .code {{ background: #1e293b; color: white; font-family: monospace; font-size: 24px; font-weight: 600; padding: 12px 24px; border-radius: 8px; letter-spacing: 4px; display: inline-block; margin: 16px 0; }}
                .button {{ display: inline-block; background: linear-gradient(135deg, #2563eb, #1e40af); color: white; text-decoration: none; padding: 14px 32px; border-radius: 12px; font-weight: 500; font-size: 16px; }}
                .footer {{ color: #64748b; font-size: 14px; text-align: center; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='card'>
                    <div class='logo'>
                        <svg width='24' height='24' viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'>
                            <path d='M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-1 .05 1.16.84 2 1.87 2 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z' fill='white'/>
                        </svg>
                    </div>
                    
                    <h1>You're Invited! 🎯</h1>
                    
                    <div class='invite-card'>
                        <p style='margin-bottom: 16px; font-size: 18px;'>
                            <strong>{inviterName}</strong> invited you to join 
                            <strong style='color: #2563eb;'>{boardName}</strong> on TaskUp
                        </p>
                        
                        <p style='margin-bottom: 8px;'>Use this access code to join:</p>
                        <div class='code'>{joinCode}</div>
                        
                        <p style='margin-top: 24px; margin-bottom: 8px;'>Or click the button below:</p>
                        <a href='{appUrl}/Dashboard/Access' class='button'>Accept Invitation →</a>
                    </div>
                    
                    <div style='background: #fff7ed; border-radius: 8px; padding: 16px; margin-top: 24px;'>
                        <p style='margin: 0; color: #9a3412; font-size: 14px;'>
                            <strong>💡 Tip:</strong> Already have a TaskUp account? Just log in and enter the code above to join the board.
                        </p>
                    </div>
                    
                    <div class='divider'></div>
                    
                    <div class='footer'>
                        <p>This invitation was sent by {inviterName} via TaskUp.</p>
                        <p style='margin-top: 24px;'>&copy; 2024 TaskUp. All rights reserved.</p>
                    </div>
                </div>
            </div>
        </body>
        </html>";
    }

    private string GetTaskAssignmentEmailTemplate(string assignerName, string taskTitle, string boardName, string appUrl) // 🔥 appUrl parametresi EKLENDİ
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <style>
                body {{ font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background-color: #f9fafb; margin: 0; padding: 0; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 40px 20px; }}
                .card {{ background-color: #ffffff; border-radius: 16px; padding: 40px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); }}
                .logo {{ width: 48px; height: 48px; background: linear-gradient(135deg, #2563eb, #1e40af); border-radius: 12px; display: flex; align-items: center; justify-content: center; margin-bottom: 24px; }}
                h1 {{ color: #0f172a; font-size: 24px; font-weight: 600; margin-bottom: 16px; }}
                p {{ color: #475569; font-size: 16px; line-height: 1.6; margin-bottom: 24px; }}
                .task-card {{ background: #f8fafc; border-left: 4px solid #2563eb; border-radius: 8px; padding: 20px; margin-bottom: 24px; }}
                .task-title {{ color: #0f172a; font-size: 18px; font-weight: 600; margin-bottom: 8px; }}
                .board-name {{ color: #2563eb; font-weight: 500; }}
                .button {{ display: inline-block; background: linear-gradient(135deg, #2563eb, #1e40af); color: white; text-decoration: none; padding: 14px 32px; border-radius: 12px; font-weight: 500; font-size: 16px; }}
                .footer {{ color: #64748b; font-size: 14px; text-align: center; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='card'>
                    <div class='logo'>
                        <svg width='24' height='24' viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'>
                            <path d='M20 6h-4V4c0-1.1-.9-2-2-2h-4c-1.1 0-2 .9-2 2v2H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zM10 4h4v2h-4V4zm10 16H4V10h16v10zm-2-4H8v-2h10v2z' fill='white'/>
                        </svg>
                    </div>
                    
                    <h1>New Task Assignment 📋</h1>
                    
                    <p><strong>{assignerName}</strong> assigned you a task in <span class='board-name'>{boardName}</span></p>
                    
                    <div class='task-card'>
                        <div class='task-title'>{taskTitle}</div>
                        <p style='margin: 8px 0 0 0; color: #64748b;'>Click the button below to view the task details</p>
                    </div>
                    
                    <a href='{appUrl}/Board/Index' class='button'>View Task →</a>
                    
                    <div class='divider'></div>
                    
                    <div class='footer'>
                        <p>&copy; 2024 TaskUp. All rights reserved.</p>
                    </div>
                </div>
            </div>
        </body>
        </html>";
    }

    private string GetMentionNotificationEmailTemplate(string mentionerName, string taskTitle, string comment, string appUrl) // 🔥 appUrl parametresi EKLENDİ
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <style>
                body {{ font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background-color: #f9fafb; margin: 0; padding: 0; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 40px 20px; }}
                .card {{ background-color: #ffffff; border-radius: 16px; padding: 40px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); }}
                .logo {{ width: 48px; height: 48px; background: linear-gradient(135deg, #2563eb, #1e40af); border-radius: 12px; display: flex; align-items: center; justify-content: center; margin-bottom: 24px; }}
                h1 {{ color: #0f172a; font-size: 24px; font-weight: 600; margin-bottom: 16px; }}
                p {{ color: #475569; font-size: 16px; line-height: 1.6; margin-bottom: 24px; }}
                .mention {{ background: #f8fafc; border-radius: 12px; padding: 24px; margin-bottom: 24px; }}
                .comment {{ background: #f1f5f9; border-radius: 8px; padding: 16px; font-style: italic; color: #334155; margin-top: 12px; }}
                .button {{ display: inline-block; background: linear-gradient(135deg, #2563eb, #1e40af); color: white; text-decoration: none; padding: 14px 32px; border-radius: 12px; font-weight: 500; font-size: 16px; }}
                .footer {{ color: #64748b; font-size: 14px; text-align: center; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='card'>
                    <div class='logo'>
                        <svg width='24' height='24' viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'>
                            <path d='M20 2H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h14l4 4V4c0-1.1-.9-2-2-2zm0 14H5.17L4 17.17V4h16v12zM7 9h10v2H7zm0 4h8v2H7z' fill='white'/>
                        </svg>
                    </div>
                    
                    <h1>🔔 You were mentioned</h1>
                    
                    <div class='mention'>
                        <p style='margin-bottom: 8px;'><strong>{mentionerName}</strong> mentioned you in a comment on:</p>
                        <p style='font-weight: 600; color: #0f172a; margin-bottom: 12px;'>{taskTitle}</p>
                        
                        <div class='comment'>
                            ""{comment}""
                        </div>
                    </div>
                    
                    <a href='{appUrl}/Board/Index' class='button'>View Conversation →</a>
                    
                    <div class='divider'></div>
                    
                    <div class='footer'>
                        <p>&copy; 2024 TaskUp. All rights reserved.</p>
                    </div>
                </div>
            </div>
        </body>
        </html>";
    }
}