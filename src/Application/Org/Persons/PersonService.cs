
using Application.Abstractions;
using Domain.Org;

namespace Application.Org.Persons;

public sealed class PersonValidationException(string message)
    : Exception(message);

public sealed class PersonNotFoundException(int id)
    : Exception($"Сотрудник с ID {id} не найден.");

public sealed class PersonConflictException(string message)
    : Exception(message);

public sealed class PersonHasDependenciesException(
    PersonDependencies dependencies)
    : Exception("Нельзя деактивировать сотрудника: существуют зависимости.")
{
    public PersonDependencies Dependencies { get; } = dependencies;
}

public sealed class PersonService
{
    private readonly IPersonRepository _repository;

    public PersonService(IPersonRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> CreateAsync(
        int userId,
        int directorateId,
        string personPosition,
        CancellationToken cancellationToken = default)
    {
        Validate(userId, directorateId, personPosition);

        if (!await _repository.UserExistsAsync(userId, cancellationToken))
            throw new PersonValidationException(
                $"Пользователь с ID {userId} не найден или неактивен.");

        if (!await _repository.DirectorateIsActiveAsync(
                directorateId, cancellationToken))
        {
            throw new PersonValidationException(
                $"Активная дирекция с ID {directorateId} не найдена.");
        }

        var person = new Person(userId, directorateId, personPosition.Trim());

        return await _repository.CreateAsync(person, cancellationToken);
    }

    public Task<IReadOnlyList<Person>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public async Task<int> UpdateAsync(
        int id,
        int directorateId,
        string personPosition,
        bool isPersonActive,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new PersonValidationException("Некорректный ID сотрудника.");

        Validate(directorateId, personPosition);

        var person = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new PersonNotFoundException(id);

        if (!await _repository.DirectorateIsActiveAsync(
                directorateId, cancellationToken))
        {
            throw new PersonValidationException(
                $"Активная дирекция с ID {directorateId} не найдена.");
        }

        person.ChangeDirectorate(directorateId);
        person.ChangePosition(personPosition.Trim());
        person.SetActive(isPersonActive);

        var updated = await _repository.UpdateAsync(
            person, cancellationToken);

        if (!updated)
            throw new PersonNotFoundException(id);

        return id;
    }

    public async Task DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new PersonValidationException("Некорректный ID сотрудника.");

        var person = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new PersonNotFoundException(id);

        var dependencies = await _repository.GetDependenciesAsync(
            id, cancellationToken);

        if (dependencies.HasAny)
            throw new PersonHasDependenciesException(dependencies);

        if (!await _repository.DeactivateAsync(id, cancellationToken))
            throw new PersonNotFoundException(id);
    }

    private static void Validate(
        int userId,
        int directorateId,
        string personPosition)
    {
        if (userId <= 0)
            throw new PersonValidationException(
                "userId должен быть положительным числом.");

        Validate(directorateId, personPosition);
    }

    private static void Validate(
        int directorateId,
        string personPosition)
    {
        if (directorateId <= 0)
            throw new PersonValidationException(
                "directorateId должен быть положительным числом.");

        if (string.IsNullOrWhiteSpace(personPosition))
            throw new PersonValidationException(
                "Должность обязательна.");

        if (personPosition.Trim().Length > 250)
            throw new PersonValidationException(
                "Должность не должна превышать 250 символов.");
    }
}
