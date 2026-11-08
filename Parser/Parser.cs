using System.Xml.Serialization;
using FiniteSteamMachine.Interpreter;

namespace FiniteSteamMachine.Parser;


public class Parser
{
    
    private List<string> code;
    private List<Instruction> outInstructions = new();

    private Dictionary<string, InstructType> instrMap;

    public Parser(string text)
    {
        code = text.Split("\n").ToList();

        instrMap = new() {
            {"pump", InstructType.UsePump}, //use water pump
            {"coal", InstructType.AddCoal}, //Add 2kg of coal
            {"drill", InstructType.Drill}, //use coal drill
            {"release_valve", InstructType.SetValve}, //set release valve
            {"high_pressure_switch", InstructType.WaitForPressureHigh}, //wait till pressure is above given value
            {"low_pressure_switch", InstructType.WaitForPressureLow}, //wait till pressure is below given value
            {"move_belt", InstructType.Move}, //move belt
            {"set_add", InstructType.Add}, //set press mode
            {"set_sub", InstructType.Sub},
            {"set_mul", InstructType.Mul},
            {"set_div", InstructType.Div},
            {"press", InstructType.Press}, //Execute press
            {"set_put", InstructType.Put}, 
            {"pull", InstructType.Pull}, //pull value from the operation stack and push to output stack.
            {"set_container", InstructType.SetContainer}, //sets current container for the release valve
            {"load_container", InstructType.LoadContainer}, //loads current container into boiler
            {"disconnect_container", InstructType.DisconnectContainer} //disconects current container
        };
    }


    public List<Instruction> Parse()
    {
        int lineCount = 1;
        foreach(string line in code)
        {
            string cline = line.Trim();
            cline = cline.Split('!')[0]; //remove comments
            cline = cline.Trim();
            
            string[] instrct = cline.Split(" ");

            
            if(instrct.Length >= 1)
            {
                //Is keyword?
                if(instrMap.TryGetValue(instrct[0], out InstructType type))
                {
                    //Has arg?
                    if(instrct.Length>1)
                    {
                        //Is arg int?
                        if(int.TryParse(instrct[1], out int result))
                        {
                            MakeInstruction(type, result);
                        }
                        else if(instrct[1] == "pull") //Is arg dynamic?
                        {
                            MakeInstruction(type, 0, true);
                        }
                        else //neither -> error
                        {
                            throw new($"Item {lineCount}: Expected int or pull.");
                        }
                    }
                    else
                    {
                        MakeInstruction(type, 0);
                    }
                    
                }
                else
                {
                    if(instrct[0].Length>0){ throw new($"Item {lineCount}: Unknown Item '{instrct[0]}'"); }
                    
                }
            }
            
            
            lineCount++;
            
        }

        return outInstructions;
    }

    private void MakeInstruction(InstructType type, int arg, bool pullValue = false)
    {
        outInstructions.Add(new(type, arg){pullValue = pullValue});
    }

}

