namespace OficinaMecanica.Domain.Enums
{
    public enum WorkOrderStatus
    {
        Received = 1,
        InDiagnosis = 2,
        WaitingForApproval = 3,
        InExecution = 4,
        Finished = 5,
        Delivered = 6,
        Cancelled = 7
    }
}
