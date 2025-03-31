namespace ITAM.DTOs
{
    public class AssetDTO
    {
    }

    public class AddAssetDto
    {
        public string? user_name { get; set; }
        public string? company { get; set; }
        public string? department { get; set; }
        public string? employee_id { get; set; }
        public string? type { get; set; }
        public string? date_acquired { get; set; }
        public string? asset_barcode { get; set; }
        public List<string>? assigned_assets { get; set; }
        public string? brand { get; set; }
        public string? model { get; set; }
        public string? ram { get; set; }
        public string? ssd { get; set; }
        public string? hdd { get; set; }
        public string? gpu { get; set; }
        public string? board { get; set; }
        public string? size { get; set; }
        public string? color { get; set; }
        public string? serial_no { get; set; }
        public string? po { get; set; }
        public string? warranty { get; set; }
        public decimal? cost { get; set; }  // Nullable decimal
        public string? remarks { get; set; }
        public List<string>? history { get; set; }
        public string? li_description { get; set; }
        public int? owner_id { get; set; }  // Nullable integer
    }


    public class AssignOwnerDto
    {
        public int asset_id { get; set; }
        public int owner_id { get; set; }
    }

    public class AssignOwnerforComputerDto
    {
        public int computer_id { get; set; }
        public int owner_id { get; set; }
    }

    public class CreateAssetDto
    {
        public string type { get; set; }
        public string asset_barcode { get; set; }
        public string brand { get; set; }
        public string model { get; set; }
        public string ram { get; set; }
        public string ssd { get; set; }
        public string hdd { get; set; }
        public string gpu { get; set; }
        public string size { get; set; }
        public string color { get; set; }
        public string serial_no { get; set; }
        public string po { get; set; }
        public string warranty { get; set; }
        public decimal cost { get; set; }
        public string remarks { get; set; }
        public string li_description { get; set; }
        public string date_acquired { get; set; }
        public string asset_image { get; set; }
    }

    public class ReassignAssetDto
    {
        public string user_name { get; set; }
        public string company { get; set; }
        public string department { get; set; }
        public string employee_id { get; set; }
    }

    public class UpdateAssetDto
    {
        public string user_name { get; set; }
        public string company { get; set; }
        public string department { get; set; }
        public string? employee_id { get; set; }
        public string type { get; set; }
        public string date_acquired { get; set; }
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
        public List<string> history { get; set; }
        public string li_description { get; set; }
    }

    public class AssetWithOwnerDTO
    {
        public int id { get; set; }
        public string type { get; set; }
        public string date_acquired { get; set; }
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
        public string status { get; set; }
        public int? owner_id { get; set; }
        public bool is_deleted { get; set; }
        public DateTime? date_created { get; set; }
        public DateTime? date_modified { get; set; }
        public OwnerDTO owner { get; set; }
        public List<OwnerDTO> historyUsers { get; set; }  // Add this new property

    }

    public class OwnerDTO
    {
        public int id { get; set; }
        public string name { get; set; }
        public string company { get; set; }
        public string department { get; set; }
        public string employee_id { get; set; }
    }


    public class AssetWithHistoryDTO
    {
        public int id { get; set; }
        public string type { get; set; }
        public DateTime date_acquired { get; set; }
        public string asset_barcode { get; set; }
        public string brand { get; set; }
        public string model { get; set; }
        public string ram { get; set; }
        public string ssd { get; set; }
        public string hdd { get; set; }
        public string gpu { get; set; }
        public string size { get; set; }
        public string color { get; set; }
        public string serial_no { get; set; }
        public string po { get; set; }
        public string warranty { get; set; }
        public string cost { get; set; }
        public string remarks { get; set; }
        public string li_description { get; set; }
        public List<string> history { get; set; }  // History as list of owner IDs
        public string asset_image { get; set; }
        public int? owner_id { get; set; }
        public bool is_deleted { get; set; }
        public DateTime date_created { get; set; }
        public DateTime date_modified { get; set; }
        public string status { get; set; }
        public OwnerDTO owner { get; set; }
        public List<OwnerDTO> historyUsers { get; set; }  // List of users in the history
    }


    public class PullInAssetRequest
    {
        public int computer_id { get; set; }  // ID of the computer to assign the asset
        public List<int> asset_ids { get; set; }      // ID of the asset being pulled in
        public string? remarks { get; set; }
    }
}
