using ServiceModule.Componet;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceModule.AppModule
{
    public class Root 
    {
        private HashSet<Component> components = new HashSet<Component>();
        private Dictionary<Type, Component> componentDict = new Dictionary<Type, Component>();
        public T AddComponent<T>()where T : Component, new()
        {
            return AddComponent<T>(null);
        }

        public T AddComponent<T>(params object[] objList) where T : Component, new()
        {
            Type type = typeof(T);
            if (componentDict.ContainsKey(type))
            {
                throw new Exception("重复添加组件+++" + type.Name);
            }
            T t = new T();
            t.Root = this;
            var aa = type.GetMethod("Init", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic| System.Reflection.BindingFlags.Public);
            if(aa!= null)
            {
                aa.Invoke(t, objList);
            }
            var aa1 = type.GetMethod("SetBaseProper", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (aa1 != null)
            {
                aa1.Invoke(t, objList);
            }
            componentDict.Add(type, t);
            return t;
        }


        public void RemoveComponent<K>() where K : Component
        {
            RemoveComponent(typeof(K));
        }

        public void RemoveComponent(Type type)
        {
            Component component;
            if (!this.componentDict.TryGetValue(type, out component))
            {
                return;
            }

            this.components?.Remove(component);
            this.componentDict.Remove(type);
            component?.OnDelete();
        }

        public K GetComponent<K>() where K : Component
        {
            Component component;
            if (!this.componentDict.TryGetValue(typeof(K), out component))
            {
                return default(K);
            }
            return (K)component;
        }
    }
}
