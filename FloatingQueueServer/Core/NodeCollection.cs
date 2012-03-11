using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FloatingQueue.Server.Core
{
    public class NodeCollection : List<INodeConfiguration>
    {
        public List<INodeConfiguration> Siblings
        {
            get
            {
                return this.Where(n => n.ServerId != Server.Configuration.ServerId).ToList();
            }
        }

        public INodeConfiguration Self
        {
            get
            {
                return this.Single(n => n.ServerId == Server.Configuration.ServerId);
            }
        } 

        public INodeConfiguration Master
        {
            get
            {
                return this.Single(n => n.IsMaster);
            }
        }



    }
}
