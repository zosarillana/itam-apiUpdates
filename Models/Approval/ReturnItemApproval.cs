namespace ITAM.Models.Approval
{
    public class ReturnItemApproval
    {
        public int id { get; set; }
        public int accountability_id { get; set; }
        public string? checked_by_user_id { get; set; }
        public string? received_by_user_id { get; set; }
        public string? confirmed_by_user_id { get; set; }
        public DateOnly? checked_date { get; set; }
        public DateOnly? received_date { get; set; } // Nullable
        public DateOnly? confirmed_date { get; set; } // Nullable
        public UserAccountabilityList accountability_list { get; set; }

    }
}
