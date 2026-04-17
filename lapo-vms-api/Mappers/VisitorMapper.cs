using lapo_vms_api.Dtos.Visitor;
using lapo_vms_api.Model;

namespace lapo_vms_api.Mappers;

public static class VisitorMapper
{
    public static VisitorDto ToVisitorDto(this Visitor visitor)
    {
        return new VisitorDto
        {
            Id = visitor.Id,
            FullName = visitor.FullName,
            PhoneNumber = visitor.PhoneNumber,
            PhotoPath = visitor.PhotoPath!,
            VisitorType = visitor.VisitorType.ToString(),
            CreatedAt = visitor.CreatedAt,
            Identification = visitor.Identification?.ToVisitorIdentificationDto(),
            WorkerDetails = visitor.WorkerDetails?.ToWorkerDetailsDto(),
            Visits = visitor.Visits.Select(v => v.ToVisitDto()).ToList()
        };
    }

    public static Visitor ToVisitorFromCreateDto(this CreateVisitorDto dto)
    {
        return new Visitor
        {
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            VisitorType = dto.VisitorType,
        
            Identification = dto.Identification == null
                ? null
                : new VisitorIdentification
                {
                    IdentificationType = dto.Identification.IdentificationType,
                    IdentificationNumber = dto.Identification.IdentificationNumber
                },
            WorkerDetails = string.IsNullOrWhiteSpace(dto.CompanyName)
                ? null
                : new WorkerDetails
                {
                    CompanyName = dto.CompanyName
                }
        };
    }

    public static Visitor ToVisitorFromCreateJsonDto(this CreateVisitorJsonDto dto)
    {
        return new Visitor
        {
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            VisitorType = dto.VisitorType,
            Identification = dto.Identification == null
                ? null
                : new VisitorIdentification
                {
                    IdentificationType = dto.Identification.IdentificationType,
                    IdentificationNumber = dto.Identification.IdentificationNumber
                },
            WorkerDetails = string.IsNullOrWhiteSpace(dto.CompanyName)
                ? null
                : new WorkerDetails
                {
                    CompanyName = dto.CompanyName
                }
        };
    }
}
