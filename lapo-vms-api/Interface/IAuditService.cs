using System;
using lapo_vms_api.Model;

namespace lapo_vms_api.Interface;

public interface IAuditService
{
    Task LogEventAsync(AuditLog log);
}
