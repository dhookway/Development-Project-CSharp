using System;
using System.Collections.Generic;

namespace Interview.Web.Classes
{
    public class iProduct
    {
        public int InstanceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ProductImageUris { get; set; }
        public string ValidSkus { get; set; }
        public DateTime CreatedTimestamp { get; set; }
        public List<iInstanceCategory> Categories { get; set; }
        public List<iAttribute> Attributes { get; set; }

    }
}
