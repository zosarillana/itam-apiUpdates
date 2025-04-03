using ITAM.DataContext;
using ITAM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITAM.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class BusinessUnitsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BusinessUnitsController(AppDbContext context)
        {
            _context = context;
        }
        [Authorize]
        // GET: api/BusinessUnits
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BusinessUnit>>> Getbusiness_unit()
        {
            return await _context.business_unit.ToListAsync();
        }

        [Authorize]
        // GET: api/BusinessUnits/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BusinessUnit>> GetBusinessUnit(int id)
        {
            var businessUnit = await _context.business_unit.FindAsync(id);

            if (businessUnit == null)
            {
                return NotFound();
            }

            return businessUnit;
        }

        [Authorize]
        // PUT: api/BusinessUnits/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBusinessUnit(int id, BusinessUnit businessUnit)
        {
            if (id != businessUnit.id)
            {
                return BadRequest();
            }

            _context.Entry(businessUnit).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BusinessUnitExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [Authorize]
        // POST: api/BusinessUnits
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<BusinessUnit>> PostBusinessUnit(BusinessUnit businessUnit)
        {
            _context.business_unit.Add(businessUnit);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetBusinessUnit", new { id = businessUnit.id }, businessUnit);
        }

        [Authorize]
        // DELETE: api/BusinessUnits/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBusinessUnit(int id)
        {
            var businessUnit = await _context.business_unit.FindAsync(id);
            if (businessUnit == null)
            {
                return NotFound();
            }

            _context.business_unit.Remove(businessUnit);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool BusinessUnitExists(int id)
        {
            return _context.business_unit.Any(e => e.id == id);
        }
    }
}
