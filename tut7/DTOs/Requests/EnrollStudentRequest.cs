using System.ComponentModel.DataAnnotations;


namespace tut7.DTOs.Requests
{
    public class EnrollStudentRequest
    {
        [Required(ErrorMessage = "Provide index number of student")]
        [MaxLength(100)]
        [RegularExpression("^s[0-9]+$")]
        public string IndexNumber { get; set; }
        [Required(ErrorMessage = "Provide first name of student")]
        [MaxLength(100)]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Provide last name of student")]
        [MaxLength(100)]
        public string LastName { get; set; }
        [Required(ErrorMessage = "Provide birth date of student")]
        public string BirthDate { get; set; }
        [Required(ErrorMessage = "Provide name of studies")]
        public string Studies { get; set; }
        [Required(ErrorMessage ="Create new password")]
        public string Password { get; set; }
    }
}
