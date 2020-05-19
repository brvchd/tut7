using System.ComponentModel.DataAnnotations;

namespace tut7.DTOs.Requests
{
    public class PromoteStudentRequest
    {
        [Required(ErrorMessage = "Provide name of study")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Provide semester")]
        public int Semester { get; set; }
    }
}
