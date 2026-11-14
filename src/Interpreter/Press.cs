using System.Text;

namespace FiniteSteamMachine.Interpreter;


public sealed class SteamPress
{
    public Instruction current;
    public Stack<int> pressStack = new(32);
    public char consoleOutput = '\0';

    private readonly double[] instrPressures = new double[32];
    public SteamPress()
    {
        instrPressures[(int)InstructType.None] = 0;
        instrPressures[(int)InstructType.Put] = 0.5;
        instrPressures[(int)InstructType.Add] = 1;
        instrPressures[(int)InstructType.Sub] = 1;
        instrPressures[(int)InstructType.Mul] = 2;
        instrPressures[(int)InstructType.Div] = 2;
        instrPressures[(int)InstructType.PrintChar] = 2;
        instrPressures[(int)InstructType.ReadChar] = 1;
    }
    

    /// <returns>Successfull/!Successfull</returns> 
    public bool Press(Boiler boiler)
    {
        double neededPressure = instrPressures[(int)current.type];

        if(boiler.pressure >= neededPressure)
        {
            boiler.TakePressure(neededPressure);
            Execute();
            return true;
        }
        return false;
    }



    private void Execute()
    {
        switch(current.type)
        {
            case InstructType.Put:
                pressStack.Push(current.value);
            break;

            case InstructType.Add:
                pressStack.Push(pressStack.Pop()+pressStack.Pop());
            break;

            case InstructType.Sub:
                int a = pressStack.Pop();
                int b = pressStack.Pop();
                pressStack.Push(b-a);
            break;

            case InstructType.Mul:
                pressStack.Push(pressStack.Pop()*pressStack.Pop());
            break;

            case InstructType.Div:
                a = pressStack.Pop();
                b = pressStack.Pop();
                pressStack.Push(b/a);
            break;

            case InstructType.PrintChar:
                consoleOutput = (char)pressStack.Pop();
            break;

            case InstructType.ReadChar:
                pressStack.Push(Console.Read());
            break;
            
        }
    }
}