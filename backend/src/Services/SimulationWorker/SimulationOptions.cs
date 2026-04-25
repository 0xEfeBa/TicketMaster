namespace TicketFlow.SimulationWorker;

public class SimulationOptions
{
    public int InitialDelaySeconds { get; set; } = 5;
    public int CycleDelaySeconds { get; set; } = 20;
    public int MaxCycles { get; set; } = 1;

    public string DefaultPassword { get; set; } = "Password123*";

    public string AdminEmail { get; set; } = "admin@local.dev";
    public string AdminPassword { get; set; } = "Admin12345!";

    public bool EnableAdminScenario { get; set; } = true;
    public bool EnableBookingScenario { get; set; } = true;
    public bool EnableRaceScenario { get; set; } = true;
    public bool EnableLoadScenario { get; set; } = true;
    public int NegativeScenarioRepeatCount { get; set; } = 3;

    public int LoadUsers { get; set; } = 80;
    public int LoadBatchSize { get; set; } = 10;
    public int LoadParallelism { get; set; } = 5;
    public int LoadBatchPauseSeconds { get; set; } = 3;
    public int LoadRegisterRetryCount { get; set; } = 3;
}
