using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MeetingRoomBooking.Data;
using MeetingRoomBooking.Models;
using MeetingRoomBooking.Models.DTOs;
using MeetingRoomBooking.Services;

namespace MeetingRoomBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthController(ApplicationDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        /// <summary>
        /// 用户注册
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
        {
            // 检查用户名是否已存在
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "用户名已存在"
                });
            }

            // 检查邮箱是否已存在
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "邮箱已被注册"
                });
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _jwtService.HashPassword(request.Password),
                Department = request.Department,
                Role = UserRole.Employee,  // 默认为普通员工
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _jwtService.GenerateToken(user);

            return Ok(new AuthResponseDto
            {
                Success = true,
                Token = token,
                Message = "注册成功",
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Department = user.Department,
                    Role = user.Role
                }
            });
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "用户名或密码错误"
                });
            }

            if (!_jwtService.VerifyPassword(request.Password, user.PasswordHash))
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "用户名或密码错误"
                });
            }

            // 更新最后登录时间
            user.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var token = _jwtService.GenerateToken(user);

            return Ok(new AuthResponseDto
            {
                Success = true,
                Token = token,
                Message = "登录成功",
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Department = user.Department,
                    Role = user.Role
                }
            });
        }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Department = user.Department,
                Role = user.Role
            });
        }
    }
}
