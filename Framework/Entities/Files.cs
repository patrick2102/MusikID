using System;
using System.Collections.Generic;

namespace Framework
{
    public partial class Files
    {
        public Files()
        {
            OnDemandResults = new HashSet<OnDemandResults>();
        }

        public long Id { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public float? Duration { get; set; }
        public string Ref { get; set; }

        public virtual ICollection<OnDemandResults> OnDemandResults { get; set; }
    }
}
