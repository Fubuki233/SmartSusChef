namespace SmartSusChef.Api.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, UserDto User);

public record UserDto(
    string Id,
    string Username,
    string Name,
    string Role
);
