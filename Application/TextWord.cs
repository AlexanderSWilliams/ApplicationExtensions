namespace Application.Types
{
    public class TextWord
    {
        public TextWord(string text, string word, int index, bool ignoreCase)
        {
            Text = string.Intern(ignoreCase ? text.ToLower() : text);
            Word = ignoreCase ? word.ToLower() : word;
            Index = index;
            IgnoreCase = ignoreCase;
        }

        public bool IgnoreCase { get; set; }

        public int Index { get; set; }

        public string Text { get; set; }

        public string Word { get; set; }
    }
}