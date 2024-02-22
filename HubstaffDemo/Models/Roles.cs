using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HubstaffDemo.Models
{
    public class Roles
    {
        [Key]
        public string Email { get; set; }
        public string Role { get; set; }
    }
}