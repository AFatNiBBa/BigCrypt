using BigCrypt;
using BigCrypt.Business;
using BigCrypt.Util;
using System.CommandLine;

var argInput = new Argument<FileInfo>("input") { Description = "The input file" };
var argKey = new Argument<FileInfo>("key") { Description = "The key file" };
var argOutput = new Argument<FileInfo>("output") { Description = "The output file", Arity = ArgumentArity.ZeroOrOne };

var argRandom = new Option<bool>("--random", "-r") { Description = "Generate a random key" };

var argSize = new Argument<long>("size")
{
    Description = "The size of the random data to generate",
    CustomParser = x =>
    {
        var span = x.Tokens[0].Value.AsSpan();
        long multiplier;

        if (span.EndsWith("kb", StringComparison.InvariantCultureIgnoreCase))
            multiplier = Static.KB;
        else if (span.EndsWith("mb", StringComparison.InvariantCultureIgnoreCase))
            multiplier = Static.KB * Static.KB;
        else if (span.EndsWith("gb", StringComparison.InvariantCultureIgnoreCase))
            multiplier = Static.KB * Static.KB * Static.KB;
        else
            return long.Parse(span);

        return multiplier * long.Parse(span[..^2]);
    }
};

var cmdXor = CreateOperationCommand<XorOperation>("xor", "bitwise XOR");

var cmdSum = CreateOperationCommand<SumOperation>("sum", "wrapped sum");

var cmdSub = CreateOperationCommand<SubOperation>("sub", "wrapped subtraction");

var cmdRnd = new Command("rnd", "Generates a random key of the given size")
{
    argKey,
    argSize
};

cmdRnd.SetAction(res =>
{
    var key = res.GetRequiredValue(argKey);
    var size = res.GetRequiredValue(argSize);
    return Cli.Rnd(key.FullName, size);
});

var root = new RootCommand("Fast One-Time Pad")
{
    cmdXor,
    cmdSum,
    cmdSub,
    cmdRnd
};

return await root.Parse(args).InvokeAsync();

Command CreateOperationCommand<T>(string name, string desc) where T : IOperation
{
    var cmd = new Command(name, $"Performs the {desc} operation between the input and key file and writes the result to the output one")
    {
        argInput,
        argKey,
        argOutput,
        argRandom
    };

    cmd.SetAction(async x =>
    {
        var input = x.GetRequiredValue(argInput);
        var key = x.GetRequiredValue(argKey);
        var output = x.GetValue(argOutput);
        var random = x.GetValue(argRandom);

        if (!input.Exists)
        {
            Console.WriteLine("The input file does not exist");
            return 1;
        }

        if (input.Length is 0)
        {
            Console.WriteLine("The input file is empty");
            return 2;
        }

        if (!random)
        {
            if (!key.Exists)
            {
                Console.WriteLine("The key file does not exist");
                return 3;
            }

            if (key.Length is 0)
            {
                Console.WriteLine("The key file is empty");
                return 4;
            }
        }

        await Cli.Op<T>(input.FullName, key.FullName, output?.FullName, random);
        return 0;
    });

    return cmd;
}