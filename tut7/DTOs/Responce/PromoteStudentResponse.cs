using System.ComponentModel.DataAnnotations;

namespace tut7.DTOs.Responce
{
    public class PromoteStudentResponse
    {
        [Required(ErrorMessage = "Provide name of study")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Provide semester")]
        public int Semester { get; set; }
    }
}
