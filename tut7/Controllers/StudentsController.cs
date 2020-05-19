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

namespace tut7.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public StudentsController(IConfiguration configuration)
        {
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
                }
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


            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                refreshToken = student.RefreshToken
            });
        }


        [HttpPost(Name = "EnrollStudent")]
        [Authorize(Roles = "employee")]
        public IActionResult EnrollStudent(EnrollStudentRequest request)
        {
            var response = new EnrollStudentResponse();
            using (var con = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18963;Integrated Security=True"))
            {
                using (var com = new SqlCommand())
                {
                    com.Connection = con;
                    com.CommandText = "Select * From Studies Where Name = @Name";
                    com.Parameters.AddWithValue("Name", request.Studies);
                    con.Open();

                    var trans = con.BeginTransaction();
                    com.Transaction = trans;
                    var dr = com.ExecuteReader();

                    if (!dr.Read())
                    {
                        dr.Close();
                        trans.Rollback();
                        return BadRequest("Specified studies does not exist");
                    }

                    int idStudy = (int)dr["IdStudy"];

                    dr.Close();

                    com.CommandText = "Select * From Enrollment Where Semester = 1 And IdStudy = @idStudy";
                    int IdEnrollment = (int)dr["IdEnrollemnt"] + 1;
                    com.Parameters.AddWithValue("IdStudy", idStudy);
                    dr = com.ExecuteReader();

                    if (dr.Read())
                    {
                        dr.Close();
                        com.CommandText = "Select MAX(idEnrollment) as 'idEnrollment' From Enrollment";
                        dr = com.ExecuteReader();
                        dr.Close();
                        DateTime StartDate = DateTime.Now;
                        com.CommandText = "Insert Into Enrollment(IdEnrollment, Semester, IdStudy, StartDate) Values (@IdEnrollemnt, 1, @IdStudy, @StartDate)";
                        com.Parameters.AddWithValue("IdEnrollemnt", IdEnrollment);
                        com.Parameters.AddWithValue("StartDate", StartDate);
                        com.ExecuteNonQuery();
                    }

                    dr.Close();

                    com.CommandText = "Select * From Student Where IndexNumber=@IndexNumber";
                    com.Parameters.AddWithValue("IndexNumber", request.IndexNumber);
                    dr = com.ExecuteReader();

                    if (!dr.Read())
                    {
                        dr.Close();
                        com.CommandText = "Insert Into Student(IndexNumber, FirstName, LastName, Birthdate, IdEnrollment) Value (@IndexNumber, @FirstName, @LastName, @BirthDate, @IdEnrollment)";
                        com.Parameters.AddWithValue("FirstName", request.FirstName);
                        com.Parameters.AddWithValue("LastName", request.LastName);
                        com.Parameters.AddWithValue("BirthDate", request.BirthDate);
                        com.Parameters.AddWithValue("IdEnrollment", IdEnrollment);
                        com.ExecuteNonQuery();
                        dr.Close();

                        response.Semester = 1;

                    }
                    else
                    {
                        dr.Close();
                        trans.Rollback();
                        return BadRequest("You can't add student with the same index number");
                    }

                    trans.Commit();
                }
            }

            return Created("EnrollStudent", response);
        }


        [Authorize(Roles = "employee")]
        [HttpPost(Name = "PromoteStudent")]
        public PromoteStudentResponse PromoteStudents(PromoteStudentRequest request)
        {
            PromoteStudentResponse response = null;
            using (var con = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18822;Integrated Security=True"))
            {
                using (SqlCommand com = new SqlCommand())
                {
                    com.Connection = con;
                    con.Open();
                    com.CommandText = "PromoteStudent";
                    com.CommandType = System.Data.CommandType.StoredProcedure;

                    com.Parameters.AddWithValue("Name", request.Name);

                    com.Parameters.AddWithValue("Semester", request.Semester);
                    var dr = com.ExecuteReader();
                    if (dr.Read())
                    {
                        dr.Close();
                        request.Name = dr["Name"].ToString();
                        request.Semester = (int)dr["Semester"];

                        dr = com.ExecuteReader();
                        dr.Read();
                        response = new PromoteStudentResponse();
                        response.Name = dr["Name"].ToString();
                        response.Semester = (int)dr["Semester"];

                        dr.Close();
                    }
                }
                return response;
            }
        }
    }
}