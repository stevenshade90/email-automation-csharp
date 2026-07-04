using CsvHelper.Configuration.Attributes;

namespace OrchestraInformation
{
    internal class Orchestra
    {
        [Name("Orchestras")]
        public required String OrchestraName { get; set; }
        [Name("Contact")]
        public required String OrchestraEmail { get; set; }

        [Name("State")]
        public required String OrchestraState { get; set; }
    }
}
