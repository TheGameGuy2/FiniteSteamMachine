using FiniteSteamMachine.Exceptions;

namespace FiniteSteamMachine.Interpreter;

//Not sure why I added the State enum, probably had more planned for it.
public enum BoilerState
{
    Ok,
    NoCoal,
    Exploded
}

public sealed class Boiler(int containerAmount)
{
    //Water in kg, steam out in L
    public double coal, water, steam, pressure, pressureLimit, releaseValve;
    public double[] steamContainers = new double[containerAmount];
    public int selectedContainer = -1;

    public BoilerState Tick()
    {
        if(coal>0) { coal--; }
        else { return BoilerState.NoCoal; }

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