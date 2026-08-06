using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pingr.Application.Features.Monitors;
using Pingr.Application.Features.Monitors.Commands;
using Pingr.Application.Features.Monitors.Queries;

namespace Pingr.Api.Controllers.v1;

/// <summary>
/// Provides endpoints related to monitors.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}/[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public sealed class MonitorsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<AddMonitorResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddMonitor(
        [FromRoute] Guid workspaceId,
        [FromBody] AddMonitorCommand request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            request with { WorkspaceId = workspaceId },
            cancellationToken);

        if (response.IsFailure)
            return response.Error.Code switch
            {
                _ => Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: response.Error.Code,
                    detail: response.Error.Message)
            };

        return CreatedAtAction(
            nameof(GetMonitorById),
            new
            {
                workspaceId = workspaceId,
                monitorId = response.Value!.MonitorId
            },
            response.Value);
    }

    [HttpGet("{monitorId:guid}")]
    [ProducesResponseType<MonitorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMonitorById(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid monitorId,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new GetMonitorByIdQuery(
                workspaceId,
                monitorId),
            cancellationToken);

        if (response.IsFailure)
            return response.Error.Code switch
            {
                _ => Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: response.Error.Code,
                    detail: response.Error.Message)
            };

        return Ok(response.Value);
    }
    
    [HttpGet]
    [ProducesResponseType<GetMonitorsByWorkspaceIdResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMonitorByWorkspaceId(
        [FromRoute] Guid workspaceId,
        [FromQuery] GetMonitorsByWorkspaceIdQuery request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            request with
            {
                WorkspaceId = workspaceId
            },
            cancellationToken);

        if (response.IsFailure)
            return response.Error.Code switch
            {
                _ => Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: response.Error.Code,
                    detail: response.Error.Message)
            };

        return Ok(response.Value);
    }

    [HttpPost("{monitorId:guid}/enable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnableMonitor(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid monitorId,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new EnableMonitorCommand(
                workspaceId,
                monitorId),
            cancellationToken);

        if (response.IsFailure)
            return response.Error.Code switch
            {
                _ => Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: response.Error.Code,
                    detail: response.Error.Message)
            };

        return NoContent();
    }
    
    [HttpPost("{monitorId:guid}/disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DisableMonitor(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid monitorId,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new DisableMonitorCommand(
                workspaceId,
                monitorId),
            cancellationToken);

        if (response.IsFailure)
            return response.Error.Code switch
            {
                _ => Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: response.Error.Code,
                    detail: response.Error.Message)
            };

        return NoContent();
    }
    
    [HttpPatch("{monitorId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMonitor(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid monitorId,
        [FromBody] UpdateMonitorCommand request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            request with 
            {
                WorkspaceId = workspaceId,
                MonitorId = monitorId
            },
            cancellationToken);

        if (response.IsFailure)
            return response.Error.Code switch
            {
                _ => Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: response.Error.Code,
                    detail: response.Error.Message)
            };

        return NoContent();
    }
    
    [HttpDelete("{monitorId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteMonitor(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid monitorId,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new DeleteMonitorCommand(
                workspaceId,
                monitorId),
            cancellationToken);

        if (response.IsFailure)
            return response.Error.Code switch
            {
                _ => Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: response.Error.Code,
                    detail: response.Error.Message)
            };

        return NoContent();
    }
}