using Email_Automation.PrimaryUser;


namespace Email_Automation_Update.Supplemental.Engines
{
    internal sealed class AccountInformationEngine 
    {
        public IConfiguration config;

        public SmtpClient UserSmtpClient { get; set; } = new SmtpClient();
        public HttpClient UserHttpClient { get; set; }

        public MimeMessage UserMimeMessage { get; set; } = new MimeMessage();

        public String MainUserEmail { get; init; } 
        public String MainUserDisplayName { get; init; } 
        public String EmailSubject { get; init; }

        public String EmailMessage { get; set; } = "";

        private const String smtpHost = "smtp.gmail.com";
        private const int smtpPort = 587;

        private delegate void EngineDelegate();
        private event EngineDelegate UserAccountCreationMethods; // Assigned to 3 methods listed in constructor

        //Primary Constructor
        public AccountInformationEngine()
        {
            config = new ConfigurationBuilder()
                .AddUserSecrets<AccountInformationEngine>()
                .Build();

            MainUserEmail = config["User:email"];
            MainUserDisplayName = config["User:displayName"];
            EmailSubject = config["User:emailSubject"];

            UserAccountCreationMethods += MimeMessageInitialization;
        }

        //Methods
        public void EventInvocation()
        {
            UserAccountCreationMethods.Invoke();
        }

        public void ClientInitialization(User u)
        {
            //User object clients are initialized, allowing communication with internet and email
            try
            {
                UserHttpClient = new HttpClient();

                SaslMechanismOAuth2 oauth2 = new SaslMechanismOAuth2(MainUserEmail, (u.UserAuthentication.AccessToken).Result);

                UserSmtpClient.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                UserSmtpClient.Authenticate(oauth2);    
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }
        }

        public void MimeMessageInitialization()
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