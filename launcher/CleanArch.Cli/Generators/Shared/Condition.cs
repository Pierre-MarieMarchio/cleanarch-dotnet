using System.Text.RegularExpressions;

namespace CleanArch.Cli.Generators.Shared;

internal sealed class Condition
{
    private static readonly Regex _token = new(@"\|\||&&|[!()]|\w+|[^\s\w!()]+", RegexOptions.None, TimeSpan.FromSeconds(2));

    private static readonly Regex _word = new(@"^\w+$", RegexOptions.None, TimeSpan.FromSeconds(2));

    private readonly Func<IReadOnlySet<string>, bool> _holds;

    private Condition(Func<IReadOnlySet<string>, bool> holds) => _holds = holds;

    internal static Condition Parse(string text, FlagSet vocabulary) => new(new Reader(text, vocabulary).Whole());

    internal bool Evaluate(IReadOnlySet<string> flags) => _holds(flags);

    private sealed class Reader(string text, FlagSet vocabulary)
    {
        private readonly Queue<string> _tokens = new(_token.Matches(text).Select(match => match.Value));

        internal Func<IReadOnlySet<string>, bool> Whole()
        {
            var whole = Or();

            return _tokens.Count == 0 ? whole : throw Refusal("'&&', '||' or the end");
        }

        private Func<IReadOnlySet<string>, bool> Or()
        {
            var holds = And();

            while (Takes("||"))
            {
                var left = holds;
                var right = And();

                holds = flags => left(flags) || right(flags);
            }

            return holds;
        }

        private Func<IReadOnlySet<string>, bool> And()
        {
            var holds = Unary();

            while (Takes("&&"))
            {
                var left = holds;
                var right = Unary();

                holds = flags => left(flags) && right(flags);
            }

            return holds;
        }

        private Func<IReadOnlySet<string>, bool> Unary()
        {
            if (Takes("!"))
            {
                var operand = Unary();

                return flags => !operand(flags);
            }

            if (Takes("("))
            {
                var inner = Or();

                return Takes(")") ? inner : throw Refusal("'&&', '||' or ')'");
            }

            return Flag();
        }

        private Func<IReadOnlySet<string>, bool> Flag()
        {
            string name = _tokens.TryPeek(out string? next) && _word.IsMatch(next) ? _tokens.Dequeue() : throw Refusal("a flag, '!' or '('");

            return vocabulary.Knows(name)
                ? flags => flags.Contains(name)
                : throw new InvalidOperationException($"'{name}' is not a flag of {vocabulary.Generator} in '{text}'.");
        }

        private bool Takes(string token)
        {
            if (!_tokens.TryPeek(out string? next) || next != token)
            {
                return false;
            }

            _tokens.Dequeue();

            return true;
        }

        private InvalidOperationException Refusal(string expected) =>
            new(_tokens.TryPeek(out string? next)
                ? $"'{next}' stands where {expected} belongs in '{text}'."
                : $"'{text}' ends where {expected} belongs.");
    }
}
