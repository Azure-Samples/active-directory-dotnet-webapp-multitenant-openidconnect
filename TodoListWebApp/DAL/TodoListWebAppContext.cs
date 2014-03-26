using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Web;
using TodoListWebApp.Models;

namespace TodoListWebApp.DAL
{
   
    public class TodoListWebAppContext : DbContext
    {
        public TodoListWebAppContext()
            : base("TodoListWebAppContext")
        { }

        public DbSet<Todo> Todoes { get; set; }
        public DbSet<Tenant> Tenants { get; set; }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}