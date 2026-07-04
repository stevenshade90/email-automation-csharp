# Mass Email Automation Program
A C# email automation program that parses structured contact records to send personalized HTML emails via SMTP. This program handles file streaming, email credentials, and user input to safely coordinate batch messaging.

## Features
* **User Information Security:** User credentials are located on the user's computer (`Environment.GetEnvironmentVariable`), preventing the need for hardcoded credentials in the program.
* **StreamReader and StreamWriter:** Utilizes `StreamReader` to load and display data from a CSV file, and uses `StreamWriter` to save email transcripts.
* **Emailing Guardrails:** Displays the email that is about to be sent for user verification, then requires explicit user confirmation (`Console.ReadKey`) before sending an email.
* **HTML Integration:** Utilizes HTML syntax to structure transcript outputs, as well as generate a user signature on the email.
* **RegEx Preview:** Implements `System.Text.RegularExpressions` to strip complex HTML tags and deliver a clean, plain-text layout preview to the terminal to ensure data interpolation accuracy.

## Project Evolution
### Phase 1: Baseline Project (Version 1.0.0.0)
The initial version of the program establishes the operational sequence of the email automation within a single, linear execution structure. This phase focuses entirely on functionality: enabling user login through non-hardcoded credentials, extracting files from the local environment, generating and sending tailored emails to recipients, and opening file streams to process and save data.

### Phase 2: Refactoring & Modularization (Version 2.0.0.0)
The second phase transitions the application from a linear script into a modular structure. The original program has now been broken down into specialized, reusable components. 

* **Modular Architecture:** Extracted core components out of `Program.cs` and isolated them into dedicated engine classes. `Program.cs` now acts exclusively as an orchestrator.
* **Asynchronous Execution (`async`/`await`):** Offloaded I/O-bound operations and data streaming to background tasks, optimizing thread utilization and keeping the application interface responsive during processing.
* **Resource Optimization:** Implemented resource disposal patterns via `using` blocks, ensuring that file streams and CSV data contexts are safely closed and cleared from memory immediately after execution.

## Future Updates
* [x] Organize the code into logical modules.
* [ ] Allow the user to select if they would like to send an email one at a time (current implementation), or as a batch utilizing async/await.
* [ ] Update the mailing service to MailKit.
* [ ] Update the authentication method to the industry-standard OAuth 2.0.
