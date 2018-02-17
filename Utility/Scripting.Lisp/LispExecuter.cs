using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml;
using System.IO;
using Bee.Eee.Utility.Extensions.ListExtensions;
using Bee.Eee.Utility.Logging;

namespace Bee.Eee.Utility.Scripting.Lisp
{
    public class LispExecuter
    {
        private ILogger _logger;
        private ICategories _categories;
        private Dictionary<string, string> _env;
        private Dictionary<string, object> _var;
        private Dictionary<string, LispRuntimeCommand> _commands;

        public LispExecuter(ILogger logger, ICategories categories)
        {
            _logger = logger.CreateSub("Lisp");
            _categories = categories ?? throw new ArgumentNullException(nameof(categories));
            _logger.RegisterCategory(_categories.ScriptLogging, "Script Logging");            

            _env = new Dictionary<string, string>();
            _var = new Dictionary<string, object>();
            _commands = new Dictionary<string, LispRuntimeCommand>();

            RegisterCommand("Get", EX_GetVariable);
            RegisterCommand("Set", EX_SetVariable);
            RegisterCommand("GetEnvironment", EX_GetEnvironment);
            RegisterCommand("If", EX_If);
            RegisterCommand("Equals", EX_Equal);
            RegisterCommand("NotEquals", (cmd, p) => !((bool)EX_Equal(cmd, p)));
            RegisterCommand("Loop", EX_Loop);
            RegisterCommand("While", EX_While);
            RegisterCommand("Not", EX_Not);
            RegisterCommand("And", Ex_And);
            RegisterCommand("Or", Ex_Or);
            RegisterCommand("Add", Ex_Add);
            RegisterCommand("Sub", Ex_Sub);
            RegisterCommand("GreaterThan", Ex_GreaterThan);
            RegisterCommand("GreaterThanEqual", Ex_GreaterThanEqual);
            RegisterCommand("LessThan", Ex_LessThan);
            RegisterCommand("LessThanEqual", Ex_LessThanEqual);
        }

        protected ILogger Logger { get { return _logger; } }

        public void RegisterCommand(string commandName, LispCommandDelegate command)
        {
            RegisterCommand(new LispRuntimeCommand() { CommandName = commandName, Command = command });
        }

        public void RegisterCommand(LispRuntimeCommand command)
        {
            if (_commands.ContainsKey(command.CommandName))
            {
                throw new Exception(string.Format("Command '{0}' already exits cannot override command.", command.CommandName));
            }
            else
            {
                _commands.Add(command.CommandName, command);
                _logger.LogIf(_categories.ScriptLogging, Level.Debug, $"Registered command: {command.CommandName}");
            }
        }

        public static void Execute(LispList program, Dictionary<string, string> environmentVariables, ILogger logger, ICategories categories)
        {
            LispExecuter executer = new LispExecuter(logger, categories);
            executer.SetEnvironment(environmentVariables);
            try
            {
                executer.Run<object>(program);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }

        public void SetEnvironment(Dictionary<string, string> environmentVariables)
        {
            foreach (var kv in environmentVariables)
                _env.Add(kv.Key, kv.Value);
        }

        public object Run(LispItem item)
        {
            object v = null;
            switch (item)
            {
                case LispList ll: v = RunList(ll); break;
                case LispString ls: v = ls.value; break;
                case LispDouble ld: v = ld.value; break;
                case LispInt li: v = li.value; break;
                case LispSymbol lsym: v = lsym.name; break;
                default:v = null;break;
            }

            return v;
        }

        public T Run<T>(LispItem item)
        {
            object v = this.Run(item);
            return (T)v;
        }

        public object RunList(LispList program)
        {
            object result = null;

            LispItem first = program.items.First();

            if (first is LispSymbol)
            {
                try
                {
                    LispSymbol sym = first as LispSymbol;
                    LispRuntimeCommand cmd;
                    if (_commands.TryGetValue(sym.name, out cmd))
                    {
                        result = cmd.Command(cmd, program);
                        _logger.LogIf(_categories.ScriptLogging, Level.Debug, $"Run {first.line}:{first.position} Cmd:{cmd.CommandName}==>{(result ?? (object)"null")}");
                    }
                    else
                        throw new LispParseException("Unknown command symbol '{0}' Line: {1}:{2}", sym.name, sym.line, sym.position);
                }
                catch (Exception ex)
                {
                    if (!(ex is LispParseException))
                    {
                        throw new LispParseException("{0} Line {1}:{2}", ex.Message, first.line, first.position);
                    }
                    else
                        throw;
                }
            }
            else if (first is LispList)
            {
                foreach (LispItem item in program.items)
                {
                    if (item is LispList)
                    {
                        result = Run(item as LispList);
                    }
                    else
                        throw new LispParseException("Expected a LispList but got a {0} Line: {0}:{1}", item.GetType(), item.line, item.position);
                }
            }
            else
                throw new LispParseException("Can only execute lists and symbols. Line: {0}:{1}", first.line, first.position);

            return result;
        }


        private object EX_SetVariable(LispRuntimeCommand cmd, LispList program)
        {
            CheckParameterCount(cmd, program, 2);

            // get parameters
            string variableName = Run(program.items[1]) as string;
            object variableValue = Run(program.items[2]);

            CheckParameterType(cmd, 1, program, variableName, typeof(string), false);

            _var[variableName] = variableValue;

            return variableValue;
        }


        private object EX_GetVariable(LispRuntimeCommand cmd, LispList program)
        {
            CheckParameterCount(cmd, program, 1);

            string variableName = Run(program.items[1]) as string;
            CheckParameterType(cmd, 1, program, variableName, typeof(string), false);

            object value = null;
            if (_var.TryGetValue(variableName, out value))
                return value;
            else
                throw new LispParseException("GetVariable command failed.  Variable '{0}' doesn't exist. Line: {1}:{2}",
                    variableName, program.items[1].line, program.items[1].position);
        }


        private object EX_GetEnvironment(LispRuntimeCommand cmd, LispList program)
        {
            CheckParameterCount(cmd, program, 1);

            string variableName = Run(program.items[1]) as string;
            CheckParameterType(cmd, 1, program, variableName, typeof(string), false);

            string value = null;
            if (_env.TryGetValue(variableName, out value))
                return value;
            else
                throw new LispParseException("GetEnvironment command failed.  Variable '{0}' doesn't exist. Line: {1}:{2}",
                    variableName, program.items[1].line, program.items[1].position);
        }

        private object EX_If(LispRuntimeCommand cmd, LispList program)
        {
            CheckParameterCount(cmd, program, 2, 3);

            LispItem conditionalStatement = program.items[1];
            LispItem trueStatement = program.items[2];

            // run the condition
            object conditionResult = Run(conditionalStatement);

            CheckParameterType(cmd, 1, program, conditionResult, typeof(bool), false);

            if ((bool)conditionResult)
            {
                return Run(trueStatement);
            }
            else if (program.items.Count == 4)
            {
                LispItem falseStatement = program.items[3];
                return Run(falseStatement);
            }
            else
                return null;
        }

        private object EX_Equal(LispRuntimeCommand cmd, LispList program)
        {
            CheckParameterCount(cmd, program, 2);

            object value1 = Run(program.items[1]);
            object value2 = Run(program.items[2]);

            if (value1 == value2)
                return true;

            if (value1 != null)
                return value1.Equals(value2);

            return false;
        }

        private object EX_Loop(LispRuntimeCommand cmd, LispList program)
        {
            object retVal = null;

            if (program.items.Count < 3)
                throw new LispParseException("'{0}' command must have at least 2 parameters. Line: {2}:{3}", cmd.CommandName, program.line, program.position);

            long loopCount = GetLongValue(cmd, program, 1, false);

            for (long i = 0; i < loopCount; i++)
            {
                for (int j = 2; j < program.items.Count; j++)
                {
                    retVal = Run(program.items[j]);
                }
            }

            return retVal;
        }

        private object EX_While(LispRuntimeCommand cmd, LispList program)
        {
            object retVal = null;
            if (program.items.Count < 2)
                throw new LispParseException("'{0}' command must have at least {1} parameter. Line: {2}:{3}",
                        cmd.CommandName, 1, program.line, program.position);

            LispItem conditionalStatement = program.items[1];

            // body of the while loop
            start:
            object conditionalResult = Run(conditionalStatement);
            CheckParameterType(cmd, 1, program, conditionalResult, typeof(bool), false);
            if ((bool)conditionalResult)
            {
                for (int i = 2; i < program.items.Count; i++)
                {
                    retVal = Run(program.items[i]);
                }
                goto start;
            }

            return retVal;
        }

        private object EX_Not(LispRuntimeCommand cmd, LispList program)
        {
            CheckParameterCount(cmd, program, 1);
            object result = Run(program.items[1]);
            CheckParameterType(cmd, 1, program, result, typeof(bool), false);
            return !((bool)result);
        }

        private object Ex_Or(LispRuntimeCommand cmd, LispList program)
        {
            CheckParameterCount(cmd, program, 2);
            object leftResult = Run(program.items[1]);
            CheckParameterType(cmd, 1, program, leftResult, typeof(bool), false);
            if ((bool)leftResult)
                return true;

            object rightResult = Run(program.items[2]);
            CheckParameterType(cmd, 2, program, leftResult, typeof(bool), false);
            return (bool)rightResult;
        }

        private object Ex_And(LispRuntimeCommand cmd, LispList program)
        {
            CheckParameterCount(cmd, program, 2);
            object leftResult = Run(program.items[1]);
            CheckParameterType(cmd, 1, program, leftResult, typeof(bool), false);
            if ((bool)leftResult)
            {
                object rightResult = Run(program.items[2]);
                CheckParameterType(cmd, 2, program, leftResult, typeof(bool), false);
                return (bool)rightResult;
            }
            else
                return false;
        }

        private object Ex_Add(LispRuntimeCommand cmd, LispList program)
        {
            if (program.items.Count - 1 < 2)
                throw new LispParseException("'{0}' command must have at least {1} parameters. Line: {2}:{3}",
                    cmd.CommandName, 2, program.line, program.position);

            long accumulator = GetLongValue(cmd, program, 1, false);
            for (int i = 2; i < program.items.Count; i++)
            {
                long number = GetLongValue(cmd, program, i, false);
                accumulator += number;
            }

            return accumulator;
        }

        private object Ex_Sub(LispRuntimeCommand cmd, LispList program)
        {
            if (program.items.Count - 1 < 2)
                throw new LispParseException("'{0}' command must have at least {1} parameters. Line: {2}:{3}",
                    cmd.CommandName, 2, program.line, program.position);

            long accumulator = GetLongValue(cmd, program, 1, false);
            for (int i = 2; i < program.items.Count; i++)
            {
                long number = GetLongValue(cmd, program, i, false);
                accumulator -= number;
            }

            return accumulator;
        }

        private object Ex_GreaterThan(LispRuntimeCommand cmd, LispList program)
        {
            CheckParameterCount(cmd, program, 2);
            long a = GetLongValue(cmd, program, 1, false);
            long b = GetLongValue(cmd, program, 2, false);
            return a > b;
        }

        private object Ex_GreaterThanEqual(LispRuntimeCommand cmd, LispList program)
        {
            CheckParameterCount(cmd, program, 2);
            long a = GetLongValue(cmd, program, 1, false);
            long b = GetLongValue(cmd, program, 2, false);
            return a >= b;
        }

        private object Ex_LessThan(LispRuntimeCommand cmd, LispList program)
        {
            CheckParameterCount(cmd, program, 2);
            long a = GetLongValue(cmd, program, 1, false);
            long b = GetLongValue(cmd, program, 2, false);
            return a < b;
        }

        private object Ex_LessThanEqual(LispRuntimeCommand cmd, LispList program)
        {
            CheckParameterCount(cmd, program, 2);
            long a = GetLongValue(cmd, program, 1, false);
            long b = GetLongValue(cmd, program, 2, false);
            return a <= b;
        }



        /// <summary>
        /// Gets a value from the parameter.  If that parameter needs sto be executed to get the value then it is.
        /// </summary>
        /// <param name="cmd">The lisp command that is being executed</param>
        /// <param name="program">The list of parameters</param>
        /// <param name="index">The index of the parameter</param>
        /// <param name="allowNull">True then a null value is allowed, false means it is not allowed and will throw an exception.</param>
        /// <returns></returns>
        protected string GetStringValue(LispRuntimeCommand cmd, LispList program, int index, bool allowNull)
        {
            object v = Run(program.items[index]);
            CheckParameterType(cmd, index, program, v, typeof(string), allowNull);
            if (null == v)
                return string.Empty;
            else
                return (string)v;
        }

        protected long GetLongValue(LispRuntimeCommand cmd, LispList program, int index, bool allowNull)
        {
            object v = Run(program.items[index]);
            CheckParameterType(cmd, index, program, v, typeof(long), allowNull);
            if (null == v)
                return 0;
            else
                return (long)v;
        }


        protected static void CheckParameterCount(LispRuntimeCommand cmd, LispList program, params int[] expectedCounts)
        {
            if (!expectedCounts.Contains(program.items.Count - 1))
            {
                if (expectedCounts.Length == 1)
                    throw new LispParseException("'{0}' command must have exactly {1} parameters. Line: {2}:{3}",
                        cmd.CommandName, expectedCounts[0], program.line, program.position);
                else
                    throw new LispParseException("'{0}' command expects {1} parameters. Line: {2}:{3}",
                        cmd.CommandName, expectedCounts.ToDelimited("", "", " or "), program.line, program.position);
            }
        }

        protected static void CheckParameterType(LispRuntimeCommand cmd, int paramIndex, LispList program, object item, Type expectedType, bool allowNull)
        {
            if (null == item)
            {
                if (allowNull)
                    return;
                else
                    throw new Exception(string.Format("'{0}' parameter {1} cannot be null. Line: {2}:{3}", cmd.CommandName, paramIndex, program.line, program.position));
            }

            if (item.GetType() != expectedType)
                throw new Exception(string.Format("'{0}' expected parameter {1} to be of type {2}. Line: {3}:{4}",
                    cmd.CommandName, paramIndex, expectedType, program.line, program.position));
        }
    }

    public delegate object LispCommandDelegate(LispRuntimeCommand cmd, LispList list);

    public class LispRuntimeCommand
    {
        public string CommandName;
        public LispCommandDelegate Command;
    }
}
