using lapo_vms_api.Dtos.User;
using lapo_vms_api.Helpers;
using lapo_vms_api.Interface;
using lapo_vms_api.Model;
using Microsoft.AspNetCore.Mvc;

namespace lapo_vms_api.Controllers;

[Route("api/users")]
[ApiController]
public class UsersController(IUserRepository userRepository) : ControllerBase
{
    private readonly IUserRepository _userRepository = userRepository;

    [HttpGet]
    public async Task<IActionResult> GetAllUsers([FromQuery] QueryParameters queryParameters)
    {
        var users = await _userRepository.GetAllAsync(queryParameters);
        return Ok(users.Select(ToUserDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
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
            return Problem(detail: "A user with this email already exists.", statusCode: StatusCodes.Status400BadRequest, title: "Duplicate Email");

        if (await _userRepository.ExistsByStaffIdAsync(staffId))
            return Problem(detail: "A user with this staff ID already exists.", statusCode: StatusCodes.Status400BadRequest, title: "Duplicate Staff ID");

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = email,
            StaffId = staffId,
            Role = dto.Role
        };

        var createdUser = await _userRepository.CreateAsync(user);

        return Ok(ToUserDto(createdUser));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser([FromRoute] int id, [FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var email = dto.Email.Trim();
        var staffId = dto.StaffId.Trim();

        if (await _userRepository.ExistsByEmailAsync(email, id))
            return Problem(detail: "A user with this email already exists.", statusCode: StatusCodes.Status400BadRequest, title: "Duplicate Email");

        if (await _userRepository.ExistsByStaffIdAsync(staffId, id))
            return Problem(detail: "A user with this staff ID already exists.", statusCode: StatusCodes.Status400BadRequest, title: "Duplicate Staff ID");

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = email,
            StaffId = staffId,
            Role = dto.Role
        };

        var updatedUser = await _userRepository.UpdateAsync(id, user);
        if (updatedUser == null) 
            return Problem(detail: $"User with ID {id} was not found.", statusCode: StatusCodes.Status404NotFound, title: "User Not Found");

        return Ok(ToUserDto(updatedUser));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser([FromRoute] int id)
    {
        var deletedUser = await _userRepository.DeleteAsync(id);
        if (deletedUser == null) 
            return Problem(detail: $"User with ID {id} was not found.", statusCode: StatusCodes.Status404NotFound, title: "User Not Found");

        return Ok(ToUserDto(deletedUser));
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
