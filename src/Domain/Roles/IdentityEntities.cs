using Domain.Common;

namespace Domain.Roles;

public sealed class User : Entity
{
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private User() { }

    public User(string fullName, string email, string passwordHash)
    {
        Rename(fullName);
        ChangeEmail(email);
        SetPasswordHash(passwordHash);
    }

    public void Rename(string fullName)
    {
        FullName = Required(fullName, 250, nameof(fullName));
    }

    public void ChangeEmail(string email)
    {
        email = Required(email, 320, nameof(email)).Trim();

        if (!email.Contains('@'))
            throw new DomainException("Некорректный адрес электронной почты.");

        Email = email.ToLowerInvariant();
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = Required(passwordHash, int.MaxValue, nameof(passwordHash));
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    private static string Required(string value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{name} не может быть пустым.");

        value = value.Trim();

        if (value.Length > maxLength)
            throw new DomainException($"{name} превышает допустимую длину.");

        return value;
    }
}

public sealed class Role : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private Role() { }

    public Role(string code, string name)
    {
        Code = Required(code, 150, nameof(code));
        Rename(name);
    }

    public void Rename(string name) =>
        Name = Required(name, 200, nameof(name));

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string Required(string value, int max, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{name} не может быть пустым.");

        value = value.Trim();
        if (value.Length > max)
            throw new DomainException($"{name} превышает допустимую длину.");

        return value;
    }
}

public sealed class Right : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private Right() { }

    public Right(string code, string description)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Код права обязателен.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Описание права обязательно.");

        Code = code.Trim();
        Description = description.Trim();
    }
}

public sealed class UserRole : Entity
{
    public int UserId { get; private set; }
    public int RoleId { get; private set; }
    public bool IsActive { get; private set; } = true;

    private UserRole() { }

    public UserRole(int userId, int roleId)
    {
        if (userId <= 0 || roleId <= 0)
            throw new DomainException("Необходимо указать пользователя и роль.");

        UserId = userId;
        RoleId = roleId;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public sealed class RoleRight : Entity
{
    public int RoleId { get; private set; }
    public int RightId { get; private set; }

    private RoleRight() { }

    public RoleRight(int roleId, int rightId)
    {
        if (roleId <= 0 || rightId <= 0)
            throw new DomainException("Необходимо указать роль и право.");

        RoleId = roleId;
        RightId = rightId;
    }
}