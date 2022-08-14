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
        private bool AllowProtectedEdit { get; set; }
        private Dictionary<string, bool> MemberValues { get; set; }

        public EditOptions(bool allowProtectedEdit = false)
        {
            MemberValues = new Dictionary<string, bool>();
            AllowProtectedEdit = allowProtectedEdit;

            IEnumerable<PropertyInfo> props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);


            if (!allowProtectedEdit)
                props = props.Where(x => !x.HasAttribute<ProtectedAttribute>());

            props.Select(x => x.Name).ToList().ForEach(x => MemberValues.Add(x, false));
        }

        public void Select(string member)
        {
            if (MemberValues.ContainsKey(member))
                MemberValues[member] = true;
        }

        public List<PropertyInfo> GetEditableProperties()
        {
            return typeof(T).GetProperties().Where(x => MemberValues.Keys.Contains(x.Name)).ToList();
        }

        public bool GetValue(string member)
        {
            MemberValues.TryGetValue(member, out bool value);
            return value;
        }

        public static EditOptions<T> Prompt(bool allowProtecedEdit = false)
        {
            MultiSelectionPrompt<EditOptionChoice> prompt = new MultiSelectionPrompt<EditOptionChoice>();
            prompt.Title = "\nSelect the fields you wish to edit";
            prompt.InstructionsText = "[grey](Press [blue]<space>[/] to toggle an option, [green]<enter>[/] to begin)[/]\n";
            prompt.PageSize = 20;

            EditOptions<T> options = new EditOptions<T>(allowProtecedEdit);

            foreach (PropertyInfo prop in options.GetEditableProperties())
            {
                EditOptionChoice choice = new EditOptionChoice(prop.Name.SplitByCase(), prop.Name);
                prompt.AddChoice(choice).Select();
            }

            List<EditOptionChoice> choices = AnsiConsole.Prompt(prompt);
            choices.ForEach(choice => options.Select(choice.Value));

            return options;
        }
    }

    public class EditOptionChoice : IMultiSelectionItem<EditOptionChoiceDetails>
    {
        public EditOptionChoice Parent { get; set; }
        public List<EditOptionChoice> Children { get; set; }
        private EditOptionChoiceDetails Details { get; set; }
        public bool IsSelected { get; set; }

        public string DisplayName
        {
            get { return Details.DisplayName; }
        }

        public string Value
        {
            get { return Details.Value; }
        }
        public EditOptionChoice(EditOptionChoiceDetails details)
        {
            Details = details;
        }

        public EditOptionChoice(string displayName, string value)
        {
            Details = new EditOptionChoiceDetails(displayName, value);
        }

        public ISelectionItem<EditOptionChoiceDetails> AddChild(string displayName, string value)
        {
            EditOptionChoiceDetails childDetails = new EditOptionChoiceDetails(displayName, value);
            return AddChild(childDetails);
        }

        public ISelectionItem<EditOptionChoiceDetails> AddChild(EditOptionChoiceDetails details)
        {
            EditOptionChoice child = new EditOptionChoice(details);
            child.Parent = this;

            if (Children == null)
                Children = new List<EditOptionChoice>();

            Children.Add(child);

            return child;
        }

        public IMultiSelectionItem<EditOptionChoiceDetails> Select()
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
    }

    public class EditOptionChoiceDetails
    {
        public string DisplayName { get; set; }
        public string Value { get; set; }

        public EditOptionChoiceDetails(string displayName, string value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }
}
