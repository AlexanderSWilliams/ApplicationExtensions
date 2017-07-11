﻿using Application.IEnumerableExtensions;
using Application.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.StringExtensions
{
    public static class StringExtensions
    {
        public static List<TextWord> GetWords(this string text, string pattern, bool ignoreCase = true)
        {
            var result = new List<TextWord>();
            var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            foreach (Match match in Regex.Matches(text, pattern, options))
            {
                result.Add(new TextWord(text, pattern, match.Index, ignoreCase));
            }

            return result;
        }

        public static int IndexOfFirstNonWhiteSpace(this string text)
        {
            return text.IndexOf<char>(c => !char.IsWhiteSpace(c));
        }

        public static int IndexOfFirstWhiteSpace(this string text)
        {
            return text.IndexOf<char>(c => char.IsWhiteSpace(c));
        }

        public static int IndexOfLastNonWhiteSpace(this string text)
        {
            var Indices = text.IndicesOf<char>(c => !char.IsWhiteSpace(c));
            return Indices.Any() ? Indices.Last() : -1;
        }

        public static int IndexOfLastWhiteSpace(this string text)
        {
            var Indices = text.IndicesOf<char>(c => char.IsWhiteSpace(c));
            return Indices.Any() ? Indices.Last() : -1;
        }

        public static string NormalizeToASCII(this string str)
        {
            var Normalized = str.Normalize(NormalizationForm.FormKD);
            var NormalizedLength = Normalized.Length;
            var result = new StringBuilder();
            for (var i = 0; i < NormalizedLength; i++)
            {
                var Character = Normalized[i];
                if ((!Char.IsControl(Character) && (int)Character < 128) || Character == '\r' || Character == '\n')
                    result.Append(Character);
            }

            return result.ToString();
        }

        public static bool? ToBool(this string obj)
        {
            bool value;
            if (obj != null && bool.TryParse(obj, out value))
                return value;
            else
                return null;
        }

        public static bool ToBoolOrThrow(this string obj, string errorMessage = "Input string is not a valid boolean.")
        {
            bool value;
            if (obj != null && bool.TryParse(obj, out value))
                return value;
            else
                throw new ApplicationException(errorMessage);
        }

        public static DateTime? ToDateTime(this string obj)
        {
            DateTime value;
            if (obj != null && DateTime.TryParse(obj, out value))
                return value;
            else
                return null;
        }

        public static DateTime ToDateTimeOrThrow(this string obj, string errorMessage = "Input string is not a valid date.")
        {
            DateTime value;
            if (obj != null && DateTime.TryParse(obj, out value))
                return value;
            else
                throw new ApplicationException(errorMessage);
        }

        public static decimal? ToDecimal(this string obj)
        {
            decimal value;
            if (obj != null && decimal.TryParse(obj, out value))
                return value;
            else
                return null;
        }

        public static decimal ToDecimalOrThrow(this string obj, string errorMessage = "Input string is not a valid decimal.")
        {
            decimal value;
            if (obj != null && decimal.TryParse(obj, out value))
                return value;
            else
                throw new ApplicationException(errorMessage);
        }

        public static Guid? ToGuid(this string obj)
        {
            Guid value;
            if (obj != null && Guid.TryParse(obj, out value))
                return value;
            else
                return null;
        }

        public static Guid ToGuidOrThrow(this string obj, string errorMessage = "Input string is not a valid guid.")
        {
            Guid value;
            if (obj != null && Guid.TryParse(obj, out value))
                return value;
            else
                throw new ApplicationException(errorMessage);
        }

        public static int? ToInt32(this string obj)
        {
            int value;
            if (obj != null && int.TryParse(obj, out value))
                return value;
            else
                return null;
        }

        public static int ToInt32OrThrow(this string obj, string errorMessage = "Input string is not a valid integer.")
        {
            int value;
            if (obj != null && int.TryParse(obj, out value))
                return value;
            else
                throw new ApplicationException(errorMessage);
        }

        public static Int64? ToInt64(this string obj)
        {
            long value;
            if (obj != null && long.TryParse(obj, out value))
                return value;
            else
                return null;
        }

        public static long ToInt64OrThrow(this string obj, string errorMessage = "Input string is not a valid long integer.")
        {
            long value;
            if (obj != null && long.TryParse(obj, out value))
                return value;
            else
                throw new ApplicationException(errorMessage);
        }

        public static MemoryStream ToStream(this string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static TimeSpan? ToTimeSpan(this string obj)
        {
            TimeSpan value;
            if (obj != null && TimeSpan.TryParse(obj, out value))
                return value;
            else
                return null;
        }

        public static TimeSpan ToTimeSpanOrThrow(this string obj, string errorMessage = "Input string is not a valid time.")
        {
            TimeSpan value;
            if (obj != null && TimeSpan.TryParse(obj, out value))
                return value;
            else
                throw new ApplicationException(errorMessage);
        }
    }
}