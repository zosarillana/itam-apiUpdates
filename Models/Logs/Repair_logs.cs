namespace ITAM.Models.Logs
{
    public class Repair_logs
    {
        public int id { get; set; }
        public string? type { get; set; }
        public string? eaf_no { get; set; }
        public string? inventory_code { get; set; }
        public string? item_id { get; set; }
        public string? computer_id { get; set; }
        public string? action { get; set; }
        public string? remarks { get; set; }
        public string? performed_by_user_id { get; set; }
        public DateTime timestamp { get; set; }
    }
}
