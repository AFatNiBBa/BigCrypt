using System.CommandLine;
using BigCrypt;

var argInput = new Argument<FileInfo>("input") { Description = "The input file" };
var argKey = new Argument<FileInfo>("key") { Description = "The key file" };
var argOutput = new Argument<FileInfo>("output") { Description = "The output file", Arity = ArgumentArity.ZeroOrOne };
var argSize = new Argument<long>("size") { Description = "The size of the random data to generate" };
var argRandom = new Option<bool>("--random", "-r") { Description = "Generate a random key" };

var cmdXor = new Command("xor", "Performs the bitwise XOR operation between the input and key file and writes the result to the output one")
{
    argInput,
    argKey,
    argOutput,
    argRandom
};

cmdXor.SetAction(async x =>
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

    await Cli.Xor(input.FullName, key.FullName, output?.FullName, random);
    return 0;
});

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
    cmdRnd
};

return await root.Parse(args).InvokeAsync();