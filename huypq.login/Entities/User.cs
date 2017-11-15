using System.Collections.Generic;

namespace huypq.login.Entities
{
    public partial class User
    {
        public User()
        {
            ApplicationUserIDNavigation = new HashSet<Application>();
            UserScopeUserIDNavigation = new HashSet<UserScope>();
        }

        public int ID { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public long TokenValidTime { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsLocked { get; set; }
        public System.DateTime CreateDate { get; set; }
        public System.DateTime LastLogin { get; set; }

		
		public ICollection<Application> ApplicationUserIDNavigation { get; set; }
		public ICollection<UserScope> UserScopeUserIDNavigation { get; set; }
    }
}
