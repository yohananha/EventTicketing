using EventTicketing.BusinessLogic.Services;
using EventTicketing.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _events;
    public EventsController(IEventService events) => _events = events;

    /// <summary>List all events with seat counts.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EventResponse>>> GetAll() =>
        Ok(await _events.GetAllAsync());

    /// <summary>Get a single event by id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventResponse>> Get(int id)
    {
        var ev = await _events.GetAsync(id);
        return ev is null ? NotFound() : Ok(ev);
    }

    /// <summary>Create an event; its seat map is generated automatically.</summary>
    [HttpPost]
    public async Task<ActionResult<EventResponse>> Create(CreateEventRequest request)
    {
        var created = await _events.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Update an event's details/status.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<EventResponse>> Update(int id, UpdateEventRequest request) =>
        Ok(await _events.UpdateAsync(id, request));

    /// <summary>Delete an event and its seats.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _events.DeleteAsync(id);
        return NoContent();
    }
}
