﻿using System;
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

        public bool IsHelpOption { get { return DisplayName == GlobalConstants.SelectionOptions.Help; } }

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

        public static ListOption CancelOption(string text = null)
        {
            return new ListOption(text ?? GlobalConstants.SelectionOptions.Cancel, () => throw new Exception(GlobalConstants.Commands.CANCEL));
        }

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

        #endregion

        #region Constructor

        public ListOption(string displayName, Func<T> function)
        {
            DisplayName = displayName;
            Function = function;
        }

        #endregion

        #region Public API

        public static new ListOption<T> CancelOption(string text = null)
        {
            return new ListOption<T>(text ?? GlobalConstants.SelectionOptions.Cancel, () => throw new Exception(GlobalConstants.Commands.CANCEL));
        }

        #endregion
    }

    public class ListOption<T, U> : ListOption
    {
        #region Properties

        public new Func<T, U> Function { get; set; }

        #endregion

        #region Constructor

        public ListOption(string displayName, Func<T, U> function)
        {
            DisplayName = displayName;
            Function = function;
        }

        #endregion

        #region Public API

        public static ListOption<T, U> CancelOption()
        {
            return new ListOption<T, U>(GlobalConstants.SelectionOptions.Cancel, (T) => throw new Exception(GlobalConstants.Commands.CANCEL));
        }

        #endregion
    }
}
