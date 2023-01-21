using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FakeServer.Controllers;

[Authorize]
[Route("admin")]
public class AdminController : Controller
{
    private readonly IDataStore _ds;

    public AdminController(IDataStore ds)
    {
        _ds = ds;
    }

    [HttpPost("reload")]
    public void ReloadFromFile()
    {
        _ds.Reload();
    }
}