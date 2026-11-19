using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudioSport.Core.Interfaces;

namespace StudioSport.API.Controllers;

[ApiController]
[Route("api/trainers")]
public class TrainersController : ControllerBase
{
    private readonly ITrainerService _trainers;

    public TrainersController(ITrainerService trainers) => _trainers = trainers;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _trainers.GetAllAsync(ct));
}
