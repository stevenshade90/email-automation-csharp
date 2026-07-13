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
        public String UserPassword { get; private set; } = "";

        public SmtpClient UserSmtpClient { get; set; }
        public HttpClient UserHttpClient { get; set; }

        public MailMessage UserMailMessage { get; set; }
        public MailAddress UserMailAddress { get; set; }

        public String MainUserEmail { get; init; } = "";
        public String? MainUserDisplayName { get; init; } = "";
        public String? EmailMessage { get; set; } = "";

        private const String smtpHost = "smtp.gmail.com";
        private const int smtpPort = 587;

        private delegate void EngineDelegate();
        private event EngineDelegate UserAccountCreationMethods; // Assigned to 3 methods listed in constructor

        //Primary Constructor
        public AccountInformationEngine()
        {
            UserAccountCreationMethods += PasswordRetrieval;
            UserAccountCreationMethods += ClientInitialization;
            UserAccountCreationMethods += MailMessageInitialization;
        }

        //Methods
        public void EventInvocation()
        {
            UserAccountCreationMethods.Invoke();
        }

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

            String passwordFile = @""; // Specify the path to the password file here
            Console.Write("Locating Password: ");
            try
            {
                using (StreamReader sr = new StreamReader(passwordFile))
                {
                    UserPassword = sr.ReadToEnd();
                    UserPassword = UserPassword.Trim();

                    if (!(String.IsNullOrEmpty(UserPassword)))
                    {
                        Console.Write("Password retrieved\n");
                    }
                    else if (String.IsNullOrEmpty(UserPassword) || !(File.Exists(passwordFile)))
                    {
                        throw new FileNotFoundException("Password file is empty or the file location does not exist.");
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Error retrieving password => {ex.Message}");
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error retrieving password => {e.Message}");
                Environment.Exit(1);
            }
        }

        private void ClientInitialization()
        {
            //User object clients are initialized, allowing communication with internet and email
            try
            {
                UserHttpClient = new HttpClient()
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                UserSmtpClient = new SmtpClient
                {
                    Host = smtpHost,
                    Port = smtpPort,
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new System.Net.NetworkCredential(MainUserEmail, UserPassword),
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

            UserMailAddress = new MailAddress(MainUserEmail, MainUserDisplayName);
            UserMailMessage = new MailMessage()
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