using Email_Automation.CoreMethods;
using Email_Automation.Engines;
using Email_Automation_Update.Supplemental.Methods;
using OAuthImplementation;
using OrchestraInformation;
using System.Collections;

/* 
 * Add youtubePagUrl, youtubeIconUrl, htmlSignature 
 */

namespace Email_Automation.PrimaryUser
{
    //At some point, maybe methods to initialize the information (UI) rather than preset?
    internal sealed class User : IEnumerable, IDisposable
    {
        public List<OrchestraRecord> AllOrchestrasFromRecord { get; set; } = new List<OrchestraRecord>();
        public List<OrchestraRecord> SuccessfulEmails { get; set; } = new List<OrchestraRecord>();
        public List<OrchestraRecord> FailedEmails { get; set; } = new List<OrchestraRecord>();

        //Email signature information
        public String youtubePageUrl { get; set; } = "";
        public String youtubeIconUrl { get; set; } = "";
        public String globeIconUrl { get; set; } = "";
        public String portfolioUrl { get; set; } = "";
        public String? HtmlSignatureProperty { get; set; } = "";

        //Containment/Delegation (Maybe make private and access with property at some point)
        public AccountInformationEngine AccountInformationEngine;
        public Authentication UserAuthentication;
        public TranscriptEngine TranscriptEngine;
        public MailingMethods MailingMethodsEngine;
        public LoadingAndDisplay LoadingAndDisplayEngine;

        //Primary constructor
        public User()
        {
            AccountInformationEngine = new AccountInformationEngine();
            UserAuthentication = new Authentication();
            TranscriptEngine = new TranscriptEngine();
            MailingMethodsEngine = new MailingMethods();
            LoadingAndDisplayEngine = new LoadingAndDisplay();
            HtmlSignatureGeneration();
        }

        public void BeginAuthentication()
        {
            UserAuthentication.OpenInternetWindow();
            UserAuthentication.AuthorizationCode = UserAuthentication.HttpListenerForAuthorizationCode();
            UserAuthentication.AccessToken = UserAuthentication.ExchangeAuthorizationCodeForAccessToken(UserAuthentication.AuthorizationCode);
            AccountInformationEngine.ClientInitialization(this);
        }

        //Methods
        public void AccountEngineInvocation()
        {
            AccountInformationEngine.EventInvocation();
        }

        private void HtmlSignatureGeneration()
        {
            HtmlSignatureProperty = $@"";
        }

        public async Task TranscriptInvocation(User PrimaryUser)
        {
            await TranscriptEngine.FuncInvocation(PrimaryUser);
        }

        public void Dispose()
        {
                AccountInformationEngine.UserSmtpClient?.Dispose();
                AccountInformationEngine.UserHttpClient?.Dispose();
                AccountInformationEngine.UserMimeMessage?.Dispose();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nResources successfully disposed\n");
                Console.ForegroundColor = ConsoleColor.White;
        }

        IEnumerator IEnumerable.GetEnumerator()
            => AllOrchestrasFromRecord.GetEnumerator();
    }
}