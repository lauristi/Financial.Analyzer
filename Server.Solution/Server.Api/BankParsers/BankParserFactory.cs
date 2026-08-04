namespace Server.Api.Parsers
{
    public class BankParserFactory
    {
        private readonly IEnumerable<IBankParser> _parsers;

        public BankParserFactory(IEnumerable<IBankParser> parsers)
        {
            _parsers = parsers;
        }

        public IBankParser GetParser(string headerLine)
        {
            var parser = _parsers.FirstOrDefault(p => p.CanParse(headerLine));
            return parser ?? throw new NotSupportedException("Formato de extrato bancário não suportado.");
        }
    }
}