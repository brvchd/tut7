using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tut7.Model
{
    public class Student
    {
        public string IndexNumber { get; set; }
        public string FirstName { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpirationDate { get; set; }
    }
}
