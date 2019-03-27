using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SF.Foundation.Analytics
{
    public class ContactDetails
    {
        public string identifier { get; set; }

        public string identifierSource { get; set; }

        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
    }
}