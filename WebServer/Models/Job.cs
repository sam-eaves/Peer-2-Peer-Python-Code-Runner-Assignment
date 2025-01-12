namespace WebServer.Models
{
    //Redundant job class
    public class Job
    {
        public int JobId { get; set; }
        public string JobData { get; set; }
        public string JobHash { get; set; }
        public string Status { get; set; } = "Pending";  // Pending, Completed, etc.
        public string? Result { get; set; }
        public int ClientId { get; set; }
        public Client? Client { get; set; } //EF navigation property
    }
}
