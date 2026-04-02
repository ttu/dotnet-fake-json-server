using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FakeServer.Controllers;

[Authorize]
[Route("admin")]
public class AdminController : Controller
{
    private readonly IDataStore _dataStore;

    public AdminController(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    [HttpPost("reload")]
    public IActionResult ReloadFromFile()
    {
        _dataStore.Reload();
        return NoContent();
    }
}
