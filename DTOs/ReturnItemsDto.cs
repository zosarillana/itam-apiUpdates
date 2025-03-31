namespace ITAM.DTOs
{
    public class ReturnItemsDto
    {
        public int id { get; set; }
        public int accountability_id { get; set; }
        public object? user { get; set; } // Store user details
        public int? asset_id { get; set; }
        public int? computer_id { get; set; }
        public string? item_type { get; set; }
        public int? component_id { get; set; }
        public string status { get; set; }
        public string? remarks { get; set; }
        public DateTime return_date { get; set; }
        public int validated_by { get; set; }
    }
}
