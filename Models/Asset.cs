﻿using System.Text.Json.Serialization;

namespace ITAM.Models
{
    public class Asset
    {
        public int id { get; set; }
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
        public string? li_description { get; set; }
        public List<string>? history { get; set; } //owner_id history
        public string? asset_image { get; set; }
        public int? owner_id { get; set; }
        public bool is_deleted { get; set; } = false;
        public string? status { get; set; }
        public List<int>? root_history { get; set; } = new List<int>();
        public int? computer_id { get; set; } // added this 
        public string? fa_code { get; set; }



        [JsonIgnore]
        public User? owner { get; set; }
        public Computer? computer { get; set; }
        public DateTime? date_created { get; set; }
        public DateTime? date_modified { get; set; }
        [JsonIgnore]
        public ICollection<ReturnItems> ReturnItems { get; set; }

    }
}
