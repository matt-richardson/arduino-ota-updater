using System.Security.Cryptography;
using arduino_ota_updater;
using Serilog;
using Serilog.Context;

class UpdateHandler
{
    // converted from the example at https://arduino-esp8266.readthedocs.io/en/latest/ota_updates/readme.html#server-request-handling
    public IResult Handle(HttpRequest req, ILogger<UpdateHandler> logger) 
    {
        if (!SdkHeaderExists(req, logger,  "User-Agent", "ESP8266-http-Update"))
            return Results.StatusCode(403);
        if(!SdkHeaderExists(req, logger,  "x-ESP8266-STA-MAC"))
            return Results.StatusCode(403);        
        if(!SdkHeaderExists(req, logger,  "x-ESP8266-AP-MAC"))
            return Results.StatusCode(403);        
        if(!SdkHeaderExists(req, logger,  "x-ESP8266-free-space"))
            return Results.StatusCode(403);        
        if(!SdkHeaderExists(req, logger,  "x-ESP8266-sketch-size"))
            return Results.StatusCode(403);        
        if(!SdkHeaderExists(req, logger,  "x-ESP8266-sketch-md5"))
            return Results.StatusCode(403);        
        if(!SdkHeaderExists(req, logger,  "x-ESP8266-chip-size"))
            return Results.StatusCode(403);        
        if(!SdkHeaderExists(req, logger,  "x-ESP8266-sdk-version"))
            return Results.StatusCode(403);
        
        var (latestVersion, localBinary) = GetLatestPackage(logger);

        if (!NeedsUpdate(req, logger, latestVersion, localBinary)) 
            return Results.StatusCode(304);
        
        using var stream = File.OpenRead(localBinary);
        return Results.File(Path.GetFullPath(localBinary), "application/octet-stream", Path.GetFileName(localBinary));
    }

    private (string version, string path) GetLatestPackage(ILogger<UpdateHandler> logger)
    {
        var files = Directory.GetFiles("packages", "*.bin");
        using (LogContext.PushProperty("Files", files))
            logger.LogDebug("Found {Count} packages", files.Length);
        var result = files
            .Select(Map)
            .OrderByDescending(x => x.version)
            .First();

        logger.LogDebug("Latest package is {Version} found at {Path}", result.version.ToString(), result.path);
        
        return (result.version.ToString(), result.path);
    }

    private static (Version version, string path) Map(string path)
    {
        var version = path
            .Replace("packages/", "")
            .Replace("water-tank-sensor.", "")
            .Replace(".bin", "");
        return (new Version(version), path);
    }

    private bool NeedsUpdate(HttpRequest req, ILogger<UpdateHandler> logger, string latestVersion, string localBinary)
    {
        if (SdkHeaderExists(req, logger,  "x-ESP8266-sdk-version") || !VersionDoesNotMatch(req, logger, latestVersion))
            return HashDoesNotMatch(req, logger, localBinary);
        return true;
    }

    private bool VersionDoesNotMatch(HttpRequest req, ILogger<UpdateHandler> logger, string latestVersion)
    {
        var deviceVersion = req.Headers["x-ESP8266-version"].ToString();
        if (latestVersion != deviceVersion)
        {
            logger.LogDebug("Checking device version {DeviceVersion} against latest version {LatestVersion} - no match", deviceVersion, latestVersion);
            return true;
        }
        logger.LogDebug("Checking device version {DeviceVersion} against latest version {LatestVersion} - they are the same", deviceVersion, latestVersion);
        return false;
    }

    private bool HashDoesNotMatch(HttpRequest req, ILogger<UpdateHandler> logger, string localBinary)
    {
        var deviceHash = req.Headers["x-ESP8266-sketch-md5"].ToString();
        var localBinaryHash = GetMD5(localBinary).ToHex();

        if (deviceHash != localBinaryHash)
        {
            logger.LogDebug("Checking device hash {DeviceHash} against latest version hash {LatestVersionHash} - no match", deviceHash, localBinaryHash);
            return true;
        }
        logger.LogDebug("Checking device hash {DeviceHash} against latest version hash {LatestVersionHash} - they are the same", deviceHash, localBinaryHash);
        return false;
    }

    bool SdkHeaderExists(HttpRequest req, ILogger<UpdateHandler> logger, string name, string? value = null)
    {
        if (!req.Headers.ContainsKey(name))
        {
            logger.LogWarning("Did not find expected header {Name}", name);
            return false;
        }

        if (value != null && req.Headers[name].ToString() != value)
        {
            logger.LogWarning("Unexpected header {Name}. Expected {Expected} but found {Actual}", name, value, req.Headers[name].ToString());
            return false;
        }
        return true;
    }

    private byte[] GetMD5(string filename)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filename);
        return md5.ComputeHash(stream);
    }
}