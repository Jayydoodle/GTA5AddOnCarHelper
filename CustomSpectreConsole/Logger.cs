using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSpectreConsole
{
    public static class Logger
    {
        public static void LogException(this Exception exception)
        {
            AnsiConsole.WriteLine();

            Rule rule = new Rule(string.Format("\n[red]Exception[/]\n")).DoubleBorder<Rule>();
            AnsiConsole.Write(rule);
            AnsiConsole.WriteException(exception);

            if (exception is AggregateException)
            {
                foreach (var e in (exception as AggregateException).InnerExceptions)
                    AnsiConsole.WriteException(e);
            }
            else
            {
                Exception e = exception.InnerException;

                while(e != null) 
                {
                    AnsiConsole.WriteException(e);
                    e = e.InnerException;
                }
            }

            rule = new Rule(string.Format("\n[red]Exception End[/]\n")).DoubleBorder<Rule>();
            AnsiConsole.Write(rule);

            AnsiConsole.WriteLine();
        }

        public static void LogWarning(string message)
        {
            AnsiConsole.WriteLine();

            Rule rule = new Rule(string.Format("\n[orange1]Warning[/]\n")).DoubleBorder<Rule>();
            AnsiConsole.Write(rule);

            AnsiConsole.MarkupLine(message);

            rule = new Rule(string.Format("\n[orange1]Warning End[/]\n")).DoubleBorder<Rule>();
            AnsiConsole.Write(rule);

            AnsiConsole.WriteLine();
        }
    }
}
