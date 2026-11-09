using FiniteSteamMachine.Exceptions;
using FiniteSteamMachine.Interpreter;
using FiniteSteamMachine.Parser;



Parser parser = new(File.ReadAllText(args[0]), args[0]);

List<List<Instruction>> instructions = parser.Parse();


SteamMachine steamMachine = new(instructions);


steamMachine.Execute(args[1] == "true" ? true : false, int.Parse(args[2]));

