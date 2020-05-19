using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using tut7.DTOs.Requests;
using tut7.DTOs.Responce;
using tut7.Model;
using tut7.Services;
using static System.Console;

namespace tut7.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IStudentDbService _studentDbService;

        public StudentsController(IConfiguration configuration, IStudentDbService studentDbService)
        {
            _studentDbService = studentDbService;
            _configuration = configuration;
        }


        [HttpGet]
        public IActionResult Login(LoginRequest request)
        {
            var student = new Student();

            using (var con = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18963;Integrated Security=True"))
            {
                using (var com = new SqlCommand())
                {
                    //login can be moved to service
                    com.Connection = con;
                    com.CommandText = "Select IndexNumber, FirstName From Student Where IndexNumber = @login AND Password = @password";
                    com.Parameters.AddWithValue("password", request.Password);
                    com.Parameters.AddWithValue("login", request.Login);
                    con.Open();

                    var dr = com.ExecuteReader();
                    if (dr.Read())
                    {
                        student.IndexNumber = dr["IndexNumber"].ToString();
                        student.FirstName = dr["FirstName"].ToString();
                    }
                    else
                    {
                        return NotFound();
                    }


                    var userclaim = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, student.IndexNumber),
                        new Claim(ClaimTypes.Name, student.FirstName),
                        new Claim(ClaimTypes.Role, "Student"),
                };

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["SecretKey"]));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                    var token = new JwtSecurityToken(
                        issuer: "Gakko",
                        audience: "Students",
                        claims: userclaim,
                        expires: DateTime.Now.AddMinutes(1),
                        signingCredentials: creds
                    );

                    student.RefreshToken = Guid.NewGuid().ToString();
                    student.RefreshTokenExpirationDate = DateTime.Now.AddDays(1);

                    com.CommandText = "Update Student set RefreshToken = @RefreshToken and RefreshTokenExpirationDate = @ExpDate";
                    com.Parameters.AddWithValue("RefreshToken", student.RefreshToken);
                    com.Parameters.AddWithValue("ExpDate", student.RefreshTokenExpirationDate);

                    return Ok(new
                    {
                        token = new JwtSecurityTokenHandler().WriteToken(token),
                        refreshToken = student.RefreshToken
                    });

                }
            }
        }

        [HttpPost("{refreshToken}/refresh")]
        public IActionResult RefreshToken([FromRoute]string refreshToken)
        {
            DateTime expirationDate = DateTime.Now;
            var student = new Student();
            using (var con = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18963;Integrated Security=True"))
            {
                using (var com = new SqlCommand())
                {
                    com.Connection = con;
                    com.CommandText = "Select * from Student Where RefreshToken = @RefreshToken";
                    com.Parameters.AddWithValue("RefreshToken", refreshToken);
                    con.Open();

                    var dr = com.ExecuteReader();
                    if (dr.Read())
                    {
                        expirationDate = Convert.ToDateTime(dr["RefreshTokenExpirationDate"].ToString());
                        student.IndexNumber = dr["IndexNumber"].ToString();
                        student.FirstName = dr["FirstName"].ToString();
                    }
                    else
                    {
                        return NotFound("Cannot find such token");
                    }

                    if (expirationDate > DateTime.Now)
                    {
                        var userclaim = new[] {
                            new Claim(ClaimTypes.NameIdentifier, student.IndexNumber),
                            new Claim(ClaimTypes.Name, student.FirstName),
                            new Claim(ClaimTypes.Role, "Student"),
                        };

                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["SecretKey"]));
                        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                        var token = new JwtSecurityToken(
                             issuer: "Gakko",
                            audience: "Students",
                            claims: userclaim,
                            expires: DateTime.Now.AddMinutes(1),
                            signingCredentials: creds
                        );

                        student.RefreshToken = Guid.NewGuid().ToString();
                        student.RefreshTokenExpirationDate = DateTime.Now.AddDays(1);

                        dr.Close();
                        com.CommandText = "Update Student set RefreshToken = @RefreshToken and RefreshTokenExpirationDate = @ExpDate";
                        com.Parameters.AddWithValue("RefreshToken", student.RefreshToken);
                        com.Parameters.AddWithValue("ExpDate", student.RefreshTokenExpirationDate);
                        
                        return Ok(new
                        {
                            token = new JwtSecurityTokenHandler().WriteToken(token),
                            refreshToken = student.RefreshToken
                        });
                    }
                    else
                    {
                        return BadRequest("Token hasn't expired yet");
                    }
                }
            }
        }


        [HttpPost(Name = "EnrollStudent")]
        [Authorize(Roles = "employee")]
        public IActionResult EnrollStudent(EnrollStudentRequest request)
        {
            EnrollStudentResponse response = null;
            try
            {
                response = _studentDbService.EnrollStudent(request);
                if (response == null) return BadRequest("Such student was not found");
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
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