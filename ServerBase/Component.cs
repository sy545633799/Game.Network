using ServiceModule.AppModule;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceModule.Componet
{
    public abstract class Component
    {
        public Root Root {
            get;
            set;
        }

        public K GetComponent<K>() where K : Component
        {
            return Root.GetComponent<K>();
        }

        public virtual void OnDelete()
        {

        }
    }
}
