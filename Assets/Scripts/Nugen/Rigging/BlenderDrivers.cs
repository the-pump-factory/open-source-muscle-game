using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;

namespace nugen.rigging
{

    [ExecuteInEditMode]
    public class BlenderDrivers : MonoBehaviour
    {

        public enum Operator
        {

            NULL, ADD, SUBTRACT, DIVIDE, MULTIPLY, GREATERTHAN, LESSTHAN, GREATERTHANOREQUAL, LESSTHANOREQUAL, EQUAL, NOTEQUAL

        }

        public enum Statement
        {

            NULL, IF, ELSE, FOR

        }

        public enum Function
        {

            NULL, GROUP, MIN, MAX, CLAMP, POW, ABS

        }

        public enum TransformParam
        {

            LOC_X, LOC_Y, LOC_Z, ROT_X, ROT_Y, ROT_Z

        }

        public enum TransformSpace
        {

            LOCAL_SPACE, WORLD_SPACE

        }

        public enum Command
        {

            NULL, NEW_DRIVER, END_DRIVER, NEW_VAR, END_VAR, EXPR

        }

        public enum Variable
        {

            TransformVar, InternalVar

        }

        public static TransformParam ParamFromString(string str)
        {

            return (TransformParam)System.Enum.Parse(typeof(TransformParam), str);

        }

        public static TransformSpace SpaceFromString(string str)
        {

            return (TransformSpace)System.Enum.Parse(typeof(TransformSpace), str);

        }

        public static Variable VariableFromString(string str)
        {

            return (Variable)System.Enum.Parse(typeof(Variable), str);

        }

        public static Command CommandFromString(string str, out string output)
        {

            output = str;

            string command_string = str.Replace(' ', '_');

            if (System.Enum.TryParse(command_string, out Command command)) 
            {

                output = "";

                return command; 
            
            }

            foreach(string commandName in System.Enum.GetNames(typeof(Command)))
            {

                int i = command_string.IndexOf(commandName);

                if (i < 0) continue;

                i = i + commandName.Length;

                output = str.Substring(i, str.Length - i);

                return (Command)System.Enum.Parse(typeof(Command), commandName);

            }

            return Command.NULL;

        }

        public bool execute = false;

        public string assetPath = "file.txt";

        public float maxWeight = 100;

        public DriverController controller;

        public bool invertRx = false;
        public bool invertRy = false;
        public bool invertRz = true;

        public bool debug = false;

        public string[] debugLines;

        void Update()
        {

            if (!execute) return;

            execute = false;

            string[] lines = debug? debugLines : ReadLines(Application.dataPath + "/" + assetPath);

            if (lines != null)
            {

                Debug.Log("Evaluating " + lines.Length + " lines");

                Evaluate(lines, controller, maxWeight, invertRx, invertRy, invertRz);

            }

        }

        public static void Evaluate(string[] lines, DriverController controller, float maxWeight = 100, bool invertRx = false, bool invertRy = false, bool invertRz = true)
        {

            if (controller == null)
            {

                Debug.LogError("Driver controller not set!");

                return;

            }

            if (controller.drivers == null) controller.drivers = new List<DriverLogic.IDriver>();

            string driver_name = "";

            List<DriverLogic.IVariable> variables = null;

            List<DriverLogic.Operation> operations = null;

            DriverLogic.Expression expression = new DriverLogic.Expression();

            bool TryGetIndex(string str, string search, out int index)
            {

                index = str.IndexOf(search);

                return index >= 0;

            }

            bool TryGetLastIndex(string str, string search, out int index)
            {

                index = str.LastIndexOf(search);

                return index >= 0;

            }

            bool TryGetOperator(string str, bool last, out Operator sym, out int index, out int length)
            {

                sym = Operator.NULL;

                index = -1;

                int index_ = -1;

                length = 0;

                if (!last ? (TryGetIndex(str, "+", out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, "+", out index_) && index_ > index))
                {

                    index = index_;

                    sym = Operator.ADD;

                    length = 1;

                }

                if (!last ? (TryGetIndex(str, "-", out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, "-", out index_) && index_ > index))
                {

                    index = index_;

                    sym = Operator.SUBTRACT;

                    length = 1;

                }

                if (!last ? (TryGetIndex(str, "*", out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, "*", out index_) && index_ > index))
                {

                    index = index_;

                    sym = Operator.MULTIPLY;

                    length = 1;

                }

                if (!last ? (TryGetIndex(str, "/", out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, "/", out index_) && index_ > index))
                {

                    index = index_;

                    sym = Operator.DIVIDE;

                    length = 1;

                }

                if (!last ? (TryGetIndex(str, ">", out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, ">", out index_) && index_ > index))
                {

                    index = index_;

                    sym = Operator.GREATERTHAN;

                    length = 1;

                }

                if (!last ? (TryGetIndex(str, "<", out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, "<", out index_) && index_ > index))
                {

                    index = index_;

                    sym = Operator.LESSTHAN;

                    length = 1;

                }

                if (!last ? (TryGetIndex(str, ">=", out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, ">=", out index_) && index_ > index))
                {

                    index = index_;

                    sym = Operator.GREATERTHANOREQUAL;

                    length = 2;

                }

                if (!last ? (TryGetIndex(str, "<=", out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, "<=", out index_) && index_ > index))
                {

                    index = index_;

                    sym = Operator.LESSTHANOREQUAL;

                    length = 2;

                }

                if (!last ? (TryGetIndex(str, "==", out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, "==", out index_) && index_ > index))
                {

                    index = index_;

                    sym = Operator.EQUAL;

                    length = 2;

                }

                if (!last ? (TryGetIndex(str, "!=", out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, "!=", out index_) && index_ > index))
                {

                    index = index_;

                    sym = Operator.NOTEQUAL;

                    length = 2;

                }

                return sym != Operator.NULL;

            }

            bool TryGetStatement(string str, bool last, out Statement sym, out int index, out int length)
            {

                sym = Statement.NULL;

                index = -1;

                int index_ = -1;

                length = 0;

                if (!last ? (TryGetIndex(str, " if ", out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, " if ", out index_) && index_ > index))
                {

                    index = index_;

                    sym = Statement.IF;

                    length = 4;

                }

                if (!last ? (TryGetIndex(str, " if", out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, " if", out index_) && index_ > index))
                {

                    index = index_;

                    sym = Statement.IF;

                    length = 3;

                }

                if (!last ? (TryGetIndex(str, " else ", out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, " else ", out index_) && index_ > index))
                {

                    index = index_;

                    sym = Statement.ELSE;

                    length = 6;

                }

                if (!last ? (TryGetIndex(str, " else", out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, " else", out index_) && index_ > index))
                {

                    index = index_;

                    sym = Statement.ELSE;

                    length = 5;

                }

                if (!last ? (TryGetIndex(str, " for ", out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, " for ", out index_) && index_ > index))
                {

                    index = index_;

                    sym = Statement.FOR;

                    length = 5;

                }

                return sym != Statement.NULL;

            }

            bool TryGetForLoop(string str, bool last, out int index, out int length, out int in_index, out int in_length, out int list_start, out int list_end)
            {

                index = in_index = list_start = list_end = -1;

                length = in_length = 0;

                if (!last ? TryGetIndex(str, " for ", out index) : TryGetLastIndex(str, " for ", out index))
                {

                    length = 5;

                    if (!last ? TryGetIndex(str, " in ", out in_index) : TryGetLastIndex(str, " in ", out in_index))
                    {

                        in_length = 4;

                    }
                    else if (!last ? TryGetIndex(str, " in", out in_index) : TryGetLastIndex(str, " in", out in_index))
                    {

                        in_length = 3;

                    }
                    else return false;

                    string brackets = str.Substring(in_index + in_length);

                    if (TryGetIndex(brackets, "[", out list_start))
                    {

                        list_end = Utils.GetIndexOfClosingBracket(brackets, list_start);

                        list_start += in_index + in_length;
                        list_end += in_index + in_length;

                    }
                    else return false;

                    return true;

                }

                return false;

            }

            string[] functionNames = { "group", "min", "max", "clamp", "pow", "abs" };

            bool TryGetFunction(string str, bool last, out Function sym, out int index, out int length)
            {

                sym = Function.NULL;

                index = -1;

                int index_ = -1;

                length = 0;

                if (!last ? (TryGetIndex(str, functionNames[0], out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, functionNames[0], out index_) && index_ > index))
                {

                    index = index_;

                    sym = Function.GROUP;

                    length = functionNames[0].Length;

                }

                if (!last ? (TryGetIndex(str, functionNames[1], out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, functionNames[1], out index_) && index_ > index))
                {

                    index = index_;

                    sym = Function.MIN;

                    length = functionNames[1].Length;

                }

                if (!last ? (TryGetIndex(str, functionNames[2], out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, functionNames[2], out index_) && index_ > index))
                {

                    index = index_;

                    sym = Function.MAX;

                    length = functionNames[2].Length;

                }

                if (!last ? (TryGetIndex(str, functionNames[3], out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, functionNames[3], out index_) && index_ > index))
                {

                    index = index_;

                    sym = Function.CLAMP;

                    length = functionNames[3].Length;

                }

                if (!last ? (TryGetIndex(str, functionNames[4], out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, functionNames[4], out index_) && index_ > index))
                {

                    index = index_;

                    sym = Function.POW;

                    length = functionNames[4].Length;

                }

                if (!last ? (TryGetIndex(str, functionNames[5], out index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, functionNames[5], out index_) && index_ > index))
                {

                    index = index_;

                    sym = Function.ABS;

                    length = functionNames[5].Length;

                }

                return sym != Function.NULL;

            }

            /*bool TryGetVar(string str, bool last, out int varIndex, out int index, out int length)
            {

                varIndex = -1;

                index = -1;

                length = 0;

                for(int a = 0; a < variables.Count; a++)
                {

                    DriverLogic.IVariable var_ = variables[a];

                    if (!last ? (TryGetIndex(str, var_.GetName(), out int index_) && (index_ < index || index < 0)) : (TryGetLastIndex(str, var_.GetName(), out index_) && index_ > index))
                    {

                        index = index_;

                        varIndex = a;

                        length = var_.GetName().Length;

                    }

                }

                return varIndex >= 0;

            }*/

            int VarFromString(string str)
            {

                str = str.RemoveWhitespace();

                int varIndex = variables.Count;

                if (float.TryParse(str, out float varVal))
                {

                    variables.Add(new DriverLogic.ConstantFloat("const_" + varIndex, varVal));

                }
                else
                {

                    varIndex = 0;

                    for (int a = 0; a < variables.Count; a++)
                    {

                        DriverLogic.IVariable var_ = variables[a];

                        if (var_.GetName() == str)
                        {

                            varIndex = a;

                            break;

                        }

                    }

                }

                return varIndex;

            }

            string InterpretNegativeConstants(string expr)
            {

                string new_expr = expr;

                int prevIndex = -1;
                int prevLength = 0;

                int sub_index = prevIndex + prevLength + 1;
                string sub_expr = new_expr.Substring(sub_index);

                while (TryGetOperator(sub_expr, false, out Operator sym, out int index, out int length))
                {

                    string l_side = sub_expr.Substring(0, index);

                    string mid = sub_expr.Substring(index, length);

                    string r_side = sub_expr.Substring(index + length);

                    bool isNegative = false;

                    if (sym == Operator.SUBTRACT)
                    {

                        string l_side_temp = new_expr.Substring(0, sub_index) + l_side;

                        for (int i = l_side_temp.Length - 1; i >= 0; i--)
                        {

                            if (l_side_temp[i] == '(' || l_side_temp[i] == ',' || l_side_temp[i] == '[' || l_side_temp[i] == ']' || 
                                (TryGetOperator(l_side_temp.Substring(0, i + 1), true, out Operator sym_, out int index_, out int length_) && (index_ + length_ - 1 == i)) || 
                                (TryGetStatement(l_side_temp.Substring(0, i + 1), true, out Statement state_, out index_, out length_) && (index_ + length_ - 1 == i))) {

                                isNegative = true;

                                break;

                            }

                            if (l_side_temp[i] != ' ') break;

                        }

                    }

                    if (isNegative)
                    {

                        int index_ = -1;
                        int length_ = 0;

                        l_side = new_expr.Substring(0, sub_index) + l_side;

                        prevIndex = index + sub_index - 1;
                        prevLength = length;

                        bool grouped = false;

                        for (int i = l_side.Length - 1; i >= 0; i--)
                        {

                            index_ = -1;
                            length_ = 0;

                            if (l_side[i] == '(' || l_side[i] == ',' || l_side[i] == '[' || l_side[i] == ']' ||
                                (TryGetOperator(l_side.Substring(0, i + 1), true, out Operator sym_, out index_, out length_) && (index_ + length_ - 1 == i)) || 
                                (TryGetStatement(l_side.Substring(0, i + 1), true, out Statement state_, out index_, out length_) && (index_ + length_ - 1 == i)))
                            {

                                int ind_ = length_ > 0 ? index_ : i;

                                l_side = (ind_ >= 0 ? l_side.Substring(0, ind_ + Mathf.Max(0, length_ - 1) + 1) : "") + "group(0" + (ind_ + Mathf.Max(0, length_ - 1) + 1 < l_side.Length ? l_side.Substring(ind_ + Mathf.Max(0, length_ - 1) + 1) : "");

                                grouped = true;

                                break;

                            }

                            if (l_side[i] == ')')
                            {

                                int o = Utils.GetIndexOfOpeningParenthesis(l_side, i) - 1;

                                bool reset(out int i1, out int i2)
                                {

                                    i1 = i2 = 0;

                                    return o >= 0;

                                }

                                while (reset(out index_, out length_) && l_side[o] != ' ' && l_side[o] != '(' && l_side[o] != ',' && l_side[o] != '[' && l_side[o] != ']' &&
                                    !(TryGetOperator(l_side.Substring(0, o + 1), true, out sym_, out index_, out length_) && (index_ + length_ - 1 == o)) && 
                                    !(TryGetStatement(l_side.Substring(0, o + 1), true, out state_, out index_, out length_) && (index_ + length_ - 1 == o)))
                                {

                                    o--;

                                }

                                int ind_ = length_ > 0 ? index_ : o;

                                l_side = (ind_ >= 0 ? l_side.Substring(0, ind_ + Mathf.Max(0, length_ - 1) + 1) : "") + "group(0" + (ind_ + Mathf.Max(0, length_ - 1) + 1 < l_side.Length ? l_side.Substring(ind_ + Mathf.Max(0, length_ - 1) + 1) : "");

                                grouped = true;

                                break;

                            }

                            if (l_side[i] == ']')
                            {

                                int o = Utils.GetIndexOfOpeningBracket(l_side, i) - 1;

                                bool reset(out int i1, out int i2)
                                {

                                    i1 = i2 = 0;

                                    return o >= 0;

                                }

                                while (reset(out index_, out length_) && l_side[o] != ' ' && l_side[o] != '(' && l_side[o] != ',' && l_side[o] != '[' && l_side[o] != ']' &&
                                    !(TryGetOperator(l_side.Substring(0, o + 1), true, out sym_, out index_, out length_) && (index_ + length_ - 1 == o)) && 
                                    !(TryGetStatement(l_side.Substring(0, o + 1), true, out state_, out index_, out length_) && (index_ + length_ - 1 == o)))
                                {

                                    o--;

                                }

                                int ind_ = length_ > 0 ? index_ : o;

                                l_side = (ind_ >= 0 ? l_side.Substring(0, ind_ + Mathf.Max(0, length_ - 1) + 1) : "") + "group(0" + (ind_ + Mathf.Max(0, length_ - 1) + 1 < l_side.Length ? l_side.Substring(ind_ + Mathf.Max(0, length_ - 1) + 1) : "");

                                grouped = true;

                                break;

                            }

                        }

                        if (!grouped) l_side = "group(0" + l_side;

                        prevIndex += 7;

                        grouped = false;

                        for (int i = 0; i < r_side.Length; i++)
                        {

                            if (r_side[i] == ')' || r_side[i] == ',' || r_side[i] == ']' || (TryGetOperator(r_side.Substring(i), false, out Operator sym_, out index_, out length_) && (index_ == 0)) || (TryGetStatement(r_side.Substring(i), false, out Statement state_, out index_, out length_) && (index_ == 0)))
                            {

                                r_side = r_side.Substring(0, i) + ")" + (i < r_side.Length ? r_side.Substring(i) : "");

                                grouped = true;

                                break;

                            }

                            if (r_side[i] == '(')
                            {

                                int c = Utils.GetIndexOfClosingParenthesis(r_side, i);

                                r_side = r_side.Substring(0, c) + ")" + (c < r_side.Length ? r_side.Substring(c) : "");

                                grouped = true;

                                break;

                            }

                            if (r_side[i] == '[')
                            {

                                int c = Utils.GetIndexOfClosingBracket(r_side, i);

                                r_side = r_side.Substring(0, c) + ")" + (c < r_side.Length ? r_side.Substring(c) : "");

                                grouped = true;

                                break;

                            }

                        }

                        if (!grouped) r_side = r_side + ")";

                        new_expr = l_side + mid + r_side;

                    }
                    else
                    {

                        prevIndex = index + sub_index - 1;
                        prevLength = length;

                    }

                    sub_index = prevIndex + prevLength + 1;
                    sub_expr = new_expr.Substring(sub_index);

                }

                Debug.Log("INTERP NEG: " + new_expr);

                return new_expr;

            }

            Operator[] prioritizedOperators = new Operator[] { Operator.MULTIPLY, Operator.DIVIDE };

            bool IsPriority(Operator op)
            {

                for (int a = 0; a < prioritizedOperators.Length; a++) if (prioritizedOperators[a] == op) return true;

                return false;

            }

            Operator[] booleanOperators = new Operator[] { Operator.GREATERTHAN, Operator.LESSTHAN, Operator.GREATERTHANOREQUAL, Operator.LESSTHANOREQUAL };

            bool IsBoolean(Operator op)
            {

                for (int a = 0; a < booleanOperators.Length; a++) if (booleanOperators[a] == op) return true;

                return false;

            }

            string PrioritizeOperators(string expr)
            {

                string new_expr = expr;

                string sub_expr = new_expr;

                int prevIndex = -1;
                int prevLength = 0;

                int sub_index = prevIndex + prevLength + 1;
                sub_expr = new_expr.Substring(sub_index);

                while (TryGetOperator(sub_expr, false, out Operator sym, out int index, out int length))
                {

                    int index_ = 0;
                    int length_ = 0;

                    string l_side = sub_expr.Substring(0, index);

                    string mid = sub_expr.Substring(index, length);

                    string r_side = sub_expr.Substring(index + length);

                    if (IsBoolean(sym))
                    {

                        l_side = new_expr.Substring(0, sub_index) + l_side;

                        prevIndex = index + sub_index - 1;
                        prevLength = length;

                        bool grouped = false;

                        for (int i = l_side.Length - 1; i >= 0; i--)
                        {

                            index_ = 0;
                            length_ = 0;

                            if (l_side[i] == '(' || l_side[i] == ',' || l_side[i] == '[' || l_side[i] == ']' ||
                                (TryGetStatement(l_side.Substring(0, i + 1), true, out Statement state_, out index_, out length_) && (index_ + length_ - 1) == i))
                            {

                                int ind_ = length_ > 0 ? index_ : i;

                                l_side = (ind_ >= 0 ? l_side.Substring(0, ind_ + Mathf.Max(0, length_ - 1) + 1) : "") + "group(" + (ind_ + Mathf.Max(0, length_ - 1) + 1 < l_side.Length ? l_side.Substring(ind_ + Mathf.Max(0, length_ - 1) + 1) : "");

                                grouped = true;

                                break;

                            }

                            if (l_side[i] == ')')
                            {

                                int o = Utils.GetIndexOfOpeningParenthesis(l_side, i) - 1;

                                bool reset(out int i1, out int i2)
                                {

                                    i1 = i2 = 0;

                                    return o >= 0;

                                }

                                while (reset(out index_, out length_) && l_side[o] != ' ' && l_side[o] != '(' && l_side[o] != ',' && l_side[o] != '[' && l_side[o] != ']' &&
                                    !(TryGetStatement(l_side.Substring(0, o + 1), true, out state_, out index_, out length_) && (index_ + length_ - 1) == o))
                                {

                                    o--;

                                }

                                int ind_ = length_ > 0 ? index_ : o;

                                l_side = (ind_ >= 0 ? l_side.Substring(0, ind_ + Mathf.Max(0, length_ - 1) + 1) : "") + "group(" + (ind_ + Mathf.Max(0, length_ - 1) + 1 < l_side.Length ? l_side.Substring(ind_ + Mathf.Max(0, length_ - 1) + 1) : "");

                                grouped = true;

                                break;

                            }

                        }

                        if (!grouped) l_side = "group(" + l_side;

                        prevIndex += 6;

                        grouped = false;

                        for (int i = 0; i < r_side.Length; i++)
                        {

                            if (r_side[i] == ')' || r_side[i] == ',' || r_side[i] == '[' || r_side[i] == ']' ||
                                (TryGetStatement(r_side.Substring(i), false, out Statement state_, out index_, out length_) && index_ == 0))
                            {

                                r_side = r_side.Substring(0, i) + ")" + (i < r_side.Length ? r_side.Substring(i) : "");

                                grouped = true;

                                break;

                            }

                            if (r_side[i] == '(')
                            {

                                int c = Utils.GetIndexOfClosingParenthesis(r_side, i);

                                r_side = r_side.Substring(0, c) + ")" + (c < r_side.Length ? r_side.Substring(c) : "");

                                grouped = true;

                                break;

                            }

                        }

                        if (!grouped) r_side = r_side + ")";

                        new_expr = l_side + ")" + mid + "group(" + r_side;

                        prevIndex += 1;

                    }
                    else
                    {

                        prevIndex = index + sub_index - 1;
                        prevLength = length;

                    }

                    sub_index = prevIndex + prevLength + 1;
                    sub_expr = new_expr.Substring(sub_index);

                }

                prevIndex = -1;
                prevLength = 0;

                sub_index = prevIndex + prevLength + 1;
                sub_expr = new_expr.Substring(sub_index);

                while (TryGetOperator(sub_expr, false, out Operator sym, out int index, out int length))
                {

                    int index_ = 0;
                    int length_ = 0;

                    string l_side = sub_expr.Substring(0, index);

                    string mid = sub_expr.Substring(index, length);

                    string r_side = sub_expr.Substring(index + length);

                    if (IsPriority(sym))
                    {

                        l_side = new_expr.Substring(0, sub_index) + l_side;

                        prevIndex = index + sub_index - 1;
                        prevLength = length;

                        bool grouped = false;

                        for (int i = l_side.Length - 1; i >= 0; i--)
                        {

                            index_ = 0;
                            length_ = 0;

                            if (l_side[i] == '(' || l_side[i] == ',' || l_side[i] == '[' || l_side[i] == ']' || 
                                (TryGetOperator(l_side.Substring(0, i + 1), true, out Operator sym_, out index_, out length_) && (index_ + length_ - 1) == i) ||
                                (TryGetStatement(l_side.Substring(0, i + 1), true, out Statement state_, out index_, out length_) && (index_ + length_ - 1) == i))
                            {

                                int ind_ = length_ > 0 ? index_ : i;

                                l_side = (ind_ >= 0 ? l_side.Substring(0, ind_ + Mathf.Max(0, length_ - 1) + 1) : "") + "group(" + (ind_ + Mathf.Max(0, length_ - 1) + 1 < l_side.Length ? l_side.Substring(ind_ + Mathf.Max(0, length_ - 1) + 1) : "");

                                grouped = true;

                                break;

                            }

                            if (l_side[i] == ')')
                            {
                                
                                int o = Utils.GetIndexOfOpeningParenthesis(l_side, i) - 1;

                                bool reset(out int i1, out int i2)
                                {

                                    i1 = i2 = 0;

                                    return o >= 0;

                                }

                                while (reset(out index_, out length_) && l_side[o] != ' ' && l_side[o] != '(' && l_side[o] != ',' && l_side[o] != '[' && l_side[o] != ']' &&
                                    !(TryGetOperator(l_side.Substring(0, o + 1), true, out sym_, out index_, out length_) && (index_ + length_ - 1) == o) &&
                                    !(TryGetStatement(l_side.Substring(0, o + 1), true, out state_, out index_, out length_) && (index_ + length_ - 1) == o))
                                {

                                    o--;

                                }

                                int ind_ = length_ > 0 ? index_ : o;

                                l_side = (ind_ >= 0 ? l_side.Substring(0, ind_ + Mathf.Max(0, length_ - 1) + 1) : "") + "group(" + (ind_ + Mathf.Max(0, length_ - 1) + 1 < l_side.Length ? l_side.Substring(ind_ + Mathf.Max(0, length_ - 1) + 1) : "");

                                grouped = true;

                                break;

                            }

                        }

                        if (!grouped) l_side = "group(" + l_side;

                        prevIndex += 6;

                        grouped = false;

                        for(int i = 0; i < r_side.Length; i++)
                        {

                            if (r_side[i] == ')' || r_side[i] == ',' || r_side[i] == '[' || r_side[i] == ']' || 
                                (TryGetOperator(r_side.Substring(i), false, out Operator sym_, out index_, out length_) && index_ == 0) ||
                                (TryGetStatement(r_side.Substring(i), false, out Statement state_, out index_, out length_) && index_ == 0))
                            {

                                r_side = r_side.Substring(0, i) + ")" + (i < r_side.Length ? r_side.Substring(i) : "");

                                grouped = true;

                                break;

                            }

                            if (r_side[i] == '(')
                            {

                                int c = Utils.GetIndexOfClosingParenthesis(r_side, i);

                                r_side = r_side.Substring(0, c) + ")" + (c < r_side.Length ? r_side.Substring(c) : "");

                                grouped = true;

                                break;

                            }

                        }

                        if (!grouped) r_side = r_side + ")";

                        new_expr = l_side + mid + r_side;

                    }
                    else
                    {

                        prevIndex = index + sub_index - 1;
                        prevLength = length;

                    }

                    sub_index = prevIndex + prevLength + 1;
                    sub_expr = new_expr.Substring(sub_index);

                }

                Debug.Log("PRIORITIZE OPS: " + new_expr);

                return new_expr;

            }

            string GroupParenthesis(string expr)
            {

                string sub = expr;

                string str = "";

                int i = sub.IndexOf("(");

                while (i >= 0)
                {

                    str = str + (i > 0 ? sub.Substring(0, i) : "") + "group" + sub.Substring(i, 1);

                    if (i + 1 >= sub.Length) break;

                    sub = sub.Substring(i + 1);

                    i = sub.IndexOf("(");

                }

                str = str + sub;

                Debug.Log(str);

                foreach (string func in functionNames)
                {

                    str = str.Replace(func + "group(", func + "(");

                }

                Debug.Log(str);

                return str;

            }

            for (int i = 0; i < lines.Length; i++)
            {

                void AddVariable(string name)
                {

                    for (int v = 0; v < variables.Count; v++) if (variables[v].GetName() == name) return;

                    Variable variable_type = VariableFromString(lines[i]);

                    switch (variable_type)
                    {

                        case Variable.InternalVar:

                            variables.Add(new DriverLogic.InternalFloat(name));

                            return;

                        case Variable.TransformVar:

                            i++;

                            string transformName = lines[i];

                            i++;

                            int space_index = lines[i].IndexOf(' ');

                            TransformParam param = ParamFromString(space_index >= 0 ? lines[i].Substring(0, space_index) : lines[i]);

                            TransformSpace space = SpaceFromString(space_index >= 0 ? lines[i].Substring(space_index + 1, lines[i].Length - (space_index + 1)) : lines[i]);

                            DriverLogic.TransformVariable variable = null;

                            if (param == TransformParam.LOC_X && space == TransformSpace.LOCAL_SPACE)
                                variable = new DriverLogic.LocalPositionX(name);
                            else if (param == TransformParam.LOC_Y && space == TransformSpace.LOCAL_SPACE)
                                variable = new DriverLogic.LocalPositionY(name);
                            else if (param == TransformParam.LOC_Z && space == TransformSpace.LOCAL_SPACE)
                                variable = new DriverLogic.LocalPositionZ(name);
                            else if (param == TransformParam.ROT_X && space == TransformSpace.LOCAL_SPACE)
                                variable = (invertRx ? ((DriverLogic.TransformVariable)new DriverLogic.InvertedLocalRotationX(name, false)) : ((DriverLogic.TransformVariable)new DriverLogic.LocalRotationX(name, false)));
                            else if (param == TransformParam.ROT_Y && space == TransformSpace.LOCAL_SPACE)
                                variable = (invertRy ? ((DriverLogic.TransformVariable)new DriverLogic.InvertedLocalRotationY(name, false)) : ((DriverLogic.TransformVariable)new DriverLogic.LocalRotationY(name, false)));
                            else if (param == TransformParam.ROT_Z && space == TransformSpace.LOCAL_SPACE)
                                variable = (invertRz ? ((DriverLogic.TransformVariable)new DriverLogic.InvertedLocalRotationZ(name, false)) : (DriverLogic.TransformVariable)(new DriverLogic.LocalRotationZ(name, false)));
                            /*else if (param == TransformParam.LOC_X && space == TransformSpace.WORLD_SPACE)
                                variable = new DriverLogic.WorldPositionX(name);
                            else if (param == TransformParam.LOC_Y && space == TransformSpace.WORLD_SPACE)
                                variable = new DriverLogic.WorldPositionY(name);
                            else if (param == TransformParam.LOC_Z && space == TransformSpace.WORLD_SPACE)
                                variable = new DriverLogic.WorldPositionZ(name);
                            else if (param == TransformParam.ROT_X && space == TransformSpace.WORLD_SPACE)
                                variable = new DriverLogic.WorldRotationX(name);
                            else if (param == TransformParam.ROT_Y && space == TransformSpace.WORLD_SPACE)
                                variable = new DriverLogic.WorldRotationY(name);
                            else if (param == TransformParam.ROT_Z && space == TransformSpace.WORLD_SPACE)
                                variable = new DriverLogic.WorldRotationZ(name);*/

                            if (variable != null)
                            {

                                Transform child = controller.transform.FindDeepChild(transformName);

                                if (child != null)
                                {

                                    Debug.Log("Added Transform Var " + name + " : " + child.name + " " + param.ToString() + " " + space.ToString());

                                    variable.SetTransform(child, controller.transform);

                                    variables.Add(variable);

                                }

                            }

                            return;

                    }

                }

                Command command = CommandFromString(lines[i], out string output_str);

                switch (command)
                {

                    default:
                        continue;

                    case Command.NEW_DRIVER:

                        i++;

                        driver_name = lines[i];

                        Debug.Log("Adding New Driver " + driver_name);

                        variables = new List<DriverLogic.IVariable>();

                        operations = new List<DriverLogic.Operation>();

                        variables.Add(new DriverLogic.DynamicDriverFloat("__driver" + controller.drivers.Count + "__"));

                        expression = new DriverLogic.Expression(0, 0, 0, 0);

                        continue;

                    case Command.END_DRIVER:

                        expression.operationsCount = operations.Count;

                        controller.drivers.Add(new BlendShapeDriver(driver_name, expression, variables.ToArray(), operations.ToArray()));

                        continue;

                    case Command.NEW_VAR:

                        i++;

                        int colon_index = lines[i].IndexOf(':') + 1;

                        string var_name = lines[i].Substring(colon_index, lines[i].Length - colon_index);

                        string raw_name = var_name;

                        var_name = "";

                        for (int a = 0; a < raw_name.Length; a++) var_name = var_name + (raw_name[a] == ' ' ? "" : ("" + raw_name[a]));

                        i++;

                        AddVariable(var_name);

                        continue;

                    case Command.EXPR:

                        int defaultOp = operations.Count;

                        operations.Add(new DriverLogic.Operation(DriverLogic.Operations.Zero));

                        string expr = (maxWeight != 1 ? "(" : "") + ("clamp(" + output_str.Substring(output_str.IndexOf("{") + 1, output_str.IndexOf("}") - (output_str.IndexOf("{") + 1)) + ")") + (maxWeight != 1 ? (") * " + maxWeight) : "");

                        Debug.Log(expr);

                        expr = PrioritizeOperators(InterpretNegativeConstants(GroupParenthesis(expr)));

                        Debug.Log(expr);

                        void EvaluateExpression(string expr_, int recursionLevel, out int internalVar, out int highestOp)
                        {

                            if (!TryGetOperator(expr_, false, out Operator __a, out int __b, out int __c) && !TryGetFunction(expr_, false, out Function __d, out __b, out __c) && !TryGetStatement(expr_, false, out Statement __e, out __b, out __c))
                            {

                                internalVar = VarFromString(expr_);

                                highestOp = operations.Count;

                                operations.Add(new DriverLogic.Operation(DriverLogic.Operations.Var, internalVar));

                                Debug.Log((operations.Count - 1) + " Added Return Var " + variables[internalVar].GetName());

                                return;

                            }

                            internalVar = variables.Count;

                            variables.Add(new DriverLogic.InternalFloat("__new_expr__"));

                            List<int> mathOperations = new List<int>();

                            int AddMathOperation(DriverLogic.Operations opType, string l_expr, string r_expr, int storageIndex = 0)
                            {

                                int l_index = 0;
                                int r_index = 0;

                                if (!string.IsNullOrEmpty(l_expr)) l_index = VarFromString(l_expr);

                                if (!string.IsNullOrEmpty(r_expr)) r_index = VarFromString(r_expr);

                                DriverLogic.Operation opA = new DriverLogic.Operation(opType, l_index, r_index);
                                DriverLogic.Operation opB = new DriverLogic.Operation(DriverLogic.Operations.Store, storageIndex);

                                int opIndexA = operations.Count;
                                operations.Add(opA);

                                int opIndexB = operations.Count;
                                operations.Add(opB);

                                DriverLogic.Operation opC = new DriverLogic.Operation(DriverLogic.Operations.ExecuteSequential, -opIndexA, -opIndexB);

                                int opIndex = operations.Count;

                                mathOperations.Add(opIndex);

                                operations.Add(opC);

                                Debug.Log(opIndexA + " Added Op: '" + variables[l_index].GetName() + " " + opType.ToString() + " " + variables[r_index].GetName() + "'");

                                return opIndex;

                            }

                            List<int> mathFunctions = new List<int>();

                            void AddMathFunction(DriverLogic.Operations opType, string ref_expr, int l_par, int r_par, int arg_count, int recursionDelta, out int internalVar, out int opIndex)
                            {

                                internalVar = variables.Count;

                                variables.Add(new DriverLogic.InternalFloat("__new_func__"));

                                List<int> commas = arg_count > 1 ? Utils.GetContainedCommas(ref_expr, l_par) : null;

                                if (arg_count <= 0)
                                {

                                    opIndex = operations.Count;

                                    operations.Add(new DriverLogic.Operation(opType));

                                }
                                else if (arg_count == 1)
                                {

                                    string contained_str = ref_expr.Substring(l_par + 1, r_par - (l_par + 1));

                                    EvaluateExpression(contained_str, recursionLevel + recursionDelta, out int arg_var, out int arg_op);

                                    opIndex = operations.Count;

                                    operations.Add(new DriverLogic.Operation(opType, arg_var));

                                    operations.Add(new DriverLogic.Operation(DriverLogic.Operations.ExecuteSequential, -arg_op, -opIndex));

                                    opIndex = operations.Count - 1;

                                }
                                else
                                {

                                    int min_args = Mathf.Min(commas.Count + 1, 3, arg_count);

                                    int arg1 = 0;
                                    int arg2 = 0;
                                    int arg3 = 0;

                                    int arg1_op = -1;
                                    int arg2_op = -1;
                                    int arg3_op = -1;

                                    for (int a = 0; a < min_args; a++)
                                    {

                                        int i1 = a == 0 ? (l_par + 1) : (commas[a - 1] + 1);
                                        int i2 = a == min_args - 1 ? r_par : commas[a];

                                        EvaluateExpression(ref_expr.Substring(i1, i2 - i1), recursionLevel + recursionDelta, out int arg_var, out int arg_op);

                                        if (a == 0) { 
                                            
                                            arg1 = arg_var;

                                            arg1_op = arg_op;
                                        
                                        }
                                        else
                                        if (a == 1) { 
                                            
                                            arg2 = arg_var;

                                            arg2_op = arg_op;

                                        }
                                        else
                                        if (a == 2) { 
                                            
                                            arg3 = arg_var;

                                            arg3_op = arg_op;

                                        }

                                    }

                                    int operationIndex = operations.Count;

                                    operations.Add(new DriverLogic.Operation(opType, arg1, arg2, arg3));

                                    opIndex = arg1_op;

                                    if (opIndex < 0)
                                    {

                                        opIndex = operations.Count;

                                        operations.Add(new DriverLogic.Operation(DriverLogic.Operations.Result));

                                    }

                                    if (arg2_op >= 0)
                                    {

                                        operations.Add(new DriverLogic.Operation(DriverLogic.Operations.ExecuteSequential, -arg2_op, -opIndex));

                                        opIndex = operations.Count - 1;

                                    }

                                    if (arg3_op >= 0)
                                    {

                                        operations.Add(new DriverLogic.Operation(DriverLogic.Operations.ExecuteSequential, -arg3_op, -opIndex));

                                        opIndex = operations.Count - 1;

                                    }

                                    operations.Add(new DriverLogic.Operation(DriverLogic.Operations.ExecuteSequential, -opIndex, -operationIndex));

                                    opIndex = operations.Count - 1;

                                }

                                Debug.Log(opIndex + " Added Func Op: '" + opType.ToString() + "'");

                                int opIndex2 = operations.Count;

                                operations.Add(new DriverLogic.Operation(DriverLogic.Operations.Store, internalVar));

                                operations.Add(new DriverLogic.Operation(DriverLogic.Operations.ExecuteSequential, -opIndex, -opIndex2));

                                opIndex = operations.Count - 1;

                                variables[internalVar] = new DriverLogic.InternalFloat("__func" + opIndex + "__");

                                mathFunctions.Add(opIndex);

                            }

                            void EvaluateFunction(Function func_, string ref_expr, int l_par, int r_par, int recursionDelta, out int internalVar, out int opIndex)
                            {

                                internalVar = opIndex = 0;

                                switch (func_)
                                {

                                    case Function.GROUP:
                                        AddMathFunction(DriverLogic.Operations.Result, ref_expr, l_par, r_par, 1, recursionDelta, out internalVar, out opIndex);
                                        break;

                                    case Function.MIN:
                                        AddMathFunction(DriverLogic.Operations.Min, ref_expr, l_par, r_par, 2, recursionDelta, out internalVar, out opIndex);
                                        break;

                                    case Function.MAX:
                                        AddMathFunction(DriverLogic.Operations.Max, ref_expr, l_par, r_par, 2, recursionDelta, out internalVar, out opIndex);
                                        break;

                                    case Function.CLAMP:
                                        AddMathFunction(DriverLogic.Operations.Clamp, ref_expr, l_par, r_par, 1, recursionDelta, out internalVar, out opIndex);
                                        break;

                                    case Function.POW:
                                        AddMathFunction(DriverLogic.Operations.Pow, ref_expr, l_par, r_par, 2, recursionDelta, out internalVar, out opIndex);
                                        break;

                                    case Function.ABS:
                                        AddMathFunction(DriverLogic.Operations.Abs, ref_expr, l_par, r_par, 1, recursionDelta, out internalVar, out opIndex);
                                        break;

                                }

                            }

                            void EvaluateOperator(Operator sym_, string l_expr, string r_expr, int storage_index = 0)
                            {

                                switch (sym_)
                                {

                                    case Operator.ADD:
                                        AddMathOperation(DriverLogic.Operations.Add, l_expr, r_expr, storage_index);
                                        break;

                                    case Operator.SUBTRACT:
                                        AddMathOperation(DriverLogic.Operations.Subtract, l_expr, r_expr, storage_index);
                                        break;

                                    case Operator.MULTIPLY:
                                        AddMathOperation(DriverLogic.Operations.Multiply, l_expr, r_expr, storage_index);
                                        break;

                                    case Operator.DIVIDE:
                                        AddMathOperation(DriverLogic.Operations.Divide, l_expr, r_expr, storage_index);
                                        break;

                                    case Operator.GREATERTHAN:
                                        AddMathOperation(DriverLogic.Operations.GreaterThan, l_expr, r_expr, storage_index);
                                        break;

                                    case Operator.GREATERTHANOREQUAL:
                                        AddMathOperation(DriverLogic.Operations.GreaterThanOrEqual, l_expr, r_expr, storage_index);
                                        break;

                                    case Operator.LESSTHAN:
                                        AddMathOperation(DriverLogic.Operations.LessThan, l_expr, r_expr, storage_index);
                                        break;

                                    case Operator.LESSTHANOREQUAL:
                                        AddMathOperation(DriverLogic.Operations.LessThanOrEqual, l_expr, r_expr, storage_index);
                                        break;

                                    case Operator.EQUAL:
                                        AddMathOperation(DriverLogic.Operations.Equal, l_expr, r_expr, storage_index);
                                        break;

                                    case Operator.NOTEQUAL:
                                        AddMathOperation(DriverLogic.Operations.NotEqual, l_expr, r_expr, storage_index);
                                        break;

                                }

                            }

                            List<int> mathStatements = new List<int>();

                            void EvaluateStatements(ref string expr__, int recursionDelta)
                            {

                                while (TryGetForLoop(expr__, false, out int index, out int length, out int in_index, out int in_length, out int list_start, out int list_end))
                                {

                                    string var_name = expr__.Substring(index + length, (in_index - (index + length))).RemoveWhitespace();

                                    int main_var = variables.Count;

                                    bool exists = false;

                                    for(int a = 0; a < variables.Count; a++)
                                    {

                                        if (variables[a].GetName() == var_name)
                                        {

                                            exists = true;

                                            main_var = a;

                                            break;

                                        }

                                    }

                                    if (!exists) variables.Add(new DriverLogic.InternalFloat(var_name));

                                    string args = expr__.Substring(list_start + 1, list_end - (list_start + 1));

                                    Debug.Log("Interpreting Loop: for " + var_name + " in [" + args + "]");

                                    EvaluateExpression(args, recursionLevel + recursionDelta, out int arg_var, out int arg_op);

                                    int storeIndex = operations.Count;

                                    operations.Add(new DriverLogic.Operation(DriverLogic.Operations.Store, main_var));

                                    mathStatements.Add(operations.Count);

                                    operations.Add(new DriverLogic.Operation(DriverLogic.Operations.ExecuteSequential, -arg_op, -storeIndex));

                                    expr__ = expr__.Substring(0, index) + (list_end + 1 < expr__.Length ? expr__.Substring(list_end + 1) : "");

                                }

                                int prevIf = -1;

                                while(TryGetStatement(expr__, false, out Statement stat, out int index, out int length))
                                {

                                    string l_side, r_side;

                                    int storeIndex, internalVar;

                                    switch(stat)
                                    {

                                        default:
                                            expr__ = expr__.Substring(0, index) + expr__.Substring(index + length);
                                            break;

                                        case Statement.IF:

                                            internalVar = variables.Count;

                                            variables.Add(new DriverLogic.InternalFloat("__new_func__"));

                                            l_side = expr__.Substring(0, index);

                                            for(int a = l_side.Length - 1; a >= 0; a--)
                                            {

                                                if (l_side[a] == '[' || l_side[a] == ']' || (TryGetStatement(l_side, true, out Statement stat_, out int index_, out int length_) && (index_ + (length_ - 1) == a)))
                                                {

                                                    l_side = l_side.Substring(a + 1);

                                                    break;

                                                }

                                            }

                                            r_side = expr__.Substring(index + length);

                                            for(int a = 0; a < r_side.Length; a++)
                                            {

                                                if (r_side[a] == '[' || r_side[a] == ']' || (TryGetStatement(r_side, false, out Statement stat_, out int index_, out int length_) && (index_ == a)))
                                                {

                                                    r_side = r_side.Substring(0, a);

                                                    break;

                                                }

                                            }

                                            Debug.Log("Interpreting If Statement: [" + l_side + "] if [" + r_side + "]");

                                            EvaluateExpression(r_side, recursionLevel + recursionDelta, out int arg_varA, out int arg_opA);

                                            EvaluateExpression(l_side, recursionLevel + recursionDelta, out int arg_varB, out int arg_opB);

                                            prevIf = operations.Count;

                                            variables[internalVar] = new DriverLogic.InternalFloat("__ifelse" + prevIf + "__");

                                            Debug.Log("Interpreted If Statement, ID: " + prevIf);

                                            operations.Add(new DriverLogic.Operation(DriverLogic.Operations.IfElse, -arg_opA, -arg_opB, -defaultOp));

                                            storeIndex = operations.Count;

                                            operations.Add(new DriverLogic.Operation(DriverLogic.Operations.Store, internalVar));

                                            operations.Add(new DriverLogic.Operation(DriverLogic.Operations.ExecuteSequential, -prevIf, -storeIndex));

                                            mathStatements.Add(operations.Count - 1);

                                            expr__ = (index - l_side.Length > 0 ? expr__.Substring(0, index - l_side.Length) : "") + (((index + length) + r_side.Length) < expr__.Length ? (variables[internalVar].GetName() + expr__.Substring((index + length) + r_side.Length)) : variables[internalVar].GetName());

                                            break;

                                        case Statement.ELSE:

                                            r_side = expr__.Substring(index + length);

                                            for (int a = 0; a < r_side.Length; a++)
                                            {

                                                if (r_side[a] == '[' || r_side[a] == ']')
                                                {

                                                    r_side = r_side.Substring(0, a);

                                                    break;

                                                }

                                            }

                                            Debug.Log("Interpreting Else Statement For If(" + prevIf + "): else [" + r_side + "]");

                                            EvaluateExpression(r_side, recursionLevel + recursionDelta, out int arg_var, out int arg_op);

                                            if (prevIf >= 0)
                                            {

                                                DriverLogic.Operation ifOp = operations[prevIf];

                                                ifOp.indexC = -arg_op;

                                                operations[prevIf] = ifOp;

                                                expr__ = expr__.Substring(0, index) + ((index + length) + r_side.Length < expr__.Length ? expr__.Substring((index + length) + r_side.Length) : "");

                                            }
                                            else
                                            {

                                                internalVar = variables.Count;

                                                storeIndex = operations.Count;

                                                operations.Add(new DriverLogic.Operation(DriverLogic.Operations.Store, internalVar));

                                                operations.Add(new DriverLogic.Operation(DriverLogic.Operations.ExecuteSequential, -arg_op, -storeIndex));

                                                variables.Add(new DriverLogic.InternalFloat("__func" + (operations.Count - 1) + "__"));

                                                mathStatements.Add(operations.Count - 1);

                                                expr__ = expr__.Substring(0, index) + (((index + length) + r_side.Length) < expr__.Length ? (variables[internalVar].GetName() + expr__.Substring((index + length) + r_side.Length)) : variables[internalVar].GetName());

                                            }

                                            break;


                                    }

                                }

                            }

                            void EvaluateFunctions(ref string expr__, int recursionDelta)
                            {

                                while (TryGetFunction(expr__, false, out Function func, out int index, out int length))
                                {

                                    string args = expr__.Substring(index + length, expr__.Length - (index + length));

                                    int l_par = args.IndexOf("(");
                                    int r_par = Utils.GetIndexOfClosingParenthesis(args, l_par);

                                    EvaluateFunction(func, args, l_par, r_par, recursionDelta, out int funcVar, out int funcOpIndex);

                                    expr__ = expr__.Substring(0, index) + variables[funcVar].GetName() + expr__.Substring(index + length + r_par + 1);

                                }

                            }

                            void EvaluateOperators(ref string expr__)
                            {

                                while (TryGetOperator(expr__, false, out Operator sym, out int index, out int length))
                                {

                                    string left_expr = expr__.Substring(0, index).RemoveWhitespace();

                                    expr__ = expr__.Substring(index + length);

                                    bool hasNext = TryGetOperator(expr__, false, out Operator sym2, out int index2, out int length2);

                                    string right_expr = hasNext ? expr__.Substring(0, index2).RemoveWhitespace() : expr__;

                                    EvaluateOperator(sym, left_expr, right_expr);

                                    expr__ = hasNext ? expr__.Substring(index2) : "";

                                }

                            }

                            Debug.Log("+++++++ rl_" + recursionLevel + ": " + expr_);

                            EvaluateStatements(ref expr_, 1);

                            expr_ = expr_.Replace("[0]", "");
                            expr_ = expr_.Replace("[", "").Replace("]", "");

                            Debug.Log(":::::::: rl_" + recursionLevel + ": " + expr_);

                            EvaluateFunctions(ref expr_, 1);

                            Debug.Log("------- rl_" + recursionLevel + ": " + expr_);

                            EvaluateOperators(ref expr_);

                            int lastOp = -1;

                            for (int i = mathOperations.Count - 1; i >= 0; i--)
                            {

                                if (lastOp < 0)
                                {

                                    lastOp = mathOperations[i];

                                }
                                else
                                {

                                    DriverLogic.Operation op = new DriverLogic.Operation(DriverLogic.Operations.ExecuteSequential, -mathOperations[i], -lastOp);

                                    lastOp = operations.Count;

                                    operations.Add(op);

                                }

                            }

                            for (int i = mathFunctions.Count - 1; i >= 0; i--)
                            {

                                if (lastOp < 0)
                                {

                                    lastOp = mathFunctions[i];

                                }
                                else
                                {

                                    DriverLogic.Operation op = new DriverLogic.Operation(DriverLogic.Operations.ExecuteSequential, -mathFunctions[i], -lastOp);

                                    lastOp = operations.Count;

                                    operations.Add(op);

                                }

                            }

                            for (int i = mathStatements.Count - 1; i >= 0; i--)
                            {

                                if (lastOp < 0)
                                {

                                    lastOp = mathStatements[i];

                                }
                                else
                                {

                                    DriverLogic.Operation op = new DriverLogic.Operation(DriverLogic.Operations.ExecuteSequential, -mathStatements[i], -lastOp);

                                    lastOp = operations.Count;

                                    operations.Add(op);

                                }

                            }

                            highestOp = operations.Count;

                            operations.Add(new DriverLogic.Operation(DriverLogic.Operations.Store, internalVar));

                            operations.Add(new DriverLogic.Operation(DriverLogic.Operations.ExecuteSequential, -lastOp, -highestOp));

                            highestOp = operations.Count - 1;

                            variables[internalVar] = new DriverLogic.ExpressionOutputFloat("__expr" + highestOp + "__");

                        }

                        EvaluateExpression(expr, 0, out int iVar, out int hOp);

                        expression.start = hOp;

                        // Consolidate identical constants

                        Dictionary<int, int> varTranslator = new Dictionary<int, int>();

                        int[] consolidated = new int[variables.Count];

                        List<DriverLogic.IVariable> newVariables = new List<DriverLogic.IVariable>();

                        for (int a = 0; a < variables.Count; a++)
                        {

                            if (variables[a] is DriverLogic.ConstantFloat constVar)
                            {

                                if (consolidated[a] > 0)
                                {

                                    varTranslator[a] = consolidated[a] - 1;

                                    continue;

                                }

                                int newIndex = newVariables.Count;

                                int count = 0;

                                for (int b = 0; b < variables.Count; b++)
                                {

                                    if (b == a) continue;

                                    if (variables[b] is DriverLogic.ConstantFloat constVar2)
                                    {

                                        if (constVar2.GetValue() == constVar.GetValue())
                                        {

                                            consolidated[b] = newIndex + 1;

                                            count++;

                                        }

                                    }

                                }

                                consolidated[a] = newIndex + 1;

                                varTranslator[a] = newIndex;

                                newVariables.Add(constVar);

                                if (count > 0) Debug.Log("Consolidated " + count + " variables with value " + constVar.GetValue());

                            }
                            else
                            {

                                consolidated[a] = newVariables.Count + 1;

                                varTranslator[a] = newVariables.Count;

                                newVariables.Add(variables[a]);

                            }

                        }

                        variables = newVariables;

                        for(int a = 0; a < operations.Count; a++)
                        {

                            DriverLogic.Operation op = operations[a];

                            if (op.indexA > 0) op.indexA = varTranslator[op.indexA];
                            if (op.indexB > 0) op.indexB = varTranslator[op.indexB];
                            if (op.indexC > 0) op.indexC = varTranslator[op.indexC];

                            if (op.indexA < 0) op.indexA = -op.indexA;
                            if (op.indexB < 0) op.indexB = -op.indexB;
                            if (op.indexC < 0) op.indexC = -op.indexC;

                            operations[a] = op;

                        }

                        Debug.Log(expression.AsString(operations, variables));

                        continue;

                }

            }

        }

        public static string[] ReadLines(string path)
        {

            if (!File.Exists(path)) {

                Debug.Log("File at " + path + " not found");

                return null;
            
            }

            FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            StreamReader reader = new StreamReader(file);

            List<string> lines = new List<string>();

            string line;

            while((line = reader.ReadLine()) != null)
            {

                lines.Add(line);

            }

            reader.Close();

            file.Close();

            return lines.ToArray();

        }

    }

}
