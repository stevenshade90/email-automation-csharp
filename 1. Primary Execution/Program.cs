using Email_Automation.CoreMethods;
using Email_Automation.PrimaryUser;
using SqlData;

using static Orchestra_Finder_API_Query.CoreProgram;


namespace Email_Automation.Core
{
    public class Program
    {
        static async Task Main()
        {
            System.ConsoleColor consoleColor;
            bool outerContinueSequence = true;

            //maybe eventually save object with serialization
            while (outerContinueSequence)
            {
                using (User PrimaryUser = new User())
                {
                    // 1. User setup and Authentication
                    PrimaryUser.AccountEngineInvocation();
                    DisplaySectionText("USER SETUP AND AUTHENTICATION", consoleColor = ConsoleColor.DarkYellow);
                    PrimaryUser.BeginAuthentication();

                    // 2. API Data Gathering
                    DisplaySectionText("DATA GATHERING", consoleColor = ConsoleColor.DarkMagenta);
                    await GoogleSearchForOrchestraInformation(PrimaryUser);

                    // 3. CRUD
                    DisplaySectionText("DATA EDITING", consoleColor = ConsoleColor.DarkCyan);
                    SqlData.Program.Read();
                    SqlData.Program.CrudOperationSelection();

                    // 4. Email sending sequence
                    DisplaySectionText("SEND EMAILS", consoleColor = ConsoleColor.DarkGreen);
                    await LoadEmailsToUser(PrimaryUser);
                                         
                    // 5. User views all emails and confirms whether or not to continue
                    PrimaryUser.LoadingAndDisplayEngine.DisplayOrchestrasAndWarning(PrimaryUser);

                    // 6. Load the email text, and then emails are displayed and sent one-by-one
                    MailingMethods.LoadEmailMessage(PrimaryUser);
                    MailingMethods.EmailSendingSequence(PrimaryUser);

                    // 7. Auto-save results and transcript
                    await PrimaryUser.TranscriptInvocation(PrimaryUser);

                    // 8.Rerun program
                    outerContinueSequence = ContinueRequest();
                } 
            }

            static void DisplaySectionText(string text, System.ConsoleColor consoleColor)
            {
                string display = string.Concat(Enumerable.Repeat("*", Console.WindowWidth));

                Console.ForegroundColor = consoleColor;
                Console.WriteLine();
                Console.WriteLine(display);
                Console.WriteLine(text.ToUpper());
                Console.WriteLine(display);
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.White;
            }

            //Also check for UTF-8?
            static async Task LoadEmailsToUser(User user)
            {
                await Task.Run(() => 
                {
                    using (var context = new OrchestraRecordContext())
                    {
                        var uniqueValues = context.OrchestraInformation
                            .AsEnumerable()
                            .DistinctBy(x => x.OrchestraName.Trim())
                            .Where(x => x.OrchestraName != "NO-NAME" && x.Email != "NO-EMAIL")
                            .OrderBy(x => x.OrchestraName)
                            .ToList();           
                         
                        foreach (var record in uniqueValues)
                        {
                            user.AllOrchestrasFromRecord.Add(record);
                        }                     
                    }
                });
            }

            static bool ContinueRequest()
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\n\nEnter 'q' to quit, or enter any other key to continue: ");
                var response = Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.White;

                bool b = response.Equals("q", StringComparison.OrdinalIgnoreCase)
                     ? false
                     : true;

                if (b == true)
                {
                    SqlData.Program.ContinueCrud = true;
                }

                return b;
            }
        }
    }
}