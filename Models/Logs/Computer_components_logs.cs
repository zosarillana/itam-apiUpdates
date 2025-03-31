namespace ITAM.Models.Logs
{
    public class Computer_components_logs
    {
        public int id { get; set; }
        public int computer_components_id { get; set; }
        public string action { get; set; }
        public string performed_by_user_id { get; set; }
        public DateTime timestamp { get; set; }
        public string details { get; set; }

        public ComputerComponents computer_components { get; set; }
    }
}
