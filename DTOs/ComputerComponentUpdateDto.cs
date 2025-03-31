namespace ITAM.DTOs
{
    public class ComputerComponentUpdateDto
    {
        public string? date_acquired { get; set; }
        public string? type { get; set; }
        public string? description { get; set; }
        public string? asset_barcode { get; set; }
        public string? uid { get; set; }
        public string? status { get; set; }
        public decimal? cost { get; set; }
        public List<string>? history { get; set; }
    }


    public class PullInComponentRequest
    {
        public int computer_id { get; set; }
        public string component_uid { get; set; }
        public string? remarks { get; set; } // Add nullable remarks
    }
}
