namespace ITAM.Models.Logs
{
    public class User_logs
    {
        public int Id { get; set; }
        public int user_id { get; set; }
        public string action { get; set; }
        public string performed_by_user_id { get; set; }
        public DateTime timestamp { get; set; }
        public string details { get; set; }

        public User user { get; set; }
    }
}
