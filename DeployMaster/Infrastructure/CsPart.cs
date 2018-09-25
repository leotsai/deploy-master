namespace DeployMaster
{
    public class CsPart
    {
        private string _text;
        public string Text => _text;
        public CsPart(string text)
        {
            _text = text;
        }

        public CsPart Add(string text, int position)
        {
            while (_text.Length < position)
            {
                _text += " ";
            }
            _text += text;
            return this;
        }

    }
}
