using Application.IEnumerableExtensions;
using Application.StringExtensions;
using Application.Types;
using System;

namespace Application.TextWordExtensions
{
    public static class TextWordExtensions
    {
        public static TextWord NextWord(this TextWord word)
        {
            try
            {
                var Rest = word.Text.Substring(word.Index);
                var StartIndex = Rest.IndexOfFirstWhiteSpace();

                StartIndex += Rest.Substring(StartIndex).IndexOfFirstNonWhiteSpace();
                var NextRest = Rest.Substring(StartIndex);

                var NextWord = NextRest.Substring(0, NextRest.IndexOfFirstWhiteSpace());

                return new TextWord(word.Text, NextWord, word.Index + StartIndex, word.IgnoreCase);
            }
            catch (ArgumentOutOfRangeException e)
            {
                return null;
            }
        }

        public static TextWord PreviousWord(this TextWord word)
        {
            try
            {
                var Previous = word.Text.Substring(0, word.Index);

                var EndIndex = Previous.IndexOfLastNonWhiteSpace() + 1;

                var PrevPrevious = Previous.Substring(0, EndIndex);
                var StartIndex = PrevPrevious.IndexOfLastWhiteSpace() + 1;
                var PreviousWord = PrevPrevious.Substring(StartIndex);

                return new TextWord(word.Text, PreviousWord, StartIndex, word.IgnoreCase);
            }
            catch (ArgumentOutOfRangeException e)
            {
                return null;
            }
        }
    }
}