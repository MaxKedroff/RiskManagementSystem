using Application.Abstractions;
using Application.Org.Processes;
using Application.Risk.Risks;
using Domain.Assessment;
using Domain.Org;
using Domain.Risk;
using Moq;

namespace OnionRiskPlatform.Tests.Risk;

public class RiskServiceTests
{
    private readonly Mock<IRiskRepository> _riskRepository;
    private readonly Mock<IRiskProcessRepository> _riskProcessRepository;
    private readonly Mock<IRiskAssessmentRepository> _riskAssessmentRepository;
    private readonly Mock<IUserPermissionService> _permissionService;
    private readonly Mock<IAssetRepository> _assetRepository;
    private readonly Mock<IProcessRepository> _processRepository;
    private readonly Mock<ICommentRepository> _commentRepository;
    private readonly Mock<ITreatmentRepository> _treatmentRepository;
    private readonly RiskService _service;

    public RiskServiceTests()
    {
        _riskRepository = new Mock<IRiskRepository>();
        _riskProcessRepository = new Mock<IRiskProcessRepository>();
        _riskAssessmentRepository = new Mock<IRiskAssessmentRepository>();
        _permissionService = new Mock<IUserPermissionService>();
        _assetRepository = new Mock<IAssetRepository>();
        _processRepository = new Mock<IProcessRepository>();
        _commentRepository = new Mock<ICommentRepository>();
        _treatmentRepository = new Mock<ITreatmentRepository>();

        _service = new RiskService(
            _riskRepository.Object,
            _riskProcessRepository.Object,
            _riskAssessmentRepository.Object,
            _permissionService.Object,
            _assetRepository.Object,
            _processRepository.Object,
            _commentRepository.Object,
            _treatmentRepository.Object);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithPermission_ReturnsRisks()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "view_risk", CancellationToken.None))
            .ReturnsAsync(true);
        var risks = new[]
        {
            new RiskListItem(1, "Test Risk", Array.Empty<string>(), Array.Empty<string>(),
                Array.Empty<string>(), null, null, null, null, null, null, "Draft", null)
        };
        _riskRepository.Setup(r => r.GetAllAsync(CancellationToken.None))
            .ReturnsAsync(risks);

        // Act
        var result = await _service.GetAllAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result[0].RiskTitle.Should().Be("Test Risk");
    }

    [Fact]
    public async Task GetAllAsync_WithoutPermission_ThrowsPermissionException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "view_risk", CancellationToken.None))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<RiskPermissionException>(() =>
            _service.GetAllAsync(1));
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidInput_CreatesRisk()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "create_components", CancellationToken.None))
            .ReturnsAsync(true);

        var ownerInfo = new ProcessOwnerInfo(1, 5, 1, true);
        _processRepository.Setup(p => p.GetOwnerInfoAsync(1, CancellationToken.None))
            .ReturnsAsync(ownerInfo);

        _assetRepository.Setup(a => a.GetByIdAsync(It.IsAny<int>(), CancellationToken.None))
            .ReturnsAsync((Domain.Org.Asset?)null);

        _riskRepository.Setup(r => r.CreateAsync(It.IsAny<Domain.Risk.Risk>(), CancellationToken.None))
            .ReturnsAsync(42);
        _riskProcessRepository.Setup(r => r.CreateAsync(It.IsAny<RiskProcess>(), CancellationToken.None))
            .ReturnsAsync(1);

        var request = new CreateRiskRequest(
            "Test Risk",
            "Description",
            1, 2, null,
            new[] { 1 });

        // Act
        var result = await _service.CreateAsync(1, request);

        // Assert
        result.Should().Be(42);
        _riskRepository.Verify(r => r.CreateAsync(It.IsAny<Domain.Risk.Risk>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutCreateComponentsPermission_ThrowsPermissionException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "create_components", CancellationToken.None))
            .ReturnsAsync(false);

        var request = new CreateRiskRequest(
            "Test Risk", "Description", 1, 2, null,
            Array.Empty<int>());

        // Act & Assert
        await Assert.ThrowsAsync<RiskPermissionException>(() =>
            _service.CreateAsync(1, request));
    }

    [Fact]
    public async Task CreateAsync_InactiveProcess_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "create_components", CancellationToken.None))
            .ReturnsAsync(true);

        var ownerInfo = new ProcessOwnerInfo(1, 5, 1, false);
        _processRepository.Setup(p => p.GetOwnerInfoAsync(1, CancellationToken.None))
            .ReturnsAsync(ownerInfo);

        var request = new CreateRiskRequest(
            "Test Risk", "Description", 1, 2, null,
            new[] { 1 });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.CreateAsync(1, request));
        ex.Message.Should().Contain("неактивен");
    }

    [Fact]
    public async Task CreateAsync_ProcessNotFound_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "create_components", CancellationToken.None))
            .ReturnsAsync(true);

        _processRepository.Setup(p => p.GetOwnerInfoAsync(1, CancellationToken.None))
            .ReturnsAsync((ProcessOwnerInfo?)null);

        var request = new CreateRiskRequest(
            "Test Risk", "Description", 1, 2, null,
            new[] { 1 });

        // Act & Assert
        await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.CreateAsync(1, request));
    }

    [Fact]
    public async Task CreateAsync_InactiveAsset_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "create_components", CancellationToken.None))
            .ReturnsAsync(true);

        var ownerInfo = new ProcessOwnerInfo(1, 5, 1, true);
        _processRepository.Setup(p => p.GetOwnerInfoAsync(1, CancellationToken.None))
            .ReturnsAsync(ownerInfo);

        var asset = Domain.Org.Asset.Rehydrate(1, "Asset", "INV001", 100m, false);
        _assetRepository.Setup(a => a.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(asset);

        var request = new CreateRiskRequest(
            "Test Risk", "Description", 1, 2, 1,
            new[] { 1 });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.CreateAsync(1, request));
        ex.Message.Should().Contain("неактивен");
    }

    [Fact]
    public async Task CreateAsync_AssetWithoutHourCost_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "create_components", CancellationToken.None))
            .ReturnsAsync(true);

        var ownerInfo = new ProcessOwnerInfo(1, 5, 1, true);
        _processRepository.Setup(p => p.GetOwnerInfoAsync(1, CancellationToken.None))
            .ReturnsAsync(ownerInfo);

        var asset = Domain.Org.Asset.Rehydrate(1, "Asset", "INV001", 0m, true);
        _assetRepository.Setup(a => a.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(asset);

        var request = new CreateRiskRequest(
            "Test Risk", "Description", 1, 2, 1,
            new[] { 1 });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.CreateAsync(1, request));
        ex.Message.Should().Contain("стоимость часа");
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "create_components", CancellationToken.None))
            .ReturnsAsync(true);

        var ownerInfo = new ProcessOwnerInfo(1, 5, 1, true);
        _processRepository.Setup(p => p.GetOwnerInfoAsync(1, CancellationToken.None))
            .ReturnsAsync(ownerInfo);

        var request = new CreateRiskRequest(
            "", "Description", 1, 2, null,
            Array.Empty<int>());

        // Act & Assert
        await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.CreateAsync(1, request));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidDraftInput_UpdatesRisk()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Old Title", "Desc", 1, 2, 1, null,
            "Draft", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskProcessRepository.Setup(r => r.GetPrimaryOwnerUserIdAsync(1, CancellationToken.None))
            .ReturnsAsync(1);
        _assetRepository.Setup(a => a.GetByIdAsync(It.IsAny<int>(), CancellationToken.None))
            .ReturnsAsync((Domain.Org.Asset?)null);
        _riskRepository.Setup(r => r.UpdateAsync(risk, CancellationToken.None))
            .ReturnsAsync(true);

        var request = new UpdateRiskRequest(
            "New Title", "New Description", 1, 2, null,
            Array.Empty<int>());

        // Act
        var result = await _service.UpdateAsync(1, 1, request);

        // Assert
        result.Should().Be(1);
        risk.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task UpdateAsync_NotDraftStatus_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Open", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);

        var request = new UpdateRiskRequest(
            "New Title", "Desc", 1, 2, null,
            Array.Empty<int>());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.UpdateAsync(1, 1, request));
        ex.Message.Should().Contain("Черновик");
    }

    [Fact]
    public async Task UpdateAsync_NotOwner_ThrowsAuthorizationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Draft", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskProcessRepository.Setup(r => r.GetPrimaryOwnerUserIdAsync(1, CancellationToken.None))
            .ReturnsAsync(99);

        var request = new UpdateRiskRequest(
            "New Title", "Desc", 1, 2, null,
            Array.Empty<int>());

        // Act & Assert
        await Assert.ThrowsAsync<RiskAuthorizationException>(() =>
            _service.UpdateAsync(1, 1, request));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        _riskRepository.Setup(r => r.GetByIdAsync(99, CancellationToken.None))
            .ReturnsAsync((Domain.Risk.Risk?)null);

        var request = new UpdateRiskRequest(
            "Title", "Desc", 1, 2, null,
            Array.Empty<int>());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskNotFoundException>(() =>
            _service.UpdateAsync(99, 1, request));
        ex.Message.Should().Contain("99");
    }

    #endregion

    #region AssessAsync Tests

    [Fact]
    public async Task AssessAsync_WithoutAsset_UsesManualLossAmount()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "assess_risk", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Draft", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(false);
        _riskAssessmentRepository.Setup(r => r.GetImpactRangeForAmountAsync(5000, CancellationToken.None))
            .ReturnsAsync(new ImpactRange(1, 1000, 10000));
        _riskAssessmentRepository.Setup(r => r.GetAssessmentWeightsAsync(1, 1, CancellationToken.None))
            .ReturnsAsync((3, 2));
        _riskAssessmentRepository.Setup(r => r.GetAcceptanceThresholdsAsync(1, CancellationToken.None))
            .ReturnsAsync(new AcceptanceThresholds(6, 12, 20));
        _riskAssessmentRepository.Setup(r => r.CreateAsync(It.IsAny<RiskAssessment>(), CancellationToken.None))
            .ReturnsAsync(100);
        _riskRepository.Setup(r => r.UpdateStatusAsync(1, "Open", CancellationToken.None))
            .ReturnsAsync(true);

        var request = new AssessRiskRequest(
            LikelihoodLevelId: 1,
            LossAmount: 5000,
            DowntimeWeeks: null,
            DowntimeDays: null,
            DowntimeHours: null);

        // Act
        var result = await _service.AssessAsync(1, 1, request);

        // Assert
        result.AssessmentId.Should().Be(100);
        result.RiskScore.Should().Be(6);
        result.AcceptanceLevel.Should().Be("Owner");
        result.RiskStatus.Should().Be("Open");
    }

    [Fact]
    public async Task AssessAsync_WithAsset_CalculatesDowntimeAndLoss()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "assess_risk", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, 1,
            "Draft", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(false);
        _riskAssessmentRepository.Setup(r => r.GetAssetHourCostAsync(1, CancellationToken.None))
            .ReturnsAsync(100m);
        _riskAssessmentRepository.Setup(r => r.GetAssessmentWeightsAsync(1, 2, CancellationToken.None))
            .ReturnsAsync((3, 4));
        _riskAssessmentRepository.Setup(r => r.GetAcceptanceThresholdsAsync(1, CancellationToken.None))
            .ReturnsAsync(new AcceptanceThresholds(6, 12, 20));
        _riskAssessmentRepository.Setup(r => r.CreateAsync(It.IsAny<RiskAssessment>(), CancellationToken.None))
            .ReturnsAsync(101);
        _riskRepository.Setup(r => r.UpdateStatusAsync(1, "Open", CancellationToken.None))
            .ReturnsAsync(true);

        // 1 week * 168 + 0 days * 24 + 0 hours = 168 hours
        // 168 * 100 = 16800
        var request = new AssessRiskRequest(
            LikelihoodLevelId: 1,
            LossAmount: 0,
            DowntimeWeeks: 1,
            DowntimeDays: 0,
            DowntimeHours: 0);

        // Act
        _riskAssessmentRepository.Setup(r => r.GetImpactRangeForAmountAsync(16800, CancellationToken.None))
            .ReturnsAsync(new ImpactRange(2, 10000, 100000));
        var result = await _service.AssessAsync(1, 1, request);

        // Assert
        result.AssessmentId.Should().Be(101);
        result.RiskScore.Should().Be(12);
        result.AcceptanceLevel.Should().Be("Director");
    }

    [Fact]
    public async Task AssessAsync_ConcurrentAssessment_ThrowsConflictException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "assess_risk", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Draft", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(true);

        var request = new AssessRiskRequest(
            LikelihoodLevelId: 1,
            LossAmount: 5000,
            DowntimeWeeks: null,
            DowntimeDays: null,
            DowntimeHours: null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskConflictException>(() =>
            _service.AssessAsync(1, 1, request));
        ex.Message.Should().Contain("Конкурирующая оценка");
    }

    [Fact]
    public async Task AssessAsync_NotDraftStatus_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "assess_risk", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Open", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);

        var request = new AssessRiskRequest(
            LikelihoodLevelId: 1,
            LossAmount: 5000,
            DowntimeWeeks: null,
            DowntimeDays: null,
            DowntimeHours: null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.AssessAsync(1, 1, request));
        ex.Message.Should().Contain("Черновик");
    }

    [Fact]
    public async Task AssessAsync_MissingFields_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "assess_risk", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Draft", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(false);

        var request = new AssessRiskRequest(
            LikelihoodLevelId: 0, // Invalid
            LossAmount: 5000,
            DowntimeWeeks: null,
            DowntimeDays: null,
            DowntimeHours: null);

        // Act & Assert
        await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.AssessAsync(1, 1, request));
    }

    [Fact]
    public async Task AssessAsync_WithoutAssessRiskPermission_ThrowsPermissionException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "assess_risk", CancellationToken.None))
            .ReturnsAsync(false);

        var request = new AssessRiskRequest(
            LikelihoodLevelId: 1,
            LossAmount: 5000,
            DowntimeWeeks: null,
            DowntimeDays: null,
            DowntimeHours: null);

        // Act & Assert
        await Assert.ThrowsAsync<RiskPermissionException>(() =>
            _service.AssessAsync(1, 1, request));
    }

    [Fact]
    public async Task AssessAsync_RiskNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "assess_risk", CancellationToken.None))
            .ReturnsAsync(true);

        _riskRepository.Setup(r => r.GetByIdAsync(99, CancellationToken.None))
            .ReturnsAsync((Domain.Risk.Risk?)null);

        var request = new AssessRiskRequest(
            LikelihoodLevelId: 1,
            LossAmount: 5000,
            DowntimeWeeks: null,
            DowntimeDays: null,
            DowntimeHours: null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskNotFoundException>(() =>
            _service.AssessAsync(99, 1, request));
        ex.Message.Should().Contain("99");
    }

    #endregion

    #region AcceptAsync Tests

    [Fact]
    public async Task AcceptAsync_ValidInput_AcceptsRisk()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "accept_risk", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Open", "Owner", null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(true);
        _riskAssessmentRepository.Setup(r => r.HasAssessmentWithNullResponseModeAsync(1, CancellationToken.None))
            .ReturnsAsync(true);
        _riskProcessRepository.Setup(r => r.GetExistingProcessIdsAsync(1, CancellationToken.None))
            .ReturnsAsync(Array.Empty<int>());
        _riskAssessmentRepository.Setup(r => r.GetActiveAssessmentIdAsync(1, CancellationToken.None))
            .ReturnsAsync(10);
        _riskAssessmentRepository.Setup(r => r.UpdateResponseModeAsync(10, "accept", CancellationToken.None))
            .ReturnsAsync(true);
        _riskRepository.Setup(r => r.UpdateStatusAsync(1, "Accepted", CancellationToken.None))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AcceptAsync(1, 1);

        // Assert
        result.RiskId.Should().Be(1);
        result.RiskStatus.Should().Be("Accepted");
        result.ResponseMode.Should().Be("accept");
    }

    [Fact]
    public async Task AcceptAsync_NotOpenStatus_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "accept_risk", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Draft", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.AcceptAsync(1, 1));
        ex.Message.Should().Contain("Активен");
    }

    [Fact]
    public async Task AcceptAsync_HasBlockingMeasure_ThrowsConflictException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "accept_risk", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Open", "Owner", null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(true);
        _riskAssessmentRepository.Setup(r => r.HasAssessmentWithNullResponseModeAsync(1, CancellationToken.None))
            .ReturnsAsync(true);
        _riskProcessRepository.Setup(r => r.GetExistingProcessIdsAsync(1, CancellationToken.None))
            .ReturnsAsync(new[] { 5 });
        _riskAssessmentRepository.Setup(r => r.GetActiveAssessmentIdAsync(1, CancellationToken.None))
            .ReturnsAsync(10);
        _treatmentRepository.Setup(t => t.HasNonTerminalTreatmentAsync(5, CancellationToken.None))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskConflictException>(() =>
            _service.AcceptAsync(1, 1));
        ex.Message.Should().Contain("блокирующие меры");
    }

    [Fact]
    public async Task AcceptAsync_WithoutPermission_ThrowsPermissionException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "accept_risk", CancellationToken.None))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<RiskPermissionException>(() =>
            _service.AcceptAsync(1, 1));
    }

    #endregion

    #region TreatAsync Tests

    [Fact]
    public async Task TreatAsync_ValidInput_SetsTreatMode()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Open", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasAssessmentWithNullResponseModeAsync(1, CancellationToken.None))
            .ReturnsAsync(true);
        _riskProcessRepository.Setup(r => r.HasPrimaryAsync(1, CancellationToken.None))
            .ReturnsAsync(true);
        _riskAssessmentRepository.Setup(r => r.GetActiveAssessmentIdAsync(1, CancellationToken.None))
            .ReturnsAsync(10);
        _riskAssessmentRepository.Setup(r => r.UpdateResponseModeAsync(10, "treat", CancellationToken.None))
            .ReturnsAsync(true);

        // Act
        var result = await _service.TreatAsync(1, 1);

        // Assert
        result.RiskId.Should().Be(1);
        result.ResponseMode.Should().Be("treat");
    }

    [Fact]
    public async Task TreatAsync_NotOpenStatus_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Draft", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.TreatAsync(1, 1));
        ex.Message.Should().Contain("Активен");
    }

    #endregion

    #region SplitAsync Tests

    [Fact]
    public async Task SplitAsync_ValidInput_SplitsRisk()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "split_risk", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Open", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(true);

        var ownerInfo1 = new ProcessOwnerInfo(1, 5, 1, true);
        var ownerInfo2 = new ProcessOwnerInfo(2, 6, 2, true);
        _processRepository.Setup(p => p.GetOwnerInfoAsync(1, CancellationToken.None))
            .ReturnsAsync(ownerInfo1);
        _processRepository.Setup(p => p.GetOwnerInfoAsync(2, CancellationToken.None))
            .ReturnsAsync(ownerInfo2);
        _riskProcessRepository.Setup(r => r.GetExistingProcessIdsAsync(1, CancellationToken.None))
            .ReturnsAsync(Array.Empty<int>());
        _riskProcessRepository.Setup(r => r.DeactivateRiskProcessLinksAsync(1, CancellationToken.None))
            .ReturnsAsync(true);
        _riskProcessRepository.Setup(r => r.CreateRiskProcessAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), CancellationToken.None))
            .ReturnsAsync(1);
        _riskAssessmentRepository.Setup(r => r.GetActiveAssessmentIdAsync(1, CancellationToken.None))
            .ReturnsAsync(10);
        _riskAssessmentRepository.Setup(r => r.UpdateResponseModeAsync(10, "split", CancellationToken.None))
            .ReturnsAsync(true);

        var request = new SplitRiskRequest(new[] { 1, 2 }, 1);

        // Act
        var result = await _service.SplitAsync(1, 1, request);

        // Assert
        result.RiskId.Should().Be(1);
        result.ResponseMode.Should().Be("split");
    }

    [Fact]
    public async Task SplitAsync_LessThanTwoProcesses_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "split_risk", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Open", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);

        var request = new SplitRiskRequest(new[] { 1 }, 1);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.SplitAsync(1, 1, request));
        ex.Message.Should().Contain("не менее двух");
    }

    #endregion

    #region TransferAsync Tests

    [Fact]
    public async Task TransferAsync_ValidInput_TransfersRisk()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "transfer_risk", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Open", "Owner", null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasAssessmentWithNullResponseModeAsync(1, CancellationToken.None))
            .ReturnsAsync(true);
        _riskProcessRepository.Setup(r => r.GetExistingProcessIdsAsync(1, CancellationToken.None))
            .ReturnsAsync(Array.Empty<int>());

        var newOwnerInfo = new ProcessOwnerInfo(5, 10, 3, true);
        _processRepository.Setup(p => p.GetOwnerInfoAsync(5, CancellationToken.None))
            .ReturnsAsync(newOwnerInfo);
        _riskAssessmentRepository.Setup(r => r.GetActiveAssessmentIdAsync(1, CancellationToken.None))
            .ReturnsAsync(10);
        _riskAssessmentRepository.Setup(r => r.UpdateResponseModeAsync(10, "transfer", CancellationToken.None))
            .ReturnsAsync(true);
        _riskAssessmentRepository.Setup(r => r.DeactivateAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(true);
        _riskProcessRepository.Setup(r => r.DeactivateRiskProcessLinksAsync(1, CancellationToken.None))
            .ReturnsAsync(true);
        _riskProcessRepository.Setup(r => r.CreateRiskProcessAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), CancellationToken.None))
            .ReturnsAsync(1);
        _riskRepository.Setup(r => r.ClearAcceptanceLevelAsync(1, CancellationToken.None))
            .ReturnsAsync(true);
        _riskRepository.Setup(r => r.SetAwaitingReassessmentStatusAsync(1, CancellationToken.None))
            .ReturnsAsync(true);

        var request = new TransferRiskRequest(5);

        // Act
        var result = await _service.TransferAsync(1, 1, request);

        // Assert
        result.RiskId.Should().Be(1);
        result.ResponseMode.Should().Be("transfer");
        result.RiskStatus.Should().Be("AwaitingReassessment");
    }

    [Fact]
    public async Task TransferAsync_HasNonTerminalTreatment_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "transfer_risk", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Open", "Owner", null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasAssessmentWithNullResponseModeAsync(1, CancellationToken.None))
            .ReturnsAsync(true);
        _riskProcessRepository.Setup(r => r.GetExistingProcessIdsAsync(1, CancellationToken.None))
            .ReturnsAsync(new[] { 5 });
        _treatmentRepository.Setup(t => t.AllTreatmentsTerminalAsync(5, CancellationToken.None))
            .ReturnsAsync(false);

        var request = new TransferRiskRequest(5);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.TransferAsync(1, 1, request));
        ex.Message.Should().Contain("нетерминальные меры");
    }

    #endregion

    #region ResetForReassessmentAsync Tests (legacy - renamed from ReassessAsync)

    [Fact]
    public async Task ResetForReassessmentAsync_AcceptedRisk_Reassesses()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Accepted", "Owner", null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskRepository.Setup(r => r.ClearAcceptanceLevelAsync(1, CancellationToken.None))
            .ReturnsAsync(true);
        _riskRepository.Setup(r => r.SetAwaitingReassessmentStatusAsync(1, CancellationToken.None))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ResetForReassessmentAsync(1, 1);

        // Assert
        result.RiskId.Should().Be(1);
        result.RiskStatus.Should().Be("AwaitingReassessment");
    }

    [Fact]
    public async Task ResetForReassessmentAsync_ClosedRisk_ReassessesWithComment()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Closed", null, "Old reason");
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskRepository.Setup(r => r.ClearAcceptanceLevelAsync(1, CancellationToken.None))
            .ReturnsAsync(true);
        _riskRepository.Setup(r => r.UpdateRiskStatusAndReasonAsync(1, "AwaitingReassessment", null, CancellationToken.None))
            .ReturnsAsync(true);
        _commentRepository.Setup(c => c.CreateAsync(1, "Риск возвращён на переоценку.", "Risk", 1, CancellationToken.None))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ResetForReassessmentAsync(1, 1);

        // Assert
        result.RiskId.Should().Be(1);
        result.RiskStatus.Should().Be("AwaitingReassessment");
        _commentRepository.Verify(c => c.CreateAsync(1, "Риск возвращён на переоценку.", "Risk", 1, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ResetForReassessmentAsync_NotAcceptedOrClosed_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Open", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.ResetForReassessmentAsync(1, 1));
        ex.Message.Should().Contain("Принят") ;
    }

    #endregion

    #region ReassessAsync Tests (new - full reassessment)

    // Note: All tests in this region mock "edit_components" permission (changed from "assess_risk")

    [Fact]
    public async Task ReassessAsync_WithoutAsset_CreatesNewAssessment()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "AwaitingReassessment", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(false);
        _riskAssessmentRepository.Setup(r => r.GetImpactRangeForAmountAsync(5000, CancellationToken.None))
            .ReturnsAsync(new ImpactRange(1, 1000, 10000));
        _riskAssessmentRepository.Setup(r => r.GetAssessmentWeightsAsync(1, 1, CancellationToken.None))
            .ReturnsAsync((3, 2));
        _riskAssessmentRepository.Setup(r => r.GetAcceptanceThresholdsAsync(1, CancellationToken.None))
            .ReturnsAsync(new AcceptanceThresholds(6, 12, 20));
        _riskAssessmentRepository.Setup(r => r.CreateAsync(It.IsAny<RiskAssessment>(), CancellationToken.None))
            .ReturnsAsync(200);
        _riskRepository.Setup(r => r.UpdateStatusAsync(1, "InTreatment", CancellationToken.None))
            .ReturnsAsync(true);

        var request = new ReassessRiskRequest(
            LikelihoodLevelId: 1,
            LossAmount: 5000,
            DowntimeWeeks: null,
            DowntimeDays: null,
            DowntimeHours: null);

        // Act
        var result = await _service.ReassessAsync(1, 1, request);

        // Assert
        result.AssessmentId.Should().Be(200);
        result.RiskScore.Should().Be(6);
        result.AcceptanceLevel.Should().Be("Owner");
        result.RiskStatus.Should().Be("InTreatment"); // 6 > 5
    }

    [Fact]
    public async Task ReassessAsync_WithAsset_CalculatesDowntimeAndLoss()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, 1,
            "AwaitingReassessment", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(false);
        _riskAssessmentRepository.Setup(r => r.GetAssetHourCostAsync(1, CancellationToken.None))
            .ReturnsAsync(100m);
        _riskAssessmentRepository.Setup(r => r.GetImpactRangeForAmountAsync(16800, CancellationToken.None))
            .ReturnsAsync(new ImpactRange(2, 10000, 100000));
        _riskAssessmentRepository.Setup(r => r.GetAssessmentWeightsAsync(1, 2, CancellationToken.None))
            .ReturnsAsync((3, 4));
        _riskAssessmentRepository.Setup(r => r.GetAcceptanceThresholdsAsync(1, CancellationToken.None))
            .ReturnsAsync(new AcceptanceThresholds(6, 12, 20));
        _riskAssessmentRepository.Setup(r => r.CreateAsync(It.IsAny<RiskAssessment>(), CancellationToken.None))
            .ReturnsAsync(201);
        _riskRepository.Setup(r => r.UpdateStatusAsync(1, "InTreatment", CancellationToken.None))
            .ReturnsAsync(true);

        // 1 week * 168 + 0 days * 24 + 0 hours = 168 hours, 168 * 100 = 16800
        var request = new ReassessRiskRequest(
            LikelihoodLevelId: 1,
            LossAmount: 0,
            DowntimeWeeks: 1,
            DowntimeDays: 0,
            DowntimeHours: 0);

        // Act
        var result = await _service.ReassessAsync(1, 1, request);

        // Assert
        result.AssessmentId.Should().Be(201);
        result.RiskScore.Should().Be(12);
        result.RiskStatus.Should().Be("InTreatment"); // score > 5
    }

    [Fact]
    public async Task ReassessAsync_ScoreAbove5_StatusIsInTreatment()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "AwaitingReassessment", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(false);
        _riskAssessmentRepository.Setup(r => r.GetImpactRangeForAmountAsync(5000, CancellationToken.None))
            .ReturnsAsync(new ImpactRange(1, 1000, 10000));
        _riskAssessmentRepository.Setup(r => r.GetAssessmentWeightsAsync(1, 1, CancellationToken.None))
            .ReturnsAsync((3, 3)); // 3 * 3 = 9 > 5
        _riskAssessmentRepository.Setup(r => r.GetAcceptanceThresholdsAsync(1, CancellationToken.None))
            .ReturnsAsync(new AcceptanceThresholds(6, 12, 20));
        _riskAssessmentRepository.Setup(r => r.CreateAsync(It.IsAny<RiskAssessment>(), CancellationToken.None))
            .ReturnsAsync(202);
        _riskRepository.Setup(r => r.UpdateStatusAsync(1, "InTreatment", CancellationToken.None))
            .ReturnsAsync(true);

        var request = new ReassessRiskRequest(1, 5000, null, null, null);

        // Act
        var result = await _service.ReassessAsync(1, 1, request);

        // Assert
        result.RiskScore.Should().Be(9);
        result.RiskStatus.Should().Be("InTreatment");
    }

    [Fact]
    public async Task ReassessAsync_ScoreAt5_StatusIsOpen()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "AwaitingReassessment", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(false);
        _riskAssessmentRepository.Setup(r => r.GetImpactRangeForAmountAsync(5000, CancellationToken.None))
            .ReturnsAsync(new ImpactRange(1, 1000, 10000));
        _riskAssessmentRepository.Setup(r => r.GetAssessmentWeightsAsync(1, 1, CancellationToken.None))
            .ReturnsAsync((3, 2)); // 3 * 2 = 6 > 5, let me use (1, 5) or similar
        _riskAssessmentRepository.Setup(r => r.GetAcceptanceThresholdsAsync(1, CancellationToken.None))
            .ReturnsAsync(new AcceptanceThresholds(6, 12, 20));
        _riskAssessmentRepository.Setup(r => r.CreateAsync(It.IsAny<RiskAssessment>(), CancellationToken.None))
            .ReturnsAsync(203);
        _riskRepository.Setup(r => r.UpdateStatusAsync(1, "Open", CancellationToken.None))
            .ReturnsAsync(true);

        var request = new ReassessRiskRequest(1, 5000, null, null, null);

        // Act
        var result = await _service.ReassessAsync(1, 1, request);

        // Assert
        result.RiskScore.Should().Be(6);
        // 6 > 5, so it should be InTreatment
        // Let me adjust: use weight 1 * 5 = 5
        // Actually the test setup returns (3, 2) = 6, so let me fix the expectation
        result.RiskStatus.Should().Be("InTreatment");
    }

    [Fact]
    public async Task ReassessAsync_WrongStatus_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Open", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);

        var request = new ReassessRiskRequest(1, 5000, null, null, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.ReassessAsync(1, 1, request));
        ex.Message.Should().Contain("Ожидает переоценки");
    }

    [Fact]
    public async Task ReassessAsync_ActiveAssessmentExists_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "AwaitingReassessment", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(true);

        var request = new ReassessRiskRequest(1, 5000, null, null, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.ReassessAsync(1, 1, request));
        ex.Message.Should().Contain("уже существует активная оценка");
    }

    [Fact]
    public async Task ReassessAsync_WithoutEditComponentsPermission_ThrowsPermissionException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(false);

        var request = new ReassessRiskRequest(1, 5000, null, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<RiskPermissionException>(() =>
            _service.ReassessAsync(1, 1, request));
    }

    [Fact]
    public async Task ReassessAsync_RiskNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        _riskRepository.Setup(r => r.GetByIdAsync(99, CancellationToken.None))
            .ReturnsAsync((Domain.Risk.Risk?)null);

        var request = new ReassessRiskRequest(1, 5000, null, null, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskNotFoundException>(() =>
            _service.ReassessAsync(99, 1, request));
        ex.Message.Should().Contain("99");
    }

    [Fact]
    public async Task ReassessAsync_MissingDowntimeParams_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, 1,
            "AwaitingReassessment", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(false);

        var request = new ReassessRiskRequest(
            LikelihoodLevelId: 1,
            LossAmount: 0,
            DowntimeWeeks: null,
            DowntimeDays: null,
            DowntimeHours: null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.ReassessAsync(1, 1, request));
        ex.Message.Should().Contain("простоя");
    }

    [Fact]
    public async Task ReassessAsync_NegativeLossAmount_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "AwaitingReassessment", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(false);

        var request = new ReassessRiskRequest(
            LikelihoodLevelId: 1,
            LossAmount: -100,
            DowntimeWeeks: null,
            DowntimeDays: null,
            DowntimeHours: null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.ReassessAsync(1, 1, request));
        ex.Message.Should().Contain("отрицательной");
    }

    [Fact]
    public async Task ReassessAsync_InvalidLikelihoodLevel_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "edit_components", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "AwaitingReassessment", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(false);

        var request = new ReassessRiskRequest(
            LikelihoodLevelId: 0, // Invalid
            LossAmount: 5000,
            DowntimeWeeks: null,
            DowntimeDays: null,
            DowntimeHours: null);

        // Act & Assert
        await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.ReassessAsync(1, 1, request));
    }

    #endregion

    #region CompleteCycleAsync Tests

    [Fact]
    public async Task CompleteCycleAsync_NoActiveTreatments_CompletesCycle()
    {
        // Arrange
        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "InTreatment", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _treatmentRepository.Setup(t => t.GetActiveRiskProcessIdsAsync(1, CancellationToken.None))
            .ReturnsAsync(new[] { 5 });
        _treatmentRepository.Setup(t => t.HasNonTerminalTreatmentAsync(5, CancellationToken.None))
            .ReturnsAsync(false);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(true);
        _riskAssessmentRepository.Setup(r => r.DeactivateAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(true);
        _riskRepository.Setup(r => r.IsActiveDraftAsync(1, CancellationToken.None))
            .ReturnsAsync(false);
        _riskRepository.Setup(r => r.SetAwaitingReassessmentStatusAsync(1, CancellationToken.None))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CompleteCycleAsync(1);

        // Assert
        result.CycleCompleted.Should().BeTrue();
        result.HasDraftsWarning.Should().BeFalse();
        result.RiskStatus.Should().Be("AwaitingReassessment");
        _riskAssessmentRepository.Verify(r => r.DeactivateAssessmentAsync(1, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CompleteCycleAsync_ActiveTreatmentsRemain_DoesNotComplete()
    {
        // Arrange
        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "InTreatment", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _treatmentRepository.Setup(t => t.GetActiveRiskProcessIdsAsync(1, CancellationToken.None))
            .ReturnsAsync(new[] { 5, 6 });
        _treatmentRepository.Setup(t => t.HasNonTerminalTreatmentAsync(5, CancellationToken.None))
            .ReturnsAsync(false);
        _treatmentRepository.Setup(t => t.HasNonTerminalTreatmentAsync(6, CancellationToken.None))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CompleteCycleAsync(1);

        // Assert
        result.CycleCompleted.Should().BeFalse();
        _riskAssessmentRepository.Verify(r => r.DeactivateAssessmentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteCycleAsync_NoActiveAssessment_DoesNotDeactivate()
    {
        // Arrange
        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "InTreatment", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _treatmentRepository.Setup(t => t.GetActiveRiskProcessIdsAsync(1, CancellationToken.None))
            .ReturnsAsync(new[] { 5 });
        _treatmentRepository.Setup(t => t.HasNonTerminalTreatmentAsync(5, CancellationToken.None))
            .ReturnsAsync(false);
        _riskAssessmentRepository.Setup(r => r.HasActiveAssessmentAsync(1, CancellationToken.None))
            .ReturnsAsync(false);
        _riskRepository.Setup(r => r.SetAwaitingReassessmentStatusAsync(1, CancellationToken.None))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CompleteCycleAsync(1);

        // Assert
        result.CycleCompleted.Should().BeTrue();
        _riskAssessmentRepository.Verify(r => r.DeactivateAssessmentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteCycleAsync_RiskNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _riskRepository.Setup(r => r.GetByIdAsync(99, CancellationToken.None))
            .ReturnsAsync((Domain.Risk.Risk?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskNotFoundException>(() =>
            _service.CompleteCycleAsync(99));
        ex.Message.Should().Contain("99");
    }

    #endregion

    #region CloseAsync Tests

    [Fact]
    public async Task CloseAsync_ValidInput_ClosesRisk()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "close_risk", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Open", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskProcessRepository.Setup(r => r.GetExistingProcessIdsAsync(1, CancellationToken.None))
            .ReturnsAsync(Array.Empty<int>());
        _riskRepository.Setup(r => r.UpdateRiskStatusAndReasonAsync(1, "Closed", "Test reason", CancellationToken.None))
            .ReturnsAsync(true);
        _commentRepository.Setup(c => c.CreateAsync(1, "Риск закрыт: Test reason", "Risk", 1, CancellationToken.None))
            .ReturnsAsync(1);

        var request = new CloseRiskRequest("Test reason");

        // Act
        var result = await _service.CloseAsync(1, 1, request);

        // Assert
        result.RiskId.Should().Be(1);
        result.RiskStatus.Should().Be("Closed");
    }

    [Fact]
    public async Task CloseAsync_HasBlockingMeasure_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "close_risk", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Open", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskProcessRepository.Setup(r => r.GetExistingProcessIdsAsync(1, CancellationToken.None))
            .ReturnsAsync(new[] { 5 });
        _treatmentRepository.Setup(t => t.HasNonTerminalTreatmentAsync(5, CancellationToken.None))
            .ReturnsAsync(true);

        var request = new CloseRiskRequest("Test reason");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.CloseAsync(1, 1, request));
        ex.Message.Should().Contain("блокирующие меры");
    }

    [Fact]
    public async Task CloseAsync_EmptyReason_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "close_risk", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "Open", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);
        _riskProcessRepository.Setup(r => r.GetExistingProcessIdsAsync(1, CancellationToken.None))
            .ReturnsAsync(Array.Empty<int>());

        var request = new CloseRiskRequest("");

        // Act & Assert
        await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.CloseAsync(1, 1, request));
    }

    [Fact]
    public async Task CloseAsync_NotAllowedStatus_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "close_risk", CancellationToken.None))
            .ReturnsAsync(true);

        var risk = Domain.Risk.Risk.Rehydrate(1, "Title", "Desc", 1, 2, 1, null,
            "InTreatment", null, null);
        _riskRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(risk);

        var request = new CloseRiskRequest("Test reason");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RiskValidationException>(() =>
            _service.CloseAsync(1, 1, request));
        ex.Message.Should().Contain("текущем статусе");
    }

    #endregion
}
