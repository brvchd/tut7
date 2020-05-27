using System;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using tut7.DTOs.Requests;
using tut7.Generator;
using tut7.Model;

namespace tut7.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
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
                    var salt = String.Empty;
                    var password = String.Empty;
                    com.CommandText = "Select IndexNumber, FirstName Password, Salt From Student Where IndexNumber = @login";
                    com.Parameters.AddWithValue("login", request.Login);
                    con.Open();
                    var dr = com.ExecuteReader();

                    if (dr.Read())
                    {
                        student.IndexNumber = dr["IndexNumber"].ToString();
                        student.FirstName = dr["FirstName"].ToString();
                        password = dr["Password"].ToString();
                        salt = dr["Salt"].ToString();
                    }
                    else
                    {
                        return NotFound("Specified student was not found.");
                    }

                    var passToCompare = HashPassword.HashPass(request.Password, salt);
                    if (!password.Equals(passToCompare)) return BadRequest("Wrong login or password");
                    dr.Close();


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

                    if (expirationDate > DateTime.Now) return BadRequest("Token has expired yet.");

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
            }
        }
    }
}