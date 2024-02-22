
using HubstaffDemo.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace HubstaffDemo.Data
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext() : base("name=DefaultConnection")
        {
        }

        // DbSet properties represent database tables
        public DbSet<User> Users { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Admin>Admin { get; set; }



    }
 
}