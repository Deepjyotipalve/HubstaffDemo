using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HubstaffDemo.Models
{
    public class Organization
    {
        [Key]
        public int Id { get; set; }

        [Required (ErrorMessage ="Please Enter the Email")]
        [RegularExpression(@"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$", ErrorMessage = "Invalid Email Address")]

        public string Email { get; set; }

        [Required(ErrorMessage = "Please Enter the Password")]
        public string Password { get; set; }
        
        [Required(ErrorMessage = "Please Enter the Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please Enter the OrganizationName")]
        public string OrganizationName { get; set; }
        [Required(ErrorMessage = "Please Enter the TeamSize")]
        public string TeamSize { get; set; }

        [Required(ErrorMessage = "Please Enter the City")]
        public string City { get; set; }

        public bool IsActive { get; set; }
    }
}