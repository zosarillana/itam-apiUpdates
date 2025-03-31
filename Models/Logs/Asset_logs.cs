namespace ITAM.Models.Logs
{
    public class Asset_logs
    {
        public int id { get; set; }
        public int asset_id { get; set; }
        public string action { get; set; }
        public string performed_by_user_id { get; set; }
        public DateTime timestamp { get; set; }
        public string details { get; set; }

        public Asset assets { get; set; }
    }
}
