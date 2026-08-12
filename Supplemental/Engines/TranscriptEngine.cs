using Email_Automation.PrimaryUser;
using OrchestraInformation;

namespace Email_Automation_Update.Supplemental.Engines
{
    internal sealed class TranscriptEngine
    {

        private readonly User _user;
        private User PrimaryUser => _user;

        internal String Transcript { get; set; } = "";

        public TranscriptEngine(User user) 
        {
            _user = user;
        }

        //Delegate
        private Func<Task> GenerateTranscript => GenerateResultsAndTranscript;


        //Methods
        internal async Task FuncInvocation()
        {
            await GenerateTranscript.Invoke();
        }

        private async Task GenerateResultsAndTranscript()
        {
            //Generates an html document listing emails that were sucessfully/unsuccessfully sent as well a transcript showing each email sent to all orchestras 
            //These files are auto saved with appropriate tags and location to the directory generated from GenerateDirectory
            String dateAndTime = DateTime.Now.ToLongDateString() + " (" + DateTime.Now.ToLongTimeString() + ")";

            DirectoryInfo dir = GenerateDirectory();

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
                    Console.ResetColor();

                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.Message);
                    Console.ResetColor();
                }
            });

            await Task.Run(() =>
            {
                try
                {
                    String transcriptPath = Path.Combine(dir.FullName, "emailTranscript.txt");

                    using (StreamWriter transcriptWriter = new StreamWriter(transcriptPath))
                    {
                        transcriptWriter.WriteLine((string)PrimaryUser.TranscriptEngine.Value.Transcript);
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Results saved to: {transcriptPath}");
                    Console.ResetColor();
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.Message);
                    Console.ResetColor();
                }
            });
        }

        private DirectoryInfo GenerateDirectory()
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