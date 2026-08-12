using Email_Automation_Update.Supplemental.Classes_and_Engines;
using Email_Automation_Update.Supplemental.Engines;
using OAuthImplementation;
using OrchestraInformation;
using System.Collections;


namespace Email_Automation.PrimaryUser
{
    //At some point, maybe methods to initialize the information (UI) rather than preset?
    internal sealed class User : IEnumerable, IDisposable 
    {
        private IConfiguration config;

        internal List<OrchestraRecord> AllOrchestrasFromRecord { get; set; } = new List<OrchestraRecord>();
        internal List<OrchestraRecord> SuccessfulEmails { get; set; } = new List<OrchestraRecord>();
        internal List<OrchestraRecord> FailedEmails { get; set; } = new List<OrchestraRecord>();

        internal HttpClient UserHttpClient { get; set; }


        //Email signature information
        private String youtubePageUrl { get; set; }
        private String portfolioUrl { get; set; }
        private String youtubeIconUrl { get; set; } = "https://upload.wikimedia.org/wikipedia/commons/e/ef/Youtube_logo.png";
        private String globeIconUrl { get; set; } = "https://img.icons8.com/ios-filled/50/000000/globe--v1.png";

        internal String? HtmlSignatureProperty { get; set; } = "";

        //Containment/Delegation (Maybe make private and access with property at some point)
        internal Authentication Authentication;
        internal Lazy<TranscriptEngine> TranscriptEngine;
        internal AccountInformationEngine AccountInformationEngine;
        internal MailingMethodsEngine MailingMethodsEngine;
        internal LoadingAndDisplayEngine LoadingAndDisplayEngine;
        internal CustomResiliencePipelineOptions PollyPipeline;

        internal ResiliencePipeline Pipeline => PollyPipeline.ResiliencePipeline;

        //Primary constructor
        public User()
        {
            config = new ConfigurationBuilder()
                .AddUserSecrets<User>()
                .Build();

            youtubePageUrl = config["User:ytPageUrl"];
            portfolioUrl = config["User:webpageUrl"];

            UserHttpClient = new HttpClient();
            TranscriptEngine = new Lazy<TranscriptEngine>(() => new TranscriptEngine(this));
            Authentication = new Authentication(this);
            AccountInformationEngine = new AccountInformationEngine(this);
            MailingMethodsEngine = new MailingMethodsEngine(this);
            LoadingAndDisplayEngine = new LoadingAndDisplayEngine(this);
            PollyPipeline = new CustomResiliencePipelineOptions();

            HtmlSignatureGeneration();
        }

        internal void BeginAuthentication()
        {
            Authentication.OpenInternetWindow();
            Authentication.AuthorizationCode = Authentication.HttpListenerForAuthorizationCode();
            Authentication.AccessToken = Authentication.ExchangeAuthorizationCodeForAccessToken(Authentication.AuthorizationCode);
        }

        //Methods
        internal void AccountEngineInvocation()
        {
            AccountInformationEngine.EventInvocation();
        }

        private void HtmlSignatureGeneration()
        {
            HtmlSignatureProperty = $@"
<div style=""font-family: Arial, sans-serif; font-size: 14px; color: #000000; line-height: 1.4; margin-top: 20px;"">
<p style=""margin: 0; font-weight: bold; color: #003366;"">{config["User:name"]}</p>
<p style=""margin: 0; font-style: italic;"">{config["User:additionalText"]}</p>
<div style=""border-bottom: 1px solid #000000; width: 180px; margin: 6px 0;""></div>
<p style=""margin: 0 0 8px 0;"">c (412) 499-4135</p>
<p style=""margin: 0;"">
    <a href=""{youtubePageUrl}"" target=""_blank"" style=""text-decoration: none; margin-right: 12px; display: inline-block; vertical-align: middle;"">
        <img src=""{youtubeIconUrl}"" alt=""YouTube Channel"" width=""25"" height=""18"" style=""border: 0; vertical-align: middle;"" />
    </a>
    <a href=""{portfolioUrl}"" target=""_blank"" style=""text-decoration: none; display: inline-block; vertical-align: middle;"">
        <img src=""{globeIconUrl}"" alt=""Portfolio Website"" width=""18"" height=""18"" style=""border: 0; vertical-align: middle;"" />
    </a>
</p>
</div>";
        }

        internal async Task TranscriptInvocation()
        {
            await TranscriptEngine.Value.FuncInvocation();
        }

        public void Dispose()
        {
                this.UserHttpClient?.Dispose();
                AccountInformationEngine.UserSmtpClient?.Dispose();
                AccountInformationEngine.UserMimeMessage?.Dispose();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nResources successfully disposed\n");
                Console.ResetColor();
        }

        IEnumerator IEnumerable.GetEnumerator()
            => AllOrchestrasFromRecord.GetEnumerator();
    }
}