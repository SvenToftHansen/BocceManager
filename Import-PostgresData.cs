/// Quick migration script to import PostgreSQL data into SQLite
/// This reads the dump_inserts.sql backup and imports players into the new database
///
/// Usage:
///   dotnet run Import-PostgresData.cs
/// Or just call ImportPlayerData() from code after rebuilding

using BocceManager.Data;
using BocceManager.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BocceManager.Scripts;

public static class DataImporter
{
    public static void ImportPlayersFromBackup()
    {
        using var db = new BocceDbContext();

        // Create Spring League if it doesn't exist
        var springLeague = db.Leagues.FirstOrDefault(l => l.Name == "Spring League");
        if (springLeague == null)
        {
            springLeague = new League
            {
                Name = "Spring League",
                Description = "Imported from PostgreSQL backup",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Leagues.Add(springLeague);
            db.SaveChanges();
            Console.WriteLine($"✓ Created Spring League (ID: {springLeague.Id})");
        }
        else
        {
            Console.WriteLine($"✓ Spring League already exists (ID: {springLeague.Id})");
        }

        // Players from backup (extracted from dump_inserts.sql)
        var playersData = new[]
        {
            ("Jerry", "Anderson", null, null, "765"),
            ("Sandy", "Anderson", "anderksandy@gmail.com", "(507) 221-0768", "619"),
            ("Bonnie", "Ask", "askbon74@gmail.com", "(320) 760-9097", "270"),
            ("Phil", "Ask", "ask.phil71@gmail.com", "(320) 219-1740", "270"),
            ("Sara", "Austin", "cs970austin@gmail.com", "(719) 557-9261", "891"),
            ("Darryl", "Banitt", "dlbanitt@gmail.com", "(507) 298-0936", "461"),
            ("Kairi", "Banitt", "dalebanitt@gmail.com", "(651) 764-0902", null),
            ("Lori", "Banitt", "dlbanitt@gmail.com", "(507) 298-0972", "461"),
            ("Darryl", "Bauer", "7dabdab7@gmail.com", "(369) 391-0393", "986"),
            ("Kellie", "Bauer", "kellybauer@frontier.com", "(360) 672-0633", "986"),
            ("Gail", "Beech", "beech.gail@gmail.com", "(701) 640-1366", "853"),
            ("Paul", "Beech", "beech.paul2010@gmail.com", "(701) 640-3466", "853"),
            ("Brenda", "Belgois", null, null, null),
            ("Craig", "Belleau", "craigorsue@gmail.com", "(920) 227-7859", null),
            ("Sue", "Belleau", "craigorsue@gmail.com", "(920) 664-2416", "651"),
            ("Donna", "Bengtson", "sbengtson6@gmail.com", "(320) 291-4994", "459"),
            ("Evy", "Billings", "evybillings@earthlink.net", "(712) 380-2300", "961"),
            ("Ken", "Billings", "evybillings@earthlink.net", "(712) 380-2300", "961"),
            ("Rita", "Blosser", "blosser58@gmail.com", "(785) 640-0664", "147"),
            ("Ruth", "Braun", "ruth.braun@sasktel.com", "(306) 731-7965", "396"),
            ("Wes", "Braun", "wes.braun@sasktel.com", "(306) 535-5883", "396"),
            ("Fred", "Bregar", "fred.bregar@aol.com", "(719) 568-6714", "762"),
            ("Diane", "Breitbach", "diane.breitbach@gmail.com", "(563) 599-3743", "488"),
            ("Steve", "Breitbach", "sbreits49@gmail.com", "(563) 599-3743", "488"),
            ("Dennis", "Bridgeman", "thephoneman51@yahoo.com", "(480) 226-3657", "599"),
            ("Rock", "Bridges", "rockbridges@gmail.com", "(641) 590-4120", "4"),
            ("Sue", "Bridges", "suziebridges@gmail.com", "(641) 590-2443", "4"),
            ("Dennis", "Brown", "janethelen75@gmail.com", "(780) 335-0422", "923"),
            ("Janet", "Brown", "janethelen75@gmail.com", "(780) 335-0422", "923"),
            ("Ben", "Browning", "greg.browning@shaw.ca", "(403) 702-1846", "895"),
            ("Greg", "Browning", "greg.g.browning@gmail.com", "(403) 702-1846", "895"),
            ("Mary", "Bulthuis", "marellen48@gmail.com", "(302) 266-0369", "655"),
            ("Ron", "Bulthuis", "rbulthuis49@gmail.com", "(701) 740-1920", "655"),
            ("Bob", "Burgi", "rburgs2@icloud.com", "(480) 323-8574", "29"),
            ("Jenny", "Burgi", "rburgs2@icloud.com", "(480) 323-8574", "29"),
            ("Fredra", "Carlson", "rickfredrac@gmail.com", "(785) 342-7376", "678"),
            ("Deb", "Casper", "dcasper58@gmail.com", "(612) 910-8539", "212"),
            ("Paul", "Casper", "pcasper1056@gmail.com", "(763) 420-7936", "212"),
            ("Leslie", "Chamberlin", "roycham59@gmail.com", "(480) 466-3231", "300"),
            ("Roy", "Chamberlin", "roycham59@gmail.com", "(480) 466-3231", "300"),
            ("Jim", "Champoux", "soxfan031966@gmail.com", "(508) 330-2511", "247"),
            ("Maureen", "Champoux", "maureen.Champoux@yahoo.com", "(508) 330-2511", "247"),
            ("Greg", "Clark", "ngregclark@gmail.com", "(623) 556-3210", null),
            ("Nona", "Clark", "ngregclark@gmail.com", "(623) 556-3210", null),
            ("Betty", "Cross", "blc10500@yahoo.com", "(989) 390-3769", "311"),
            ("Sheryl", "Dahlke", "shermrichp@gmail.com", "(651) 200-0987", "419"),
        };

        int importedCount = 0;
        foreach (var (firstName, lastName, email, phone, lotNumber) in playersData)
        {
            // Check if player already exists (by first and last name)
            var existing = db.Players.FirstOrDefault(p => p.FirstName == firstName && p.LastName == lastName);
            if (existing != null)
            {
                Console.WriteLine($"⊘ {firstName} {lastName} already exists (ID: {existing.Id})");
                continue;
            }

            var player = new Player
            {
                FirstName = firstName,
                LastName = lastName,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                LotNumber = string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Players.Add(player);
            importedCount++;
        }

        db.SaveChanges();
        Console.WriteLine($"\n✓ Import complete: {importedCount} new players added");
        Console.WriteLine($"✓ Total players in database: {db.Players.Count()}");
    }
}
