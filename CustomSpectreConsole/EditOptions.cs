using SharpCompress.Common;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CustomSpectreConsole
{
    public class EditOptions<T>
    {
        #region Properties

        private bool AllowProtectedEdit { get; set; }
        private Dictionary<string, bool> MemberEditabilityLookup { get; set; }

        #endregion

        #region Constructor

        public EditOptions(bool canEdit = true, bool allowProtectedEdit = false)
        {
            MemberEditabilityLookup = new Dictionary<string, bool>();
            AllowProtectedEdit = allowProtectedEdit;

            IEnumerable<PropertyInfo> props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (!allowProtectedEdit)
                props = props.Where(x => !x.HasAttribute<ProtectedAttribute>());

            props.Select(x => x.Name).ToList().ForEach(x => MemberEditabilityLookup.Add(x, canEdit));
        }

        #endregion

        #region Public API

        public void Select(string member)
        {
            if (MemberEditabilityLookup.ContainsKey(member))
                MemberEditabilityLookup[member] = true;
        }

        public List<PropertyInfo> GetEditableProperties()
        {
            return GetProperties().Where(x => MemberEditabilityLookup[x.Name]).ToList();
        }

        public bool GetValue(string member)
        {
            MemberEditabilityLookup.TryGetValue(member, out bool value);
            return value;
        }

        public static EditOptions<T> Prompt(bool allowProtecedEdit = false)
        {
            MultiSelectionPrompt<EditOptionChoice<T>> prompt = new MultiSelectionPrompt<EditOptionChoice<T>>();
            prompt.Title = "\nSelect the fields you wish to edit";
            prompt.InstructionsText = "[grey](Press [blue]<space>[/] to toggle an option, [green]<enter>[/] to begin)[/]\n";
            prompt.PageSize = 20;

            EditOptions<T> options = new EditOptions<T>(false, allowProtecedEdit);

            foreach (PropertyInfo prop in options.GetProperties())
            {
                EditOptionChoice<T> choice = new EditOptionChoice<T>(prop.Name.SplitByCase(), prop.Name);
                prompt.AddChoice(choice).Select();
            }

            List<EditOptionChoice<T>> choices = AnsiConsole.Prompt(prompt);
            choices.ForEach(choice => options.Select(choice.Value));

            return options;
        }

        #endregion

        #region Private API

        private List<PropertyInfo> GetProperties()
        {
            return typeof(T).GetProperties().Where(x => MemberEditabilityLookup.Keys.Contains(x.Name)).ToList();
        }

        #endregion
    }

    public class EditOptionChoice<T> : IMultiSelectionItem<EditOptionChoiceDetails<T>>
    {
        #region Properties

        public EditOptionChoice<T> Parent { get; set; }
        public List<EditOptionChoice<T>> Children { get; set; }
        private EditOptionChoiceDetails<T> Details { get; set; }
        public bool IsSelected { get; set; }

        public string DisplayName
        {
            get { return Details.DisplayName; }
        }

        public string Value
        {
            get { return Details.Value; }
        }

        public Func<T, bool> Action
        {
            get { return Details.Action; }
        }

        #endregion

        #region Constructor

        public EditOptionChoice(EditOptionChoiceDetails<T> details)
        {
            Details = details;
        }

        public EditOptionChoice(string displayName, string value)
        {
            Details = new EditOptionChoiceDetails<T>(displayName, value);
        }

        public EditOptionChoice(string displayName, Func<T, bool> action)
        {
            Details = new EditOptionChoiceDetails<T>(displayName, action);
        }

        #endregion

        #region Public API

        public ISelectionItem<EditOptionChoiceDetails<T>> AddChild(string displayName, string value)
        {
            EditOptionChoiceDetails<T> childDetails = new EditOptionChoiceDetails<T>(displayName, value);
            return AddChild(childDetails);
        }

        public ISelectionItem<EditOptionChoiceDetails<T>> AddChild(EditOptionChoiceDetails<T> details)
        {
            EditOptionChoice<T> child = new EditOptionChoice<T>(details);
            child.Parent = this;

            if (Children == null)
                Children = new List<EditOptionChoice<T>>();

            Children.Add(child);

            return child;
        }

        public IMultiSelectionItem<EditOptionChoiceDetails<T>> Select()
        {
            IsSelected = true;

            if (Parent != null)
                Parent.Select();

            return this;
        }

        public override string ToString()
        {
            return Details.DisplayName;
        }

        #endregion
    }

    public class EditOptionChoiceDetails<T>
    {
        #region Properties

        public string DisplayName { get; set; }
        public string Value { get; set; }
        public Func<T, bool> Action { get; set; }   

        #endregion

        #region Constructor

        public EditOptionChoiceDetails(string displayName, string value)
        {
            DisplayName = displayName;
            Value = value;
        }

        public EditOptionChoiceDetails(string displayName, Func<T, bool> action)
        {
            DisplayName = displayName;
            Action = action;
        }

        #endregion
    }
}
