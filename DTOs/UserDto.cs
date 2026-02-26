namespace Blockbuster.API.DTOs;
/// <summary>
/// UserDto es lo que se envia hacia el FE cuando se solicita info del user
/// NO incluye informacion sensible / privada
/// </summary>
public class UserDto
{
    /**
     * Id para identificar el usuario
     */
    public string Id { get; set; } = string.Empty;
    
    /**
     * Fullname nombre visible del usuario
     * Se muestre en sus ratings
     */
    public string FullName { get; set; } = string.Empty;
    
    /**
     * Solo si es necesario
     */
    public string Email { get; set; } = string.Empty;
    
    /**
     * Si es un admin o un regular user (mostrar que acceso tiene)
     */
    public string Role { get; set; } = "user";
    
    /**
     * ProfilePictureUrl foto de perfil
     */
    public string ProfilePictureUrl { get; set; } = string.Empty;
    
    /**
     * TotalRatings cuantas ha calificado
     */
    public int TotalRating { get; set; }
}

/// <summary>
/// RegisterDto es lo que recibira el backend cuando alguien se registra
/// </summary>
public class RegisterDto
{
    /**
     * Email correo para la cuenta
     */
    public string Email { get; set; } = string.Empty;
    
    /**
     * Password es la clave / contraseña (esto va encriptado por HTTPS)
     */
    
    public string Password { get; set; } = string.Empty;
    
    /**
     * FullName nombre que aparecera en el perfil
     */
    public string FullName { get; set; } = string.Empty;
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
/// <summary>
/// AuthResponse es la informacion que recibimos de nuestra peticion desde el FE
/// Contiene el token que el FE recibe para futuras peticiones
/// </summary>
public class AuthResponseDto
{
    /**
     * Bool para saber el login fue exitoso o no
     */
    public bool Success { get; set; }
    
    /**
     * Mensaje de exito o error
     */
    public string Message { get; set; } = string.Empty;
    
    /**
     * Token es manipulado / administrado por el JWT guarda y envia en cada peticion
     */
    public string Token { get; set; } = string.Empty;
    
    /**
     * User va extraer la informacion del usuario autenticado
     */
    public UserDto User { get; set; } = new UserDto();
}