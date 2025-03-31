using ITAM.Models;

namespace ITAM.DTOs
{
    public class UserAccountabilityListDto
    {
        public int owner_id { get; set; }
        public List<int>? asset_ids { get; set; }  // ✅ Change to List<int>
        public List<int>? computer_ids { get; set; } // ✅ Change to List<int>

        // Adding the missing properties
        public int employee_id { get; set; } // Add employee_id
        public string name { get; set; } // Add name
        public string department { get; set; } // Add department
        public string designation { get; set; }
        public string date_hired { get; set; }  // Keep as string
        public string date_resignation { get; set; }  // Keep as string
        public string company { get; set; } // Add company
    }




    public class UserAccountabilityListDTO
    {
        public int id { get; set; }
        public string accountability_code { get; set; }
        public string tracking_code { get; set; }
        public int owner_id { get; set; }
        public bool is_active { get; set; }
        public OwnerDto owner_details { get; set; }  // Added owner details
        public List<Asset> asset_details { get; set; }
        public List<Computer> computer_details { get; set; }
    }

    // DTOs with snake_case
    public class AssetAccountabilityDto
    {
        public int id { get; set; }
        public string type { get; set; }
        public string asset_barcode { get; set; }
        public string brand { get; set; }
        public string model { get; set; }
        public string size { get; set; }
        public string color { get; set; }
        public string serial_no { get; set; }
        public string po { get; set; }
        public string warranty { get; set; }
        public decimal cost { get; set; }
        public string remarks { get; set; }
        public string li_description { get; set; }
        public List<int> history { get; set; }
        public string asset_image { get; set; }
        public int? owner_id { get; set; }
        public bool is_deleted { get; set; }
        public DateTime? date_created { get; set; }
        public DateTime? date_modified { get; set; }
        public string date_acquired { get; set; }
        public string status { get; set; }
        public List<int>? root_history { get; set; } = new List<int>();
        public List<OwnerDetailsDto> history_details { get; set; }


    }

    public class ComputerAccountabilityDto
    {
        public int id { get; set; }
        public string type { get; set; }
        public string asset_barcode { get; set; }
        public string brand { get; set; }
        public string model { get; set; }
        public string size { get; set; }
        public string color { get; set; }
        public string serial_no { get; set; }
        public string po { get; set; }
        public string warranty { get; set; }
        public decimal cost { get; set; }
        public string remarks { get; set; }
        public string li_description { get; set; }
        public List<string> history { get; set; }
        public string asset_image { get; set; }
        public int? owner_id { get; set; }
        public bool is_deleted { get; set; }
        public DateTime? date_created { get; set; }
        public DateTime? date_modified { get; set; }
        public string date_acquired { get; set; }
        public string status { get; set; }
        //public string ram_description { get; set; }
        //public string ssd_description { get; set; }
        //public string hdd_description { get; set; }
        //public string gpu_description { get; set; }
        public string assigned_assets { get; set; }
        public List<AssetAccountabilityDto> AssignedAssetDetails { get; set; } // New property for asset details
        public List<OwnerDetailsDto> history_details { get; set; }
        public object components { get; set; }




    }

    public class OwnerDetailsDto
    {
        public int id { get; set; }
        public string name { get; set; }
        public string company { get; set; }
        public string department { get; set; }
        public string employee_id { get; set; }
        public string designation { get; set; }
    }
}
