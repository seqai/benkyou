namespace Benkyou.Infrastructure
{
    public class EnumStringAliasAttribute : Attribute
    {
        public IReadOnlyCollection<string> Aliases { get; }

        public EnumStringAliasAttribute(params string[] aliases)
        {
            Aliases = aliases;
        }
    }
}
