using Email_Automation.PrimaryUser;


namespace Email_Automation_Update.Supplemental.Engines
{
    internal sealed class AccountInformationEngine 
    {
        private IConfiguration config;


        private readonly User _user;
        private User PrimaryUser => _user;

        internal MailKit.Net.Smtp.SmtpClient UserSmtpClient { get; set; } 

        internal MimeMessage UserMimeMessage { get; set; } = new MimeMessage();

        private String MainUserEmail { get; init; } 
        private String MainUserDisplayName { get; init; } 
        private String EmailSubject { get; init; }
        internal String EmailMessage { get; set; } = "";

        private const String smtpHost = "smtp.gmail.com";
        private const int smtpPort = 587;

        private delegate void EngineDelegate();
        private event EngineDelegate UserAccountCreationMethods; // Assigned to methods listed in constructor

        //Primary Constructor
        public AccountInformationEngine(User user)
        {
            this._user = user;

            config = new ConfigurationBuilder()
                .AddUserSecrets<AccountInformationEngine>()
                .Build();

            MainUserEmail = config["User:email"];
            MainUserDisplayName = config["User:displayName"];
            EmailSubject = config["User:emailSubject"];

            UserAccountCreationMethods += MimeMessageInitialization;
        }

        //Methods
        internal void EventInvocation()
        {
            UserAccountCreationMethods.Invoke();
        }

        internal void ClientInitialization()
        {
            //This initializes the SMTP client prior to sending emails to prevent timeouts
            UserSmtpClient = new MailKit.Net.Smtp.SmtpClient();

            //User object clients are initialized, allowing communication with internet and email
            try
            {
                SaslMechanismOAuth2 oauth2 = new SaslMechanismOAuth2(MainUserEmail, (PrimaryUser.Authentication.AccessToken).Result);

                UserSmtpClient.Connect(smtpHost, 587, MailKit.Security.SecureSocketOptions.StartTls);
                UserSmtpClient.Authenticate(oauth2);    
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }
        }

        internal void MimeMessageInitialization()
        {
            UserMimeMessage.From.Add(new MailboxAddress(MainUserDisplayName, MainUserEmail));
            UserMimeMessage.Subject = EmailSubject;
            UserMimeMessage.Body = new TextPart("html")
            {
                Text = "" //Filled later
            };
        }
    }
}