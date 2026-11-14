namespace FiniteSteamMachine.Interpreter;

public sealed class WaterPump
{
    public static double conversionFactor = 0.8;
    public bool Pump(Boiler boiler, double usedPressure)
    {
        if(boiler.pressure >= usedPressure)
        {
            boiler.TakePressure(usedPressure);
            boiler.water += usedPressure*conversionFactor;
            return true;
        }
        return false;
    }
}