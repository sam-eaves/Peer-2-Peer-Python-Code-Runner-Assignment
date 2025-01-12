namespace WebServer.Models
{
    public class JobRequest
    {
        public string JobData { get; set; }
        public string JobHash { get; set; }
        public int ClientId { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
