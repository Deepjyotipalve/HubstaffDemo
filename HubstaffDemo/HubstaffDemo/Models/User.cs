
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HubstaffDemo.Models
{
    public class User
    {
        [Key]
        public int UId { get; set; }

        [Required(ErrorMessage = "Please Enter the Name")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Please Enter the Email")]
        [RegularExpression(@"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$", ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Required (ErrorMessage = "Please Enter the Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Required(ErrorMessage = "Please Enter the Designation")]
        public string Designation { get; set; }

        public bool IsActive { get; set; }

        public string UrlsJson { get; set; }

        //For Time 
        [NotMapped]
        public DateTime? LoginTime { get; set; }
        [NotMapped]
        public DateTime? LogoutTime { get; set; }
        [NotMapped]
        public TimeSpan TotalHours { get; set; }
        [NotMapped]
        public DateTime? LastLogoutTime { get; set; }
        [NotMapped]
        public long ElapsedMilliseconds { get; set; }
        public User()
        {
            TotalHours = TimeSpan.Zero;
        }
        [NotMapped]
        public string FormattedTotalHours
        {
            get
            {
                return CalculateTotalHours.ToString(@"hh\:mm\:ss");
            }
        }
        [NotMapped]
        public TimeSpan CalculateTotalHours
        {
            get
            {
                if (LogoutTime.HasValue && LoginTime.HasValue)
                {
                    return LogoutTime.Value.Subtract(LoginTime.Value);
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }

        }
        public int Id { get; set; }
        public virtual Organization Organization { get; set; }

    }
}