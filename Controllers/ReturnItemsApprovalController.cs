using ITAM.DataContext;
using ITAM.Models.Approval;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.Controllers
{
  
    [Route("api/[controller]")]
    [ApiController]
    public class ReturnItemsApprovalController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ReturnItemsApprovalController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet("by-accountability/{accountabilityId}")]
        public IActionResult GetByAccountabilityId(int accountabilityId)
        {
            var approval = _context.return_item_approval
                .Where(a => a.accountability_id == accountabilityId)
                .Select(a => new
                {
                    a.id,
                    a.accountability_id,
                    a.checked_by_user_id,
                    a.received_by_user_id,
                    a.confirmed_by_user_id,
                    a.checked_date,
                    a.received_date,
                    a.confirmed_date,

                    CheckedByUser = _context.Users
                        .Where(u => u.id.ToString() == a.checked_by_user_id)
                        .Select(u => new
                        {
                            u.name,
                            u.company,
                            u.department,
                            u.designation,
                            u.employee_id,
                            u.e_signature
                        }).FirstOrDefault(),

                    ReceivedByUser = _context.Users
                        .Where(u => u.id.ToString() == a.received_by_user_id)
                        .Select(u => new
                        {
                            u.name,
                            u.company,
                            u.department,
                            u.designation,
                            u.employee_id,
                            u.e_signature
                        }).FirstOrDefault(),

                    ConfirmedByUser = _context.Users
                        .Where(u => u.id.ToString() == a.confirmed_by_user_id)
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
        [HttpPost("check")] // Check by user
        public IActionResult CheckByUser(int accountabilityId, string userId)
        {
            var approval = new ReturnItemApproval
            {
                accountability_id = accountabilityId,
                checked_by_user_id = userId,
                received_by_user_id = null,
                confirmed_by_user_id = null,
                checked_date = DateOnly.FromDateTime(DateTime.Now),
                received_date = null,
                confirmed_date = null
            };
            _context.return_item_approval.Add(approval);
            _context.SaveChanges();
            return Ok(approval);
        }

        [Authorize]
        [HttpPut("receive")] // Receive by user
        public IActionResult ReceiveByUser(int id, string userId)
        {
            var approval = _context.return_item_approval.FirstOrDefault(a => a.id == id);
            if (approval == null) return NotFound();
            approval.received_by_user_id = userId;
            approval.received_date = DateOnly.FromDateTime(DateTime.Now);
            _context.SaveChanges();
            return Ok(approval);
        }

        [Authorize]
        [HttpPut("confirm")] // Confirm by user
        public IActionResult ConfirmByUser(int id, string userId)
        {
            var approval = _context.return_item_approval.FirstOrDefault(a => a.id == id);
            if (approval == null) return NotFound();
            approval.confirmed_by_user_id = userId;
            approval.confirmed_date = DateOnly.FromDateTime(DateTime.Now);
            _context.SaveChanges();
            return Ok(approval);
        }
    }
}
