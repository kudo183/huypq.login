using System.Collections.Generic;

namespace huypq.login.Entities
{
    public partial class RedirectUri
    {
        public RedirectUri()
        {
        }

        public int ID { get; set; }
        public int ApplicationID { get; set; }
        public string Uri { get; set; }

        public Application ApplicationIDNavigation { get; set; }
		
    }
}
