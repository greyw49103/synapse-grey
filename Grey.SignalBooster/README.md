# Signal Booster Assignment (Grey Wilson)

📘 IDE

   - Visual Studio Community 2022 was used for this assignment.

🧪 AI Development Tools

   - GitHub Copilot was utilized to assist in code completion.

📄 Assumptions, Limitations, Improvements

   - Code assumes an input file format similiar to physician_note1.txt included in the Grey.SignalBooster project root.
   - The ExtractUrl setting `https://alert-api.com/DrExtract` is a placeholder and should be replaced with a real endpoint.
   - Future improvements could include:
     - Adding support for more DME device types.
     - Handling file formatting variations (e.g., JSON-wrapped notes), like physician_note2.txt.
     - Implementing a more sophisticated LLM-based extraction method.
     - Configurability for file paths and API endpoints.
   
📄 Instructions to run the project:

   - Ensure .NET SDK is installed.
   - Open the solution in Visual Studio.
   - Set Grey.SignalBooster as the startup project.
   - Run the project.
   - Grey.NoteService.Test contains xUnit tests for the Grey.NoteService project. The tests can be run independently of Grey.SignalBooster. 
