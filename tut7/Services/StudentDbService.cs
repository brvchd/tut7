using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using tut7.DTOs.Requests;
using tut7.DTOs.Responce;
using tut7.Generator;

namespace tut7.Services
{
    public class StudentDbService : IStudentDbService
    {
        public EnrollStudentResponse EnrollStudent(EnrollStudentRequest request)
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
                        return null;
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
                        com.CommandText = "Insert Into Enrollment(IdEnrollment, Semester, IdStudy, StartDate, Password) Values (@IdEnrollemnt, 1, @IdStudy, @StartDate, @Password)";
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
                        var salt = SaltGenerator.GenerateSalt();
                        dr.Close();
                        com.CommandText = "Insert Into Student(IndexNumber, FirstName, LastName, Birthdate, IdEnrollment, Password, Salt) Value (@IndexNumber, @FirstName, @LastName, @BirthDate, @IdEnrollment, @Password, @Salt)";
                        com.Parameters.AddWithValue("FirstName", request.FirstName);
                        com.Parameters.AddWithValue("LastName", request.LastName);
                        com.Parameters.AddWithValue("BirthDate", request.BirthDate);
                        com.Parameters.AddWithValue("IdEnrollment", IdEnrollment);
                        com.Parameters.AddWithValue("Password", HashPassword.HashPass(request.Password,salt));
                        com.Parameters.AddWithValue("Salt", salt);

                        com.ExecuteNonQuery();
                        dr.Close();

                        response.Semester = 1;

                    }
                    else
                    {
                        dr.Close();
                        trans.Rollback();
                        throw new Exception("You can't add student with the same index number");
                    }

                    trans.Commit();
                }
            }

            return response;
        }

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

