using System.Data.Common;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using FiniteSteamMachine.Exceptions;

namespace FiniteSteamMachine.Interpreter;



public class CoalDrill
{
    public double pressurePerCoal = 2;

    public bool Drill(Boiler boiler, double inputPressure)
    {
        if(boiler.pressure>=inputPressure)
        {
            boiler.coal += Math.Floor(inputPressure);   
            return true;
        }
        return false;
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
    

    private string placeReserve = 
                "                                      "+
                "                                      ";
    private StringBuilder outputBuilder = new(1024);

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

    private void DisplayState(bool hasMoveAm)
    {
        var resetPos = Console.GetCursorPosition();

        outputBuilder.Clear();

        outputBuilder.AppendFormat("Water: {0}L{1}\n", Math.Round(boiler.water, 4), placeReserve);
        outputBuilder.AppendFormat("Coal: {0}kg{1}\n", Math.Round(boiler.coal, 4), placeReserve);
        outputBuilder.AppendFormat("Steam: {0}L{1}\n", Math.Round(boiler.steam, 4), placeReserve);
        outputBuilder.AppendFormat("Pressure: {0}bar ", Math.Round(boiler.pressure, 4));
        
        if(boiler.pressure > 120)
        {
            outputBuilder.Append("<!>");
        }
        outputBuilder.AppendFormat("{0}\n", placeReserve);

        if(boiler.selectedContainer > -1)
        {
            outputBuilder.AppendFormat("Container({0}): {1}bar {2}\n", boiler.selectedContainer, boiler.steamContainers[boiler.selectedContainer], placeReserve);
        }

        outputBuilder.AppendFormat("PC (belt {0}): {1}{2}\n", currentBelt.id, currentBelt.pc, placeReserve);
        
        outputBuilder.Append("Work: ");
        if(hasMoveAm)
        {
            outputBuilder.Append("(move) ");
        }

        outputBuilder.Append(currentBelt.instructions[currentBelt.pc].type);
        
        if(currentBelt.instructions[currentBelt.pc].pullValue)
        {
            outputBuilder.AppendFormat(" pull ({0}){1}\n", press.pressStack.Peek(), placeReserve);
        }
        else
        {
            outputBuilder.AppendFormat(" {0}{1}\n", currentBelt.instructions[currentBelt.pc].value, placeReserve);
        }
        

        Console.Write(outputBuilder);
        Console.SetCursorPosition(resetPos.Left, resetPos.Top);
        /*$"Stack: {((Func<string>)(() => {string s = ""; foreach(int val in press.pressStack.ToArray()){ s+=val.ToString();}return s;}))()}{placeReserve}");*/
    }

    public void Execute(bool showDisplay = true, bool manualTick = false, int tickDelay = 0)
    {

        Console.CursorVisible = false;
        Console.WriteLine("");
        
        int moveAmount = 0;

        while (currentBelt.pc < currentBelt.instructions.Count && currentBelt.pc >= 0)
        {

            if(manualTick) { Console.ReadKey(); }
            
            if(showDisplay)
            {
                DisplayState(moveAmount != 0);
            }
            
            
            if(tickDelay > 0)
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
                if(boiler.pressure >= 1)
                {   
                    boiler.TakePressure(1);

                    int dir = int.CopySign(1, moveAmount);
                    currentBelt.pc += dir;

                    if(currentBelt.pc >= currentBelt.instructions.Count)
                    { 
                        break; 
                    }
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
                    if(!pump.Pump(boiler, Math.Max(0, current.value)))
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
                    if(boiler.pressure < current.value*0.1d) { TickEnd(); continue;}
                break;

                case InstructType.WaitForPressureLow:
                    if(boiler.pressure > current.value*0.1d) { TickEnd(); continue;}
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
                    if(!press.Press(boiler))
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
            if(boiler.pressure < boiler.releaseValve) 
            {
                boiler.steamContainers[boiler.selectedContainer] += boiler.pressure;
                boiler.pressure = 0;
                boiler.steam = 0;
                return;
            }
            else if(boiler.pressure > 0)
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