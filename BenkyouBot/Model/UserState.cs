namespace BenkyouBot.Model;

public record UserState(bool IsImporting, ImportParameters? ImportParameters)
{
    public static UserState None = new(false, null);
}