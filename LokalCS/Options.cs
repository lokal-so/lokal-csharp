using System.Collections.Generic;

namespace Lokal
{
    public class Options
    {
        public List<string> BasicAuth { get; set; } = new List<string>();
        public List<string> CIDRAllow { get; set; } = new List<string>();
        public List<string> CIDRDeny { get; set; } = new List<string>();
        public List<string> RequestHeaderAdd { get; set; } = new List<string>();
        public List<string> RequestHeaderRemove { get; set; } = new List<string>();
        public List<string> ResponseHeaderAdd { get; set; } = new List<string>();
        public List<string> ResponseHeaderRemove { get; set; } = new List<string>();
        public List<string> HeaderKey { get; set; } = new List<string>();
    }
}