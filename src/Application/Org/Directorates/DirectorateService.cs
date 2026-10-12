using Application.Abstractions;
using Domain.Org;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Org.Directorates
{
    public sealed class DirectorateService
    {
        private readonly IDirectorateRepository _repository;
        public DirectorateService(IDirectorateRepository repository)
        {
            _repository = repository;
        }

        public async Task<int> CreateAsync(
            string? title,
            CancellationToken cancellationToken = default)
        {
            var normalizedTitle = ValidateTitle(title);

            if (await _repository.TitleExistsAsync(
                normalizedTitle,
                cancellationToken: cancellationToken
                ))
            {
                throw new DirectorateConflictException(
                "Дирекция с таким названием уже существует.");
            }

            var directorate = new Directorate(normalizedTitle);

            return await _repository.CreateAsync(
                directorate, cancellationToken);
        }

        public Task<IReadOnlyList<Directorate>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

        public async Task<int> UpdateAsync(
            int id,
            string? title,
            bool isActive,
            CancellationToken cancellation = default)
        {
            if (id < 0)
                throw new DirectorateValidationException("Идентификатор дирекции должен быть положительным");

            var directorate = await _repository.GetByIdAsync(id, cancellation);

            if (directorate is null)
                throw new DirectorateNotFoundException(id);

            var normalizedTitle = ValidateTitle(title);

            if (await _repository.TitleExistsAsync(normalizedTitle, id, cancellation))
            {
                throw new DirectorateConflictException(
                "Дирекция с таким названием уже существует.");
            }

            directorate.Rename(normalizedTitle);
            directorate.SetActive(isActive);

            await _repository.UpdateAsync(directorate, cancellation);

            return id;
        }

        public async Task DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
        {
            if (id <= 0)
                throw new DirectorateValidationException(
                    "Идентификатор дирекции должен быть положительным.");

            var directorate = await _repository.GetByIdAsync(
                id, cancellationToken);

            if (directorate is null)
                throw new DirectorateNotFoundException(id);

            var dependencies = await _repository.GetDependenciesAsync(
                id, cancellationToken);

            if (dependencies.HasAny)
                throw new DirectorateHasDependenciesException(dependencies);

            directorate.SetActive(false);

            await _repository.DeactivateAsync(id, cancellationToken);
        }

        private static string ValidateTitle(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DirectorateValidationException(
                    "Название дирекции обязательно.");

            var normalized = title.Trim();

            if (normalized.Length > 250)
                throw new DirectorateValidationException(
                    "Название дирекции не должно превышать 250 символов.");

            return normalized;
        }
    }

    public sealed class DirectorateValidationException(string message)
    : Exception(message);

    public sealed class DirectorateNotFoundException(int id)
    : Exception($"Дирекция с ID {id} не найдена.");

    public sealed class DirectorateConflictException(string message)
        : Exception(message);

    public sealed class DirectorateHasDependenciesException(
    DirectorateDependencies dependencies)
    : Exception("Дирекцию нельзя деактивировать: существуют связанные записи.")
    {
        public DirectorateDependencies Dependencies { get; } = dependencies;
    }
}
