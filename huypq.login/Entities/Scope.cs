using System.Collections.Generic;

namespace huypq.login.Entities
{
    public partial class Scope
    {
        public Scope()
        {
            UserScopeScopeIDNavigation = new HashSet<UserScope>();
        }

        public int ID { get; set; }
        public int ApplicationID { get; set; }
        public string ScopeName { get; set; }

        public Application ApplicationIDNavigation { get; set; }
		
		public ICollection<UserScope> UserScopeScopeIDNavigation { get; set; }
    }
}
