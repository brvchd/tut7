using System;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tut7.DTOs.Requests;
using tut7.DTOs.Responce;
using tut7.Services;
using static System.Console;

namespace tut7.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IStudentDbService _studentDbService;

        public EnrollmentsController(IStudentDbService studentDbService)
        {
            _studentDbService = studentDbService;
        }

        [HttpPost(Name = "EnrollStudent")]
        [Authorize(Roles = "employee")]
        public IActionResult EnrollStudent(EnrollStudentRequest request)
        {
            EnrollStudentResponse response = null;
            try
            {
                response = _studentDbService.EnrollStudent(request);
                if (response == null) return NotFound("Such student was not found");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
            return Created("EnrollStudent", response);
        }


        [Authorize(Roles = "employee")]
        [HttpPost(Name = "PromoteStudent")]
        public IActionResult PromoteStudents(PromoteStudentRequest request)
        {
            PromoteStudentResponse response = null;
            try
            {
                response = _studentDbService.PromoteStudents(request);
            }
            catch (SqlException sqlex)
            {
                WriteLine(sqlex.Message);
            }
            return Created("PromoteStudent", response);
        }
    }
}