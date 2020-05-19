using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tut7.DTOs.Requests;
using tut7.DTOs.Responce;

namespace tut7.Services
{
    public interface IStudentDbService
    {
        public PromoteStudentResponse PromoteStudents(PromoteStudentRequest request);
        public EnrollStudentResponse EnrollStudent(EnrollStudentRequest request);

    }
}
