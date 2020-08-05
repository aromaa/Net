using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.API.Metadata
{
    public interface IMetadatable
    {
        public MetadataMap Metadata { get; }
    }
}
