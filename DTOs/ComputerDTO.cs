using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using static ITAM.DTOs.UpdateComputerDto;


namespace ITAM.DTOs
{
    public class ComputerDto
    {
        public int id { get; set; }
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
        public int? owner_id { get; set; }
        public string li_description { get; set; }

        public List<string> history { get; set; }
        public DateTime date_created { get; set; }
        public List<ComputerComponentDto> Components { get; set; }
        public OwnerDTO owner { get; set; } // Owner details

    }

    public class UpdateComputerDto
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
        public string ram { get; set; }
        public string ssd { get; set; }
        public string hdd { get; set; }
        public string gpu { get; set; }
        public string board { get; set; }
        public string size { get; set; }
        public string color { get; set; }
        public string serial_no { get; set; }
        public string po { get; set; }
        public string warranty { get; set; }
        public decimal cost { get; set; }
        public string remarks { get; set; }
        public List<string> history { get; set; }
        public string li_description { get; set; }

        // Computer components
        [JsonIgnore]
        public List<ComputerComponentDto>? computer_components { get; set; } = null;

        public class ComputerComponentDto
        {
            public int id { get; set; }
            public string type { get; set; }
            public string description { get; set; }
            public string asset_barcode { get; set; }
            public string? status { get; set; }
            public List<string>? history { get; set; }
            public int? owner_id { get; set; }
            public int? computer_id { get; set; }
            public string? uid { get; set; }
        }

        public class CreateComputerComponentsDTO
        {
            public int id { get; set; }
            public string type { get; set; }
            public string description { get; set; }
            public string? uid { get; set; }
            public string? date_acquired { get; set; }
            public decimal? cost { get; set; }

        }



        public class ComputerWithOwnerDTO
        {
            public int id { get; set; }
            public string type { get; set; }
            public string date_acquired { get; set; }
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
            public List<string> history { get; set; }
            public string asset_image { get; set; }
            public int? owner_id { get; set; }
            public string? status { get; set; }
            public bool is_deleted { get; set; }
            public DateTime? date_created { get; set; }
            public DateTime? date_modified { get; set; }
            public OwnerDTO owner { get; set; } // Owner details
        }


        public class ComputerWithOwnerDTO_v2
        {
            public int id { get; set; }
            public string type { get; set; }
            public DateTime? date_acquired { get; set; }  // Changed to nullable DateTime
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
            public List<string> history { get; set; } // List of strings
            public string asset_image { get; set; }
            public int? owner_id { get; set; }
            public bool is_deleted { get; set; }
            public DateTime? date_created { get; set; }
            public DateTime? date_modified { get; set; }
            public OwnerDTO owner { get; set; } // Owner details
        }

        public class VacantComputerDto
        {
            public int id { get; set; }
            public string asset_barcode { get; set; }
            public string type { get; set; }
            public DateTime? date_acquired { get; set; }
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
            public List<string> history { get; set; }
            public string asset_image { get; set; }
            public bool is_deleted { get; set; }
            public DateTime? date_created { get; set; }
            public DateTime? date_modified { get; set; }
        }
    }
}
