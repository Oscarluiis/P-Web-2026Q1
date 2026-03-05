using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Blockbuster.API.DTOs;
using Blockbuster.API.Models;
using FirebaseAdmin.Auth;
using Microsoft.IdentityModel.Tokens;

namespace Blockbuster.API.Services;

/// <summary>
///
///  AuthService implementa la autenticacion de usuarios
///  Gestionar el registro, login y la generacion de tokens
/// 
/// </summary>

public class AuthService : IAuthService 
{
    private readonly IConfiguration _configuration;
    private readonly FirebaseService _firebaseService;
    
    /**
     * Constructor que va recibir las dependencias inyectadas
     * FirabaseService: Para acceder a FB
     * IConfiguration: Para leer valores del appsetting (JWT)
     */
    
    /**
     * Crea un nuevo usuario
     *
     * Proceso:
     * 1. Validar datos de entrada
     * 2. Verificar que el correo no existe
     * 3. Crear el documento en FS
     * 4. Devolver el usuario el user creado
     * 
     */
    public async Task<User> Register(RegisterDto registerDto)
    {

        try
        {
            //Validar el correo y password != null
            if (string.IsNullOrWhiteSpace(registerDto.Email) || 
                string.IsNullOrWhiteSpace(registerDto.Password))
            {
                throw new ArgumentNullException("Email y password son requeridos");
            }
            
            // Validar la longitud del password
            if (registerDto.Password.Length < 6)
            {
                throw new ArgumentNullException("Password debe contener al menos 6 caracteres");
            }
            
            // Obtener la coleccion del usuario en FS
            var userCollection = _firebaseService.GetCollection("users");
            
            // Verificar que el correo no este registrado
            // Query para buscar documentos donde el email == registerDto.Email
            var query = await userCollection
                .WhereEqualTo("Email", registerDto.Email)
                .GetSnapshotAsync();
            
            // Si hay resultados, significa que ya existe
            if (query.Count > 0)
            {
                throw new InvalidOperationException("El email ya esta registrado");
            }

            
             // Crear el usuario nuevo
             // El ID se genera autimaticamente por FB

            var newUser = new User
            {
                Id = Guid.NewGuid().ToString(), //Generar el id unico
                Email = registerDto.Email,
                Fullname = registerDto.FullName,
                Role = "user", //Por defecto, role de user normal
                TotalRatings = 0,
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                IsActive = true
            };
            
            // Guardar el usuario en FS
            // SetAsync guardar en el documento el usuario (Si no existe, lo crea y si existe lo actualiza)
            await userCollection.Document(newUser.Id).SetAsync(newUser);
            
            // Devolver el usuario creado (sin password)
            return newUser;


        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al registrar usuario: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Autentica el usuario
    /// 1. Buscar el correo
    /// 2. Validar el correo
    /// 3. Validar password
    /// 4. Generar token JWT
    /// 5. Actualizar LastLogin
    /// 6. Devolver el usuario y el token
    /// </summary>
    public async Task<(User user, string token)> Login(LoginDto loginDto)
    {
        try
        {
            // Validar inputs
            if (string.IsNullOrWhiteSpace(loginDto.Email) ||
                string.IsNullOrWhiteSpace(loginDto.Password))
            {
                throw new ArgumentNullException("Email y password son requeridos");
            }

            // Obtener la coleccion del usuario en FS
            var userCollection = _firebaseService.GetCollection("users");

            // Buscar un usuario por email
            var query = await userCollection
                .WhereEqualTo("Email", loginDto.Email)
                .GetSnapshotAsync();

            // Si no hay resultados, el correo no existe
            if (query.Count == 0)
            {
                throw new InvalidOperationException("El email o password incorrecto");
            }

            // Obtener el documento (registro) del usuario
            var userDoc = query.Documents[0];
            var user = userDoc.ConvertTo<User>();

            // Generar el token
            var token = GenerateJwtToken(user);

            // Actualizar el lastlogin en FS
            await userCollection.Document(user.Id).UpdateAsync(
                new Dictionary<string, object>
                {
                    { "LastLogin", DateTime.UtcNow }
                }

            );

            return (user, token);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error en login: {e.Message}");
            throw;
        }
        
    }

    // Verificar si un token es valido
    public async Task<bool> ValidateToken(string token)
    {
        try
        {
            // Obtener la clave secreta appsettings.json
            var secretKey = _configuration["Jwt:SecretKey"];

            if (string.IsNullOrEmpty(secretKey))
            {
                return false;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);
            
            // Validar el token
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
        catch (Exception e)
        {
            return false;
        }
    }
    
    // Obtiene un usuario por su ID
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

            return doc.ConvertTo<User>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener usuario: {ex.Message}");
            return null;
        }
    }

    // Crea un token JWT para un usuario
    // El token contiene:
    // - Claim "sub" (subject): ID del usuario
    // - Claim "email": Email del usuario
    // - Claim "role": Rol del usuario (user o admin)
    // - Expiración: 24 horas desde ahora
    // El frontend guardará este token y lo enviará en cada petición:
    // Authorization: Bearer {token}
    public string GenerateJwtToken(User user)
    {
        try
        {
            // Obtener valores de configuración
            var secretKey = _configuration["Jwt:SecretKey"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT SecretKey no configurado");
            }

            // Convertir la clave a bytes
            var key = Encoding.ASCII.GetBytes(secretKey);

            // Crear los claims (datos que van dentro del token)
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("sub", user.Id), // ID del usuario
                    new Claim("email", user.Email), // Email
                    new Claim("name", user.Fullname), // Nombre
                    new Claim("role", user.Role) // Rol (user o admin)
                }),
                Expires = DateTime.UtcNow.AddHours(24), // Válido por 24 horas
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };

            // Crear el token
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // Convertir a string
            return tokenHandler.WriteToken(token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al generar token: {ex.Message}");
            throw;
        }
    }
}