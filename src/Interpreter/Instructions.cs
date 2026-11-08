namespace FiniteSteamMachine.Interpreter;

public enum InstructType : byte
{
    None,
    Put,
    Pull,
    Press,
    Drill,
    Add,
    Sub,
    Mul,
    Div,
    UsePump,
    AddCoal,
    SetValve,
    WaitForPressureHigh,
    WaitForPressureLow,
    Move,
    SetContainer,
    LoadContainer,
    DisconnectContainer

}


public struct Instruction(InstructType type, int value)
{
    public InstructType type = type;
    public int value = value;
    public bool pullValue;
    
}