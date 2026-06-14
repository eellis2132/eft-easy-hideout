using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EasyHideout.Data;
using EasyHideout.Helpers;
using EasyHideout.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyHideout.Services;

public class ApiResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
}

// ── JSON response shapes ──────────────────────────────────────────────────────

public class GraphQlResponse
{
    [JsonPropertyName("data")]
    public GraphQlData? Data { get; set; }
}

public class GraphQlData
{
    [JsonPropertyName("hideoutStations")]
    public List<ApiStation>? HideoutStations { get; set; }

    [JsonPropertyName("traders")]
    public List<ApiTrader>? Traders { get; set; }

    [JsonPropertyName("items")]
    public List<ApiItem>? Items { get; set; }
}

public class ApiStation
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("normalizedName")]
    public string NormalizedName { get; set; } = "";

    [JsonPropertyName("levels")]
    public List<ApiLevel>? Levels { get; set; }
}

public class ApiLevel
{
    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("itemRequirements")]
    public List<ApiItemRequirement>? ItemRequirements { get; set; }

    [JsonPropertyName("stationLevelRequirements")]
    public List<ApiStationRequirement>? StationLevelRequirements { get; set; }

    [JsonPropertyName("traderRequirements")]
    public List<ApiTraderRequirement>? TraderRequirements { get; set; }
}

public class ApiItemRequirement
{
    [JsonPropertyName("item")]
    public ApiItem? Item { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("attributes")]
    public List<ApiItemAttribute>? Attributes { get; set; }
}

public class ApiItemAttribute
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "";
}

public class ApiItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("shortName")]
    public string ShortName { get; set; } = "";

    [JsonPropertyName("iconLink")]
    public string? IconLink { get; set; }

    [JsonPropertyName("minLevelForFlea")]
    public int? MinLevelForFlea { get; set; }

    [JsonPropertyName("avg24hPrice")]
    public int? Avg24hPrice { get; set; }
}

public class ApiStationRequirement
{
    [JsonPropertyName("station")]
    public ApiStationRef? Station { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }
}

public class ApiStationRef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("normalizedName")]
    public string NormalizedName { get; set; } = "";
}

public class ApiTraderRequirement
{
    [JsonPropertyName("trader")]
    public ApiTraderRef? Trader { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }
}

public class ApiTraderRef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

public class ApiTrader
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("levels")]
    public List<ApiTraderLoyaltyLevel>? Levels { get; set; }
}

public class ApiTraderLoyaltyLevel
{
    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("requiredPlayerLevel")]
    public int RequiredPlayerLevel { get; set; }
}

// ── Service ───────────────────────────────────────────────────────────────────

public class TarkovApiService
{
    private static readonly HttpClient _http = new();
    private const string ApiUrl = "https://api.tarkov.dev/graphql";

    private static readonly string GraphQlQuery = """
        {
          hideoutStations {
            id
            name
            normalizedName
            levels {
              level
              itemRequirements {
                item {
                  id
                  name
                  shortName
                  iconLink
                  minLevelForFlea
                  avg24hPrice
                }
                count
                attributes {
                  type
                  value
                }
              }
              stationLevelRequirements {
                station {
                  id
                  normalizedName
                }
                level
              }
              traderRequirements {
                trader {
                  id
                  name
                }
                level
              }
            }
          }
          traders {
            id
            name
            levels {
              level
              requiredPlayerLevel
            }
          }
        }
        """;

    public async Task<ApiResult> PullHideoutDataAsync(IProgress<string>? progress = null)
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppMode.AppDataFolder, "logs");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "api_log.txt");

        try
        {
            progress?.Report("Connecting to tarkov.dev...");
            AppendLog(logPath, "API pull started.");

            var body = JsonSerializer.Serialize(new { query = GraphQlQuery });
            var request = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(ApiUrl, request);

            if (!response.IsSuccessStatusCode)
            {
                var msg = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                AppendLog(logPath, $"FAILED — {msg}");
                return new ApiResult { Success = false, Message = msg };
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GraphQlResponse>(json);
            var stations = result?.Data?.HideoutStations;
            var traders = result?.Data?.Traders;

            if (stations == null || stations.Count == 0)
            {
                AppendLog(logPath, "FAILED — Empty response from API.");
                return new ApiResult { Success = false, Message = "Empty response from API." };
            }

            progress?.Report($"Received {stations.Count} stations. Saving to database...");

            await SnapshotPricesAsync();
            await UpsertStationsAsync(stations, progress, logPath);

            if (traders != null && traders.Count > 0)
            {
                progress?.Report("Saving trader loyalty level data...");
                await UpsertTraderLoyaltyLevelsAsync(traders);
                AppendLog(logPath, $"Upserted {traders.Count} traders.");
            }

            var iconDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppMode.AppDataFolder, "icons");
            Directory.CreateDirectory(iconDir);
            await CacheIconsAsync(stations, iconDir, progress);

            using var db = ServiceLocator.Get<AppDbContext>();
            var settings = db.AppSettings.First(s => s.Id == 1);
            settings.LastApiRefresh = DateTime.UtcNow;
            db.SaveChanges();

            var summary = $"Success — {stations.Count} stations synced.";
            AppendLog(logPath, summary);
            return new ApiResult { Success = true, Message = summary };
        }
        catch (Exception ex)
        {
            var msg = $"Exception: {ex.Message}";
            AppendLog(logPath, $"FAILED — {msg}");
            return new ApiResult { Success = false, Message = msg };
        }
    }

    public async Task<ApiResult> PullPricesOnlyAsync(IProgress<string>? progress = null)
    {
        try
        {
            progress?.Report("Snapshotting current prices...");
            await SnapshotPricesAsync();

            List<string> itemIds;
            using (var db = ServiceLocator.Get<AppDbContext>())
            {
                itemIds = db.ItemRequirements
                    .Select(r => r.TarkovItemId)
                    .Distinct()
                    .ToList();
            }

            if (itemIds.Count == 0)
                return new ApiResult { Success = false, Message = "No items found — pull hideout data first." };

            progress?.Report($"Fetching prices for {itemIds.Count} items from tarkov.dev...");

            var idsJson = string.Join("\", \"", itemIds);
            var query = $$"""
                {
                  items(ids: ["{{idsJson}}"]) {
                    id
                    avg24hPrice
                    minLevelForFlea
                  }
                }
                """;

            var body = JsonSerializer.Serialize(new { query });
            var requestContent = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(ApiUrl, requestContent);

            if (!response.IsSuccessStatusCode)
                return new ApiResult { Success = false, Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}" };

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GraphQlResponse>(json);
            var items = result?.Data?.Items;

            if (items == null || items.Count == 0)
                return new ApiResult { Success = false, Message = "No price data received." };

            progress?.Report($"Saving {items.Count} updated prices...");

            var priceMap = items.ToDictionary(i => i.Id, i => (Price: i.Avg24hPrice ?? 0, FleaLevel: i.MinLevelForFlea ?? 0));

            using (var db = ServiceLocator.Get<AppDbContext>())
            {
                var requirements = db.ItemRequirements.ToList();
                foreach (var req in requirements)
                {
                    if (priceMap.TryGetValue(req.TarkovItemId, out var p))
                    {
                        req.AvgPrice = p.Price;
                        req.MinLevelForFlea = p.FleaLevel;
                    }
                }
                db.SaveChanges();
            }

            return new ApiResult { Success = true, Message = $"Prices refreshed — {items.Count} items updated." };
        }
        catch (Exception ex)
        {
            return new ApiResult { Success = false, Message = $"Exception: {ex.Message}" };
        }
    }

    private static async Task SnapshotPricesAsync()
    {
        using var db = ServiceLocator.Get<AppDbContext>();
        var now = DateTime.UtcNow;

        var currentPrices = db.ItemRequirements
            .Where(r => r.AvgPrice > 0)
            .GroupBy(r => r.TarkovItemId)
            .Select(g => new { TarkovItemId = g.Key, Price = g.Max(r => r.AvgPrice) })
            .ToList();

        foreach (var item in currentPrices)
        {
            var snap = db.ItemPriceSnapshots.Find(item.TarkovItemId);
            if (snap != null)
            {
                snap.PreviousAvgPrice = item.Price;
                snap.SnapshotAt = now;
            }
            else
            {
                db.ItemPriceSnapshots.Add(new Models.ItemPriceSnapshot
                {
                    TarkovItemId = item.TarkovItemId,
                    PreviousAvgPrice = item.Price,
                    SnapshotAt = now,
                });
            }
        }

        db.SaveChanges();
        await Task.CompletedTask;
    }

    private static async Task UpsertStationsAsync(
        List<ApiStation> stations, IProgress<string>? progress, string logPath)
    {
        using var db = ServiceLocator.Get<AppDbContext>();

        foreach (var apiStation in stations)
        {
            var station = db.HideoutStations
                .Include(s => s.Levels)
                .FirstOrDefault(s => s.TarkovStationId == apiStation.Id);

            if (station == null)
            {
                station = new HideoutStation
                {
                    TarkovStationId = apiStation.Id,
                    Name = apiStation.Name,
                    NormalizedName = apiStation.NormalizedName,
                    MaxLevel = apiStation.Levels?.Count ?? 0
                };
                db.HideoutStations.Add(station);
                db.SaveChanges();
            }
            else
            {
                station.Name = apiStation.Name;
                station.NormalizedName = apiStation.NormalizedName;
                station.MaxLevel = apiStation.Levels?.Count ?? 0;
                db.SaveChanges();
            }

            foreach (var apiLevel in apiStation.Levels ?? [])
            {
                var level = db.HideoutLevels
                    .Include(l => l.ItemRequirements)
                    .Include(l => l.StationDependencies)
                    .Include(l => l.TraderRequirements)
                    .FirstOrDefault(l => l.StationId == station.Id && l.Level == apiLevel.Level);

                if (level == null)
                {
                    level = new HideoutLevel { StationId = station.Id, Level = apiLevel.Level };
                    db.HideoutLevels.Add(level);
                    db.SaveChanges();
                }

                db.ItemRequirements.RemoveRange(level.ItemRequirements);
                db.SaveChanges();

                foreach (var req in apiLevel.ItemRequirements ?? [])
                {
                    if (req.Item == null) continue;
                    db.ItemRequirements.Add(new ItemRequirement
                    {
                        HideoutLevelId = level.Id,
                        TarkovItemId = req.Item.Id,
                        ItemName = req.Item.Name,
                        ShortName = req.Item.ShortName,
                        Quantity = req.Count,
                        IconUrl = req.Item.IconLink ?? "",
                        FoundInRaid = req.Attributes?.Any(a => a.Type == "foundInRaid" && a.Value == "true") ?? false,
                        MinLevelForFlea = req.Item.MinLevelForFlea ?? 0,
                        AvgPrice = req.Item.Avg24hPrice ?? 0,
                    });
                }
                db.SaveChanges();

                db.StationDependencies.RemoveRange(level.StationDependencies);
                db.TraderRequirements.RemoveRange(level.TraderRequirements);
                db.SaveChanges();
            }
        }

        // Second pass: station dependencies and trader requirements (all stations must exist first)
        foreach (var apiStation in stations)
        {
            var station = db.HideoutStations
                .First(s => s.TarkovStationId == apiStation.Id);

            foreach (var apiLevel in apiStation.Levels ?? [])
            {
                var level = db.HideoutLevels
                    .First(l => l.StationId == station.Id && l.Level == apiLevel.Level);

                foreach (var dep in apiLevel.StationLevelRequirements ?? [])
                {
                    if (dep.Station == null) continue;
                    var reqStation = db.HideoutStations
                        .FirstOrDefault(s => s.TarkovStationId == dep.Station.Id);
                    if (reqStation == null) continue;

                    db.StationDependencies.Add(new StationDependency
                    {
                        HideoutLevelId = level.Id,
                        RequiredStationId = reqStation.Id,
                        RequiredLevel = dep.Level
                    });
                }

                foreach (var req in apiLevel.TraderRequirements ?? [])
                {
                    if (req.Trader == null) continue;
                    db.TraderRequirements.Add(new TraderRequirement
                    {
                        HideoutLevelId = level.Id,
                        TraderId = req.Trader.Id,
                        TraderName = req.Trader.Name,
                        RequiredLoyaltyLevel = req.Level
                    });
                }
            }
        }
        db.SaveChanges();

        AppendLog(logPath, $"Upserted {stations.Count} stations to database.");
    }

    private static async Task UpsertTraderLoyaltyLevelsAsync(List<ApiTrader> traders)
    {
        using var db = ServiceLocator.Get<AppDbContext>();

        // Full replace — trader LL data changes rarely
        db.TraderLoyaltyLevels.ExecuteDelete();

        foreach (var trader in traders)
        {
            foreach (var lvl in trader.Levels ?? [])
            {
                db.TraderLoyaltyLevels.Add(new TraderLoyaltyLevel
                {
                    TraderId = trader.Id,
                    TraderName = trader.Name,
                    LoyaltyLevel = lvl.Level,
                    RequiredPlayerLevel = lvl.RequiredPlayerLevel
                });
            }
        }
        db.SaveChanges();

        await Task.CompletedTask;
    }

    private static async Task CacheIconsAsync(
        List<ApiStation> stations, string iconDir, IProgress<string>? progress)
    {
        var urls = stations
            .SelectMany(s => s.Levels ?? [])
            .SelectMany(l => l.ItemRequirements ?? [])
            .Where(r => !string.IsNullOrEmpty(r.Item?.IconLink))
            .Select(r => r.Item!.IconLink!)
            .Distinct()
            .ToList();

        int cached = 0;
        foreach (var url in urls)
        {
            try
            {
                var fileName = Path.GetFileName(new Uri(url).LocalPath);
                var dest = Path.Combine(iconDir, fileName);
                if (File.Exists(dest)) { cached++; continue; }

                var bytes = await _http.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(dest, bytes);
                cached++;

                if (cached % 10 == 0)
                    progress?.Report($"Caching icons... {cached}/{urls.Count}");

                await Task.Delay(20);
            }
            catch { /* skip failed icon downloads */ }
        }

        progress?.Report($"Icons cached: {cached}/{urls.Count}");
    }

    private static void AppendLog(string logPath, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        File.AppendAllText(logPath, line + Environment.NewLine);
    }
}
