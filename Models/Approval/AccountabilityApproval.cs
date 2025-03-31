namespace ITAM.Models.Approval
{
    public class AccountabilityApproval
    {
        public int id { get; set; }
        public int? accountability_id { get; set; }
        public string? prepared_by_user_id { get; set; }
        public string? approved_by_user_id { get; set; }
        public string? confirmed_by_user_id { get; set; }
        public DateOnly? prepared_date { get; set; }
        public DateOnly? approved_date { get; set; } // Nullable
        public DateOnly? confirmed_date { get; set; } // Nullable

        public UserAccountabilityList accountability_list { get; set; }
    }
}
