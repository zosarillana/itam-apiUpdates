using System.Text.Json.Serialization;

namespace ITAM.Models
{
    public class ComputerComponents
    {
        public int id { get; set; }
        public string? date_acquired { get; set; }
        public string type { get; set; }
        public string description { get; set; }
        public string? asset_barcode { get; set; }
        public string uid { get; set; }
        public decimal cost { get; set; }
        public string? status { get; set; }
        public List<string>? history { get; set; } //computer_id
        public int? owner_id { get; set; }
        public User? owner { get; set; }
        public bool? is_deleted { get; set; } = false;
        public string? component_image { get; set; }



        // Add computer_id as a foreign key
        public int? computer_id { get; set; }

        [JsonIgnore]
        public Computer? computer { get; set; }
        [JsonIgnore]
        public ICollection<ReturnItems>? ReturnItems { get; set; }

    }
}
