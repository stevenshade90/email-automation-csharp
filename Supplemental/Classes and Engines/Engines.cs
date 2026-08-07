using Email_Automation.PrimaryUser;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using OrchestraInformation;

/*
 * Needs the following filled prior to use:
 *  1. _mainUserEmail
 *  2. _mainuserDisplayName 
 *  3. password directory in PasswordRetrieval()
 *  4. subject line in MailMessageInitialization()
 *  5. Directory info to transcript folder (...Email_Automation_2.0\Program Files\Transcripts)
 */

namespace Email_Automation.Engines
{
    internal sealed class AccountInformationEngine 
    {
        public String UserPassword { get; private set; } = "";

        public SmtpClient UserSmtpClient { get; set; } = new SmtpClient();
        public HttpClient UserHttpClient { get; set; }

        public MimeMessage UserMimeMessage { get; set; } = new MimeMessage();

        public String MainUserEmail { get; init; } = "";
        public String MainUserDisplayName { get; init; } = "";
        public String EmailSubject = "";
        public String EmailMessage { get; set; } = "";

        private const String smtpHost = "smtp.gmail.com";
        private const int smtpPort = 587;

        private delegate void EngineDelegate();
        private event EngineDelegate UserAccountCreationMethods; // Assigned to 3 methods listed in constructor

        //Primary Constructor
        public AccountInformationEngine()
        {
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

    internal sealed class TranscriptEngine
    {
        public String ReturnTranscript { get; set; } = "";

        //Constructors
        public TranscriptEngine() { }
        public TranscriptEngine(String transcript)
        {
            ReturnTranscript = transcript;
        }

        //Delegate
        private Func<User, Task> GenerateTranscript => GenerateResultsAndTranscript;


        //Methods
        public async Task FuncInvocation(User PrimaryUser)
        {
            await GenerateTranscript.Invoke(PrimaryUser);
        }

        private async Task GenerateResultsAndTranscript(User PrimaryUser)
        {
            //Generates an html document listing emails that were sucessfully/unsuccessfully sent as well a transcript showing each email sent to all orchestras 
            //These files are auto saved with appropriate tags and location to the directory generated from GenerateDirectory
            String dateAndTime = DateTime.Now.ToLongDateString() + " (" + DateTime.Now.ToLongTimeString() + ")";

            DirectoryInfo dir = GenerateDirectory(PrimaryUser);

            await Task.Run(() =>
            {
                Console.WriteLine("\n--- Sending Complete ---");
                if (PrimaryUser.FailedEmails.Count() > 0)
                {
                    Console.WriteLine($"Emails failed to send ({PrimaryUser.FailedEmails.Count()}):");
                    foreach (OrchestraRecord o in PrimaryUser.FailedEmails)
                    {
                        Console.WriteLine($"- {o.Email}");
                    }
                }
                else
                {
                    Console.WriteLine("All emails sent successfully");
                }
                Console.WriteLine();
            });

            await Task.Run(() =>
            {
                try
                {
                    String htmlPath = Path.Combine(dir.FullName, "results.html");

                    using (StreamWriter htmlWriter = new StreamWriter(Path.Combine(dir.FullName, "results.html")))
                    {
                        //Document header
                        htmlWriter.WriteLine("<html><body>");
                        htmlWriter.WriteLine($"<H1>Emailing Results</H1>");
                        htmlWriter.WriteLine($"<H3>Date: {dateAndTime}</H3>");
                        htmlWriter.WriteLine($"<H3><B><u><span style=\"color: green;\">Successful Emails: {PrimaryUser.SuccessfulEmails.Count()} of {PrimaryUser.SuccessfulEmails.Count()
                           + PrimaryUser.FailedEmails.Count()}</span></u></B></H3>");

                        //List successful emails
                        foreach (OrchestraRecord o in PrimaryUser.SuccessfulEmails)
                        {
                            htmlWriter.WriteLine("&nbsp;&nbsp;&nbsp;&nbsp;[+] {0} : {1}<br>", o.OrchestraName, o.Email);
                        }

                        //List failed emails
                        htmlWriter.WriteLine($"<H3><B><u><span style=\"color: red;\">Failed Emails: {PrimaryUser.FailedEmails.Count()}</span></u></B></H3>");
                        foreach (OrchestraRecord o in PrimaryUser.FailedEmails)
                        {
                            htmlWriter.WriteLine($"&nbsp;&nbsp;&nbsp;&nbsp;[-] {o.Email}<br>");
                        }
                        htmlWriter.WriteLine("</body></html>");
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Results saved to: {htmlPath}");
                    Console.ForegroundColor = ConsoleColor.White;

                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.Message);
                    Console.ForegroundColor = ConsoleColor.White;
                }
            });

            await Task.Run(() =>
            {
                try
                {
                    String transcriptPath = Path.Combine(dir.FullName, "emailTranscript.txt");

                    using (StreamWriter transcriptWriter = new StreamWriter(transcriptPath))
                    {
                        transcriptWriter.WriteLine((string)PrimaryUser.TranscriptEngine.ReturnTranscript);
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Results saved to: {transcriptPath}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.Message);
                    Console.ForegroundColor = ConsoleColor.White;
                }
            });
        }

        private DirectoryInfo GenerateDirectory(User PrimaryUser)
        {
            //Generates and returns the directory used to autosave the html and transcript documents
            String stateCode = String.IsNullOrWhiteSpace(PrimaryUser.AllOrchestrasFromRecord[0].State) 
                ? "N/A" 
                : PrimaryUser.AllOrchestrasFromRecord[0].State;

            DirectoryInfo dir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\transcripts");
            dir.Create();
            
            DirectoryInfo subd = new DirectoryInfo(Path.Combine(dir.FullName, stateCode));
            subd.Create();

            return subd;
        }
    }
}