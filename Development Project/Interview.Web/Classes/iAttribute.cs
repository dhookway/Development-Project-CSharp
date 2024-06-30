using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace Interview.Web.Classes
{
    public class iAttribute
    {
        public int InstanceId { get; set; }
        public string Key {  get; set; }
        public string Value { get; set; }
    }
}
