﻿using System;
using System.Linq;

namespace Jamiras.Components
{
    /// <summary>
    /// Helper functions for common sort algorithms
    /// </summary>
    public static class SortFunctions
    {
        /// <summary>
        /// Case insensitive sort that honors numerical values (11 is not between 1 and 2)
        /// </summary>
        public static int NumericStringCaseInsensitiveCompare(string left, string right)
        {
            if (left == null || right == null)
                return String.Compare(left, right);

            string shorter = (left.Length < right.Length) ? left : right;
            bool hasNumbers = shorter.Any(c => c >= '0' && c <= '9');
            if (!hasNumbers)
                return String.Compare(left, right, StringComparison.OrdinalIgnoreCase);

            int i = 0, j = 0;
            do
            {
                if (i == left.Length)
                {
                    if (j == right.Length)
                        return 0;

                    return -1;
                }

                if (j == right.Length)
                    return 1;

                char rightChar = right[j];
                char leftChar = left[i];

                if (leftChar <= '9' && leftChar >= '0')
                {
                    if (rightChar > '9' || rightChar < '0')
                        return (leftChar - rightChar);

                    int leftValue = ReadNumber(left, ref i);
                    int rightValue = ReadNumber(right, ref j);

                    int diff = leftValue - rightValue;
                    if (diff != 0)
                        return diff;
                }
                else
                {
                    if (leftChar != rightChar)
                    {
                        leftChar = Char.ToLower(leftChar);
                        rightChar = Char.ToLower(rightChar);
                        if (leftChar != rightChar)
                            return (leftChar - rightChar);
                    }

                    i++;
                    j++;
                }
            } while (true);
        }

        private static int ReadNumber(string word, ref int i)
        {
            int value = 0;
            do
            {
                Char c = word[i];
                if (!Char.IsNumber(c))
                    break;

                value *= 10;
                value += (c - '0');

                i++;
            } while (i < word.Length);

            return value;
        }

        /// <summary>
        /// Case insensitive sort that ignores leading words like A, AN, and THE.
        /// </summary>
        public static int TitleCompare(string left, string right)
        {
            if (left == null || right == null)
                return String.Compare(left, right);

            Token leftToken = Tokenizer.RemoveArticle(new Token(left, 0, left.Length));
            Token rightToken = Tokenizer.RemoveArticle(new Token(right, 0, right.Length));
            return leftToken.CompareTo(rightToken, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
