using lapo_vms_api.Dtos.WorkerDetails;
using lapo_vms_api.Model;

namespace lapo_vms_api.Mappers;

public static class WorkerDetailsMapper
{
    public static WorkerDetailsDto ToWorkerDetailsDto(this WorkerDetails workerDetails)
    {
        return new WorkerDetailsDto
        {
            Id = workerDetails.Id,
            VisitorId = workerDetails.VisitorId,
            CompanyName = workerDetails.CompanyName
        };
    }

    public static WorkerDetails ToWorkerDetailsFromCreateDto(this CreateWorkerDetailsDto dto)
    {
        return new WorkerDetails
        {
            CompanyName = dto.CompanyName
        };
    }
}