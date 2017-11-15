using System.Collections.Generic;

namespace huypq.login.Entities
{
    public partial class UserScope
    {
        public UserScope()
        {
        }

        public int ID { get; set; }
        public int UserID { get; set; }
        public int ScopeID { get; set; }

        public User UserIDNavigation { get; set; }
        public Scope ScopeIDNavigation { get; set; }
		
    }
}
