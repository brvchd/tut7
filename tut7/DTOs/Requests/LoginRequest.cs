using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace tut7.DTOs.Requests
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Provide both login and password")]
        public string Login { get; set; }
        [Required(ErrorMessage = "Provide both login and password")]
        public string Password { get; set; }
    }
}
