using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FloatingQueue.Server.Core
{
    public class NodeCollection : List<INodeConfiguration>
    {
        public List<INodeConfiguration> Siblings { get
        {
            return this.Where(n => !n.IsMaster).ToList();
        }} 

        public INodeConfiguration Master
        {
            get
            {
                return this.Single(n => n.IsMaster);
            }
        }



    }
}
