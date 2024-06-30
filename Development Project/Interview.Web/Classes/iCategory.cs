using System;
using System.Collections.Generic;

namespace Interview.Web.Classes
{
    public class iCategory
    {
        public int InstanceID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedTimeStamp { get; set; }
        public List<iCategory> Categories { get; set; }
        public List<iAttribute> Attributes { get; set; }
    }
}
