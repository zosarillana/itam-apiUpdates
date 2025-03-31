using System.Text.Json.Serialization;

namespace ITAM.Models
{
    public class ReturnItems
    {
        public int id { get; set; }
        public int accountability_id { get; set; } // Links to accountability
        public int user_id { get; set; }
        public int? asset_id { get; set; } // Nullable if returning a computer
        public int? computer_id { get; set; } // Nullable if returning an asset
        public string? item_type { get; set; } // Example: "Computer", "Assets" , "Components"
        public int? component_id { get; set; } // Nullable if not a component return
        public string status { get; set; } = "pending"; // "returned", "missing", "damaged", "pending"
        public string? remarks { get; set; }
        public DateTime return_date { get; set; } = DateTime.UtcNow;
        public int validated_by { get; set; } // Staff who verifies return

        [JsonIgnore]
        public User? user { get; set; } // Navigation property

        [JsonIgnore]
        public Asset? asset { get; set; } // Navigation property

        [JsonIgnore]
        public Computer? computer { get; set; } // Navigation property
        [JsonIgnore]
        public ComputerComponents? components { get; set; } // Navigation property

        [JsonIgnore]
        public UserAccountabilityList? user_accountability_list { get; set; } // Navigation property
    }
}
