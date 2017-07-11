using System.Collections.Generic;
using System.Net.Mail;

namespace Application.Network
{
    public static class Email
    {
        public static void SendEmail(string to, string from, string subject, string body, string smtpClient, bool useSSL = false,
            IEnumerable<Attachment> attachments = null, string userName = null, string password = null)
        {
            var client = new SmtpClient(smtpClient);
            client.UseDefaultCredentials = userName == null;
            client.EnableSsl = useSSL;
            if (userName != null)
                client.Credentials = new System.Net.NetworkCredential(userName, password);
            var message = new MailMessage(from, to);
            message.Subject = subject;
            message.Body = body;
            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    message.Attachments.Add(attachment);
                }
            }

            client.Send(message);
        }
    }
}