using Blockbuster.API.DTOs;
using Blockbuster.API.Models;
using Google.Cloud.Firestore;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using BCrypt.Net;

namespace Blockbuster.API.Services
{
    /// <summary>
    /// AuthService implementa la autenticación de usuarios
    /// Gestiona registro, login y generación de tokens JWT
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly FirebaseService _firebaseService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        /// <summary>
        /// Constructor: Recibe las dependencias inyectadas
        /// </summary>
        public AuthService(
            FirebaseService firebaseService, 
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _firebaseService = firebaseService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Register: Crea un nuevo usuario en la aplicación
        /// </summary>
        public async Task<User> Register(RegisterDto registerDto)
        {
            try
            {
                // Validar que el DTO no es nulo
                if (registerDto == null)
                {
                    throw new ArgumentException("El cuerpo de la petición es requerido");
                }

                // Validar que email y password no están vacíos
                if (string.IsNullOrWhiteSpace(registerDto.Email) || 
                    string.IsNullOrWhiteSpace(registerDto.Password))
                {
                    throw new ArgumentException("Email y contraseña son requeridos");
                }

                // Validar que la contraseña tenga longitud mínima
                if (registerDto.Password.Length < 6)
                {
                    throw new ArgumentException("La contraseña debe tener al menos 6 caracteres");
                }

                // Obtener la colección de usuarios desde Firestore
                var usersCollection = _firebaseService.GetCollection("users");

                if (usersCollection == null)
                {
                    throw new InvalidOperationException("No se pudo obtener la colección de usuarios");
                }

                // Verificar que el email no está registrado
                var query = await usersCollection
                    .WhereEqualTo("Email", registerDto.Email)
                    .GetSnapshotAsync();

                if (query.Count > 0)
                {
                    throw new InvalidOperationException("El email ya está registrado");
                }

                // Hashear la contraseña con BCrypt
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

                // Crear nuevo usuario
                var newUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = registerDto.Email,
                    Fullname = registerDto.FullName,
                    Role = "user",
                    TotalRatings = 0,
                    CreatedAt = DateTime.UtcNow,
                    LastLogin = DateTime.UtcNow,
                    IsActive = true
                };

                // Guardar el usuario en Firestore usando Dictionary
                var userData = new Dictionary<string, object>
                {
                    { "Id", newUser.Id },
                    { "Email", newUser.Email },
                    { "Fullname", newUser.Fullname },
                    { "Role", newUser.Role },
                    { "ProfilePictureUrl", newUser.ProfilePictureUrl },
                    { "TotalRatings", newUser.TotalRatings },
                    { "CreatedAt", newUser.CreatedAt },
                    { "LastLogin", newUser.LastLogin },
                    { "IsActive", newUser.IsActive },
                    { "PasswordHash", passwordHash }  // Guardar hash, NO la contraseña
                };

                await usersCollection.Document(newUser.Id).SetAsync(userData);

                return newUser;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError($"Error de validación en Register: {ex.Message}");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"Error lógico en Register: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error inesperado en Register: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Login: Autentica un usuario y devuelve un token JWT
        /// </summary>
        public async Task<(User user, string token)> Login(LoginDto loginDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loginDto.Email) || 
                    string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    throw new ArgumentException("Email y contraseña son requeridos");
                }

                var usersCollection = _firebaseService.GetCollection("users");

                if (usersCollection == null)
                {
                    throw new InvalidOperationException("No se pudo obtener la colección de usuarios");
                }

                var query = await usersCollection
                    .WhereEqualTo("Email", loginDto.Email)
                    .GetSnapshotAsync();

                if (query.Count == 0)
                {
                    throw new InvalidOperationException("Email o contraseña incorrectos");
                }

                var userDoc = query.Documents[0];
                var userDict = userDoc.ToDictionary();

                // Obtener el hash de contraseña guardado
                var passwordHash = userDict["PasswordHash"].ToString();

                // Validar la contraseña contra el hash con BCrypt
                if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, passwordHash))
                {
                    throw new InvalidOperationException("Email o contraseña incorrectos");
                }

                // Convertir el diccionario a objeto User
                var user = new User
                {
                    Id = userDict["Id"].ToString(),
                    Email = userDict["Email"].ToString(),
                    Fullname = userDict["Fullname"].ToString(),
                    Role = userDict["Role"].ToString(),
                    ProfilePictureUrl = userDict["ProfilePictureUrl"].ToString(),
                    TotalRatings = (int)(long)userDict["TotalRatings"],
                    CreatedAt = ((Timestamp)userDict["CreatedAt"]).ToDateTime(),
                    LastLogin = ((Timestamp)userDict["LastLogin"]).ToDateTime(),
                    IsActive = (bool)userDict["IsActive"]
                };

                var token = GenerateJwtToken(user);

                // Actualizar LastLogin
                await usersCollection.Document(user.Id).UpdateAsync(
                    new Dictionary<string, object>
                    {
                        { "LastLogin", DateTime.UtcNow }
                    }
                );

                return (user, token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en Login: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// ValidateToken: Verifica si un token JWT es válido
        /// </summary>
        public async Task<bool> ValidateToken(string token)
        {
            try
            {
                var secretKey = _configuration["Jwt:SecretKey"];
                if (string.IsNullOrEmpty(secretKey))
                {
                    return false;
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(secretKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validando token: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// GetUserById: Obtiene un usuario por su ID
        /// </summary>
        public async Task<User?> GetUserById(string userId)
        {
            try
            {
                var usersCollection = _firebaseService.GetCollection("users");
                var doc = await usersCollection.Document(userId).GetSnapshotAsync();

                if (!doc.Exists)
                {
                    return null;
                }

                var userDict = doc.ToDictionary();

                var user = new User
                {
                    Id = userDict["Id"].ToString(),
                    Email = userDict["Email"].ToString(),
                    Fullname = userDict["Fullname"].ToString(),
                    Role = userDict["Role"].ToString(),
                    ProfilePictureUrl = userDict["ProfilePictureUrl"].ToString(),
                    TotalRatings = (int)(long)userDict["TotalRatings"],
                    CreatedAt = ((Timestamp)userDict["CreatedAt"]).ToDateTime(),
                    LastLogin = ((Timestamp)userDict["LastLogin"]).ToDateTime(),
                    IsActive = (bool)userDict["IsActive"]
                };

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener usuario: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// GenerateJwtToken: Crea un token JWT para un usuario
        /// </summary>
        public string GenerateJwtToken(User user)
        {
            try
            {
                var secretKey = _configuration["Jwt:SecretKey"];
                var issuer = _configuration["Jwt:Issuer"];
                var audience = _configuration["Jwt:Audience"];

                if (string.IsNullOrEmpty(secretKey))
                {
                    throw new InvalidOperationException("JWT SecretKey no configurado");
                }

                var key = Encoding.ASCII.GetBytes(secretKey);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim("sub", user.Id),
                        new Claim("email", user.Email),
                        new Claim("name", user.Fullname),
                        new Claim("role", user.Role)
                    }),
                    Expires = DateTime.UtcNow.AddHours(24),
                    Issuer = issuer,
                    Audience = audience,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key), 
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);

                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al generar token: {ex.Message}");
                throw;
            }
        }
    }
}