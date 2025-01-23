using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

class Program
{
    static void Main()
    {
        //A kommentek azzal a céllal készültek, hogy megkönnyítsék saját magam számára a munkám nyomonkövetését.

        // Bemeneti fájl elérési útja
        string filePath = @"C:\Users\Martin\Desktop\orak\beadandosajat\beadando\shapedifference\adatok.txt";  // A fájl neve, amit be kell olvasni

        // Ellenörzöm, hogy létezik-e a fájl
        if (!File.Exists(filePath))
        {
            Console.WriteLine("A fájl nem található!");
            return;
        }

        // Geometriák beolvasása a fájlból
        var geometries = LoadGeometries(filePath);

        // Ellenőrzöm, hogy vannak-e érvényes geometriák
        if (geometries.Count == 0)
        {
            Console.WriteLine("Nem található érvényes geometria a fájlban!");
            return;
        }

        // Szimmetrikus különbség kiszámítása
        var symmetricDifference = CalculateSymmetricDifference(geometries);


         // Eredmény WKT formátumban
        var writer = new WKTWriter();
        var wktResults = new List<string>();

        if (symmetricDifference is GeometryCollection geometryCollection)
        {
            foreach (var geometry in geometryCollection.Geometries)
            {
                wktResults.Add(writer.Write(geometry));
            }
        }
        else
        {
            wktResults.Add(writer.Write(symmetricDifference));
        }

        // Eredmény kiírása fájlba külön sorokba
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string outputFilePath = Path.Combine(desktopPath, "eredmeny.txt");
        File.WriteAllLines(outputFilePath, wktResults);

        // Eredmény kiírása konzolra külön sorokba
        Console.WriteLine("A szimmetrikus különbség eredménye:");
        foreach (var wkt in wktResults)
        {
            Console.WriteLine(wkt);
        }

        Console.WriteLine($"Az eredmény fájlba íródott: {Path.GetFullPath(outputFilePath)}");
    }
    

    // Geometriák beolvasása a fájlból WKT formátumban
    static List<Geometry> LoadGeometries(string filePath)
    {
        var geometries = new List<Geometry>();
        var reader = new WKTReader();

        foreach (var line in File.ReadAllLines(filePath))
        {
            try
            {
                // WKT formátumban beolvasom a geometriai objektumot
                var geometry = reader.Read(line);
                geometries.Add(geometry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba történt a geometriák beolvasásakor: {ex.Message}");
            }
        }

        return geometries;
    }

    // Szimmetrikus különbség kiszámítása heterogén geometriákra
    static Geometry CalculateSymmetricDifference(List<Geometry> geometries)
    {
        if (geometries == null || geometries.Count == 0)
        {
            throw new ArgumentException("A geometria lista nem lehet üres.");
        }

        // Szétváloggatom a geometriákat típus szerint
        var polygons = geometries.OfType<Polygon>().Cast<Geometry>().ToList();
        var lineStrings = geometries.OfType<LineString>().Cast<Geometry>().ToList();
        var points = geometries.OfType<Point>().Cast<Geometry>().ToList();

        // Kiszámitom a szimmetrikus különbséget az azonos tipusú geometriákra
        Geometry polygonDifference = ComputeSymmetricDifferenceForType(polygons);
        Geometry lineStringDifference = ComputeSymmetricDifferenceForType(lineStrings);
        Geometry pointDifference = ComputeSymmetricDifferenceForType(points);

        // Egyesítem az eredményeket egy GeometryCollection-be
        var factory = new GeometryFactory();
        var allDifferences = new List<Geometry> { polygonDifference, lineStringDifference, pointDifference }
            .Where(g => g != null)
            .ToArray();

        return factory.BuildGeometry(allDifferences);
    }

    // Szimmetrikus különbség számítása 
    static Geometry ComputeSymmetricDifferenceForType(List<Geometry> geometries)
    {
        if (geometries == null || geometries.Count == 0)
            return null;

        Geometry result = geometries[0];
        for (int i = 1; i < geometries.Count; i++)
        {
            result = result.SymmetricDifference(geometries[i]);
        }
        return result;
    }
}
