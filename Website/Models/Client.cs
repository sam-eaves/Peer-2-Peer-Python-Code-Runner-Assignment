namespace Website.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string IpAddress { get; set; } = "localhost";
        public int Port { get; set; }
        public int JobsCompleted { get; set; }
        public DateTime LastSend { get; set; }
    }
}
