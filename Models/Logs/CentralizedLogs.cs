namespace ITAM.Models.Logs
{
    public class CentralizedLogs
    {
        public int id { get; set; }
        public string? type { get; set; }
        public string? asset_barcode { get; set; }
        public string? action { get; set; }
        public string? performed_by_user_id { get; set; }
        public DateTime? timestamp { get; set; }
        public string? details { get; set; }
    }
}
