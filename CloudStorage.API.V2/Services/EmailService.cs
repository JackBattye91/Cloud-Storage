using CloudStorage.API.V2.Models;

namespace CloudStorage.API.V2.Services
{
    public interface IEmailService
    {
        Task SendAccountActivationKeyAsync(User user, string activationKey);
    }

    public class EmailService : IEmailService
    {
        public readonly JB.Email.IWrapper _emailWrapper;

        public EmailService(JB.Email.IWrapper emailWrapper)
        {
            _emailWrapper = emailWrapper;
        }

        public async Task SendAccountActivationKeyAsync(User user, string activationKey)
        {
            JB.Email.Interfaces.IEmailHeader header = new EmailHeader() 
            { 
                Sender = "jack.battye@hotmail.co.uk",
                To = new List<string>()
                {
                    user.Email
                },
                Subject = "Cloud Storage Account Activation"
            };
            JB.Email.Interfaces.IEmailBody body = new EmailBody()
            {
                IsHtml = true,
                Content = string.Format(Consts.EmailTemplates.AccountActivation, activationKey)
            };

            var sendEmailRc = await _emailWrapper.Send(header, body);

            if (sendEmailRc.Failed)
            {
                
            }
        }
    }
}
