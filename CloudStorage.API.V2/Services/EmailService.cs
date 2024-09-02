using CloudStorage.API.V2.Models;
using Microsoft.Extensions.Options;

namespace CloudStorage.API.V2.Services
{
    public interface IEmailService
    {
        Task SendAccountActivationKeyAsync(User user, string activationKey);
    }

    public class EmailService : IEmailService
    {
        private readonly JB.Email.IWrapper _emailWrapper;
        private readonly AppSettings _appSettings;

        public EmailService(JB.Email.IWrapper emailWrapper, IOptions<AppSettings> appSettings)
        {
            _emailWrapper = emailWrapper;
            _appSettings = appSettings.Value;
        }

        public async Task SendAccountActivationKeyAsync(User user, string activationKey)
        {
            JB.Email.Interfaces.IEmailHeader header = new EmailHeader() 
            { 
                Sender = _appSettings.Email.Sender,
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
