# Email Automation Program
An asynchronous C# emailing program that dynamically discovers contacts via SerpApi, provides interactive CRUD data filtering, and automates personalized HTML email delivery using OAuth 2.0.

## Features
* **User Information Security:** User credentials are stored in `IConfiguration` secrets, preventing the need for hardcoded credentials in the program.
* **StreamReader and StreamWriter:** Utilizes `StreamReader` to load and display data from a .txt file, and uses `StreamWriter` to save email transcripts.
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

### Phase 3: OAuth2, Dynamic API Discovery & Interactive CRUD (Version 3.0.0.0)
The third phase transitions the application from relying on static local data files to dynamically discovering targets via web search APIs, securing access via modern OAuth2 protocols, and giving the user real-time control over the target data via CRUD operations.

* **OAuth2 Authentication:** Integrated OAuth2 identity validation at program startup, replacing basic credentials with token-based authentication for secure session initialization and API authorization.
* **Dynamic Web Searches:** Embedded in-app API query engine using SerpApi to perform real-time web searches, allowing users to automatically aggregate orchestra directory data by county without manual list preparation.
* **CRUD & Filtering Engine:** Built an interactive CRUD layer directly into the workflow. Data is parsed and filtered automatically, then exposed through CRUD functionality so the user can review and modify data before approving the final email queue.

#### Update: Resiliency Pipelines & IConfiguration (Version 3.0.1.0)
* Added `Polly` resiliency pipelines to the API query engine to prevent data loss during network interruptions. The program will now automatically retry failed requests to ensure that all data is retrieved successfully.
* Implemented `IConfiguration` to allow user secrets to be added, removing the need for hardcoded credentials in the program.

#### Update: Memory Efficiency & Modularization (Version 3.0.2.0)
* Implemented `yield return`, eliminating display latency caused during CRUD read operations.
* Secured email message content by moving the email body text into User Secrets and updated syntax logic to accurately parse data.
* Refactored `TranscriptEngine` into a `Lazy<TranscriptEngine>` object to defer instantiation until needed at the end of the program lifecycle.
* Modularized all engine files to improve maintainability.

## Future Updates
* [x] Organize the code into logical modules.
* [x] Update the mailing service to MailKit.
* [x] Update the authentication method to the industry-standard OAuth 2.0.
* [x] Implement `IConfiguration` to allow for dynamic configuration of the program without requiring code changes.
* [x] Implement `Polly` to improve API call resilience and prevent data loss during network interruptions.
* [ ] Implement `Dependency Injection` to allow for better testability and maintainability of the code.
* [ ] Implement a logging framework to allow for better debugging and error tracking.
* [ ] Allow the user to select if they would like to send an email one at a time (current implementation), or as a batch via parallel async tasks (`Parallel.ForEachAsync`).
* [ ] Implement an asynchronous API search to allow multiple concurrent requests.
* [ ] Implement WinUI3 to modernize the experience and allow for more intuitive user interaction.
* [ ] Implement a serialization method to allow for saving and loading of user data and preferences.
