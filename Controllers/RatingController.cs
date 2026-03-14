using Blockbuster.API.DTOs;
using Blockbuster.API.Services;
using Microsoft.AspNetCore.Mvc;
namespace Blockbuster.API.Controllers;


/// <summary>
/// RatingController maneja todo lo relacionado con calificaciones
/// Endpoints para crear, obtener, editar y eliminar calificaciones
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RatingController : ControllerBase
{
    private readonly IRatingService _ratingService;
        private readonly ILogger<RatingController> _logger;

        /// <summary>
        /// Constructor: Recibe IRatingService inyectado
        /// </summary>
        public RatingController(IRatingService ratingService, ILogger<RatingController> logger)
        {
            _ratingService = ratingService;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/rating/movie/{movieId}
        /// 
        /// Obtiene todas las calificaciones de una película
        /// 
        /// Parámetro:
        ///   movieId: ID de la película
        /// 
        /// Respuesta exitosa (200):
        /// [
        ///   {
        ///     "id": "rating_001",
        ///     "movieId": "movie_001",
        ///     "movieTitle": "Inception",
        ///     "score": 8.5,
        ///     "review": "Great movie!",
        ///     "userName": "Juan Pérez",
        ///     "createdAt": "2026-02-10T15:30:00Z"
        ///   }
        /// ]
        /// </summary>
        [HttpGet("movie/{movieId}")]
        public async Task<IActionResult> GetRatingsByMovieId(string movieId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(movieId))
                {
                    return BadRequest(new { message = "El ID de película es requerido" });
                }

                var ratings = await _ratingService.GetRatingsByMovieId(movieId);
                return Ok(ratings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener ratings: {ex.Message}");
                return StatusCode(500, new { message = "Error al obtener ratings" });
            }
        }

        /// <summary>
        /// GET /api/rating/user/{userId}
        /// 
        /// Obtiene todas las calificaciones hechas por un usuario
        /// 
        /// Parámetro:
        ///   userId: ID del usuario
        /// 
        /// Respuesta exitosa (200): Lista de RatingDto
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetRatingsByUserId(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(new { message = "El ID de usuario es requerido" });
                }

                var ratings = await _ratingService.GetRatingsByUserId(userId);
                return Ok(ratings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener ratings del usuario: {ex.Message}");
                return StatusCode(500, new { message = "Error al obtener ratings" });
            }
        }

        /// <summary>
        /// POST /api/rating
        /// 
        /// Crea una nueva calificación para una película
        /// 
        /// Header requerido:
        ///   Authorization: Bearer {token}
        /// 
        /// Cuerpo esperado (JSON):
        /// {
        ///   "movieId": "movie_001",
        ///   "score": 8.5,
        ///   "review": "Great movie! I really enjoyed it."
        /// }
        /// 
        /// Respuesta exitosa (201):
        /// {
        ///   "id": "rating_001",
        ///   "movieId": "movie_001",
        ///   "score": 8.5,
        ///   "review": "Great movie! I really enjoyed it.",
        ///   ...
        /// }
        /// 
        /// Errores:
        /// 400: Score fuera de rango, usuario ya calificó, película no existe
        /// 401: No autenticado
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateRating([FromBody] CreateRatingDto createRatingDto)
        {
            try
            {
                if (createRatingDto == null)
                {
                    return BadRequest(new { message = "El cuerpo de la petición es requerido" });
                }

                if (string.IsNullOrWhiteSpace(createRatingDto.MovieId))
                {
                    return BadRequest(new { message = "El ID de película es requerido" });
                }

                if (createRatingDto.Score < 1 || createRatingDto.Score > 10)
                {
                    return BadRequest(new { message = "La calificación debe estar entre 1 y 10" });
                }

                // TODO: Obtener userId del token JWT
                var userId = "user_123"; // Por ahora, hardcodeado

                var rating = await _ratingService.CreateRating(createRatingDto, userId);

                _logger.LogInformation($"Rating creado: Usuario {userId} calificó película {createRatingDto.MovieId}");

                return Created($"/api/rating/{rating.Id}", rating);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al crear rating: {ex.Message}");
                return StatusCode(500, new { message = "Error al crear rating" });
            }
        }

        /// <summary>
        /// PUT /api/rating/{ratingId}
        /// 
        /// Edita una calificación existente (solo el propietario)
        /// 
        /// Header requerido:
        ///   Authorization: Bearer {token}
        /// 
        /// Parámetro:
        ///   ratingId: ID de la calificación a editar
        /// 
        /// Cuerpo: Mismo formato que CreateRatingDto
        /// 
        /// Respuesta exitosa (200): La calificación actualizada
        /// 
        /// Errores:
        /// 404: Calificación no encontrada
        /// 403: No eres el propietario
        /// 401: No autenticado
        /// </summary>
        [HttpPut("{ratingId}")]
        public async Task<IActionResult> UpdateRating(string ratingId, [FromBody] CreateRatingDto createRatingDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ratingId))
                {
                    return BadRequest(new { message = "El ID de rating es requerido" });
                }

                if (createRatingDto == null)
                {
                    return BadRequest(new { message = "El cuerpo de la petición es requerido" });
                }

                if (createRatingDto.Score < 1 || createRatingDto.Score > 10)
                {
                    return BadRequest(new { message = "La calificación debe estar entre 1 y 10" });
                }

                // TODO: Obtener userId del token JWT
                var userId = "user_123"; // Por ahora, hardcodeado

                var rating = await _ratingService.UpdateRating(ratingId, createRatingDto, userId);

                _logger.LogInformation($"Rating actualizado: {ratingId}");

                return Ok(rating);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(); // 403
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al actualizar rating: {ex.Message}");
                return StatusCode(500, new { message = "Error al actualizar rating" });
            }
        }

        /// <summary>
        /// DELETE /api/rating/{ratingId}
        /// 
        /// Elimina una calificación (solo el propietario)
        /// 
        /// Header requerido:
        ///   Authorization: Bearer {token}
        /// 
        /// Parámetro:
        ///   ratingId: ID de la calificación a eliminar
        /// 
        /// Respuesta exitosa (204): No content
        /// 
        /// Errores:
        /// 404: Calificación no encontrada
        /// 403: No eres el propietario
        /// 401: No autenticado
        /// </summary>
        [HttpDelete("{ratingId}")]
        public async Task<IActionResult> DeleteRating(string ratingId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ratingId))
                {
                    return BadRequest(new { message = "El ID de rating es requerido" });
                }

                // TODO: Obtener userId del token JWT
                var userId = "user_123"; // Por ahora, hardcodeado

                await _ratingService.DeleteRating(ratingId, userId);

                _logger.LogInformation($"Rating eliminado: {ratingId}");

                return NoContent(); // 204
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(); // 403
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al eliminar rating: {ex.Message}");
                return StatusCode(500, new { message = "Error al eliminar rating" });
            }
        }
}