using System.Collections.Generic;

namespace huypq.login.Entities
{
    public partial class Application
    {
        public Application()
        {
            RedirectUriApplicationIDNavigation = new HashSet<RedirectUri>();
            ScopeApplicationIDNavigation = new HashSet<Scope>();
        }

        public int ID { get; set; }
        public int UserID { get; set; }
        public bool RequireLogin { get; set; }

        public User UserIDNavigation { get; set; }
		
		public ICollection<RedirectUri> RedirectUriApplicationIDNavigation { get; set; }
		public ICollection<Scope> ScopeApplicationIDNavigation { get; set; }
    }
}
