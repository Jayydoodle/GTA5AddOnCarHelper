using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSpectreConsole
{
    public class ListOption
    {
        #region Properties

        public virtual string DisplayName { get; set; }
        public virtual Action Function { get; set; }

        #endregion

        #region Constructor

        protected ListOption()
        {
        }

        public ListOption(string displayName, Action function)
        {
            DisplayName = displayName;
            Function = function;
        }

        #endregion

        #region Public API

        public override string ToString()
        {
            return DisplayName;
        }

        #endregion
    }

    public class ListOption<T> : ListOption
    {
        #region Properties

        public new Func<T> Function { get; set; }
        public Action<T> Action { get; set; }

        #endregion

        #region Constructor

        public ListOption(string displayName, Func<T> function)
        {
            DisplayName = displayName;
            Function = function;
        }

        public ListOption(string displayName, Action<T> action)
        {
            DisplayName = displayName;
            Action = action;
        }

        #endregion
    }
}
