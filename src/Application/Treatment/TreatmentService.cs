using Application.Abstractions;
using Application.Risk.Risks;
using Domain.Treatment;

namespace Application.Treatment;

public sealed class TreatmentService
{
    private readonly ITreatmentRepository _treatmentRepository;
    private readonly IApprovalRequestRepository _approvalRequestRepository;
    private readonly IUserPermissionService _permissionService;
    private readonly IProcessRepository _processRepository;
    private readonly ITreatmentApprovalSettingsRepository _approvalSettingsRepository;
    private readonly RiskService _riskService;
    private readonly IRiskEventRepository _eventRepository;

    public TreatmentService(
        ITreatmentRepository treatmentRepository,
        IApprovalRequestRepository approvalRequestRepository,
        IUserPermissionService permissionService,
        IProcessRepository processRepository,
        ITreatmentApprovalSettingsRepository approvalSettingsRepository,
        RiskService riskService,
        IRiskEventRepository eventRepository)
    {
        _treatmentRepository = treatmentRepository;
        _approvalRequestRepository = approvalRequestRepository;
        _permissionService = permissionService;
        _processRepository = processRepository;
        _approvalSettingsRepository = approvalSettingsRepository;
        _riskService = riskService;
        _eventRepository = eventRepository;
    }

    // Measure events go to the history of their risk (the card's «История»).
    private async Task LogAsync(int riskProcessId, string kind, string text, int userId, CancellationToken cancellationToken)
    {
        if (await _treatmentRepository.GetProcessRiskIdAsync(riskProcessId, cancellationToken) is int riskId)
            await _eventRepository.AddAsync(riskId, kind, text, userId, cancellationToken);
    }

    private static string MeasureId(int id) => $"М-{id:D4}";

    private static string LevelCode(ApprovalLevel level) => level.ToString().ToUpperInvariant();

    // Grouped like the UI (1 500 000 ₽) without depending on the server's culture data.
    private static string Money(decimal value) =>
        value.ToString("#,0", new System.Globalization.NumberFormatInfo { NumberGroupSeparator = "\u00a0" }) + " ₽";

    // POST /v1/treatments
    public async Task<int> CreateAsync(
        int userId,
        CreateTreatmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "create_components", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право create_components.");

        var riskId = await _treatmentRepository.GetProcessRiskIdAsync(request.RiskProcessId, cancellationToken)
            ?? throw new TreatmentNotFoundException(request.RiskProcessId);

        var riskStatus = await _treatmentRepository.GetRiskStatusAsync(riskId, cancellationToken)
            ?? throw new TreatmentNotFoundException(riskId);

        if (riskStatus != "Open")
            throw new TreatmentValidationException("Риск должен быть в статусе 'Активен'.");

        if (await _treatmentRepository.GetActiveAssessmentIdAsync(riskId, cancellationToken) is null)
            throw new TreatmentValidationException("Для риска не найдена активная оценка.");

        await EnsureTreatModeAsync(riskId, cancellationToken);

        if (!await _treatmentRepository.IsLinkActiveAsync(request.RiskProcessId, cancellationToken))
            throw new TreatmentValidationException("Процесс больше не связан с риском.");

        if (request.DueDate is null)
            throw new TreatmentValidationException("Необходимо указать срок выполнения.");

        var treatment = new Domain.Treatment.Treatment(
            request.RiskProcessId,
            request.TreatmentTitle,
            request.TreatmentDescription,
            request.DueDate ?? DateTimeOffset.UtcNow.AddDays(30),
            request.TreatmentCostPlan,
            userId);

        var treatmentId = await _treatmentRepository.CreateAsync(treatment, cancellationToken);
        await LogAsync(request.RiskProcessId, "measure", $"Создана мера {MeasureId(treatmentId)} «{treatment.Title}» в статусе «Черновик»", userId, cancellationToken);
        return treatmentId;
    }

    // PUT /v1/treatments/{id}
    public async Task<int> UpdateAsync(
        int treatmentId,
        int userId,
        UpdateTreatmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "edit_components", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право edit_components.");

        var treatment = await _treatmentRepository.GetByIdAsync(treatmentId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatmentId);
        await EnsureOwnerAsync(treatmentId, userId, cancellationToken);

        if (treatment.Status != TreatmentStatus.Planned)
            throw new TreatmentValidationException("Мероприятие должно быть в статусе 'Черновик'.");

        if (request.TreatmentTitle is not null)
            treatment.Rename(request.TreatmentTitle);
        if (request.TreatmentDescription is not null)
            treatment.ChangeDescription(request.TreatmentDescription);
        if (request.DueDate.HasValue)
            treatment.Reschedule(request.DueDate.Value);
        if (request.TreatmentCostPlan.HasValue)
            treatment.SetPlannedCost(request.TreatmentCostPlan.Value);
        if (request.TreatmentCostAct.HasValue)
            treatment.SetActualCost(request.TreatmentCostAct.Value);

        await _treatmentRepository.UpdateAsync(treatment, cancellationToken);
        await LogAsync(treatment.RiskProcessId, "measure", $"Мера {MeasureId(treatmentId)} отредактирована", userId, cancellationToken);
        return treatmentId;
    }

    // POST /v1/treatments/{id}/submit-approval
    // GET /v1/approval-requests
    public async Task<IReadOnlyList<ApprovalRequestItem>> GetApprovalRequestsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "view_risk", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право view_risk.");

        return await _approvalRequestRepository.GetAllAsync(cancellationToken);
    }

    // GET /v1/treatments
    public async Task<IReadOnlyList<TreatmentListItem>> GetListAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "view_risk", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право view_risk.");

        return await _treatmentRepository.GetListAsync(cancellationToken);
    }

    public async Task<SubmitApprovalResponse> SubmitApprovalAsync(
        int treatmentId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "execute_treatment", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право execute_treatment.");

        var treatment = await _treatmentRepository.GetByIdAsync(treatmentId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatmentId);
        await EnsureOwnerAsync(treatmentId, userId, cancellationToken);

        if (treatment.Status != TreatmentStatus.Planned)
            throw new TreatmentValidationException("Мероприятие должно быть в статусе 'Черновик'.");

        var riskId = await _treatmentRepository.GetProcessRiskIdAsync(treatment.RiskProcessId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatment.RiskProcessId);

        var riskStatus = await _treatmentRepository.GetRiskStatusAsync(riskId, cancellationToken)
            ?? throw new TreatmentNotFoundException(riskId);

        if (riskStatus != "Open")
            throw new TreatmentValidationException("Риск должен быть в статусе 'Активен'.");

        if (await _treatmentRepository.GetActiveAssessmentIdAsync(riskId, cancellationToken) is null)
            throw new TreatmentValidationException("Для риска не найдена активная оценка.");

        await EnsureTreatModeAsync(riskId, cancellationToken);

        if (await _approvalRequestRepository.HasOpenApprovalAsync(treatmentId, cancellationToken))
            throw new TreatmentValidationException("Уже существует открытый запрос на согласование.");

        if (await _treatmentRepository.HasConflictingTreatmentAsync(riskId, treatmentId, cancellationToken))
            throw new TreatmentValidationException("По этой дирекции уже есть мера в работе или на согласовании.");

        var settings = await _approvalSettingsRepository.GetAsync(cancellationToken);
        if (treatment.PlannedCost < settings.OwnerThreshold)
            throw new TreatmentValidationException("Согласование не требуется: передайте меру к выполнению.");

        ApprovalLevel approvalLevel;
        // Thresholds are inclusive lower bounds of each level
        if (treatment.PlannedCost >= settings.CeoThreshold)
            approvalLevel = ApprovalLevel.Ceo;
        else if (treatment.PlannedCost >= settings.DirectorThreshold)
            approvalLevel = ApprovalLevel.Director;
        else
            approvalLevel = ApprovalLevel.Owner;

        var approvalRequest = new ApprovalRequest(treatmentId, approvalLevel, userId);
        var approvalRequestId = await _approvalRequestRepository.CreateAsync(approvalRequest, cancellationToken);

        treatment.SubmitForApproval();
        await _treatmentRepository.UpdateStatusAsync(treatmentId, "PendingApproval", cancellationToken);
        await LogAsync(treatment.RiskProcessId, "approval", $"{MeasureId(treatmentId)} отправлена на согласование: {Money(treatment.PlannedCost)}, уровень {LevelCode(approvalLevel)}", userId, cancellationToken);

        return new SubmitApprovalResponse(approvalRequestId, approvalLevel.ToString(), "PendingApproval");
    }

    // POST /v1/approval-requests/{id}/approve
    public async Task<ApproveResponse> ApproveAsync(
        int approvalRequestId,
        int userId,
        string? approvalComment,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "approve_treatment", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право approve_treatment.");

        var approvalRequest = await _approvalRequestRepository.GetByIdAsync(approvalRequestId, cancellationToken)
            ?? throw new TreatmentNotFoundException(approvalRequestId);

        if (approvalRequest.Status != ApprovalStatus.Pending)
            throw new TreatmentValidationException("Запрос уже обработан.");

        var treatment = await _treatmentRepository.GetByIdAsync(approvalRequest.TreatmentId, cancellationToken)
            ?? throw new TreatmentNotFoundException(approvalRequest.TreatmentId);
        await EnsureApproverAsync(approvalRequest, userId, cancellationToken);

        var riskId = await _treatmentRepository.GetProcessRiskIdAsync(treatment.RiskProcessId, cancellationToken) ?? 0;
        if (await _treatmentRepository.HasConflictingTreatmentAsync(riskId, treatment.Id, cancellationToken))
            throw new TreatmentValidationException("По этой дирекции уже есть мера в работе или на согласовании.");

        treatment.Approve();
        approvalRequest.Approve(userId, approvalComment);
        await _approvalRequestRepository.UpdateAsync(approvalRequest, cancellationToken);
        await _treatmentRepository.UpdateStatusAsync(treatment.Id, "Approved", cancellationToken);
        await LogAsync(treatment.RiskProcessId, "approval", $"{MeasureId(treatment.Id)} согласована на уровне {LevelCode(approvalRequest.RequiredLevel)}", userId, cancellationToken);

        return new ApproveResponse(approvalRequestId, "Approved", "Approved");
    }

    // POST /v1/approval-requests/{id}/reject
    public async Task<RejectResponse> RejectAsync(
        int approvalRequestId,
        int userId,
        string approvalReason,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "approve_treatment", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право approve_treatment.");

        var approvalRequest = await _approvalRequestRepository.GetByIdAsync(approvalRequestId, cancellationToken)
            ?? throw new TreatmentNotFoundException(approvalRequestId);

        if (approvalRequest.Status != ApprovalStatus.Pending)
            throw new TreatmentValidationException("Запрос уже обработан.");

        if (string.IsNullOrWhiteSpace(approvalReason))
            throw new TreatmentValidationException("Причина отклонения обязательна.");

        var treatment = await _treatmentRepository.GetByIdAsync(approvalRequest.TreatmentId, cancellationToken)
            ?? throw new TreatmentNotFoundException(approvalRequest.TreatmentId);
        await EnsureApproverAsync(approvalRequest, userId, cancellationToken);

        treatment.Reject();
        approvalRequest.Reject(userId, approvalReason);
        await _approvalRequestRepository.UpdateAsync(approvalRequest, cancellationToken);
        await _treatmentRepository.UpdateStatusAsync(treatment.Id, "Planned", cancellationToken);
        await LogAsync(treatment.RiskProcessId, "approval", $"{MeasureId(treatment.Id)} отклонена на уровне {LevelCode(approvalRequest.RequiredLevel)}. Причина: {approvalReason.Trim()}", userId, cancellationToken);

        return new RejectResponse(approvalRequestId, "Rejected", "Planned");
    }

    // POST /v1/approval-requests/{id}/revoke
    public async Task<RevokeResponse> RevokeAsync(
        int approvalRequestId,
        int userId,
        string revokeReason,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.IsAdminAsync(userId, cancellationToken))
            throw new TreatmentPermissionException("Отзывает согласование только администратор.");

        var approvalRequest = await _approvalRequestRepository.GetByIdAsync(approvalRequestId, cancellationToken)
            ?? throw new TreatmentNotFoundException(approvalRequestId);

        if (approvalRequest.Status is not (ApprovalStatus.Pending or ApprovalStatus.Approved))
            throw new TreatmentValidationException("Можно отозвать только ожидающий или согласованный запрос.");

        if (string.IsNullOrWhiteSpace(revokeReason))
            throw new TreatmentValidationException("Причина отзыва обязательна.");

        var treatment = await _treatmentRepository.GetByIdAsync(approvalRequest.TreatmentId, cancellationToken)
            ?? throw new TreatmentNotFoundException(approvalRequest.TreatmentId);

        if (treatment.Status is not (TreatmentStatus.PendingApproval or TreatmentStatus.Approved))
            throw new TreatmentValidationException("Мера уже передана к выполнению, согласование не отозвать.");

        treatment.Revoke();
        approvalRequest.Revoke(userId, revokeReason);
        await _approvalRequestRepository.UpdateAsync(approvalRequest, cancellationToken);
        await _treatmentRepository.UpdateStatusAsync(treatment.Id, treatment.Status.ToString(), cancellationToken);
        await LogAsync(treatment.RiskProcessId, "approval", $"Согласование {MeasureId(treatment.Id)} отозвано администратором. Причина: {revokeReason.Trim()}", userId, cancellationToken);

        if (treatment.Status == TreatmentStatus.Cancelled)
        {
            var riskId = await _treatmentRepository.GetProcessRiskIdAsync(treatment.RiskProcessId, cancellationToken);
            if (riskId.HasValue && !await HasBlockingTreatmentsAsync(riskId.Value, cancellationToken))
                await _riskService.CompleteCycleAsync(riskId.Value, cancellationToken);
        }

        return new RevokeResponse(approvalRequestId, "Revoked", treatment.Status.ToString());
    }

    // POST /v1/treatments/{id}/execute
    public async Task<ExecuteResponse> ExecuteAsync(
        int treatmentId,
        int userId,
        ExecuteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "execute_treatment", cancellationToken) ||
            !await _permissionService.HasRightAsync(userId, "assign_treatment_executor", cancellationToken))
            throw new TreatmentPermissionException("Отсутствуют необходимые права.");

        var treatment = await _treatmentRepository.GetByIdAsync(treatmentId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatmentId);
        await EnsureOwnerAsync(treatmentId, userId, cancellationToken);

        if (treatment.Status is not (TreatmentStatus.Planned or TreatmentStatus.Approved))
            throw new TreatmentValidationException("Мероприятие должно быть в статусе 'Черновик' или 'Согласовано'.");

        if (request.AssigneeId <= 0)
            throw new TreatmentValidationException("Исполнитель не выбран.");

        var riskId = await _treatmentRepository.GetProcessRiskIdAsync(treatment.RiskProcessId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatment.RiskProcessId);

        if (await _treatmentRepository.GetRiskStatusAsync(riskId, cancellationToken) != "Open")
            throw new TreatmentValidationException("Риск должен быть в статусе 'Активен'.");

        await EnsureTreatModeAsync(riskId, cancellationToken);

        if (treatment.Status == TreatmentStatus.Planned)
        {
            var settings = await _approvalSettingsRepository.GetAsync(cancellationToken);
            if (treatment.PlannedCost >= settings.OwnerThreshold)
                throw new TreatmentValidationException("Мера требует согласования: отправьте её на согласование.");
        }

        if (await _treatmentRepository.HasConflictingTreatmentAsync(riskId, treatmentId, cancellationToken))
            throw new TreatmentValidationException("По этой дирекции уже есть мера в работе или на согласовании.");

        await EnsureExecutorAsync(request.AssigneeId, treatmentId, cancellationToken);

        treatment.AssignTo(request.AssigneeId);
        await _treatmentRepository.UpdateAssigneeAsync(treatmentId, request.AssigneeId, userId, cancellationToken);
        await _treatmentRepository.UpdateStatusAsync(treatmentId, "AwaitingExecution", cancellationToken);
        await LogAsync(treatment.RiskProcessId, "measure", $"{MeasureId(treatmentId)} передана к выполнению", userId, cancellationToken);

        return new ExecuteResponse(treatmentId, "AwaitingExecution", 0);
    }

    // POST /v1/treatments/{id}/replace-assignee
    public async Task<ReplaceAssigneeResponse> ReplaceAssigneeAsync(
        int treatmentId,
        int userId,
        ReplaceAssigneeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "assign_treatment_executor", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право assign_treatment_executor.");

        var treatment = await _treatmentRepository.GetByIdAsync(treatmentId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatmentId);
        await EnsureOwnerAsync(treatmentId, userId, cancellationToken);

        if (treatment.Status is not (TreatmentStatus.AwaitingExecution or TreatmentStatus.InProgress or TreatmentStatus.Hold))
            throw new TreatmentValidationException("Недопустимый статус для замены исполнителя.");

        if (treatment.AssignedToId is null && treatment.Status != TreatmentStatus.Hold)
            throw new TreatmentValidationException("Назначение отсутствует.");

        if (request.AssigneeId <= 0)
            throw new TreatmentValidationException("Новый исполнитель недопустим.");

        await EnsureExecutorAsync(request.AssigneeId, treatmentId, cancellationToken);

        await _treatmentRepository.UpdateAssigneeAsync(treatmentId, request.AssigneeId, userId, cancellationToken);
        await LogAsync(treatment.RiskProcessId, "measure", $"{MeasureId(treatmentId)}: назначен другой исполнитель", userId, cancellationToken);

        return new ReplaceAssigneeResponse(treatmentId, request.AssigneeId);
    }

    // POST /v1/treatments/{id}/remove-assignee
    public async Task<RemoveAssigneeResponse> RemoveAssigneeAsync(
        int treatmentId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "assign_treatment_executor", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право assign_treatment_executor.");

        var treatment = await _treatmentRepository.GetByIdAsync(treatmentId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatmentId);
        await EnsureOwnerAsync(treatmentId, userId, cancellationToken);

        if (treatment.Status != TreatmentStatus.Hold)
            throw new TreatmentValidationException("Мероприятие должно быть в статусе 'Холд'.");

        if (treatment.AssignedToId is null)
            throw new TreatmentValidationException("Назначение отсутствует.");

        await _treatmentRepository.UpdateAssigneeAsync(treatmentId, null, userId, cancellationToken);
        await LogAsync(treatment.RiskProcessId, "measure", $"{MeasureId(treatmentId)}: исполнитель снят", userId, cancellationToken);

        return new RemoveAssigneeResponse(treatmentId);
    }

    // POST /v1/treatments/{id}/start
    public async Task<StartResponse> StartAsync(
        int treatmentId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "execute_treatment", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право execute_treatment.");

        var treatment = await _treatmentRepository.GetByIdAsync(treatmentId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatmentId);
        await EnsureOwnerAsync(treatmentId, userId, cancellationToken);

        if (treatment.Status != TreatmentStatus.AwaitingExecution)
            throw new TreatmentValidationException("Мероприятие должно быть в статусе 'Ожидает выполнения'.");

        if (treatment.AssignedToId is null)
            throw new TreatmentValidationException("Необходимо назначить исполнителя.");

        await EnsureExecutorAsync(treatment.AssignedToId.Value, treatmentId, cancellationToken);

        var riskId = await _treatmentRepository.GetProcessRiskIdAsync(treatment.RiskProcessId, cancellationToken) ?? 0;
        if (await _treatmentRepository.HasConflictingTreatmentAsync(riskId, treatmentId, cancellationToken))
            throw new TreatmentValidationException("Существует конфликтующее мероприятие.");

        treatment.StartExecution();
        await _treatmentRepository.UpdateStatusAsync(treatmentId, "InProgress", cancellationToken);
        await LogAsync(treatment.RiskProcessId, "measure", $"{MeasureId(treatmentId)} взята в работу", userId, cancellationToken);

        return new StartResponse(treatmentId, "InProgress");
    }

    // POST /v1/treatments/{id}/hold
    public async Task<HoldResponse> HoldAsync(
        int treatmentId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "execute_treatment", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право execute_treatment.");

        var treatment = await _treatmentRepository.GetByIdAsync(treatmentId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatmentId);
        await EnsureOwnerAsync(treatmentId, userId, cancellationToken);

        if (treatment.Status != TreatmentStatus.InProgress)
            throw new TreatmentValidationException("Мероприятие должно быть в статусе 'В работе'.");

        treatment.Hold();
        await _treatmentRepository.UpdateStatusAsync(treatmentId, "Hold", cancellationToken);
        await LogAsync(treatment.RiskProcessId, "measure", $"{MeasureId(treatmentId)} приостановлена (Холд)", userId, cancellationToken);

        return new HoldResponse(treatmentId, "Hold");
    }

    // POST /v1/treatments/{id}/resume
    public async Task<ResumeResponse> ResumeAsync(
        int treatmentId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "execute_treatment", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право execute_treatment.");

        var treatment = await _treatmentRepository.GetByIdAsync(treatmentId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatmentId);
        await EnsureOwnerAsync(treatmentId, userId, cancellationToken);

        if (treatment.Status != TreatmentStatus.Hold)
            throw new TreatmentValidationException("Мероприятие должно быть в статусе 'Холд'.");

        if (treatment.AssignedToId is null)
            throw new TreatmentValidationException("Необходимо назначить исполнителя.");

        var riskId = await _treatmentRepository.GetProcessRiskIdAsync(treatment.RiskProcessId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatment.RiskProcessId);

        var riskStatus = await _treatmentRepository.GetRiskStatusAsync(riskId, cancellationToken);
        if (riskStatus != "Open")
            throw new TreatmentValidationException("Риск должен быть в статусе 'Активен'.");

        if (await _treatmentRepository.GetActiveAssessmentIdAsync(riskId, cancellationToken) is null)
            throw new TreatmentValidationException("Для риска не найдена активная оценка.");

        await EnsureTreatModeAsync(riskId, cancellationToken);

        if (!await _treatmentRepository.IsLinkActiveAsync(treatment.RiskProcessId, cancellationToken))
            throw new TreatmentValidationException("Процесс больше не связан с риском.");

        await EnsureExecutorAsync(treatment.AssignedToId.Value, treatmentId, cancellationToken);

        if (await _treatmentRepository.HasConflictingTreatmentAsync(riskId, treatmentId, cancellationToken))
            throw new TreatmentValidationException("По этой дирекции уже есть мера в работе или на согласовании.");

        treatment.Resume();
        await _treatmentRepository.UpdateStatusAsync(treatmentId, "InProgress", cancellationToken);
        await LogAsync(treatment.RiskProcessId, "measure", $"{MeasureId(treatmentId)} возобновлена", userId, cancellationToken);

        return new ResumeResponse(treatmentId, "InProgress");
    }

    // POST /v1/treatments/{id}/complete
    public async Task<CompleteResponse> CompleteAsync(
        int treatmentId,
        int userId,
        CompleteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "execute_treatment", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право execute_treatment.");

        var treatment = await _treatmentRepository.GetByIdAsync(treatmentId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatmentId);
        await EnsureOwnerAsync(treatmentId, userId, cancellationToken);

        if (treatment.Status != TreatmentStatus.InProgress)
            throw new TreatmentValidationException("Мероприятие должно быть в статусе 'В работе'.");

        if (request.TreatmentCostAct < 0)
            throw new TreatmentValidationException("Фактическая стоимость не может быть отрицательной.");

        var riskId = await _treatmentRepository.GetProcessRiskIdAsync(treatment.RiskProcessId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatment.RiskProcessId);

        var activeProcessIds = await _treatmentRepository.GetActiveRiskProcessIdsAsync(riskId, cancellationToken);
        var hasOtherActiveTreatments = false;
        foreach (var processId in activeProcessIds)
        {
            if (processId != treatment.RiskProcessId &&
                await _treatmentRepository.HasActiveTreatmentAsync(processId, cancellationToken))
            {
                hasOtherActiveTreatments = true;
                break;
            }
        }

        treatment.Complete(request.TreatmentCostAct);
        await _treatmentRepository.UpdateAsync(treatment, cancellationToken);
        await _treatmentRepository.UpdateStatusAsync(treatmentId, "Completed", cancellationToken);
        await LogAsync(treatment.RiskProcessId, "done", $"{MeasureId(treatmentId)} выполнена: факт {Money(request.TreatmentCostAct)} при плане {Money(treatment.PlannedCost)}", userId, cancellationToken);

        if (hasOtherActiveTreatments)
        {
            // Active treatments remain — only update treatment status (already done)
            var currentRiskStatus = await _treatmentRepository.GetRiskStatusAsync(riskId, cancellationToken) ?? "Unknown";
            return new CompleteResponse(treatmentId, "Completed", currentRiskStatus, false, false);
        }

        // No active treatments remain — trigger system cycle completion
        var cycleResult = await _riskService.CompleteCycleAsync(riskId, cancellationToken);

        return new CompleteResponse(
            treatmentId,
            "Completed",
            cycleResult.RiskStatus,
            cycleResult.CycleCompleted,
            cycleResult.HasDraftsWarning);
    }

    // POST /v1/treatments/{id}/cancel
    public async Task<CancelResponse> CancelAsync(
        int treatmentId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "execute_treatment", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право execute_treatment.");

        var treatment = await _treatmentRepository.GetByIdAsync(treatmentId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatmentId);
        await EnsureOwnerAsync(treatmentId, userId, cancellationToken);

        if (treatment.Status is not (TreatmentStatus.Planned or TreatmentStatus.AwaitingExecution or TreatmentStatus.InProgress))
            throw new TreatmentValidationException("Недопустимый статус для отмены.");

        var riskId = await _treatmentRepository.GetProcessRiskIdAsync(treatment.RiskProcessId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatment.RiskProcessId);

        var wasDraft = treatment.Status == TreatmentStatus.Planned;
        treatment.CancelExecution();
        await _treatmentRepository.UpdateStatusAsync(treatmentId, "Cancelled", cancellationToken);
        await LogAsync(treatment.RiskProcessId, "measure", $"{MeasureId(treatmentId)} отменена", userId, cancellationToken);

        if (wasDraft)
        {
            var riskStatus = await _treatmentRepository.GetRiskStatusAsync(riskId, cancellationToken) ?? "Unknown";
            return new CancelResponse(treatmentId, "Cancelled", riskStatus, false, false);
        }

        var activeProcessIds = await _treatmentRepository.GetActiveRiskProcessIdsAsync(riskId, cancellationToken);
        var hasOtherActiveTreatments = false;
        foreach (var processId in activeProcessIds)
        {
            if (await _treatmentRepository.HasActiveTreatmentAsync(processId, cancellationToken))
            {
                hasOtherActiveTreatments = true;
                break;
            }
        }

        if (hasOtherActiveTreatments)
        {
            var currentRiskStatus = await _treatmentRepository.GetRiskStatusAsync(riskId, cancellationToken) ?? "Unknown";
            return new CancelResponse(treatmentId, "Cancelled", currentRiskStatus, false, false);
        }

        // No active treatments remain — trigger system cycle completion
        var cycleResult = await _riskService.CompleteCycleAsync(riskId, cancellationToken);

        return new CancelResponse(
            treatmentId,
            "Cancelled",
            cycleResult.RiskStatus,
            cycleResult.CycleCompleted,
            cycleResult.HasDraftsWarning);
    }

    private async Task EnsureOwnerAsync(int treatmentId, int userId, CancellationToken cancellationToken)
    {
        if (await _permissionService.IsAdminAsync(userId, cancellationToken))
            return;

        if (!await _permissionService.IsTreatmentOwnerAsync(userId, treatmentId, cancellationToken))
            throw new TreatmentPermissionException("Действие доступно владельцу процесса меры.");
    }

    private async Task EnsureApproverAsync(ApprovalRequest request, int userId, CancellationToken cancellationToken)
    {
        if (await _permissionService.IsAdminAsync(userId, cancellationToken))
            return;

        var processId = await _treatmentRepository.GetTreatmentProcessIdAsync(request.TreatmentId, cancellationToken)
            ?? throw new TreatmentValidationException("Мера не связана с процессом.");

        if (!await _permissionService.CanActAtLevelAsync(userId, request.RequiredLevel.ToString(), processId, cancellationToken))
            throw new TreatmentPermissionException("Запрос этого уровня согласует другой участник.");
    }

    private async Task EnsureExecutorAsync(int assigneeId, int treatmentId, CancellationToken cancellationToken)
    {
        if (!await _permissionService.IsValidExecutorAsync(assigneeId, treatmentId, cancellationToken))
            throw new TreatmentValidationException(
                "Исполнитель должен быть активным сотрудником дирекции процесса с ролью OWNER или EMPLOYEE.");
    }

    private async Task EnsureTreatModeAsync(int riskId, CancellationToken cancellationToken)
    {
        var mode = await _treatmentRepository.GetResponseModeAsync(riskId, cancellationToken);
        if (mode is not ("treat" or "split"))
            throw new TreatmentValidationException("Меры создаются после выбора режима «Обработать» или «Разделить».");
    }

    private async Task<bool> HasBlockingTreatmentsAsync(int riskId, CancellationToken cancellationToken)
    {
        foreach (var riskProcessId in await _treatmentRepository.GetActiveRiskProcessIdsAsync(riskId, cancellationToken))
        {
            if (await _treatmentRepository.HasActiveTreatmentAsync(riskProcessId, cancellationToken))
                return true;
        }

        return false;
    }
}

// DTOs
public sealed record CreateTreatmentRequest(
    int RiskProcessId,
    string TreatmentTitle,
    DateTimeOffset? DueDate,
    decimal TreatmentCostPlan,
    string? TreatmentDescription);

public sealed record UpdateTreatmentRequest(
    string? TreatmentTitle,
    string? TreatmentDescription,
    DateTimeOffset? DueDate,
    decimal? TreatmentCostPlan,
    decimal? TreatmentCostAct,
    int? RiskProcessId);

public sealed record ExecuteRequest(int AssigneeId);
public sealed record ReplaceAssigneeRequest(int AssigneeId);
public sealed record CompleteRequest(decimal TreatmentCostAct);

// Response DTOs
public sealed record CreateTreatmentResponse(int TreatmentId, string TreatmentStatus);
public sealed record UpdateTreatmentResponse(int TreatmentId);
public sealed record SubmitApprovalResponse(int ApprovalRequestId, string ApprovalRequiredLevel, string TreatmentStatus);
public sealed record ApproveResponse(int ApprovalRequestId, string ApprovalRequestStatus, string TreatmentStatus);
public sealed record RejectResponse(int ApprovalRequestId, string ApprovalRequestStatus, string TreatmentStatus);
public sealed record RevokeResponse(int ApprovalRequestId, string ApprovalRequestStatus, string TreatmentStatus);
public sealed record ExecuteResponse(int TreatmentId, string TreatmentStatus, int AssignmentId);
public sealed record ReplaceAssigneeResponse(int TreatmentId, int AssigneeId);
public sealed record RemoveAssigneeResponse(int TreatmentId);
public sealed record StartResponse(int TreatmentId, string TreatmentStatus);
public sealed record HoldResponse(int TreatmentId, string TreatmentStatus);
public sealed record ResumeResponse(int TreatmentId, string TreatmentStatus);
public sealed record CompleteResponse(int TreatmentId, string TreatmentStatus, string RiskStatus, bool CycleCompleted, bool HasDraftsWarning);
public sealed record CancelResponse(int TreatmentId, string TreatmentStatus, string RiskStatus, bool CycleCompleted, bool HasDraftsWarning);

// Exceptions
public sealed class TreatmentValidationException(string message) : Exception(message);
public sealed class TreatmentNotFoundException(int id) : Exception($"Мероприятие с ID {id} не найдено.");
public sealed class TreatmentPermissionException(string message) : Exception(message);
