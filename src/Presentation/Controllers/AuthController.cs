using Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [Route("v1/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;
        private readonly IPasswordHashService _passwordHashService;
        private readonly IJwtTokenGenerator _tokenGenerator;

        public AuthController(
        IAuthRepository authRepository,
        IPasswordHashService passwordHashService,
        IJwtTokenGenerator tokenGenerator)
        {
            _authRepository = authRepository;
            _passwordHashService = passwordHashService;
            _tokenGenerator = tokenGenerator;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponse>> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            var user = await _authRepository.FindByEmailAsync(
                request.Email,
                cancellationToken);

            if (user is null ||
                !_passwordHashService.Verify(
                    request.Password,
                    user.PasswordHash))
            {
                return Unauthorized(new
                {
                    message = "Неверный email или пароль."
                });
            }

            var token = _tokenGenerator.Generate(user);

            return Ok(new LoginResponse(
                token,
                "Bearer",
                60));
        }
    }
}

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresInMinutes);
