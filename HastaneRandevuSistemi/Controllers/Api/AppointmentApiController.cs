using HastaneRandevuSistemi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HastaneRandevuSistemi.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AppointmentApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/AppointmentApi
        [HttpGet]
        public async Task<IActionResult> GetAppointments()
        {
            var appointments = await _context.Appointments
                .Select(a => new { a.Id, a.PatientName, a.PatientSurname, a.AppointmentDate, a.Status, a.IsCollected })
                .OrderByDescending(a => a.AppointmentDate)
                .Take(20)
                .ToListAsync();

            return Ok(appointments);
        }
    }
}
