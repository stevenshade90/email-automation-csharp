using Email_Automation.PrimaryUser;
using Email_Automation.CoreMethods;


namespace Email_Automation.Core
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            bool outerContinueSequence = true;

            //maybe eventually save object with serialization
            while (outerContinueSequence)
            {
                bool innerContinueSequence = true;

                using (User PrimaryUser = new User())
                {
                    //1. User setup
                    PrimaryUser.AccountEngineInvocation();


                    //2. Gather all orchestra information and display
                    while (innerContinueSequence)
                    {
                        await MailingMethods.BuildURL(PrimaryUser);

                        Task<bool> csvTask = MailingMethods.ReadCsvAndLogOrchestraInformation(PrimaryUser);
                        PrimaryUser.LoadingAndDisplayEngine.LoadingImage(csvTask);

                        innerContinueSequence = await csvTask;
                    }


                    //3. User views all emails and confirms to continue
                    PrimaryUser.LoadingAndDisplayEngine.DisplayOrchestrasAndWarning(PrimaryUser);


                    //4. Emails are displayed and sent one-by-one
                    MailingMethods.SendEmail(PrimaryUser);


                    //5. Auto-save results and transcript
                    await PrimaryUser.TranscriptInvocation(PrimaryUser);

                    //6.Rerun program
                    outerContinueSequence = ContinueRequest(PrimaryUser);
                }
                
            }

            static bool ContinueRequest(User PrimaryUser)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\n\nEnter 'q' to quit, or enter any other key to enter another URL: ");
                var response = Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.White;

                bool b = response.Equals("q", StringComparison.OrdinalIgnoreCase)
                     ? false
                     : true;

                return b;
            }
        }
    }
}