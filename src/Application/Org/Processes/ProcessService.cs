using Application.Abstractions;
using Domain.Common;
using Domain.Org;

namespace Application.Org.Processes;

public sealed class ProcessService
{
    private readonly IProcessRepository _repository;
    private readonly IPersonRepository _personRepository;

    public ProcessService(
        IProcessRepository repository,
        IPersonRepository personRepository)
    {
        _repository = repository;
        _personRepository = personRepository;
    }

    public async Task<int> CreateAsync(
        int directorateId,
        string processTitle,
        int ownerId,
        string? processDescription,
        CancellationToken cancellationToken = default)
    {
        Validate(directorateId, processTitle, ownerId);

        if (!await _repository.DirectorateIsActiveAsync(directorateId, cancellationToken))
            throw new ProcessValidationException(
                $"Активная дирекция с ID {directorateId} не найдена.");

        var person = await _personRepository.GetByIdAsync(ownerId, cancellationToken)
            ?? throw new ProcessValidationException($"Сотрудник с ID {ownerId} не найден.");

        if (!person.IsActive)
            throw new ProcessValidationException("Сотрудник неактивен.");

        if (!person.UserId.HasValue)
            throw new ProcessValidationException("У сотрудника не привязан пользователь.");

        if (!await _repository.UserHasRoleAsync(person.UserId.Value, "OWNER", cancellationToken))
            throw new ProcessValidationException(
                $"Сотрудник с ID {ownerId} не имеет роли OWNER.");

        var process = new BusinessProcess(
            directorateId,
            processTitle,
            ownerId,
            processDescription);

        return await _repository.CreateAsync(process, cancellationToken);
    }

    public Task<IReadOnlyList<BusinessProcess>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public async Task<BusinessProcess> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ProcessValidationException("Некорректный ID процесса.");

        return await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new ProcessNotFoundException(id);
    }

    public async Task<int> UpdateAsync(
        int id,
        int? directorateId,
        string? processTitle,
        int? ownerId,
        string? processDescription,
        bool? isProcessActive,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ProcessValidationException("Некорректный ID процесса.");

        if (directorateId is null
            && processTitle is null
            && ownerId is null
            && processDescription is null
            && isProcessActive is null)
        {
            throw new ProcessValidationException("Не передано ни одного поля для обновления.");
        }

        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new ProcessNotFoundException(id);

        // Валидация только переданных полей
        if (directorateId.HasValue)
        {
            if (directorateId.Value <= 0)
                throw new ProcessValidationException("directorateId должен быть положительным.");

            if (!await _repository.DirectorateIsActiveAsync(directorateId.Value, cancellationToken))
                throw new ProcessValidationException(
                    $"Активная дирекция с ID {directorateId} не найдена.");
        }

        if (processTitle is not null)
        {
            if (string.IsNullOrWhiteSpace(processTitle))
                throw new ProcessValidationException("Название процесса обязательно.");

            if (processTitle.Trim().Length > 250)
                throw new ProcessValidationException("Название процесса слишком длинное.");
        }

        if (ownerId.HasValue)
        {
            if (ownerId.Value <= 0)
                throw new ProcessValidationException("ownerId должен быть положительным.");

            var updatePerson = await _personRepository.GetByIdAsync(ownerId.Value, cancellationToken)
                ?? throw new ProcessValidationException($"Сотрудник с ID {ownerId.Value} не найден.");

            if (!updatePerson.IsActive)
                throw new ProcessValidationException("Сотрудник неактивен.");

            if (!updatePerson.UserId.HasValue)
                throw new ProcessValidationException("У сотрудника не привязан пользователь.");

            if (!await _repository.UserHasRoleAsync(updatePerson.UserId.Value, "OWNER", cancellationToken))
                throw new ProcessValidationException(
                    $"Сотрудник с ID {ownerId.Value} не имеет роли OWNER.");
        }

        if (processDescription is not null && processDescription.Length > 1000)
            throw new ProcessValidationException("Описание процесса слишком длинное.");

        // Если деактивируем — проверяем зависимости
        if (isProcessActive == false)
        {
            var dependencies = await _repository.GetDependenciesAsync(id, cancellationToken);
            if (dependencies.HasAny)
                throw new ProcessHasDependenciesException(dependencies);
        }

        var update = new ProcessUpdate
        {
            Id = id,
            DirectorateId = directorateId,
            Title = processTitle?.Trim(),
            OwnerId = ownerId,
            Description = processDescription?.Trim(),
            IsActive = isProcessActive
        };

        var updated = await _repository.UpdateAsync(update, cancellationToken);
        if (!updated)
            throw new ProcessNotFoundException(id);

        return id;
    }

    public async Task DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ProcessValidationException("Некорректный ID процесса.");

        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new ProcessNotFoundException(id);

        var dependencies = await _repository.GetDependenciesAsync(id, cancellationToken);
        if (dependencies.HasAny)
            throw new ProcessHasDependenciesException(dependencies);

        if (!await _repository.DeactivateAsync(id, cancellationToken))
            throw new ProcessNotFoundException(id);
    }

    private static void Validate(int directorateId, string processTitle, int ownerId)
    {
        if (directorateId <= 0)
            throw new ProcessValidationException("directorateId должен быть положительным числом.");

        if (ownerId <= 0)
            throw new ProcessValidationException("ownerId должен быть положительным числом.");

        if (string.IsNullOrWhiteSpace(processTitle))
            throw new ProcessValidationException("Название процесса обязательно.");

        if (processTitle.Trim().Length > 250)
            throw new ProcessValidationException("Название процесса не должно превышать 250 символов.");
    }
}

public sealed class ProcessValidationException(string message)
    : Exception(message);

public sealed class ProcessNotFoundException(int id)
    : Exception($"Процесс с ID {id} не найден.");

public sealed class ProcessConflictException(string message)
    : Exception(message);

public sealed class ProcessHasDependenciesException(
    ProcessDependencies dependencies)
    : Exception("Нельзя деактивировать процесс: существуют блокирующие зависимости.")
{
    public ProcessDependencies Dependencies { get; } = dependencies;
}