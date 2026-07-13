using OrchestraInformation;
using Email_Automation.Engines;
using Email_Automation.CoreMethods;

using System.Collections;
using Email_Automation.SupplementalMethods;

/* Add youtubePagUrl, youtubeIconUrl, htmlSignature 
 */

namespace Email_Automation.PrimaryUser
{
    //At some point, maybe methods to initialize the information (UI) rather than preset?
    internal sealed class User : IEnumerable, IDisposable
    {
        private List<Orchestra> _allOrchestras = new List<Orchestra>();
        private List<Orchestra> _successfullySentEmails = new List<Orchestra>();
        private List<Orchestra> _unsentAndFailedEmails = new List<Orchestra>();

        //Containment/Delegation (Maybe make private and access with property at some point)
        public AccountInformationEngine AccountInformationEngine;
        public TranscriptEngine TranscriptEngine;
        public MailingMethods MailingMethodsEngine;
        public LoadingAndDisplay LoadingAndDisplayEngine;

        //Email signature information
        public String youtubePageUrl { get; set; } = "";
        public String youtubeIconUrl { get; set; } = "";
        public String? HtmlSignatureProperty { get; set; } = "";


        //Primary constructor
        public User()
        {
            AccountInformationEngine = new AccountInformationEngine();
            TranscriptEngine = new TranscriptEngine();
            MailingMethodsEngine = new MailingMethods();
            LoadingAndDisplayEngine = new LoadingAndDisplay();
            HtmlSignatureGeneration();
        }

        //Properties
        public List<Orchestra> AllOrchestras
        {
            get => this._allOrchestras;
            set => this._allOrchestras = value;
        }
        public List<Orchestra> SuccessfulEmails
        {
            get => _successfullySentEmails;
            set => _successfullySentEmails = value;
        }

        public List<Orchestra> FailedEmails
        {
            get => _unsentAndFailedEmails;
            set => _unsentAndFailedEmails = value;
        }

        //Methods
        private void HtmlSignatureGeneration()
        {
            HtmlSignatureProperty = $@"";
        }

        public void AccountEngineInvocation()
        {
            AccountInformationEngine.EventInvocation();
        }

        public async Task TranscriptInvocation(User PrimaryUser)
        {
            await TranscriptEngine.FuncInvocation(PrimaryUser);
        }

        public void Dispose()
        {
                AccountInformationEngine.UserSmtpClient?.Dispose();
                AccountInformationEngine.UserHttpClient?.Dispose();
                AccountInformationEngine.UserMailMessage?.Dispose();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nResources successfully disposed\n");
                Console.ForegroundColor = ConsoleColor.White;
        }

        IEnumerator IEnumerable.GetEnumerator()
            => AllOrchestras.GetEnumerator();
    }
}