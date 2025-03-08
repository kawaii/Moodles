using Moodles.Moodles.Services.Interfaces;

namespace Moodles.Moodles.Services.Structs;

internal struct PetSheetData : IPetSheetData
{
    public int Model { get; private set; }
   
    public PetSheetData(int model)
    {
        Model = model;
    }
}
