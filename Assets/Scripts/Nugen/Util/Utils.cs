using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace nugen
{
    public static class Utils
    {

        public static int GetIndexOfOpeningParenthesis(string str, int closingIndex)
        {

            int i = 0;

            for (int a = closingIndex - 1; a >= 0; a--)
            {

                if (str[a] == '(')
                {

                    i--;

                    if (i < 0) return a;

                }
                else if (str[a] == ')') i++;

            }

            return 0;

        }

        public static int GetIndexOfClosingParenthesis(string str, int openingIndex)
        {

            int i = 0;

            for (int a = openingIndex + 1; a < str.Length; a++)
            {

                if (str[a] == ')')
                {

                    i--;

                    if (i < 0) return a;

                }
                else if (str[a] == '(') i++;

            }

            return str.Length - 1;

        }

        public static int GetIndexOfOpeningBracket(string str, int closingIndex)
        {

            int i = 0;

            for (int a = closingIndex - 1; a >= 0; a--)
            {

                if (str[a] == '[')
                {

                    i--;

                    if (i < 0) return a;

                }
                else if (str[a] == ']') i++;

            }

            return 0;

        }

        public static int GetIndexOfClosingBracket(string str, int openingIndex)
        {

            int i = 0;

            for (int a = openingIndex + 1; a < str.Length; a++)
            {

                if (str[a] == ']')
                {

                    i--;

                    if (i < 0) return a;

                }
                else if (str[a] == '[') i++;

            }

            return str.Length - 1;

        }

        public static List<int> GetContainedCommas(string str, int openingIndex)
        {

            List<int> indices = new List<int>();

            int i = 0;

            for (int a = openingIndex + 1; a < str.Length; a++)
            {

                if (str[a] == ',')
                {

                    if (i <= 0) indices.Add(a);

                }
                else if (str[a] == ')')
                {

                    i--;

                    if (i < 0) return indices;

                }
                else if (str[a] == '(') i++;

            }

            return indices;

        }

    }
}
