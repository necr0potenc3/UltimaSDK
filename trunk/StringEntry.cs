
namespace Ultima
{
    public class StringEntry
    {
        private readonly int m_Number;
        private readonly string m_Text;

        public int Number { get { return m_Number; } }
        public string Text { get { return m_Text; } }

        public StringEntry(int number, string text)
        {
            m_Number = number;
            m_Text = text;
        }
    }
}