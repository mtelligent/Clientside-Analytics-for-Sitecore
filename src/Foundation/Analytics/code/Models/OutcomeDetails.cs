using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SF.Foundation.Analytics
{
    public class OutcomeDetails
    {
        public OutcomeDetails()
        {
            //defaults
            currency = "USD";
            value = 0;
        }

        public string outcome { get; set; }

        public string outcomeId { get; set; }

        public string currency { get; set; }

        public decimal value { get; set; }

        public string pageId { get; set; }
    }
}