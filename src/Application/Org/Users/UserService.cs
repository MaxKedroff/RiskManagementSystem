using Application.Abstractions;
using Domain.Roles;
using static Domain.Roles.User;

namespace Application.Org.Users;

public sealed class UserService
{
    private static readonly HashSet<string> AllowedResponsibilityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "CEO",
        "DIRECTOR",
        "RISK_SPECIALIST"
    };

    private readonly IUserRepository _repository;
    private readonly IPasswordHashService _passwordHasher;
    private readonly ITemporaryPasswordGenerator _passwordGenerator;

    public UserService(
        IUserRepository repository,
        IPasswordHashService passwordHasher,
        ITemporaryPasswordGenerator passwordGenerator)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _passwordGenerator = passwordGenerator;
    }

    public async Task<int> CreateAsync(
        string userFio,
        string userEmail,
        IReadOnlyList<int> roleIds,
        int? personId,
        int? directorateId,
        string? responsibilityType,
        CancellationToken cancellationToken = default)
    {
        Validate(userFio, userEmail, roleIds, responsibilityType);

        foreach (var roleId in roleIds)
        {
            if (!await _repository.RoleExistsAsync(roleId, cancellationToken))
                throw new UserValidationException($"Роль с ID {roleId} не найдена.");
        }

        if (personId.HasValue
            && !await _repository.PersonExistsAsync(personId.Value, cancellationToken))
        {
            throw new UserValidationException($"Сотрудник с ID {personId} не найден.");
        }

        if (directorateId.HasValue
            && !await _repository.DirectorateIsActiveAsync(directorateId.Value, cancellationToken))
        {
            throw new UserValidationException($"Активное подразделение с ID {directorateId} не найдено.");
        }

        var temporaryPassword = _passwordGenerator.Generate();
        var passwordHash = _passwordHasher.Hash(temporaryPassword);

        var user = new User(
            userFio,
            userEmail,
            passwordHash,
            personId,
            directorateId,
            responsibilityType);

        var userId = await _repository.CreateAsync(user, roleIds, cancellationToken);

        return userId;
    }

    public Task<IReadOnlyList<User>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public async Task<(User User, IReadOnlyList<Role> Roles)> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new UserValidationException("Некорректный ID пользователя.");

        var user = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new UserNotFoundException(id);

        var roles = await _repository.GetRolesAsync(id, cancellationToken);

        return (user, roles);
    }

    public async Task<int> UpdateAsync(
        int id,
        string? userFio,
        IReadOnlyList<int>? roleIds,
        bool? isUserActive,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new UserValidationException("Некорректный ID пользователя.");

        if (userFio is null && roleIds is null && isUserActive is null)
            throw new UserValidationException("Не передано ни одного поля для обновления.");

        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new UserNotFoundException(id);

        if (userFio is not null)
        {
            if (string.IsNullOrWhiteSpace(userFio))
                throw new UserValidationException("ФИО обязательно.");

            if (userFio.Trim().Length > 250)
                throw new UserValidationException("ФИО слишком длинное.");
        }

        if (roleIds is not null)
        {
            if (roleIds.Count == 0)
                throw new UserValidationException("Список ролей не может быть пустым.");

            foreach (var roleId in roleIds)
            {
                if (!await _repository.RoleExistsAsync(roleId, cancellationToken))
                    throw new UserValidationException($"Роль с ID {roleId} не найдена.");
            }
        }

        if (isUserActive == false && await _repository.IsLastAdminAsync(id, cancellationToken))
            throw new UserLastAdminException();

        // 12 §5.1: an executor of a treatment in work cannot be blocked until it is reassigned
        if (isUserActive == false && await _repository.IsActiveExecutorAsync(id, cancellationToken))
            throw new UserValidationException(
                "Пользователь назначен исполнителем мер «Ожидает выполнения» или «В работе». Сначала замените исполнителя.");

        var update = new UserUpdate
        {
            Id = id,
            FullName = userFio?.Trim(),
            IsActive = isUserActive,
            RoleIds = roleIds
        };

        var updated = await _repository.UpdateAsync(update, cancellationToken);
        if (!updated)
            throw new UserNotFoundException(id);

        return id;
    }

    public async Task<string> ResetPasswordAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new UserValidationException("Некорректный ID пользователя.");

        var user = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new UserNotFoundException(id);

        var temporaryPassword = _passwordGenerator.Generate();
        var passwordHash = _passwordHasher.Hash(temporaryPassword);

        await _repository.UpdatePasswordHashAsync(id, passwordHash, cancellationToken);


        return temporaryPassword;
    }

    private static void Validate(
        string userFio,
        string userEmail,
        IReadOnlyList<int> roleIds,
        string? responsibilityType)
    {
        if (string.IsNullOrWhiteSpace(userFio))
            throw new UserValidationException("ФИО обязательно.");

        if (userFio.Trim().Length > 250)
            throw new UserValidationException("ФИО не должно превышать 250 символов.");

        if (string.IsNullOrWhiteSpace(userEmail))
            throw new UserValidationException("Email обязателен.");

        userEmail = userEmail.Trim().ToLowerInvariant();
        if (userEmail.Length > 320)
            throw new UserValidationException("Email слишком длинный.");

        if (!userEmail.Contains('@'))
            throw new UserValidationException("Некорректный email.");

        if (roleIds is null || roleIds.Count == 0)
            throw new UserValidationException("Необходимо указать хотя бы одну роль.");

        if (responsibilityType is not null
            && !AllowedResponsibilityTypes.Contains(responsibilityType.Trim()))
        {
            throw new UserValidationException(
                "Недопустимый тип ответственности. Разрешены: CEO, DIRECTOR, RISK_SPECIALIST.");
        }
    }
}
public sealed class UserValidationException(string message)
    : Exception(message);

public sealed class UserNotFoundException(int id)
    : Exception($"Пользователь с ID {id} не найден.");

public sealed class UserConflictException(string message)
    : Exception(message);

public sealed class UserLastAdminException()
    : Exception("Нельзя деактивировать последнего администратора.");
