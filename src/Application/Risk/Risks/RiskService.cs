using Application.Abstractions;
using Application.Org.Processes;
using Domain.Assessment;
using Domain.Org;
using Domain.Risk;

namespace Application.Risk.Risks;

public sealed class RiskService
{
    private readonly IRiskRepository _riskRepository;
    private readonly IRiskProcessRepository _riskProcessRepository;
    private readonly IRiskAssessmentRepository _riskAssessmentRepository;
    private readonly IUserPermissionService _permissionService;
    private readonly IAssetRepository _assetRepository;
    private readonly IProcessRepository _processRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly ITreatmentRepository _treatmentRepository;
    private readonly int _assessmentMethodId = 1; // Default assessment method

    public RiskService(
        IRiskRepository riskRepository,
        IRiskProcessRepository riskProcessRepository,
        IRiskAssessmentRepository riskAssessmentRepository,
        IUserPermissionService permissionService,
        IAssetRepository assetRepository,
        IProcessRepository processRepository,
        ICommentRepository commentRepository,
        ITreatmentRepository treatmentRepository)
    {
        _riskRepository = riskRepository;
        _riskProcessRepository = riskProcessRepository;
        _riskAssessmentRepository = riskAssessmentRepository;
        _permissionService = permissionService;
        _assetRepository = assetRepository;
        _processRepository = processRepository;
        _commentRepository = commentRepository;
        _treatmentRepository = treatmentRepository;
    }

    // GET /v1/risks
    public async Task<IReadOnlyList<RiskListItem>> GetAllAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "view_risk", cancellationToken))
        {
            throw new RiskPermissionException("Отсутствует право view_risk.");
        }

        var risks = await _riskRepository.GetAllAsync(cancellationToken);
        return risks;
    }

    // POST /v1/risks
    public async Task<int> CreateAsync(
        int authorId,
        CreateRiskRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(authorId, "create_components", cancellationToken))
        {
            throw new RiskPermissionException("Отсутствует право create_components.");
        }

        // Validate all processIds are active and get owner info
        var processOwnerInfos = new List<ProcessOwnerInfo>();
        foreach (var processId in request.ProcessIds)
        {
            var ownerInfo = await _processRepository.GetOwnerInfoAsync(processId, cancellationToken)
                ?? throw new RiskValidationException($"Процесс с ID {processId} не найден.");

            if (!ownerInfo.IsActive)
            {
                throw new RiskValidationException($"Процесс с ID {processId} неактивен.");
            }

            processOwnerInfos.Add(ownerInfo);
        }

        // Check asset if provided
        if (request.AssetId.HasValue)
        {
            var asset = await _assetRepository.GetByIdAsync(request.AssetId.Value, cancellationToken);
            if (asset is null)
            {
                throw new RiskValidationException("Указанный объект не найден.");
            }
            if (!asset.IsActive)
            {
                throw new RiskValidationException("Указанный объект неактивен.");
            }
            if (asset.HourCost <= 0)
            {
                throw new RiskValidationException("У объекта не задана стоимость часа (assetHourCost).");
            }
        }

        // Validate required fields
        var normalizedTitle = ValidateTitle(request.RiskTitle);
        var normalizedDescription = ValidateDescription(request.RiskDescription);

        if (request.HazardId <= 0)
            throw new RiskValidationException("Необходимо указать опасность (hazardId).");
        if (request.CategoryId <= 0)
            throw new RiskValidationException("Необходимо указать категорию (categoryId).");

        // Create Risk with status "Draft"
        var risk = new Domain.Risk.Risk(
            normalizedTitle,
            normalizedDescription,
            request.HazardId,
            request.CategoryId,
            authorId,
            request.AssetId);

        var riskId = await _riskRepository.CreateAsync(risk, cancellationToken);

        // Create RiskProcess (one isPrimary = true)
        // The first process becomes the primary
        if (processOwnerInfos.Count > 0)
        {
            var primary = processOwnerInfos[0];
            // Use owner's user_id if available, otherwise fall back to authorId
            var ownerUserId = primary.OwnerUserId ?? authorId;

            var riskProcess = new RiskProcess(
                riskId,
                ownerUserId,
                primary.OwnerPersonId,
                true); // isPrimary

            await _riskProcessRepository.CreateAsync(riskProcess, cancellationToken);
        }

        return riskId;
    }

    // PUT /v1/risks/{id}
    public async Task<int> UpdateAsync(
        int riskId,
        int userId,
        UpdateRiskRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "edit_components", cancellationToken))
        {
            throw new RiskPermissionException("Отсутствует право edit_components.");
        }

        var risk = await _riskRepository.GetByIdAsync(riskId, cancellationToken)
            ?? throw new RiskNotFoundException(riskId);

        // Check status is "Draft"
        if (risk.Status != Domain.Risk.RiskStatus.Draft)
        {
            throw new RiskValidationException("Операция редактирования недоступна для текущего статуса риска. Допустим только статус 'Черновик'.");
        }

        // Check user is owner of primary process
        var primaryOwnerUserId = await _riskProcessRepository.GetPrimaryOwnerUserIdAsync(riskId, cancellationToken);
        if (primaryOwnerUserId.HasValue && primaryOwnerUserId.Value != userId)
        {
            throw new RiskAuthorizationException("Вы не являетесь владельцем основного процесса этого риска.");
        }

        // Validate completeness
        var normalizedTitle = ValidateTitle(request.RiskTitle);
        var normalizedDescription = ValidateDescription(request.RiskDescription);

        if (request.HazardId <= 0)
            throw new RiskValidationException("Необходимо указать опасность (hazardId).");
        if (request.CategoryId <= 0)
            throw new RiskValidationException("Необходимо указать категорию (categoryId).");

        // Check asset if provided
        if (request.AssetId.HasValue)
        {
            var asset = await _assetRepository.GetByIdAsync(request.AssetId.Value, cancellationToken);
            if (asset is null)
            {
                throw new RiskValidationException("Указанный объект не найден.");
            }
            if (!asset.IsActive)
            {
                throw new RiskValidationException("Указанный объект неактивен.");
            }
            if (asset.HourCost <= 0)
            {
                throw new RiskValidationException("У объекта не задана стоимость часа (assetHourCost).");
            }
        }

        // Update risk classification (without changing status, author, or process links)
        risk.ChangeClassification(request.HazardId, request.CategoryId, request.AssetId);
        risk.Rename(normalizedTitle);
        risk.ChangeDescription(normalizedDescription);

        await _riskRepository.UpdateAsync(risk, cancellationToken);

        return riskId;
    }

    // POST /v1/risks/{id}/assess
    public async Task<AssessRiskResponse> AssessAsync(
        int riskId,
        int userId,
        AssessRiskRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "assess_risk", cancellationToken))
        {
            throw new RiskPermissionException("Отсутствует право assess_risk.");
        }

        var risk = await _riskRepository.GetByIdAsync(riskId, cancellationToken)
            ?? throw new RiskNotFoundException(riskId);

        // Check status is "Draft"
        if (risk.Status != Domain.Risk.RiskStatus.Draft)
        {
            throw new RiskValidationException("Оценка доступна только для рисков со статусом 'Черновик'.");
        }

        // Check no active assessments exist (concurrent assessment protection)
        if (await _riskAssessmentRepository.HasActiveAssessmentAsync(riskId, cancellationToken))
        {
            throw new RiskConflictException("Конкурирующая оценка: уже существует активная оценка для этого риска.");
        }

        // Validate required fields
        if (request.LikelihoodLevelId <= 0)
            throw new RiskValidationException("Необходимо указать уровень вероятности (likelihoodLevelId).");

        // Calculate lossAmount
        decimal lossAmount;
        decimal? downtimeTotalHours = null;
        decimal? hourlyDowntimeCost = null;
        decimal? downtimeWeeks = null;
        decimal? downtimeDays = null;
        decimal? downtimeHours = null;

        if (risk.AssetId.HasValue)
        {
            // Asset is filled: calculate downtime and loss
            if (request.DowntimeWeeks is null && request.DowntimeDays is null && request.DowntimeHours is null)
            {
                throw new RiskValidationException("Необходимо заполнить параметры простоя (downtimeWeeks, downtimeDays, downtimeHours).");
            }

            downtimeWeeks = request.DowntimeWeeks ?? 0;
            downtimeDays = request.DowntimeDays ?? 0;
            downtimeHours = request.DowntimeHours ?? 0;

            downtimeTotalHours = downtimeWeeks.Value * 168 + downtimeDays.Value * 24 + downtimeHours.Value;

            var assetHourCost = await _riskAssessmentRepository.GetAssetHourCostAsync(risk.AssetId.Value, cancellationToken);
            if (assetHourCost is null || assetHourCost.Value <= 0)
            {
                throw new RiskValidationException("Для указанного объекта не задана hourlyDowntimeCost (стоимость простоя в час).");
            }

            hourlyDowntimeCost = assetHourCost.Value;
            lossAmount = decimal.Round(downtimeTotalHours.Value * hourlyDowntimeCost.Value, 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            // No asset: use manual lossAmount
            if (request.LossAmount < 0)
            {
                throw new RiskValidationException("Сумма потерь не может быть отрицательной.");
            }
            lossAmount = request.LossAmount;
        }

        // Determine impactLevelId by monetary range
        var impactRange = await _riskAssessmentRepository.GetImpactRangeForAmountAsync(lossAmount, cancellationToken)
            ?? throw new RiskValidationException("Не удалось определить уровень влияния по сумме потерь. Проверьте справочник уровней влияния.");

        // Get weights and calculate riskScore
        var (likelihoodWeight, impactWeight) = await _riskAssessmentRepository.GetAssessmentWeightsAsync(
            request.LikelihoodLevelId,
            impactRange.ImpactLevelId,
            cancellationToken);

        var riskScore = likelihoodWeight * impactWeight;

        // Determine acceptanceLevel by thresholds
        var thresholds = await _riskAssessmentRepository.GetAcceptanceThresholdsAsync(_assessmentMethodId, cancellationToken);
        string acceptanceLevel;

        if (thresholds is not null)
        {
            if (riskScore <= thresholds.OwnerMaxScore)
                acceptanceLevel = "Owner";
            else if (riskScore <= thresholds.DirectorMaxScore)
                acceptanceLevel = "Director";
            else
                acceptanceLevel = "Ceo";
        }
        else
        {
            // Fallback: use simple thresholds
            acceptanceLevel = riskScore <= 4 ? "Owner" : riskScore <= 9 ? "Director" : "Ceo";
        }

        // Create Assessment
        var assessment = new RiskAssessment(
            riskId,
            _assessmentMethodId,
            request.LikelihoodLevelId,
            impactRange.ImpactLevelId,
            lossAmount,
            riskScore,
            userId,
            downtimeWeeks,
            downtimeDays,
            downtimeHours,
            downtimeTotalHours,
            hourlyDowntimeCost,
            responseMode: null);

        var assessmentId = await _riskAssessmentRepository.CreateAsync(assessment, cancellationToken);

        // Transition risk to "Open" status
        await _riskRepository.UpdateStatusAsync(riskId, "Open", cancellationToken);

        return new AssessRiskResponse(
            assessmentId,
            riskScore,
            acceptanceLevel,
            "Open");
    }

    // POST /v1/risks/{id}/accept
    public async Task<AcceptRiskResponse> AcceptAsync(
        int riskId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "accept_risk", cancellationToken))
            throw new RiskPermissionException("Отсутствует право accept_risk.");

        var risk = await _riskRepository.GetByIdAsync(riskId, cancellationToken)
            ?? throw new RiskNotFoundException(riskId);

        if (risk.Status != Domain.Risk.RiskStatus.Open)
            throw new RiskValidationException("Риск должен быть в статусе 'Активен'.");

        if (!await _riskAssessmentRepository.HasActiveAssessmentAsync(riskId, cancellationToken))
            throw new RiskValidationException("Для риска не найдена активная оценка.");

        var assessmentId = await GetActiveAssessmentIdAsync(riskId, cancellationToken);

        // Check responseMode is null
        // We need to check if the assessment has responseMode set
        // Since we don't have a direct method to get the assessment, we'll check via HasAssessmentWithNullResponseMode
        // If it returns false, responseMode is already set
        if (!await _riskAssessmentRepository.HasAssessmentWithNullResponseModeAsync(riskId, cancellationToken))
            throw new RiskValidationException("Режим ответа уже выбран.");

        // Check no blocking measures (non-terminal treatments on any linked process)
        var existingProcessIds = await _riskProcessRepository.GetExistingProcessIdsAsync(riskId, cancellationToken);
        foreach (var processId in existingProcessIds)
        {
            if (await _treatmentRepository.HasNonTerminalTreatmentAsync(processId, cancellationToken))
                throw new RiskConflictException("Риск имеет блокирующие меры обработки.");
        }

        // Set responseMode = "accept" and status = "Accepted"
        await _riskAssessmentRepository.UpdateResponseModeAsync(assessmentId, "accept", cancellationToken);
        await _riskRepository.UpdateStatusAsync(riskId, "Accepted", cancellationToken);

        return new AcceptRiskResponse(riskId, "Accepted", "accept");
    }

    // POST /v1/risks/{id}/treat
    public async Task<TreatRiskResponse> TreatAsync(
        int riskId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "edit_components", cancellationToken))
            throw new RiskPermissionException("Отсутствует право edit_components.");

        var risk = await _riskRepository.GetByIdAsync(riskId, cancellationToken)
            ?? throw new RiskNotFoundException(riskId);

        if (risk.Status != Domain.Risk.RiskStatus.Open)
            throw new RiskValidationException("Риск должен быть в статусе 'Активен'.");

        if (!await _riskAssessmentRepository.HasAssessmentWithNullResponseModeAsync(riskId, cancellationToken))
            throw new RiskValidationException("Режим ответа уже выбран или оценка отсутствует.");

        // Check primary process link exists
        if (!await _riskProcessRepository.HasPrimaryAsync(riskId, cancellationToken))
            throw new RiskValidationException("У риска не указана основная связь с процессом.");

        var assessmentId = await GetActiveAssessmentIdAsync(riskId, cancellationToken);
        await _riskAssessmentRepository.UpdateResponseModeAsync(assessmentId, "treat", cancellationToken);

        return new TreatRiskResponse(riskId, "treat");
    }

    // POST /v1/risks/{id}/split
    public async Task<SplitRiskResponse> SplitAsync(
        int riskId,
        int userId,
        SplitRiskRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "split_risk", cancellationToken))
            throw new RiskPermissionException("Отсутствует право split_risk.");

        var risk = await _riskRepository.GetByIdAsync(riskId, cancellationToken)
            ?? throw new RiskNotFoundException(riskId);

        if (risk.Status != Domain.Risk.RiskStatus.Open)
            throw new RiskValidationException("Риск должен быть в статусе 'Активен'.");

        // Validate processIds >= 2 BEFORE checking assessment
        if (request.ProcessIds.Length < 2)
            throw new RiskValidationException("Необходимо указать не менее двух процессов.");

        // Check uniqueness
        var uniqueProcessIds = request.ProcessIds.Distinct().ToArray();
        if (uniqueProcessIds.Length != request.ProcessIds.Length)
            throw new RiskValidationException("Указаны дублирующиеся процессы.");

        // Check primaryProcessId is in the list
        if (!request.ProcessIds.Contains(request.PrimaryProcessId))
            throw new RiskValidationException("Основной процесс должен быть в списке выбранных.");

        if (!await _riskAssessmentRepository.HasActiveAssessmentAsync(riskId, cancellationToken))
            throw new RiskValidationException("Для риска не найдена активная оценка.");

        // Check all processes are active
        foreach (var processId in request.ProcessIds)
        {
            if (!await _processRepository.UserExistsAsync(processId, cancellationToken))
            {
                // ProcessRepository.UserExistsAsync checks if a user exists, not a process
                // We need to check if the process is active
                // Let me use GetOwnerInfoAsync instead
            }
        }

        // Check processes are active using GetOwnerInfoAsync
        var processOwnerInfos = new List<ProcessOwnerInfo>();
        foreach (var processId in request.ProcessIds)
        {
            var ownerInfo = await _processRepository.GetOwnerInfoAsync(processId, cancellationToken);
            if (ownerInfo is null || !ownerInfo.IsActive)
                throw new RiskValidationException($"Процесс с ID {processId} не найден или неактивен.");
            processOwnerInfos.Add(ownerInfo);
        }

        // Check no blocking measures on excluded processes
        var existingProcessIds = await _riskProcessRepository.GetExistingProcessIdsAsync(riskId, cancellationToken);
        var excludedProcessIds = existingProcessIds.Except(request.ProcessIds).ToArray();
        foreach (var processId in excludedProcessIds)
        {
            if (await _treatmentRepository.HasNonTerminalTreatmentAsync(processId, cancellationToken))
                throw new RiskValidationException($"Процесс с ID {processId} имеет нетерминальную меру обработки и не может быть исключён.");
        }

        // Deactivate existing links
        await _riskProcessRepository.DeactivateRiskProcessLinksAsync(riskId, cancellationToken);

        // Create new risk_processes for selected processIds
        foreach (var ownerInfo in processOwnerInfos)
        {
            var ownerUserId = ownerInfo.OwnerUserId ?? userId;
            await _riskProcessRepository.CreateRiskProcessAsync(
                riskId,
                ownerUserId,
                ownerInfo.OwnerPersonId,
                ownerInfo.ProcessId == request.PrimaryProcessId,
                cancellationToken);
        }

        var assessmentId = await GetActiveAssessmentIdAsync(riskId, cancellationToken);
        await _riskAssessmentRepository.UpdateResponseModeAsync(assessmentId, "split", cancellationToken);

        return new SplitRiskResponse(riskId, "split");
    }

    // POST /v1/risks/{id}/transfer
    public async Task<TransferRiskResponse> TransferAsync(
        int riskId,
        int userId,
        TransferRiskRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "transfer_risk", cancellationToken))
            throw new RiskPermissionException("Отсутствует право transfer_risk.");

        var risk = await _riskRepository.GetByIdAsync(riskId, cancellationToken)
            ?? throw new RiskNotFoundException(riskId);

        if (risk.Status != Domain.Risk.RiskStatus.Open)
            throw new RiskValidationException("Риск должен быть в статусе 'Активен'.");

        if (!await _riskAssessmentRepository.HasAssessmentWithNullResponseModeAsync(riskId, cancellationToken))
            throw new RiskValidationException("Режим ответа уже выбран или оценка отсутствует.");

        // Check all treatments on current processes are terminal
        var existingProcessIds = await _riskProcessRepository.GetExistingProcessIdsAsync(riskId, cancellationToken);
        foreach (var processId in existingProcessIds)
        {
            if (!await _treatmentRepository.AllTreatmentsTerminalAsync(processId, cancellationToken))
                throw new RiskValidationException("Риск имеет нетерминальные меры обработки.");
        }

        // Check new process is active
        var newOwnerInfo = await _processRepository.GetOwnerInfoAsync(request.NewProcessId, cancellationToken)
            ?? throw new RiskValidationException("Новый процесс не найден.");

        if (!newOwnerInfo.IsActive)
            throw new RiskValidationException("Новый процесс неактивен.");

        // Update assessment.responseMode = "transfer"
        var assessmentId = await GetActiveAssessmentIdAsync(riskId, cancellationToken);
        await _riskAssessmentRepository.UpdateResponseModeAsync(assessmentId, "transfer", cancellationToken);

        // Deactivate current assessment
        await _riskAssessmentRepository.DeactivateAssessmentAsync(riskId, cancellationToken);

        // Fill unlinkedAt for previous risk_processes
        await _riskProcessRepository.DeactivateRiskProcessLinksAsync(riskId, cancellationToken);

        // Create new primary process link
        var newOwnerUserId = newOwnerInfo.OwnerUserId ?? userId;
        await _riskProcessRepository.CreateRiskProcessAsync(
            riskId,
            newOwnerUserId,
            newOwnerInfo.OwnerPersonId,
            true,
            cancellationToken);

        // Clear acceptanceLevel
        await _riskRepository.ClearAcceptanceLevelAsync(riskId, cancellationToken);

        // Set status = "AwaitingReassessment"
        await _riskRepository.SetAwaitingReassessmentStatusAsync(riskId, cancellationToken);

        return new TransferRiskResponse(riskId, "transfer", "AwaitingReassessment");
    }

    // POST /v1/risks/{id}/reassess
    public async Task<ReassessRiskResponse> ReassessAsync(
        int riskId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "edit_components", cancellationToken))
            throw new RiskPermissionException("Отсутствует право edit_components.");

        var risk = await _riskRepository.GetByIdAsync(riskId, cancellationToken)
            ?? throw new RiskNotFoundException(riskId);

        if (risk.Status != Domain.Risk.RiskStatus.Accepted &&
            risk.Status != Domain.Risk.RiskStatus.Closed)
            throw new RiskValidationException("Риск должен быть в статусе 'Принят' или 'Закрыт'.");

        // Deactivate current assessment if exists
        if (await _riskAssessmentRepository.HasActiveAssessmentAsync(riskId, cancellationToken))
            await _riskAssessmentRepository.DeactivateAssessmentAsync(riskId, cancellationToken);

        // Clear acceptanceLevel
        await _riskRepository.ClearAcceptanceLevelAsync(riskId, cancellationToken);

        // Clear closeReason if closed
        if (risk.Status == Domain.Risk.RiskStatus.Closed)
        {
            await _riskRepository.UpdateRiskStatusAndReasonAsync(riskId, "AwaitingReassessment", null, cancellationToken);

            // Create system comment for closed risk
            await _commentRepository.CreateAsync(
                userId,
                "Риск возвращён на переоценку.",
                "Risk",
                riskId,
                cancellationToken);
        }
        else
        {
            await _riskRepository.SetAwaitingReassessmentStatusAsync(riskId, cancellationToken);
        }

        return new ReassessRiskResponse(riskId, "AwaitingReassessment");
    }

    // POST /v1/risks/{id}/close
    public async Task<CloseRiskResponse> CloseAsync(
        int riskId,
        int userId,
        CloseRiskRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "close_risk", cancellationToken))
            throw new RiskPermissionException("Отсутствует право close_risk.");

        var risk = await _riskRepository.GetByIdAsync(riskId, cancellationToken)
            ?? throw new RiskNotFoundException(riskId);

        // Check allowed statuses
        var allowed = risk.Status is Domain.Risk.RiskStatus.Draft or
                      Domain.Risk.RiskStatus.Open or
                      Domain.Risk.RiskStatus.Accepted or
                      Domain.Risk.RiskStatus.AwaitingReassessment;

        if (!allowed)
            throw new RiskValidationException("Риск не может быть закрыт в текущем статусе.");

        // Check no blocking measures
        var existingProcessIds = await _riskProcessRepository.GetExistingProcessIdsAsync(riskId, cancellationToken);
        foreach (var processId in existingProcessIds)
        {
            if (await _treatmentRepository.HasNonTerminalTreatmentAsync(processId, cancellationToken))
                throw new RiskValidationException("Риск имеет блокирующие меры обработки.");
        }

        // Validate closeReason
        var reason = ValidateCloseReason(request.CloseReason);

        // Atomically save status "Closed", closeReason, and create system comment
        await _riskRepository.UpdateRiskStatusAndReasonAsync(riskId, "Closed", reason, cancellationToken);
        await _commentRepository.CreateAsync(
            userId,
            $"Риск закрыт: {reason}",
            "Risk",
            riskId,
            cancellationToken);

        return new CloseRiskResponse(riskId, "Closed");
    }

    private async Task<int> GetActiveAssessmentIdAsync(
        int riskId,
        CancellationToken cancellationToken)
    {
        var assessmentId = await _riskAssessmentRepository.GetActiveAssessmentIdAsync(riskId, cancellationToken)
            ?? throw new RiskValidationException("Для риска не найдена активная оценка.");

        return assessmentId;
    }

    private static string ValidateCloseReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new RiskValidationException("Причина закрытия риска обязательна.");

        var normalized = reason.Trim();
        if (normalized.Length > 1000)
            throw new RiskValidationException("Причина закрытия слишком длинная.");

        return normalized;
    }

    private static string ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new RiskValidationException("Название риска обязательно.");

        var normalized = title.Trim();
        if (normalized.Length > 300)
            throw new RiskValidationException("Название риска слишком длинное.");

        return normalized;
    }

    private static string ValidateDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new RiskValidationException("Описание риска обязательно.");

        return description.Trim();
    }
}

// DTOs
public sealed record CreateRiskRequest(
    string RiskTitle,
    string RiskDescription,
    int HazardId,
    int CategoryId,
    int? AssetId,
    int[] ProcessIds);

public sealed record UpdateRiskRequest(
    string RiskTitle,
    string RiskDescription,
    int HazardId,
    int CategoryId,
    int? AssetId,
    int[] ProcessIds);

public sealed record AssessRiskRequest(
    int LikelihoodLevelId,
    decimal LossAmount,
    decimal? DowntimeWeeks,
    decimal? DowntimeDays,
    decimal? DowntimeHours);

public sealed record AssessRiskResponse(
    int AssessmentId,
    int RiskScore,
    string AcceptanceLevel,
    string RiskStatus);

// Lifecycle DTOs
public sealed record SplitRiskRequest(int[] ProcessIds, int PrimaryProcessId);
public sealed record TransferRiskRequest(int NewProcessId);
public sealed record CloseRiskRequest(string CloseReason);

public sealed record AcceptRiskResponse(int RiskId, string RiskStatus, string ResponseMode);
public sealed record TreatRiskResponse(int RiskId, string ResponseMode);
public sealed record SplitRiskResponse(int RiskId, string ResponseMode);
public sealed record TransferRiskResponse(int RiskId, string ResponseMode, string RiskStatus);
public sealed record ReassessRiskResponse(int RiskId, string RiskStatus);
public sealed record CloseRiskResponse(int RiskId, string RiskStatus);

// Exceptions
public sealed class RiskValidationException(string message) : Exception(message);
public sealed class RiskNotFoundException(int id) : Exception($"Риск с ID {id} не найден.");
public sealed class RiskConflictException(string message) : Exception(message);
public sealed class RiskPermissionException(string message) : Exception(message);
public sealed class RiskAuthorizationException(string message) : Exception(message);
