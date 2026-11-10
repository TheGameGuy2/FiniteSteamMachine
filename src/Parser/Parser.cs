using System.Xml.Serialization;
using FiniteSteamMachine.Interpreter;

namespace FiniteSteamMachine.Parser;


public class Parser
{
    
    private List<string> code;

    private static Dictionary<string, InstructType> instrMap;
    private static Dictionary<string, int> beltMap = new(); //Mapping file names to belt ids (indicies)
    private static int beltCounter = 0; //used for belt id'S

    private static List<List<Instruction>> instructionBelts = new();

    public Parser(string text, string fileName)
    {
        beltMap.Add(fileName, beltCounter);
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
            {"disconnect_container", InstructType.DisconnectContainer}, //disconects current container
            {"switch_belt", InstructType.SwitchBelt}, //switch instruction belt to given file
            {"push_belt_number", InstructType.PushBeltID},
            {"set_write_chr", InstructType.PrintChar}
        };
    }


    public List<List<Instruction>> Parse()
    {
        int lineCount = 1;

        List<Instruction> outInstructions = new();
        instructionBelts.Add(outInstructions);

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
                        if(instrct[0] == "switch_belt") //Is belt? (needs file parsing)
                        {
                            if(instrct[1] == "pull")
                            {
                                MakeInstruction(outInstructions, InstructType.SwitchBelt, 0, true);
                            }
                            else if(int.TryParse(instrct[1], out int belt))
                            {
                                MakeInstruction(outInstructions, InstructType.SwitchBelt, belt);
                            }
                            else if(beltMap.TryGetValue(instrct[1], out int beltID))
                            {
                                MakeInstruction(outInstructions, InstructType.SwitchBelt, beltID);
                            }
                            else
                            {
                                beltCounter++;
                                MakeInstruction(outInstructions, InstructType.SwitchBelt, beltCounter);
                                Parser p = new(File.ReadAllText(instrct[1]), instrct[1]);
                                p.Parse();
                            }
                            continue;
                            
                        }
                        
                        //Is arg int?
                        if(int.TryParse(instrct[1], out int result))
                        {
                            MakeInstruction(outInstructions, type, result);
                        }
                        else if(instrct[1] == "pull") //Is arg dynamic?
                        {
                            MakeInstruction(outInstructions, type, 0, true);
                        }
                        else //neither -> error
                        {
                            throw new($"Item {lineCount}: Expected int or pull.");
                        }
                    }
                    else
                    {
                        MakeInstruction(outInstructions, type, 0);
                    }
                    
                }
                else
                {
                    if(instrct[0].Length>0){ throw new($"Item {lineCount}: Unknown Item '{instrct[0]}'"); }
                    
                }
            }
            
            
            lineCount++;
            
        }
        
        return instructionBelts;
    }

    

    private void MakeInstruction(List<Instruction> outInstructions, InstructType type, int arg, bool pullValue = false)
    {
        outInstructions.Add(new(type, arg){pullValue = pullValue});
    }

}

