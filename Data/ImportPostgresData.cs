using BocceManager.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BocceManager.Data;

/// <summary>
/// Imports player data, spare lists, and looking-for-team records from PostgreSQL backup.
/// Run via: var importer = new ImportPostgresData(); importer.Execute();
/// </summary>
public class ImportPostgresData
{
    private readonly BocceDbContext _db;

    public ImportPostgresData()
    {
        _db = new BocceDbContext();
    }

    public void Execute()
    {
        try
        {
            Console.WriteLine("Starting PostgreSQL data import...\n");

            // Step 1: Create Spring League
            var league = CreateOrGetSpringLeague();
            Console.WriteLine($"✓ Spring League ready (ID: {league.Id})");

            // Step 2: Create Spring Season
            var season = CreateOrGetSpringseason(league);
            Console.WriteLine($"✓ Spring Season ready (ID: {season.Id})");

            // Step 3: Import players
            var importedCount = ImportPlayers();
            Console.WriteLine($"✓ Imported {importedCount} players");

            // Step 4: Populate LookingForTeams
            var lookingCount = PopulateLookingForTeams(league, season);
            Console.WriteLine($"✓ Created {lookingCount} LookingForTeams entries");

            // Step 5: Create and populate SpareLists
            var spareCount = PopulateSpareLists(league);
            Console.WriteLine($"✓ Created SpareList with {spareCount} players");

            Console.WriteLine($"\n✓ Import complete!");
            Console.WriteLine($"  Total players: {_db.Players.Count()}");
            Console.WriteLine($"  Total LookingForTeams: {_db.LookingForTeams.Count()}");
            Console.WriteLine($"  Total SpareLists: {_db.SpareLists.Count()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    private League CreateOrGetSpringLeague()
    {
        var existing = _db.Leagues.FirstOrDefault(l => l.Name == "Spring League");
        if (existing != null) return existing;

        var league = new League
        {
            Name = "Spring League",
            Description = "Spring season league imported from PostgreSQL",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Leagues.Add(league);
        _db.SaveChanges();
        return league;
    }

    private Season CreateOrGetSpringseason(League league)
    {
        var existing = _db.Seasons.FirstOrDefault(s => s.Name == "Spring Season" && s.LeagueId == league.Id);
        if (existing != null) return existing;

        var season = new Season
        {
            LeagueId = league.Id,
            Name = "Spring Season",
            Status = "building",
            IsCurrent = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Seasons.Add(season);
        _db.SaveChanges();
        return season;
    }

    private int ImportPlayers()
    {
        var playersData = GetPlayersData();
        int importedCount = 0;

        foreach (var (id, firstName, lastName, email, phone, lotNumber) in playersData)
        {
            // Skip if already exists
            if (_db.Players.Any(p => p.FirstName == firstName && p.LastName == lastName))
                continue;

            var player = new Player
            {
                FirstName = firstName,
                LastName = lastName,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                LotNumber = string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber,
                IsActive = true,
                LookingForTeam = false,
                CreatedAt = DateTime.UtcNow
            };
            _db.Players.Add(player);
            importedCount++;
        }

        _db.SaveChanges();
        return importedCount;
    }

    private int PopulateLookingForTeams(League league, Season season)
    {
        // Player IDs from looking_for_team table in PostgreSQL backup
        var lookingForTeamPlayerIds = new[] { 1, 2, 8, 10, 17, 28, 46, 47, 53, 55, 74, 140, 149, 163, 164, 191, 223, 224, 237, 238 };
        int createdCount = 0;

        foreach (var postgresPlayerId in lookingForTeamPlayerIds)
        {
            // Find the player by matching the sequence from the backup
            var player = GetPlayerByPostgresId(postgresPlayerId);
            if (player == null) continue;

            // Skip if already exists
            if (_db.LookingForTeams.Any(l => l.PlayerId == player.Id && l.LeagueId == league.Id))
                continue;

            var lookingForTeam = new LookingForTeam
            {
                LeagueId = league.Id,
                PlayerId = player.Id,
                TeamId = null
            };
            _db.LookingForTeams.Add(lookingForTeam);
            createdCount++;
        }

        _db.SaveChanges();
        return createdCount;
    }

    private int PopulateSpareLists(League league)
    {
        // Player IDs from spare_list_players table in PostgreSQL backup
        var spareListPlayerIds = new[] { 20, 21, 34, 37, 38, 39, 42, 43, 44, 45, 52, 54, 57, 58, 59, 60, 63, 64, 67, 68, 79, 82, 83, 84, 94, 95, 100, 101, 102, 106, 107, 115, 121, 122, 124, 125, 131, 134, 137, 149, 150, 160, 165, 167, 172, 178, 186, 196, 203, 204, 222, 232, 1, 2 };

        // Create or get the spare list
        var spareList = _db.SpareLists.FirstOrDefault(sl => sl.LeagueId == league.Id && sl.IsActive);
        if (spareList == null)
        {
            spareList = new SpareList
            {
                LeagueId = league.Id,
                PlayerId = spareListPlayerIds.First(),  // Temporary; we'll fix below
                IsActive = true
            };
            _db.SpareLists.Add(spareList);
            _db.SaveChanges();
        }

        int addedCount = 0;

        foreach (var postgresPlayerId in spareListPlayerIds)
        {
            var player = GetPlayerByPostgresId(postgresPlayerId);
            if (player == null) continue;

            // Skip if already in the spare list
            if (_db.SpareLists.Any(sl => sl.PlayerId == player.Id && sl.LeagueId == league.Id))
                continue;

            var spareListEntry = new SpareList
            {
                LeagueId = league.Id,
                PlayerId = player.Id,
                IsActive = true
            };
            _db.SpareLists.Add(spareListEntry);
            addedCount++;
        }

        _db.SaveChanges();
        return addedCount;
    }

    /// <summary>
    /// Gets a player by matching the sequence order from the PostgreSQL backup.
    /// The backup player IDs don't directly match SQLite IDs, so we match by name.
    /// </summary>
    private Player? GetPlayerByPostgresId(int postgresId)
    {
        var playersData = GetPlayersData();
        var playerInfo = playersData.FirstOrDefault(p => p.Id == postgresId);

        if (playerInfo == default) return null;

        return _db.Players.FirstOrDefault(p => p.FirstName == playerInfo.firstName && p.LastName == playerInfo.lastName);
    }

    /// <summary>
    /// Player data extracted from PostgreSQL backup (dump_inserts.sql lines 268-467+)
    /// Format: (postgresId, firstName, lastName, email, phone, lotNumber)
    /// </summary>
    private List<(int Id, string firstName, string lastName, string? email, string? phone, string? lotNumber)> GetPlayersData()
    {
        return new()
        {
            (3, "Jerry", "Anderson", null, null, "765"),
            (4, "Sandy", "Anderson", "anderksandy@gmail.com", "(507) 221-0768", "619"),
            (5, "Bonnie", "Ask", "askbon74@gmail.com", "(320) 760-9097", "270"),
            (6, "Phil", "Ask", "ask.phil71@gmail.com", "(320) 219-1740", "270"),
            (7, "Sara", "Austin", "cs970austin@gmail.com", "(719) 557-9261", "891"),
            (9, "Darryl", "Banitt", "dlbanitt@gmail.com", "(507) 298-0936", "461"),
            (10, "Kairi", "Banitt", "dalebanitt@gmail.com", "(651) 764-0902", null),
            (11, "Lori", "Banitt", "dlbanitt@gmail.com", "(507) 298-0972", "461"),
            (12, "Darryl", "Bauer", "7dabdab7@gmail.com", "(369) 391-0393", "986"),
            (13, "Kellie", "Bauer", "kellybauer@frontier.com", "(360) 672-0633", "986"),
            (14, "Gail", "Beech", "beech.gail@gmail.com", "(701) 640-1366", "853"),
            (15, "Paul", "Beech", "beech.paul2010@gmail.com", "(701) 640-3466", "853"),
            (16, "Brenda", "Belgois", null, null, null),
            (17, "Craig", "Belleau", "craigorsue@gmail.com", "(920) 227-7859", null),
            (18, "Sue", "Belleau", "craigorsue@gmail.com", "(920) 664-2416", "651"),
            (19, "Donna", "Bengtson", "sbengtson6@gmail.com", "(320) 291-4994", "459"),
            (20, "Evy", "Billings", "evybillings@earthlink.net", "(712) 380-2300", "961"),
            (21, "Ken", "Billings", "evybillings@earthlink.net", "(712) 380-2300", "961"),
            (22, "Rita", "Blosser", "blosser58@gmail.com", "(785) 640-0664", "147"),
            (23, "Ruth", "Braun", "ruth.braun@sasktel.com", "(306) 731-7965", "396"),
            (24, "Wes", "Braun", "wes.braun@sasktel.com", "(306) 535-5883", "396"),
            (25, "Fred", "Bregar", "fred.bregar@aol.com", "(719) 568-6714", "762"),
            (26, "Diane", "Breitbach", "diane.breitbach@gmail.com", "(563) 599-3743", "488"),
            (27, "Steve", "Breitbach", "sbreits49@gmail.com", "(563) 599-3743", "488"),
            (28, "Dennis", "Bridgeman", "thephoneman51@yahoo.com", "(480) 226-3657", "599"),
            (29, "Rock", "Bridges", "rockbridges@gmail.com", "(641) 590-4120", "4"),
            (30, "Sue", "Bridges", "suziebridges@gmail.com", "(641) 590-2443", "4"),
            (31, "Dennis", "Brown", "janethelen75@gmail.com", "(780) 335-0422", "923"),
            (32, "Janet", "Brown", "janethelen75@gmail.com", "(780) 335-0422", "923"),
            (33, "Ben", "Browning", "greg.browning@shaw.ca", "(403) 702-1846", "895"),
            (34, "Greg", "Browning", "greg.g.browning@gmail.com", "(403) 702-1846", "895"),
            (35, "Mary", "Bulthuis", "marellen48@gmail.com", "(302) 266-0369", "655"),
            (36, "Ron", "Bulthuis", "rbulthuis49@gmail.com", "(701) 740-1920", "655"),
            (37, "Bob", "Burgi", "rburgs2@icloud.com", "(480) 323-8574", "29"),
            (38, "Jenny", "Burgi", "rburgs2@icloud.com", "(480) 323-8574", "29"),
            (39, "Fredra", "Carlson", "rickfredrac@gmail.com", "(785) 342-7376", "678"),
            (40, "Deb", "Casper", "dcasper58@gmail.com", "(612) 910-8539", "212"),
            (41, "Paul", "Casper", "pcasper1056@gmail.com", "(763) 420-7936", "212"),
            (42, "Leslie", "Chamberlin", "roycham59@gmail.com", "(480) 466-3231", "300"),
            (43, "Roy", "Chamberlin", "roycham59@gmail.com", "(480) 466-3231", "300"),
            (44, "Jim", "Champoux", "soxfan031966@gmail.com", "(508) 330-2511", "247"),
            (45, "Maureen", "Champoux", "maureen.Champoux@yahoo.com", "(508) 330-2511", "247"),
            (46, "Greg", "Clark", "ngregclark@gmail.com", "(623) 556-3210", null),
            (47, "Nona", "Clark", "ngregclark@gmail.com", "(623) 556-3210", null),
            (48, "Betty", "Cross", "blc10500@yahoo.com", "(989) 390-3769", "311"),
            (49, "Sheryl", "Dahlke", "shermrichp@gmail.com", "(651) 200-0987", "419"),
            (50, "Karen", "Daniel", "karendaniel@hotmail.com", "(970) 542-0842", "528"),
            (51, "RW", "Daniel", "golddaniel@hotmail.com", "(970) 451-7406", "528"),
            (52, "Brenda", "Davis", "bkdavis29@yahoo.com", "(218) 390-6348", "637"),
            (53, "Dennis", "Davis", "suziq201084@gmail.com", "(651) 308-1541", null),
            (54, "Ron", "Davis", "trdavis725@yahoo.com", "(218) 390-6348", "637"),
            (55, "Sue", "Davis", "suziq201084@gmail.com", "(651) 308-1541", null),
            (56, "Dwayne", "DeBoer", "deboer11@netscape.net", "(712) 541-7958", "269"),
            (57, "Denise", "Delaney", "denisedelaney71@gmail.com", "(605) 695-9977", "284"),
            (58, "William", "Delaney", "williamdelaney75@gmail.com", "(605) 695-9495", "284"),
            (59, "Cheryl", "Donohoe", "gregdonohoe@shaw.ca", "(778) 686-4660", "1035"),
            (60, "Greg", "Donohoe", "gregdonohoe@shaw.ca", "(778) 686-4660", "1035"),
            (61, "Deb", "Dougherty", "ddougherty1915@gmail.com", "(317) 502-5515", "683"),
            (62, "Mike", "Dougherty", "ddougherty1915@gmail.com", "(317) 502-5517", "683"),
            (63, "Jerry", "Eastin", "jk_east10@yahoo.com", "(719) 691-5474", "699"),
            (64, "Karen", "Eastin", "jk_east10@yahoo.com", "(719) 688-0474", "699"),
            (65, "Lowell", "Eichenberger", "lowelleichenberger@gmail.com", "(515) 420-3891", "751"),
            (66, "Sue", "Ellsworth", "docnsue@yahoo.com", "(515) 320-1896", "935"),
            (67, "Pat", "Fontana", "ylipat@yahoo.com", "(530) 218-6932", "766"),
            (68, "Pete", "Fontana", "ylipat@yahoo.com", "(530) 218-6932", "766"),
            (69, "Dennis", "Forbeck", "forbeckellyn11.gmail.com", "(920) 205-8653", "171"),
            (70, "Ellyn", "Forbeck", "forbeckellyn11.gmail.com", "(920) 205-8653", "171"),
            (71, "Gale", "Fossen", "jcfossen@hotmail.com", "(701) 893-8516", "95"),
            (72, "Janet", "Fossen", "jcfossen@hotmail.com", "(701) 866-5108", "95"),
            (73, "Eileen", "Frendenburg", "vefredenburg@gmail.com", "(315) 882-3956", "874"),
            (74, "Bridget", "Gaff", "bagaff58@gmail.com", "(260) 615-8213", "520"),
            (75, "Cindy", "Gavin", "cjgavin@netwtc.net", "(563) 299-2522", "819"),
            (76, "Jim", "Gavin", "gavinjames@netwtc.net", "(563) 506-5112", "819"),
            (77, "Ann", "Gentry", "tagentry51@att.net", "(479) 629-3673", null),
            (78, "Ted", "Gentry", "tagentry51@att.net", "(479) 629-3673", null),
            (79, "Chris", "Gibson", "hootchris@thurston.com", "(360) 790-2223", "526"),
            (2, "Debra", "Aman", "dgaman@abe.midco.net", "(605) 380-6559", "929"),
            (8, "Dale", "Banitt", "dalebanitt@gmail.com", "(651) 764-0902", null),
            (80, "Kevin", "Gillett", "kevincnbrj5@gmail.com", "(319) 493-1280", "500"),
            (81, "Susan", "Gillett", "cnbrj5@gmail.com", "(319) 493-1330", "500"),
            (82, "Dan", "Glaza", "chitown830@gmail.com", "(630) 217-1906", "86"),
            (83, "Donna", "Gohde", "dgohde53563@gmail.com", "(608) 436-0146", "108"),
            (84, "Robert", "Gohde", "rgohde53563@gmail.com", "(608) 868-3422", "108"),
            (85, "Linda", "Goulet", "deerhunter491@aol.com", "(989) 450-6188", "632"),
            (86, "Norm", "Goulet", "deerhunter491@aol.com", "(989) 450-1458", "632"),
            (87, "Bill", "Greenlee", "greenleebk@gmail.com", "(303) 478-0449", "373"),
            (88, "Kay", "Greenlee", "greenleebk@gmail.com", "(303) 478-0449", "373"),
            (89, "Dan", "Grill", "bbqgrill@hotmail.com", "(507) 236-5338", "824"),
            (90, "Julie", "Grill", "momgrill@hotmail.com", "(507) 236-2251", "824"),
            (91, "Carol", "Grothus", "kgrothous@woh.rr.com", "(419) 863-9123", "635"),
            (92, "Bob", "Guck", "rjguck@gmail.com", "(320) 290-5398", "72"),
            (93, "Cheryl", "Guck", "cpguck@gmail.com", "(320) 492-0836", "72"),
            (94, "Greg", "Gutzman", "jeangreggutzman@gmail.com", "(605) 291-2198", "245"),
            (95, "Jean", "Gutzman", "jeangreggutzman@gmail.com", "(605) 270-9399", "245"),
            (96, "Chari", "Hamilton", "charihamilton68@gmail.com", "(507) 530-7762", "1010"),
            (97, "Doug", "Hamilton", "charihamilton68@gmail.com", "(507) 530-7762", "1010"),
            (98, "Barry", "Hanke", null, "(780) 221-5224", "262"),
            (99, "Susan", "Hansen", "susanannhansen@shaw.ca", "(403) 803-5749", "239"),
            (100, "Sven", "Hansen", "svenhansen@shaw.ca", "(403) 542-6689", "239"),
            (101, "Bob", "Haugerud", "bobhaugerud5@gmail.com", "(715) 377-8495", "818"),
            (102, "Nancie", "Hineline", "thehinelines@comcast.com", "(303) 668-4729", "544"),
            (103, "Carol", "Hoewisch", "choewisch@yahoo.com", "(920) 540-4141", "878"),
            (104, "Bob", "Holmes", "bobholmes76@gmail.com", "(507) 381-1488", "698"),
            (105, "Wendy", "Holmes", "xrwendy@hotmail.com", "(507) 381-1485", "698"),
            (106, "Paul", "Hultgren", "gwing2@msn.com", "(605) 270-3526", "49"),
            (107, "Ruth", "Hultgren", "gwing2@msn.com", "(605) 291-9517", "49"),
            (108, "Don", "James", "aeknowlton29@aol.com", "(989) 928-4331", "399"),
            (109, "Duane", "Jangula", "duanejangula@gmail.com", "(701) 425-5358", "689"),
            (110, "Lon", "Kaste", "lonscaligirl@yahoo.com", "(701) 337-0987", "387"),
            (111, "Shelley", "Kaste", "lonscaligirl@yahoo.com", "(701) 337-0988", "387"),
            (112, "Arnold", "Kayl", null, "(208) 304-2220", "911"),
            (113, "Donna", "Keefer", "glawayne@aol.com", "(989) 857-9459", "931"),
            (114, "Gary", "Keefer", "glawayne@aol.com", "(989) 287-2335", "931"),
            (115, "Bonnie", "Kennett", "b_fkennett@yahoo.com", "(651) 325-7028", "818"),
            (116, "Carolyn", "King", "caking@thomasandsons.biz", "(989) 915-9562", "332"),
            (117, "Dawn", "Klatt", "hwklatt@outlook.com", "(602) 526-1845", "235"),
            (118, "Harley", "Klatt", "hwklatt@outlook.com", "(602) 526-1845", "235"),
            (119, "Edward", "Klitzke", "choewisch@yahoo.com", "(920) 810-7676", "878"),
            (120, "Anne", "Knowlton", "aeknowlton29@aol.com", "(989) 928-4331", "399"),
            (121, "Marsha", "Kopecky", "mrkopecky3@gmail.com", "(402) 394-8294", "680"),
            (122, "Richard", "Kopecky", "mrkopecky15@gmail.com", "(402) 394-8364", "680"),
            (123, "Gary", "Kost", "dgkost@nvc.net", "(605) 228-8821", "443"),
            (124, "Jon", "Kragt", "jonkragt@gmail.com", "(402) 679-1175", "154"),
            (125, "Pam", "Kragt", "jonkragt@gmail.com", "(402) 679-1175", "154"),
            (126, "Marlene", "Kucera", "kuceramr@gmail.com", "(319) 404-4776", "358"),
            (127, "Rich", "Kucera", "kuchrjk1952@gmail.com", "(319) 404-4776", "358"),
            (128, "Denise", "Kulesa", "eddkulesa@yahoo.com", "(763) 248-4934", "107"),
            (129, "Ed", "Kulesa", "eddkulesa@yahoo.com", "(763) 248-4934", "107"),
            (130, "Sam", "Landon", "samlandon76@gmail.com", "(952) 649-7054", "339"),
            (131, "Tammy", "Landon", "tammaralandon@gmail.com", "(952) 649-7054", "339"),
            (132, "Marlene", "Lane", "marlenelane6@gmail.com", "(403) 988-7881", "210"),
            (133, "Chris", "Leeper", "chrisleeper429@yahoo.com", "(360) 280-2474", "91"),
            (134, "Tom", "Linahon", "tom.linahon@gmail.com", "(641) 529-7403", "959"),
            (135, "Joe", "Litzinger", "joeblueangel4@gmail.com", "(218) 779-9349", "309"),
            (136, "Mark", "Loch", "markjoanloch@hotmail.com", "(320) 290-6586", "666"),
            (137, "Linda", "Locken", "llocken@nvc.net", "(605) 380-9802", "234"),
            (138, "Mary", "Mahoney", "irishmmjd@msn.com", "(515) 238-3557", "340"),
            (139, "Michael", "Mahoney", "irishmmjd@msn.com", "(515) 238-3557", "340"),
            (140, "Kelly", "Maxfield", "kmaxfield10@yahoo.com", "(780) 255-9198", null),
            (141, "Darlene", "May", null, "(503) 739-3673", "68"),
            (142, "Rick", "May", null, "(503) 739-3673", "68"),
            (143, "Marilyn", "McBride", "mcbridemarilyn123@gmail.com", "(616) 862-0812", "226"),
            (144, "Kathy", "McCune", "kmc062509@yahoo.com", "(316) 641-2918", "376"),
            (145, "Kendall", "McCune", "kmc062509@yahoo.com", "(316) 641-2918", "376"),
            (146, "Karen", "McGee", "terrymcgee6@gmail.com", "(320) 469-1069", "657"),
            (147, "Terry", "McGee", "terrymcgee6@gmail.com", "(320) 469-1069", "657"),
            (148, "Doris", "Mingay", "djmingay@mt.net", "(406) 980-2011", "1028"),
            (149, "Martin", "Mingay", "mmingay1955@gmail.com", "(406) 980-1033", "1028"),
            (150, "Jeanne", "Mitchell", "jemitchell6@gmail.com", "(303) 325-3388", "251"),
            (151, "Mary", "Nalan", "mjnalan@yahoo.com", "(641) 430-9886", "959"),
            (152, "Doral", "Nall", "nalscanyon@yahoo,com", "(480) 227-4689", "368"),
            (153, "Bob", "Nelson", "kathyragona1962@gmail.com", "(563) 505-7830", "240"),
            (154, "Boyd", "Nelson", "boydnelson16@icloud.com", "(715) 781-9203", "642"),
            (155, "Kathy", "Nelson", "kathyragona1962@gmail.com", "(563) 505-7830", "642"),
            (156, "Donna", "O'Connor", "jimoconnor@westelk.com", "(970) 361-2649", "164"),
            (157, "Jim", "O'Connor", "jimoconnor@westelk.com", "(970) 275-9294", "164"),
            (158, "Mike", "Petschl", "mbpetschl@myctl.net", "(612) 296-8313", "833"),
            (159, "Gary", "Piper", "pied4piper@aol.com", "(319) 321-3872", "181"),
            (160, "Paula", "Piper", "pied4piper@aol.com", "(319) 321-3872", "181"),
            (161, "Connie", "Posselt", "cposs0210@gmail.com", "(920) 585-2039", "151"),
            (162, "Curt", "Posselt", "cposs0210@gmail.com", "(920) 585-2039", "151"),
            (163, "Bruce", "Preston", "bruceboss1577@gmail.com", "(260) 438-3069", "915"),
            (164, "Deb", "Preston", "vikings136472@gmail.com", "(260) 437-0245", "915"),
            (165, "Mary", "Ramos", null, "(480) 330-8134", null),
            (166, "Jan", "Reiner", "ljreiner55@hotmail.com", "(612) 868-4033", "845"),
            (167, "Lyle", "Reiner", "ljreiner55@hotmail.com", "(612) 868-4033", "845"),
            (168, "Mike", "Richey", "richeymichael26@gmail.com", "(815) 764-5320", "622"),
            (169, "Terri", "Richey", "terririchey19@gmail.com", "(815) 761-3535", "622"),
            (170, "Barb", "Roberts", "brobertsmn@gmail.com", "(218) 371-1304", "152"),
            (171, "Ann", "Roebbeke", "danojd280@outlook.com", "(612) 616-2029", "826"),
            (172, "Dan", "Roebbeke", "danojd280@outlook.com", "(612) 616-2029", "826"),
            (173, "Glenn", "Roiger", "glenroiger@hotmail.com", "(320) 834-2027", "760"),
            (174, "Mary", "Roiger", "roiger@gctel.net", "(320) 491-7464", "760"),
            (175, "Mike", "Roslin", "sandyroslin@gmail.com", "(612) 201-3217", "643"),
            (176, "Jack", "Roth", "jackroth243@gmail.com", "(719) 691-2431", "566"),
            (177, "Sue", "Roth", "jackroth243@gmail.com", "(719) 688-7684", "566"),
            (178, "Mike", "Rowan", "mikejrowan@shaw.ca", "(403) 870-8972", "756"),
            (179, "Mary", "Russell", "wyrealty@tctwest.net", "(307) 272-0004", "585"),
            (180, "Char", "Satter", "charlene.satter44@gmail.com", "(507) 360-1783", "574"),
            (181, "Greg", "Schiller", "julie.schiller@hotmail.com", "(612) 703-6504", "679"),
            (182, "Julie", "Schiller", "julie.schiller@hotmail.com", "(612) 703-6504", "679"),
            (183, "Harvey", "Schilling", "harvey3140@gmail.com", "(701) 226-1860", "722"),
            (184, "Norm", "Schnider", "nschnider2@gmail.com", "(208) 215-8879", "862"),
            (185, "Brenda", "Seidal", "brendabelbas@gmail.com", "(204) 851-3378", null),
            (186, "Don", "Seidal", "doncc60@yahoo.com", "(810) 325-0573", "383"),
            (187, "Barb", "Shinnick", "barbjshinnick@yahoo.com", "(612) 360-8865", "641"),
            (188, "Jeff", "Shoemaker", null, "(425) 299-8828", null),
            (189, "Kris", "Shoemaker", null, "(425) 299-8828", null),
            (190, "Karen", "Smith", "klsmith103@gmail.com", "(480) 519-0100", "712"),
            (191, "Stan", "Smith", "ssrambo69@gmail.com", "(972) 288-6535", "644"),
            (192, "Jerry", "Snyder", "yvetteandjerry@comcast.net", "(509) 309-4835", "336"),
            (193, "Yvette", "Snyder", "yvetteandjerry@comcast.net", "(509) 309-4836", "336"),
            (194, "Ken", "Sobolik", "marysobolik@yahoo.com", "(701) 740-5074", "998"),
            (195, "Mary", "Sobolik", "marysobolik@yahoo.com", "(701) 740-1920", "998"),
            (196, "Marianne", "Squibb", "m1squibb@gmail.com", "(360) 421-3720", "433"),
            (197, "Joleen", "Squire", "tjjsquire@mediacombb.net", "(952) 826-9647", "367"),
            (198, "Tom", "Squire", "tjjsquire@mediacombb.net", "(952) 826-9647", "367"),
            (199, "Karen", "Stewart", "lstewart@gwtc.net", "(605) 660-7876", "352"),
            (200, "Larry", "Stewart", "lstewart@gwtc.net", "(605) 660-7876", "352"),
        };
    }
}
