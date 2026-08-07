namespace OrchestraInformation
{
    public class OrchestraRecord
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string State { get; set; } = string.Empty;
        public string County { get; set; } = string.Empty;
        public string OrchestraName { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;


        public override string ToString() => $"{Id} : {State} : {County} : {OrchestraName} : {Website} : {Email}"; 
    }
}
