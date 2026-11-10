using FiniteSteamMachine.Exceptions;
using FiniteSteamMachine.Interpreter;
using FiniteSteamMachine.Parser;



void ThrowError(string text)
{
    Console.Error.WriteLine(text);
    Environment.Exit(-1);
}

if(args.Length < 3)
{
    ThrowError("Insufficient arguments.\n"+
        "Arguments (in order): input_file manual_tick tick_delay_ms [-no-display] [-containers=uint]\n");
}

string text = "";

try
{
    text = File.ReadAllText(args[0]);
}
catch
{
    ThrowError($"Failed to load file: {args[0]}");
}


Parser parser = new(text, args[0]);

List<List<Instruction>> instructions = parser.Parse();


//Parse runtime args

bool display = true;
int containers = 32;

foreach(string arg in args)
{
    if(arg.StartsWith("-containers"))
    {
        string[] argVals = arg.Split("=");
        if(argVals.Length != 2)
        {
            ThrowError("Malformed container argument. Expected: -containers=[number].");
        }
        containers = int.Parse(argVals[1]);
    }
    else if(arg == "-no-display")
    {
        display = false;
    }
}

SteamMachine steamMachine = new(instructions, containers);


steamMachine.Execute(display, args[1] == "true" ? true : false, int.Parse(args[2]));

