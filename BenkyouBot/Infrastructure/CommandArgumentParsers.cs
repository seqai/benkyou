using System.Globalization;
using Benkyou.DAL.Entities;
using BenkyouBot.Model;
using Mono.Options;
using static Benkyou.Infrastructure.EnumHelpers;

namespace BenkyouBot.Infrastructure;

public static class CommandArgumentParsers
{
    public static (int count, RecordType recordType, DateOnly from, DateOnly till, bool showIgnored, bool flat, string helpMessage) ParseTopCommandArguments(string[] args)
    {
        var count = 10;
        var recordType = RecordType.Any;
        var from = DateOnly.MinValue;
        var till = DateOnly.MaxValue;
        var flat = false;
        var showIgnored = false;
        var showHelpMessage = false;
        var helpMessage = string.Empty;

        var options = new OptionSet
        {
            {"c|count=", "Number of records to show", c => count = int.Parse(c)},
            {"k|kind=", "Record kind", t => recordType = FromAlias(t, withDefaultName: true, withFallback: true, defaultValue: RecordType.Any)},
            {"f|from=", "From date", f => from = DateOnly.ParseExact(f, "yyyy-MM-dd", CultureInfo.InvariantCulture)},
            {"t|till=", "Till date", t => till = DateOnly.ParseExact(t, "yyyy-MM-dd", CultureInfo.InvariantCulture)},
            {"s|flat", "Show records in flat format", f => flat = true},
            {"i|ignored", "Show ignored records", i => showIgnored = i != null},
            {"h|help", "Show help message", h => showHelpMessage = h != null},
        };

        options.Parse(args);

        if (showHelpMessage)
        {
            using var helpMessageWriter = new StringWriter();
            options.WriteOptionDescriptions(helpMessageWriter);
            helpMessage = helpMessageWriter.ToString();
        }

        return (count, recordType, from, till, showIgnored, flat, helpMessage);
    }

    public static (ImportParameters parameters, string helpMessage) ParseImportCommandArguments(string[] args)
    {
        var importType = ImportType.Unknown;
        var addScore = true;
        var contentColumnIndex = 1;
        var dateColumnIndex = 0;
        var typeColumnIndex = 0;
        var assumedDate = DateOnly.MinValue;

        var showHelpMessage = false;
        var helpMessage = string.Empty;

        var options = new OptionSet
        {
            {"t|type=", "Import type", t => importType = FromAlias(t, withDefaultName: true, withFallback: true, defaultValue: ImportType.Unknown)},
            {"c|content=", "Content column index", c => contentColumnIndex = int.Parse(c)},
            {"d|date=", "Date column index", d => dateColumnIndex = int.Parse(d)},
            {"k|kind=", "Record kind column index", k => typeColumnIndex = int.Parse(k)},
            {"s|score", "Add score", s => addScore = s != null},
            {"a|assumed-date=", "Assumed date", a => assumedDate = DateOnly.ParseExact(a, "yyyy-MM-dd", CultureInfo.InvariantCulture)},
            {"h|help", "Show help message", h => showHelpMessage = h != null},
        };

        options.Parse(args);

        if (showHelpMessage)
        {
            using var helpMessageWriter = new StringWriter();
            options.WriteOptionDescriptions(helpMessageWriter);
            helpMessage = helpMessageWriter.ToString();
        }

        return (new ImportParameters(importType, addScore, contentColumnIndex, typeColumnIndex, dateColumnIndex, assumedDate), helpMessage);
    }

    public static (IReadOnlyList<string> content, RecordType recordType, string helpMessage) ParseIgnoreOrIncludeCommandArguments(string[] args)
    {
        var content = new List<string>();
        var recordType = RecordType.Any;

        var showHelpMessage = false;
        var helpMessage = string.Empty;

        var options = new OptionSet
        {
            {"<>", "Content", c => content.Add(c)},
            {"k|kind=", "Record kind", t => recordType = FromAlias(t, withDefaultName: true, withFallback: true, defaultValue: RecordType.Any)},
            {"h|help", "Show help message", h => showHelpMessage = h != null},
        };

        options.Parse(args);

        if (showHelpMessage)
        {
            using var helpMessageWriter = new StringWriter();
            options.WriteOptionDescriptions(helpMessageWriter);
            helpMessage = helpMessageWriter.ToString();
        }

        return (content, recordType, helpMessage);
    }

    public static (RecordType recordType, DateOnly from, DateOnly till, bool includeIgnored, string helpMessage) ParseExportCommandArguments(string[] args)
    {
        var recordType = RecordType.Any;
        var from = DateOnly.MinValue;
        var till = DateOnly.MaxValue;
        var includeIgnored = false;

        var showHelpMessage = false;
        var helpMessage = string.Empty;

        var options = new OptionSet
        {
            {"k|kind=", "Record kind", t => recordType = FromAlias(t, withDefaultName: true, withFallback: true, defaultValue: RecordType.Any)},
            {"f|from=", "From date", f => from = DateOnly.ParseExact(f, "yyyy-MM-dd", CultureInfo.InvariantCulture)},
            {"t|till=", "Till date", t => till = DateOnly.ParseExact(t, "yyyy-MM-dd", CultureInfo.InvariantCulture)},
            {"i|ignored", "Include ignored records", i => includeIgnored = i != null},
            {"h|help", "Show help message", h => showHelpMessage = h != null},
        };

        options.Parse(args);

        if (showHelpMessage)
        {
            using var helpMessageWriter = new StringWriter();
            options.WriteOptionDescriptions(helpMessageWriter);
            helpMessage = helpMessageWriter.ToString();
        }

        return (recordType, from, till, includeIgnored, helpMessage);
    }
}