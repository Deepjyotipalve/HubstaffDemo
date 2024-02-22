using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HubstaffDemo.Models
{
    public class LoginData
    {
        [Key]
        [Required(ErrorMessage = "Please Enter the Email")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Please Enter the Password")]
        public string Password { get; set; }
    }
}