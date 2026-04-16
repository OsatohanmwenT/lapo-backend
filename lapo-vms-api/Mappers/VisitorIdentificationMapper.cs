using lapo_vms_api.Dtos.VisitorIdentification;
using lapo_vms_api.Model;

namespace lapo_vms_api.Mappers;

public static class VisitorIdentificationMapper
{
    public static VisitorIdentificationDto ToVisitorIdentificationDto(this VisitorIdentification identification)
    {
        return new VisitorIdentificationDto
        {
            IdentificationType = identification.IdentificationType,
            IdentificationNumber = identification.IdentificationNumber
        };
    }

    public static VisitorIdentification ToVisitorIdentificationFromCreateDto(this CreateVisitorIdentificationDto dto)
    {
        return new VisitorIdentification
        {
            IdentificationType = dto.IdentificationType,
            IdentificationNumber = dto.IdentificationNumber
        };
    }
}