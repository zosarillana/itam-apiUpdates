namespace ITAM.Models
{
    public class UserAccountabilityList
    {
        public int id { get; set; }
        public string accountability_code { get; set; }  // This is a string, not an int
        public string tracking_code { get; set; }
        public int owner_id { get; set; }
        public string? asset_ids { get; set; }
        public string? computer_ids { get; set; }
        public DateTime? date_created { get; set; }
        public DateTime? date_modified { get; set; }
        public bool is_deleted { get; set; } = false;
        public bool is_active { get; set; }


        public User owner { get; set; }
        public List<Asset> assets { get; set; }
        public List<Computer> computer { get; set; }
        public ICollection<ReturnItems>? ReturnItems { get; set; }

    }
}
