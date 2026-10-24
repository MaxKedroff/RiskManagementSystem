using Application.Abstractions;
using Domain.Treatment;

namespace Application.Treatment;

public sealed class TreatmentService
{
    private readonly ITreatmentRepository _treatmentRepository;
    private readonly IApprovalRequestRepository _approvalRequestRepository;
    private readonly IUserPermissionService _permissionService;
    private readonly IProcessRepository _processRepository;
    private readonly ITreatmentApprovalSettingsRepository _approvalSettingsRepository;

    public TreatmentService(
        ITreatmentRepository treatmentRepository,
        IApprovalRequestRepository approvalRequestRepository,
        IUserPermissionService permissionService,
        IProcessRepository processRepository,
        ITreatmentApprovalSettingsRepository approvalSettingsRepository)
    {
        _treatmentRepository = treatmentRepository;
        _approvalRequestRepository = approvalRequestRepository;
        _permissionService = permissionService;
        _processRepository = processRepository;
        _approvalSettingsRepository = approvalSettingsRepository;
    }

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

        var treatment = new Domain.Treatment.Treatment(
            request.RiskProcessId,
            request.TreatmentTitle,
            request.TreatmentDescription,
            request.DueDate ?? DateTimeOffset.UtcNow.AddDays(30),
            request.TreatmentCostPlan,
            userId);

        return await _treatmentRepository.CreateAsync(treatment, cancellationToken);
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

        if (treatment.Status != TreatmentStatus.Planned)
            throw new TreatmentValidationException("Мероприятие должно быть в статусе 'Черновик'.");

        if (request.TreatmentTitle is not null)
            treatment.Rename(request.TreatmentTitle);
        if (request.TreatmentDescription is not null)
            treatment.ChangeDescription(request.TreatmentDescription);
        if (request.DueDate.HasValue)
            treatment.Reschedule(request.DueDate.Value);
        if (request.TreatmentCostPlan.HasValue)
            treatment.SetActualCost(request.TreatmentCostPlan.Value);
        if (request.TreatmentCostAct.HasValue)
            treatment.SetActualCost(request.TreatmentCostAct.Value);

        await _treatmentRepository.UpdateAsync(treatment, cancellationToken);
        return treatmentId;
    }

    // POST /v1/treatments/{id}/submit-approval
    public async Task<SubmitApprovalResponse> SubmitApprovalAsync(
        int treatmentId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "edit_components", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право edit_components.");

        var treatment = await _treatmentRepository.GetByIdAsync(treatmentId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatmentId);

        if (treatment.Status != TreatmentStatus.Planned)
            throw new TreatmentValidationException("Мероприятие должно быть в статусе 'Черновик'.");

        if (treatment.PlannedCost <= 0)
            throw new TreatmentValidationException("Необходимо указать плановую стоимость.");

        var riskId = await _treatmentRepository.GetProcessRiskIdAsync(treatment.RiskProcessId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatment.RiskProcessId);

        var riskStatus = await _treatmentRepository.GetRiskStatusAsync(riskId, cancellationToken)
            ?? throw new TreatmentNotFoundException(riskId);

        if (riskStatus != "Open")
            throw new TreatmentValidationException("Риск должен быть в статусе 'Активен'.");

        if (await _treatmentRepository.GetActiveAssessmentIdAsync(riskId, cancellationToken) is null)
            throw new TreatmentValidationException("Для риска не найдена активная оценка.");

        if (await _approvalRequestRepository.HasOpenApprovalAsync(treatmentId, cancellationToken))
            throw new TreatmentValidationException("Уже существует открытый запрос на согласование.");

        if (await _treatmentRepository.HasConflictingTreatmentAsync(riskId, treatmentId, cancellationToken))
            throw new TreatmentValidationException("Существует конфликтующее мероприятие.");

        var settings = await _approvalSettingsRepository.GetAsync(cancellationToken);
        ApprovalLevel approvalLevel;
        if (treatment.PlannedCost <= settings.OwnerThreshold)
            approvalLevel = ApprovalLevel.Owner;
        else if (treatment.PlannedCost <= settings.DirectorThreshold)
            approvalLevel = ApprovalLevel.Director;
        else
            approvalLevel = ApprovalLevel.Ceo;

        var approvalRequest = new ApprovalRequest(treatmentId, approvalLevel, userId);
        var approvalRequestId = await _approvalRequestRepository.CreateAsync(approvalRequest, cancellationToken);

        treatment.SubmitForApproval();
        await _treatmentRepository.UpdateStatusAsync(treatmentId, "PendingApproval", cancellationToken);

        return new SubmitApprovalResponse(approvalRequestId, approvalLevel.ToString(), "PendingApproval");
    }

    // POST /v1/approval-requests/{id}/approve
    public async Task<ApproveResponse> ApproveAsync(
        int approvalRequestId,
        int userId,
        string approvalComment,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "approve_treatment", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право approve_treatment.");

        var approvalRequest = await _approvalRequestRepository.GetByIdAsync(approvalRequestId, cancellationToken)
            ?? throw new TreatmentNotFoundException(approvalRequestId);

        if (approvalRequest.Status != ApprovalStatus.Pending)
            throw new TreatmentValidationException("Запрос уже обработан.");

        approvalRequest.Approve(userId, approvalComment);
        await _approvalRequestRepository.UpdateAsync(approvalRequest, cancellationToken);

        var treatment = await _treatmentRepository.GetByIdAsync(approvalRequest.TreatmentId, cancellationToken);
        treatment.Approve();
        await _treatmentRepository.UpdateStatusAsync(treatment.Id, "Approved", cancellationToken);

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

        approvalRequest.Reject(userId, approvalReason);
        await _approvalRequestRepository.UpdateAsync(approvalRequest, cancellationToken);

        var treatment = await _treatmentRepository.GetByIdAsync(approvalRequest.TreatmentId, cancellationToken);
        treatment.Reject();
        await _treatmentRepository.UpdateStatusAsync(treatment.Id, "Planned", cancellationToken);

        return new RejectResponse(approvalRequestId, "Rejected", "Planned");
    }

    // POST /v1/approval-requests/{id}/revoke
    public async Task<RevokeResponse> RevokeAsync(
        int approvalRequestId,
        int userId,
        string revokeReason,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "admin", cancellationToken))
            throw new TreatmentPermissionException("Отсутствует право admin.");

        var approvalRequest = await _approvalRequestRepository.GetByIdAsync(approvalRequestId, cancellationToken)
            ?? throw new TreatmentNotFoundException(approvalRequestId);

        if (approvalRequest.Status is not (ApprovalStatus.Pending or ApprovalStatus.Approved))
            throw new TreatmentValidationException("Можно отозвать только ожидающий или согласованный запрос.");

        if (string.IsNullOrWhiteSpace(revokeReason))
            throw new TreatmentValidationException("Причина отзыва обязательна.");

        approvalRequest.Revoke(userId, revokeReason);
        await _approvalRequestRepository.UpdateAsync(approvalRequest, cancellationToken);

        var treatment = await _treatmentRepository.GetByIdAsync(approvalRequest.TreatmentId, cancellationToken);
        treatment.Revoke();
        await _treatmentRepository.UpdateStatusAsync(treatment.Id, treatment.Status.ToString(), cancellationToken);

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

        if (treatment.Status is not (TreatmentStatus.Planned or TreatmentStatus.Approved))
            throw new TreatmentValidationException("Мероприятие должно быть в статусе 'Черновик' или 'Согласовано'.");

        if (request.AssigneeId <= 0)
            throw new TreatmentValidationException("Исполнитель не выбран.");

        treatment.AssignTo(request.AssigneeId);
        await _treatmentRepository.UpdateStatusAsync(treatmentId, "AwaitingExecution", cancellationToken);

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

        if (treatment.Status is not (TreatmentStatus.AwaitingExecution or TreatmentStatus.InProgress or TreatmentStatus.Hold))
            throw new TreatmentValidationException("Недопустимый статус для замены исполнителя.");

        if (treatment.AssignedToId is null)
            throw new TreatmentValidationException("Назначение отсутствует.");

        if (request.AssigneeId <= 0)
            throw new TreatmentValidationException("Новый исполнитель недопустим.");

        await _treatmentRepository.UpdateAssigneeAsync(treatmentId, request.AssigneeId, userId, cancellationToken);

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

        if (treatment.Status != TreatmentStatus.Hold)
            throw new TreatmentValidationException("Мероприятие должно быть в статусе 'Холд'.");

        if (treatment.AssignedToId is null)
            throw new TreatmentValidationException("Назначение отсутствует.");

        await _treatmentRepository.UpdateAssigneeAsync(treatmentId, null, userId, cancellationToken);

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

        if (treatment.Status is not (TreatmentStatus.AwaitingExecution or TreatmentStatus.Hold))
            throw new TreatmentValidationException("Мероприятие должно быть в статусе 'Ожидает выполнения' или 'Холд'.");

        if (treatment.AssignedToId is null)
            throw new TreatmentValidationException("Необходимо назначить исполнителя.");

        var riskId = await _treatmentRepository.GetProcessRiskIdAsync(treatment.RiskProcessId, cancellationToken) ?? 0;
        if (await _treatmentRepository.HasConflictingTreatmentAsync(riskId, treatmentId, cancellationToken))
            throw new TreatmentValidationException("Существует конфликтующее мероприятие.");

        treatment.StartExecution();
        await _treatmentRepository.UpdateStatusAsync(treatmentId, "InProgress", cancellationToken);

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

        if (treatment.Status != TreatmentStatus.InProgress)
            throw new TreatmentValidationException("Мероприятие должно быть в статусе 'В работе'.");

        treatment.Hold();
        await _treatmentRepository.UpdateStatusAsync(treatmentId, "Hold", cancellationToken);

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

        if (await _treatmentRepository.HasConflictingTreatmentAsync(riskId, treatmentId, cancellationToken))
            throw new TreatmentValidationException("Существует конфликтующее мероприятие.");

        treatment.Resume();
        await _treatmentRepository.UpdateStatusAsync(treatmentId, "InProgress", cancellationToken);

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

        var riskStatus = hasOtherActiveTreatments
            ? await _treatmentRepository.GetRiskStatusAsync(riskId, cancellationToken) ?? "Unknown"
            : "AwaitingReassessment";

        return new CompleteResponse(treatmentId, "Completed", riskStatus);
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

        if (treatment.Status is not (TreatmentStatus.Planned or TreatmentStatus.AwaitingExecution or TreatmentStatus.InProgress))
            throw new TreatmentValidationException("Недопустимый статус для отмены.");

        var riskId = await _treatmentRepository.GetProcessRiskIdAsync(treatment.RiskProcessId, cancellationToken)
            ?? throw new TreatmentNotFoundException(treatment.RiskProcessId);

        treatment.CancelExecution();
        await _treatmentRepository.UpdateStatusAsync(treatmentId, "Cancelled", cancellationToken);

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

        var riskStatus = hasOtherActiveTreatments
            ? await _treatmentRepository.GetRiskStatusAsync(riskId, cancellationToken) ?? "Unknown"
            : "AwaitingReassessment";

        return new CancelResponse(treatmentId, "Cancelled", riskStatus);
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
public sealed record CompleteResponse(int TreatmentId, string TreatmentStatus, string RiskStatus);
public sealed record CancelResponse(int TreatmentId, string TreatmentStatus, string RiskStatus);

// Exceptions
public sealed class TreatmentValidationException(string message) : Exception(message);
public sealed class TreatmentNotFoundException(int id) : Exception($"Мероприятие с ID {id} не найдено.");
public sealed class TreatmentPermissionException(string message) : Exception(message);
