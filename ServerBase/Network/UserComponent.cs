using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Base
{
    /// <summary>
    /// 用户组件
    /// </summary>
    public abstract class UserComponent
    {
        internal NetUser netUser;
        public NetUser User
        {
            get
            {
                return netUser;
            }
        }

        /// <summary>
        /// 被添加到user
        /// </summary>
        public virtual void OnAdd() { }

        /// <summary>
        /// 从user移除
        /// </summary>
        public virtual void OnRemove() { }
    }
}
