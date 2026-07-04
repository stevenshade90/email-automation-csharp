using Email_Automation.PrimaryUser;
using OrchestraInformation;
using System.Net.Mail;

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
        private String _password;

        private SmtpClient _smtpClient;
        private HttpClient _httpClient;

        private MailMessage _mailMessage;
        private MailAddress _mailAddress;

        private const String smtpHost = "smtp.gmail.com";
        private const int smtpPort = 587;
        private String _mainUserEmail { get; init; } = "";
        private String? _mainUserDisplayName { get; init; } = "";
        private String? _emailMessage;

        private delegate void EngineDelegate();
        private event EngineDelegate UserAccountCreationMethods; // Assigned to 3 methods listed in constructor

        //Properties
        public String UserPassword => _password;
        public String EmailMessage
        {
            get => _emailMessage;
            set => _emailMessage = value;
        }
        public SmtpClient UserSmtpClient
        {
            get => _smtpClient;
            set => _smtpClient = value;
        }
        public HttpClient UserHttpClient
        {
            get => _httpClient;
            set => _httpClient = value;
        }
        public MailMessage UserMailMessage
        {
            get => _mailMessage;
            set => _mailMessage = value;
        }
        public MailAddress UserMailAddress => _mailAddress;

        public void EventInvocation()
        {
            UserAccountCreationMethods.Invoke();
        }

        //Primary Constructor
        public AccountInformationEngine()
        {
            UserAccountCreationMethods += PasswordRetrieval;
            UserAccountCreationMethods += ClientInitialization;
            UserAccountCreationMethods += MailMessageInitialization;
        }

        //Methods
        private void PasswordRetrieval()
        {
            //Finds user password from a text file 
            //Like the URL method, maybe have validation logic to take user input? Maybe one generic method to process both, and a way for PC to scan for appropriate txt file name regardless of location?

            /*
            while (String.IsNullOrEmpty(_password))
            {
                Console.Write("Enter your password: ");
                _password = Console.ReadLine();
                if (String.IsNullOrEmpty(_password))
                {
                    Console.WriteLine("Password cannot be empty. Please try again.");
                }
            }
            Console.WriteLine("Password retrieved successfully.");
            */

            
            Console.Write("Locating Password: ");

            using (StreamReader sr = new StreamReader(@""))
            {
                _password = sr.ReadToEnd();
                _password = _password.Trim();
            }

            if (!(String.IsNullOrEmpty(_password)))
            {
                Console.Write("Password retrieved\n");
            }
            else
            {
                Console.Write("Could not find your password. Exiting program");
                Environment.Exit(1);
            }
            
        }

        private void ClientInitialization()
        {
            //User object clients are initialized, allowing communication with internet and email

            try
            {
                _httpClient = new HttpClient()
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                _smtpClient = new SmtpClient
                {
                    Host = smtpHost,
                    Port = smtpPort,
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new System.Net.NetworkCredential(_mainUserEmail, UserPassword),
                    Timeout = 10000
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }
        }

        private void MailMessageInitialization()
        {
            //Generation of User's mailmessage/address objects to send the emails later

            _mailAddress = new MailAddress(_mainUserEmail, _mainUserDisplayName);
            _mailMessage = new MailMessage()
            {
                From = UserMailAddress,
                Subject = "",
                Body = "", //Filled later prior to send
                IsBodyHtml = true
            };
        }
    }

    internal sealed class TranscriptEngine
    {
        private String _transcript;

        //Constructors
        public TranscriptEngine() { }
        public TranscriptEngine(String transcript)
        {
            _transcript = transcript;
        }

        //Delegate
        private Func<User, Task> GenerateTranscript => GenerateResultsAndTranscript;

        //Properties
        public String ReturnTranscript => _transcript;

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
                    foreach (Orchestra o in PrimaryUser.FailedEmails)
                    {
                        Console.WriteLine($"- {o.OrchestraEmail}");
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
                        foreach (Orchestra o in PrimaryUser.SuccessfulEmails)
                        {
                            htmlWriter.WriteLine("&nbsp;&nbsp;&nbsp;&nbsp;[+] {0} : {1}<br>", o.OrchestraName, o.OrchestraEmail);
                        }

                        //List failed emails
                        htmlWriter.WriteLine($"<H3><B><u><span style=\"color: red;\">Failed Emails: {PrimaryUser.FailedEmails.Count()}</span></u></B></H3>");
                        foreach (Orchestra o in PrimaryUser.FailedEmails)
                        {
                            htmlWriter.WriteLine($"&nbsp;&nbsp;&nbsp;&nbsp;[-] {o.OrchestraEmail}<br>");
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
                        transcriptWriter.WriteLine(PrimaryUser.TranscriptEngine.ReturnTranscript);
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

            String stateCode = String.IsNullOrWhiteSpace(PrimaryUser.AllOrchestras[0].OrchestraState) 
                ? "N/A" 
                : PrimaryUser.AllOrchestras[0].OrchestraState;

            DirectoryInfo dir = new DirectoryInfo(@"");
            dir.Create();
            
            DirectoryInfo subd = new DirectoryInfo(Path.Combine(dir.FullName, stateCode));
            subd.Create();

            return subd;
        }
    }
}
