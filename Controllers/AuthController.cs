using Blockbuster.API.DTOs;
using Blockbuster.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Blockbuster.API.Controllers
{
    /// <summary>
    /// AuthController maneja todo lo relacionado con autenticación
    /// Endpoints para registro e inicio de sesión
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Constructor: Recibe IAuthService inyectado
        /// Los servicios se inyectan automáticamente desde Program.cs
        /// </summary>
        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/auth/register
        /// 
        /// Endpoint para registrar un nuevo usuario
        /// 
        /// Cuerpo esperado (JSON):
        /// {
        ///   "email": "usuario@example.com",
        ///   "password": "demo123",
        ///   "fullName": "Juan Pérez"
        /// }
        /// 
        /// Respuesta exitosa (201):
        /// {
        ///   "id": "user_123",
        ///   "email": "usuario@example.com",
        ///   "fullName": "Juan Pérez",
        ///   "role": "user",
        ///   "createdAt": "2026-02-10T15:30:00Z"
        /// }
        /// 
        /// Errores:
        /// 400: Email ya existe, password muy corta, datos inválidos
        /// 500: Error del servidor
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                // Validar que el DTO no es nulo
                if (registerDto == null)
                {
                    return BadRequest(new { message = "El cuerpo de la petición es requerido" });
                }

                // Validar que email y password no están vacíos
                if (string.IsNullOrWhiteSpace(registerDto.Email) || 
                    string.IsNullOrWhiteSpace(registerDto.Password))
                {
                    return BadRequest(new { message = "Email y contraseña son requeridos" });
                }

                // Llamar al servicio para registrar
                var user = await _authService.Register(registerDto);

                _logger.LogInformation($"Usuario registrado: {user.Email}");

                // Devolver 201 (Created) con el usuario creado
                return Created($"/api/auth/users/{user.Id}", user);
            }
            catch (ArgumentException ex)
            {
                // Errores de validación
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Email ya existe, etc.
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en registro: {ex.Message}");
                return StatusCode(500, new { message = "Error al registrar usuario" });
            }
        }

        /// <summary>
        /// POST /api/auth/login
        /// 
        /// Endpoint para iniciar sesión
        /// 
        /// Cuerpo esperado (JSON):
        /// {
        ///   "email": "usuario@example.com",
        ///   "password": "demo123"
        /// }
        /// 
        /// Respuesta exitosa (200):
        /// {
        ///   "success": true,
        ///   "message": "Login exitoso",
        ///   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        ///   "user": {
        ///     "id": "user_123",
        ///     "email": "usuario@example.com",
        ///     "fullName": "Juan Pérez",
        ///     "role": "user"
        ///   }
        /// }
        /// 
        /// El token debe ser guardado en el frontend y enviado en cada petición:
        /// Authorization: Bearer {token}
        /// 
        /// Errores:
        /// 400: Email o contraseña incorrectos
        /// 500: Error del servidor
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                // Validar entrada
                if (loginDto == null)
                {
                    return BadRequest(new { message = "El cuerpo de la petición es requerido" });
                }

                if (string.IsNullOrWhiteSpace(loginDto.Email) || 
                    string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    return BadRequest(new { message = "Email y contraseña son requeridos" });
                }

                // Llamar al servicio para hacer login
                var (user, token) = await _authService.Login(loginDto);

                _logger.LogInformation($"Usuario inició sesión: {user.Email}");

                // Devolver el token y datos del usuario
                var response = new AuthResponseDto
                {
                    Success = true,
                    Message = "Login exitoso",
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        FullName = user.Fullname,
                        Email = user.Email,
                        Role = user.Role,
                        TotalRating = user.TotalRatings
                    }
                };

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                // Email no existe o contraseña incorrecta
                return BadRequest(new 
                { 
                    success = false,
                    message = ex.Message 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en login: {ex.Message}");
                return StatusCode(500, new 
                { 
                    success = false,
                    message = "Error al iniciar sesión" 
                });
            }
        }

        /// <summary>
        /// GET /api/auth/users/{userId}
        /// 
        /// Obtiene información de un usuario por su ID
        /// 
        /// Parámetro:
        ///   userId: ID del usuario (en la URL)
        /// 
        /// Respuesta exitosa (200):
        /// {
        ///   "id": "user_123",
        ///   "email": "usuario@example.com",
        ///   "fullName": "Juan Pérez",
        ///   "role": "user",
        ///   "totalRatings": 5
        /// }
        /// 
        /// Errores:
        /// 404: Usuario no encontrado
        /// </summary>
        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUser(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(new { message = "El ID de usuario es requerido" });
                }

                var user = await _authService.GetUserById(userId);

                if (user == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    FullName = user.Fullname,
                    Email = user.Email,
                    Role = user.Role,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    TotalRating = user.TotalRatings
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener usuario: {ex.Message}");
                return StatusCode(500, new { message = "Error al obtener usuario" });
            }
        }
    }
}