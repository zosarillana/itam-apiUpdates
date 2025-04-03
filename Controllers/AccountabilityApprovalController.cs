using ITAM.DataContext;
using ITAM.Models.Approval;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITAM.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class AccountabilityApprovalController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AccountabilityApprovalController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet("by-accountability/{accountabilityId}")]
        public IActionResult GetByAccountabilityId(int accountabilityId)
        {
            var approval = _context.accountability_approval
                .Where(a => a.accountability_id == accountabilityId)
                .AsSplitQuery() // Splitting into multiple queries
                .Select(a => new
                {
                    a.id,
                    a.accountability_id,
                    a.prepared_by_user_id,
                    a.approved_by_user_id,
                    a.prepared_date,
                    a.approved_date,

                    PreparedByUser = _context.Users
                        .Where(u => u.id.ToString() == a.prepared_by_user_id)
                        .Select(u => new
                        {
                            u.name,
                            u.company,
                            u.department,
                            u.designation,
                            u.employee_id,
                            u.e_signature
                        }).FirstOrDefault(),

                    ApprovedByUser = _context.Users
                        .Where(u => u.id.ToString() == a.approved_by_user_id)
                        .Select(u => new
                        {
                            u.name,
                            u.company,
                            u.department,
                            u.designation,
                            u.employee_id,
                            u.e_signature
                        }).FirstOrDefault()
                })
                .FirstOrDefault();

            if (approval == null) return NotFound();

            return Ok(approval);
        }


        [Authorize]
        [HttpPost("prepared-by-user-id")] // Check by user
        public IActionResult PreparedByUser(int accountabilityId, string userId)
        {
            var approval = new AccountabilityApproval
            {
                accountability_id = accountabilityId,
                prepared_by_user_id = userId,
                approved_by_user_id = null,
                confirmed_by_user_id = null,
                prepared_date = DateOnly.FromDateTime(DateTime.Now),
                approved_date = null,
                confirmed_date = null
            };
            _context.accountability_approval.Add(approval);
            _context.SaveChanges();
            return Ok(approval);
        }

        [Authorize]
        [HttpPut("approved-by-user-id")] // Receive by user
        public IActionResult ApprovedByUser(int id, string userId)
        {
            var approval = _context.accountability_approval.FirstOrDefault(a => a.id == id);
            if (approval == null) return NotFound();
            approval.approved_by_user_id = userId;
            approval.approved_date = DateOnly.FromDateTime(DateTime.Now);
            _context.SaveChanges();
            return Ok(approval);
        }

        [Authorize]
        [HttpPut("confirm")] // Confirm by user
        public IActionResult ConfirmByUser(int id, string userId)
        {
            var approval = _context.accountability_approval.FirstOrDefault(a => a.id == id);
            if (approval == null) return NotFound();
            approval.confirmed_by_user_id = userId;
            approval.confirmed_date = DateOnly.FromDateTime(DateTime.Now);
            _context.SaveChanges();
            return Ok(approval);
        }
    }
}
