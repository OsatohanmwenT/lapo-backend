using lapo_vms_api.Dtos.VisitItem;
using lapo_vms_api.Model;

namespace lapo_vms_api.Mappers;

public static class VisitItemMapper
{
    public static VisitItemDto ToVisitItemDto(this VisitItem item)
    {
        return new VisitItemDto
        {
            Id = item.Id,
            SerialNumber = item.SerialNumber,
            LaptopModel = item.LaptopModel,
            VisitId = item.VisitId
        };
    }

    public static VisitItem ToVisitItemFromCreateDto(this CreateVisitItemDto dto)
    {
        return new VisitItem
        {
            SerialNumber = dto.SerialNumber,
            LaptopModel = dto.LaptopModel,
            VisitId = dto.VisitId
        };
    }
}
