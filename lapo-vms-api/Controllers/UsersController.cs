using lapo_vms_api.Dtos.User;
using lapo_vms_api.Helpers;
using lapo_vms_api.Interface;
using lapo_vms_api.Model;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace lapo_vms_api.Controllers;

[Route("api/users")]
[ApiController]
public class UsersController(
    IUserRepository userRepository,
    IAuditService auditService,
    ILogger<UsersController> logger) : ControllerBase
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IAuditService _auditService = auditService;
    private readonly ILogger<UsersController> _logger = logger;

    [HttpGet]
    public async Task<IActionResult> GetAllUsers([FromQuery] QueryParameters queryParameters)
    {
        var users = await _userRepository.GetAllAsync(queryParameters);
        return Ok(users.Select(ToUserDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return Problem(detail: $"User with ID {id} was not found.", statusCode: StatusCodes.Status404NotFound, title: "User Not Found");

        return Ok(ToUserDto(user));
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var email = dto.Email.Trim();
        var staffId = dto.StaffId.Trim();

        if (await _userRepository.ExistsByEmailAsync(email))
        {
            LogUserFailure("Create", "DuplicateEmail");
            return Problem(detail: "A user with this email already exists.", statusCode: StatusCodes.Status400BadRequest, title: "Duplicate Email");
        }

        if (await _userRepository.ExistsByStaffIdAsync(staffId))
        {
            LogUserFailure("Create", "DuplicateStaffId");
            return Problem(detail: "A user with this staff ID already exists.", statusCode: StatusCodes.Status400BadRequest, title: "Duplicate Staff ID");
        }

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = email,
            StaffId = staffId,
            Role = dto.Role
        };

        var createdUser = await _userRepository.CreateAsync(user);
        await _auditService.LogEventAsync(new AuditLog
        {
            EventType = "USER_CREATED",
            ActorId = createdUser.Id,
            ActorRole = createdUser.Role?.ToString(),
            Timestamp = DateTime.UtcNow,
            Metadata = $"Email: {createdUser.Email}; StaffId: {createdUser.StaffId}"
        });

        LogUserEvent("User created", createdUser);
        return Ok(ToUserDto(createdUser));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser([FromRoute] Guid id, [FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var email = dto.Email.Trim();
        var staffId = dto.StaffId.Trim();

        if (await _userRepository.ExistsByEmailAsync(email, id))
        {
            LogUserFailure("Update", "DuplicateEmail", id);
            return Problem(detail: "A user with this email already exists.", statusCode: StatusCodes.Status400BadRequest, title: "Duplicate Email");
        }

        if (await _userRepository.ExistsByStaffIdAsync(staffId, id))
        {
            LogUserFailure("Update", "DuplicateStaffId", id);
            return Problem(detail: "A user with this staff ID already exists.", statusCode: StatusCodes.Status400BadRequest, title: "Duplicate Staff ID");
        }

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = email,
            StaffId = staffId,
            Role = dto.Role
        };

        var updatedUser = await _userRepository.UpdateAsync(id, user);
        if (updatedUser == null)
        {
            LogUserFailure("Update", "UserNotFound", id);
            return Problem(detail: $"User with ID {id} was not found.", statusCode: StatusCodes.Status404NotFound, title: "User Not Found");
        }

        LogUserEvent("User updated", updatedUser);
        return Ok(ToUserDto(updatedUser));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser([FromRoute] Guid id)
    {
        var deletedUser = await _userRepository.DeleteAsync(id);
        if (deletedUser == null)
        {
            LogUserFailure("Delete", "UserNotFound", id);
            return Problem(detail: $"User with ID {id} was not found.", statusCode: StatusCodes.Status404NotFound, title: "User Not Found");
        }

        LogUserEvent("User deleted", deletedUser);
        return Ok(ToUserDto(deletedUser));
    }

    private void LogUserEvent(string eventName, User affectedUser)
    {
        _logger.LogInformation(
            "{EventName}. UserId={UserId} AffectedStaffId={AffectedStaffId} AffectedRole={AffectedRole} ActorId={ActorId} StaffId={StaffId} Role={Role}",
            eventName,
            affectedUser.Id,
            affectedUser.StaffId,
            affectedUser.Role,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.FindFirstValue("staffId"),
            User.FindFirstValue(ClaimTypes.Role));
    }

    private void LogUserFailure(string operation, string reason, Guid? userId = null)
    {
        _logger.LogWarning(
            "User operation failed. Operation={Operation} Reason={Reason} UserId={UserId} ActorId={ActorId} StaffId={StaffId} Role={Role}",
            operation,
            reason,
            userId,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.FindFirstValue("staffId"),
            User.FindFirstValue(ClaimTypes.Role));
    }

    private static UserDto ToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name ?? string.Empty,
            Email = user.Email ?? string.Empty,
            StaffId = user.StaffId ?? string.Empty,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }
}
