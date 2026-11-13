using System.Data.Common;
using System.Reflection.Metadata;
using FiniteSteamMachine.Exceptions;

namespace FiniteSteamMachine.Interpreter;


public enum BoilerState
{
    Ok,
    NoCoal,
    Exploded
}

public struct Boiler(int containerAmount)
{
    //Water in kg, steam out in L
    public double coal, water, steam, pressure, pressureLimit, releaseValve;
    public double[] steamContainers = new double[containerAmount];
    public int selectedContainer = -1;

    public BoilerState Tick()
    {
        if(coal>0) { coal--; }
        else {return BoilerState.NoCoal;}

        double newSteam = 0.01*(coal*coal);

        if(water-newSteam>0) 
        { 
            water -= newSteam*0.2d; 
        }
        else 
        {
            newSteam = water/0.2d;
            water = 0;
        }
        
        steam += newSteam;
        pressure += newSteam/2;

        if(pressure > pressureLimit)
        {
            return BoilerState.Exploded;
        }

        return BoilerState.Ok;
    }

    public void AddPressure(double pressure)
    {
        this.pressure += pressure;
        steam += pressure*2;
        if(this.pressure>pressureLimit) { throw new BoilerExplosion(); }
    }

    /// <summary>
    /// Takes pressure for work and adjusts steam volume accordingly
    /// </summary>
    public void TakePressure(double pressure)
    {
        if(this.pressure - pressure >= 0)
        {
            this.pressure -= pressure;
            steam -= pressure*2;
        }
        else
        {
            this.pressure = 0;
            steam = 0;
        }
    }

}

public struct WaterPump()
{
    public bool Pump(ref Boiler boiler, double usedPressure)
    {
        if(boiler.pressure >= usedPressure)
        {
            boiler.TakePressure(usedPressure);
            boiler.water += usedPressure*0.8;
            return true;
        }
        return false;
    }
}

public struct CoalDrill()
{
    public double pressurePerCoal = 2;

    public bool Drill(ref Boiler boiler, double inputPressure)
    {
        if(boiler.pressure>=inputPressure)
        {

            boiler.coal += Math.Floor(inputPressure);
            
            return true;
        }
        return false;
    }
}

public struct SteamPress
{
    public Instruction current;
    public Stack<int> pressStack = new(32);
    
    

    private readonly double[] instrPressures = new double[32];
    public SteamPress()
    {
        instrPressures[(int)InstructType.None] = 0;
        instrPressures[(int)InstructType.Put] = 0.5;
        instrPressures[(int)InstructType.Add] = 1;
        instrPressures[(int)InstructType.Sub] = 1;
        instrPressures[(int)InstructType.Mul] = 2;
        instrPressures[(int)InstructType.Div] = 2;
        instrPressures[(int)InstructType.PrintChar] = 1;

    }
    

    /// <returns>Successfull/!Successfull</returns> 
    public bool Press(ref Boiler boiler)
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
                Console.WriteLine((char)pressStack.Pop());
            break;
            
        }
    }
}

internal sealed class CodeBelt(int id, int pc, List<Instruction> instructions)
{
    public int id = id;
    public int pc = pc;
    public List<Instruction> instructions = instructions;
}

public sealed class SteamMachine
{   
    private List<CodeBelt> belts = new();
    private CodeBelt currentBelt;

    private Boiler boiler;
    private WaterPump pump = new();
    private SteamPress press = new();
    

    public SteamMachine(List<List<Instruction>> instructions, int containerAmount)
    {
        boiler = new(containerAmount);
        boiler.water = 10;
        boiler.pressureLimit = 150;

        int idCount = 0;
        foreach(List<Instruction> belt in instructions)
        {
            belts.Add(new(idCount, idCount == 0 ? 0 : -1, belt));
            idCount++;
        }

        currentBelt = belts[0];
        
    }


    private string placeReserve = 
                "                                      "+
                "                                      ";
    private void DisplayState((int,int) resetPos, bool hasMoveAm)
    {
        Console.SetCursorPosition(resetPos.Item1, resetPos.Item2-1);
            Console.WriteLine(
                $"Water: {Math.Round(boiler.water,4)}L{placeReserve}\n"+
                $"Coal: {Math.Round(boiler.coal,4)}kg{placeReserve}\n" +
                $"Steam: {Math.Round(boiler.steam,4)}L{placeReserve}\n"+
                $"Pressure: {Math.Round(boiler.pressure,4)}bar{$"{(boiler.pressure > 120 ? "<!>" : "")}"}{placeReserve}\n"+
                $"{(boiler.selectedContainer > -1 ? $"Container({boiler.selectedContainer}): {boiler.steamContainers[boiler.selectedContainer]}bar" : "")}{placeReserve}\n"+
                $"PC (belt {currentBelt.id}): {currentBelt.pc} {placeReserve}\n"+
                $"Work: {(hasMoveAm ? " (move) " : " ")}{currentBelt.instructions[currentBelt.pc].type} {(currentBelt.instructions[currentBelt.pc].pullValue? $"pull ({press.pressStack.Peek()})" : currentBelt.instructions[currentBelt.pc].value)}{placeReserve}\n"
                /*$"Stack: {((Func<string>)(() => {string s = ""; foreach(int val in press.pressStack.ToArray()){ s+=val.ToString();}return s;}))()}{placeReserve}"*/);
    }

    public void Execute(bool showDisplay = true, bool manualTick = false, int tickDelay = 0)
    {

        Console.CursorVisible = false;
        Console.WriteLine("");

        var resetPos = Console.GetCursorPosition();
        
        int moveAmount = 0;

        while (currentBelt.pc < currentBelt.instructions.Count && currentBelt.pc >= 0)
        {

            if(manualTick) { Console.ReadKey(); }
            
            if(showDisplay)
            {
                DisplayState(resetPos, moveAmount != 0);
            }
            
            if(tickDelay>0)
            {
                Thread.Sleep(tickDelay);
            }
            

            BoilerState boilerState = boiler.Tick();

            if(boilerState == BoilerState.Exploded)
            {
                throw new BoilerExplosion();
            } 

            Instruction current = currentBelt.instructions[currentBelt.pc]; 
            if(current.pullValue) 
            { 
                current.value = press.pressStack.Peek(); 
            }

            if(moveAmount != 0)
            {
                TickEnd();
                if(boiler.pressure>=1)
                {   
                    boiler.TakePressure(1);
                    int dir = int.CopySign(1, moveAmount);
                    currentBelt.pc += dir;

                    if(currentBelt.pc >= currentBelt.instructions.Count){ break; }
                    else if(currentBelt.pc <= 0) 
                    { 
                        currentBelt.pc = 0;
                        moveAmount = 0; 
                        continue; 
                    }

                    moveAmount -= dir;
                    continue;
                }
                
                moveAmount = 0;
                continue;
            }

            switch(current.type)
            {
                case InstructType.UsePump:
                    if(!pump.Pump(ref boiler, Math.Max(0, current.value)))
                    {
                        boiler.TakePressure(boiler.releaseValve);
                        continue;
                    }
                break;

                case InstructType.AddCoal:
                    boiler.coal += Math.Max(0, current.value); 
                break;

                case InstructType.SetValve:
                    
                    boiler.releaseValve = Math.Max(0, current.value*0.1);
                    
                break;

                case InstructType.WaitForPressureHigh:
                    if(boiler.pressure<current.value*0.1d) { TickEnd(); continue;}
                break;

                case InstructType.WaitForPressureLow:
                    if(boiler.pressure>current.value*0.1d) { TickEnd(); continue;}
                break;

                case InstructType.Move:
                    if(boiler.pressure >= 1)
                    {
                        moveAmount = current.value;
                        continue;
                    }
                break;
                
                case InstructType.SwitchBelt:
                    currentBelt = belts[current.value];
                    if(currentBelt.pc+1 >= currentBelt.instructions.Count) {currentBelt.pc = -1;}
                break;

                case InstructType.PushBeltID:
                    press.pressStack.Push(currentBelt.id);
                    currentBelt.pc++;
                continue;

                case InstructType.Pull:
                    Console.WriteLine(press.pressStack.Pop());
                break;

                case InstructType.Press:
                    if(!press.Press(ref boiler))
                    {
                        TickEnd();
                        continue;
                    }
                break;

                case InstructType.SetContainer:
                    boiler.selectedContainer = Math.Clamp(current.value, 0, boiler.steamContainers.Length);
                    
                break;

                case InstructType.DisconnectContainer:
                    boiler.selectedContainer = -1;
                break;

                case InstructType.LoadContainer:
                    if(boiler.selectedContainer > -1)
                    {
                        boiler.AddPressure(boiler.steamContainers[boiler.selectedContainer]);
                        boiler.steamContainers[boiler.selectedContainer] = 0;
                    }
                break;

                default:
                    press.current = current;
                break;

            }

            TickEnd();

            if(current.pullValue) { press.pressStack.Pop(); }
            currentBelt.pc++;
            

        }
        
        Console.CursorVisible = true;

    }

    private void TickEnd()
    {
        

        if(boiler.selectedContainer > -1)
        {
            if(boiler.pressure<boiler.releaseValve) 
            {
                boiler.steamContainers[boiler.selectedContainer] += boiler.pressure;
                boiler.pressure = 0;
                boiler.steam = 0;
                return;
            }
            else if(boiler.pressure>0)
            { 
                boiler.steamContainers[boiler.selectedContainer] += boiler.releaseValve;
                boiler.TakePressure(boiler.releaseValve);
            }
            
        }
        else
        {
            boiler.TakePressure(boiler.releaseValve);
        }
    }

}