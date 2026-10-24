using Application.Abstractions;

namespace Application.Risk.Risks;

public sealed class UserPermissionService : IUserPermissionService
{
    private readonly IUserPermissionRepository _repository;

    public UserPermissionService(IUserPermissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> HasRightAsync(
        int userId,
        string rightCode,
        CancellationToken cancellationToken = default)
    {
        return await _repository.HasRightAsync(userId, rightCode, cancellationToken);
    }
}
