using System.Text.Json.Serialization;

namespace ITAM.Models
{
    public class User
    {
        public int id { get; set; }
        public string name { get; set; }
        public string company { get; set; }
        public string department { get; set; }
        public string? designation { get; set; }
        public string? role { get; set; }
        public string? employee_id { get; set; }
        public string? password { get; set; }
        public string? e_signature { get; set; }
        public DateTime? date_created { get; set; }
        public bool is_active { get; set; }
        public string? date_hired { get; set; } = string.Empty;
        public string? date_resignation { get; set; } = string.Empty;


        [JsonIgnore]
        public ICollection<Asset>? assets { get; set; }

        [JsonIgnore]
        public ICollection<Computer>? computer { get; set; }

        [JsonIgnore]
        public ICollection<ComputerComponents>? computer_components { get; set; }
        [JsonIgnore]
        public ICollection<ReturnItems>? ReturnItems { get; set; }

    }
}
