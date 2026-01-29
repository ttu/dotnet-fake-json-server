using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FakeServer.Controllers;

[Authorize]
[Route("admin")]
public class AdminController : Controller
{
    private readonly IDataStore _ds;
    private readonly IConfiguration _config;

    public AdminController(IDataStore ds, IConfiguration config)
    {
        _ds = ds;
        _config = config;
    }

    [HttpPost("reload")]
    public void ReloadFromFile()
    {
        _ds.Reload();
    }
    
    [HttpPost("restore-backup")]
    public IActionResult RestoreFromBackup()
    {
        var jsonFilePath = Path.Combine(_config["currentPath"], _config["file"]);
        var backupPath = jsonFilePath + ".backup";
        
        if (!System.IO.File.Exists(backupPath))
        {
            return NotFound("No backup file found");
        }
        
        System.IO.File.Copy(backupPath, jsonFilePath, true);
        _ds.Reload();
        
        return Ok("Data restored from backup");
    }
}