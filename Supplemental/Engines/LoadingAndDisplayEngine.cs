using Email_Automation.PrimaryUser;


namespace Email_Automation_Update.Supplemental.Engines
{
    internal class LoadingAndDisplayEngine
    {
        internal static string divider = new string('_', Console.WindowWidth);

        private readonly User _user;
        private User PrimaryUser => _user;

        //Delegates
        internal Action DisplayOrchestrasAndWarning => DisplayAndWarning;
        private Action Warning;

        public LoadingAndDisplayEngine(User user)
        {
            this._user = user;
        }

        //Methods
        [Obsolete("May use later for certain loading tasks", true)]
        private void LoadingImage(Task awaitingTask)
        {
            //Loading image that will only display as the csv processing task is running
            int iterator = 1;
            Console.ForegroundColor = ConsoleColor.Green;
            char[] loading = { '|', '/', '-', '\\' };

            while (!awaitingTask.IsCompleted)
            {
                Console.Write("\rLoading information... {0}", loading[iterator % 4]);
                iterator++;
                Thread.Sleep(100);
            }
            Console.ResetColor();
            Console.Write($"\r{divider}\n");
        }

        private void DisplayAndWarning()
        {
            //A warning for each email that is about to send, and obtains user confirmation to send or exit program
            int iterator = 0;

            PrimaryUser.AllOrchestrasFromRecord.ForEach(o => Console.WriteLine("{0, 3: ##0}. {1, -45} : {2, -45}", ++iterator, o.OrchestraName, o.Email));

            PrimaryUser.LoadingAndDisplayEngine.Warning = delegate
            {
                string notice = $"\aYOU ARE ABOUT TO EMAIL THIS NUMBER OF RECIPIENTS: {iterator}";

                Console.WriteLine($"{divider}");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n{notice}");
                Console.Write("ENTER 'Y' TO CONTINUE: ");

                ConsoleKeyInfo keyPressed = Console.ReadKey();
                if (keyPressed.Key == ConsoleKey.Y)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n\nContinuing with send...");
                }
                else
                {
                    Console.WriteLine($"\n\nYou pressed: {keyPressed.KeyChar}");
                    Console.WriteLine("Terminating program");
                    Environment.Exit(0);
                }
                Console.ResetColor();
                Console.WriteLine($"{divider}\n");
                Thread.Sleep(3000);
            };

            PrimaryUser.LoadingAndDisplayEngine.Warning?.Invoke();
        }
    }
}