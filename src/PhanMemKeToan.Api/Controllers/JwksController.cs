using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Api.Controllers;

[ApiController]
[AllowAnonymous]
public class JwksController(IJwtService jwtService) : ControllerBase
{
    [HttpGet("/.well-known/jwks.json")]
    public IActionResult GetJwks()
    {
        Response.Headers.CacheControl = "public, max-age=3600";
        return Ok(jwtService.GetJwks());
    }
}
