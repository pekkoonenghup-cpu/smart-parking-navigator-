namespace CarparkAvailability.ApiApp.Models;

public sealed record HdbCarparkInfo(
    string CarparkNo,
    string Address,
    double XCoord,
    double YCoord,
    string CarparkType,
    string TypeOfParkingSystem,
    string ShortTermParking,
    string FreeParking,
    string NightParking,
    int CarparkDecks,
    double GantryHeight,
    bool CarparkBasement,
    double Latitude,
    double Longitude);
