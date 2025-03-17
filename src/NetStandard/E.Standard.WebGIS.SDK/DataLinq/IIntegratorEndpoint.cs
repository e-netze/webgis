using System.Threading.Tasks;

namespace E.Standard.WebGIS.SDK.DataLinq;

public interface IDataLinqEndpoint
{
    bool Init(string connectionString);


    //
    // Rückgabe ist immer ein Array aus anonymen Objekten
    // Alle Objekte sollten die gleiche Struktur (die gleichen Attribute) haben
    //
    Task<object[]> Select(string statement);
}
