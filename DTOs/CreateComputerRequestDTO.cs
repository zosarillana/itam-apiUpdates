namespace ITAM.DTOs
{
    public class CreateComputerRequest
    {
        // Computer Table Fields
        public string? type { get; set; }
        public string? date_acquired { get; set; } = string.Empty;
        public string? asset_barcode { get; set; }
        public string? brand { get; set; }
        public string? model { get; set; }
        public string? size { get; set; }
        public string? color { get; set; }
        public string? serial_no { get; set; }
        public string? po { get; set; }
        public string? warranty { get; set; }
        public decimal cost { get; set; }
        public string? remarks { get; set; }

        // List of Components (RAM, SSD, etc.)
        public List<ComponentDTOs>? components { get; set; } = new List<ComponentDTOs>();

        // Asset Information
        public List<AssetDTOs> assets { get; set; } // <-- Updated to list
    }

    // Computer Components DTO
    public class ComponentDTOs
    {
        public string? date_acquired { get; set; }
        public string type { get; set; }  // Example: "RAM", "SSD"
        public string description { get; set; }  // Example: "8GB DDR4"
        public decimal cost { get; set; }
    }

    // Asset DTO
    public class AssetDTOs
    {
        public string? type { get; set; }
        public string date_acquired { get; set; } = string.Empty;
        public string? asset_barcode { get; set; }
        public string? brand { get; set; }
        public string? model { get; set; }
        public string? size { get; set; }
        public string? color { get; set; }
        public string? serial_no { get; set; }
        public string? po { get; set; }
        public string? warranty { get; set; }
        public decimal cost { get; set; }
        public string? remarks { get; set; }
    }

    public class PullOutRequest
    {
        public string? remarks { get; set; }
    }
}
