using MediatR;

namespace IdeatecAPI.Application.Features.Demo.SendDemoRequest;

public record SendDemoRequestCommand(
    string Name,
    string Company,
    string Phone
) : IRequest<SendDemoRequestResult>;

public record SendDemoRequestResult(bool Success, string Message);