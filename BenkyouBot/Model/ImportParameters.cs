namespace BenkyouBot.Model
{
    public record ImportParameters(ImportType Type, bool AddScore, int ContentColumnIndex, int RecordTypeColumnIndex, int DateColumnIndex, DateOnly AssumedDate);
}
