//using MailKit.Net.Smtp;
//using MimeKit;
//using MimeKit.Text;
//using JwtApi.DTOs;
//using MailKit.Security;

//namespace JwtApi.Services
//{
//    public class EmailService : IEmailService
//    {
//        public async Task SendEmail(string toEmail, string name, string subjectLine, string messageContent)
//        {
//            var message = new MimeMessage();
//            message.From.Add(new MailboxAddress("Joakim", "theonlyjoakim@gmail.com"));
//            message.To.Add(new MailboxAddress(name, toEmail));
//            message.Subject = subjectLine;

//            message.Body = new TextPart("plain")
//            {
//                Text = messageContent
//            };

//            using (var client = new SmtpClient())
//            {
//                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
//                await client.AuthenticateAsync("theonlyjoakim@gmail.com", ""); // rmuc gfpg orpi uguv

//                await client.SendAsync(message);
//                await client.DisconnectAsync(true);
//            }
//        }

//        public async Task SendConfirmationEmail(string toEmail, string name, string confirmToken)
//        {
//            string subjectLine = "Confirm your account registration for JDs Universe.";
//            string url = "https://localhost:7146/confirm-email?email=" + toEmail + "&confirmToken=" + confirmToken;
//            string messageContent = $"Dear User,\n\nClick here to confirm your email:\n\n{url}\n\nLove, Joakim";
//            await SendEmail(toEmail, name, subjectLine, messageContent);
//        }

//        public async Task ContactFormSubmission(ContactFormDTO formData)
//        {
//            var messageContent = $"Name: {formData.Name}\nEmail: {formData.Email}\nMessage: {formData.Message}";
//            await SendEmail("theonlyjoakim@gmail.com", "Joakim's Universe", "Contact form submission", messageContent);
//        }
//    }
//}
