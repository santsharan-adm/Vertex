using IPCSoftware.App.Bending.Models;

public class BendingMoitorModel1
{
    public string Product { get; set; }

    public string QRCode { get; set; }

    //-- Load (N) --//

    public ParameterLImitValues Load { get; set; }

    //-- Temp (°C) --//

    public ParameterLImitValues Temprature { get; set; }

    //-- Bending Time --//

    public double BendingTime { get; set; }
    
    //-- Result --//

    public bool Result { get; set; }
}