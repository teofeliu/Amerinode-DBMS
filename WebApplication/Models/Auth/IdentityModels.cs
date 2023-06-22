using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using WebApplication.Models.Auth;
using System.Collections.Generic;

namespace WebApplication.Models
{
    public class CustomIdentityUser : IdentityUser
    {
        public string NameIdentifier { get; set; }
    }
    public class ApplicationUser : CustomIdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);

            userIdentity.AddClaim(new Claim("WebApplication.Models.RegisterViewModel.NameIdentifier", NameIdentifier));
            userIdentity.AddClaim(new Claim("WebApplication.Models.RegisterViewModel.Email", Email));

            return userIdentity;
        }
    }

    //public class ApplicationRole : IdentityRole
    //{
    //    public ApplicationRole(): base() { }
    //    public ApplicationRole(string roleName): base(roleName) { }

    //    // Add your custom properties above

    //}

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext() : base("DataModel", throwIfV1Schema: false)
        {
            
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public System.Data.Entity.DbSet<WebApplication.Models.Application.Batch> Batches { get; set; }

        public System.Data.Entity.DbSet<WebApplication.Models.Application.Model> Models { get; set; }

        public System.Data.Entity.DbSet<WebApplication.Models.Application.BatchStock> BatchStock { get; set; }
    }
}