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

    public Task<bool> IsAdminAsync(int userId, CancellationToken cancellationToken = default) =>
        _repository.IsAdminAsync(userId, cancellationToken);

    public Task<bool> HasRoleAsync(int userId, string roleCode, CancellationToken cancellationToken = default) =>
        _repository.HasRoleAsync(userId, roleCode, cancellationToken);

    public Task<bool> IsRiskOwnerAsync(int userId, int riskId, CancellationToken cancellationToken = default) =>
        _repository.IsRiskOwnerAsync(userId, riskId, cancellationToken);

    public Task<bool> IsTreatmentOwnerAsync(int userId, int treatmentId, CancellationToken cancellationToken = default) =>
        _repository.IsTreatmentOwnerAsync(userId, treatmentId, cancellationToken);

    public Task<bool> CanActAtLevelAsync(int userId, string level, int processId, CancellationToken cancellationToken = default) =>
        _repository.CanActAtLevelAsync(userId, level, processId, cancellationToken);

    public Task<bool> IsValidExecutorAsync(int assigneeUserId, int treatmentId, CancellationToken cancellationToken = default) =>
        _repository.IsValidExecutorAsync(assigneeUserId, treatmentId, cancellationToken);
}
