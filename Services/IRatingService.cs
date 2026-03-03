using Blockbuster.API.DTOs;
using Blockbuster.API.Models;

namespace Blockbuster.API.Services;

public interface IRatingService
{
    Task<List<RatingDto>> GetRatingsByMovieId(string movieId);
    
    Task<List<RatingDto>> GetRatingsByUserId(string userId);
    
    Task<Rating> CreateRating(CreateRatingDto createRatingDto, string userId);
    
    Task<Rating> UpdateRating(string ratingId, CreateRatingDto createRatingDto, string userId);
    
    Task DeleteRating(string ratingId, string userId);
    
    Task<bool> HasUserRatedMovie(string userId, string movieId);
    
}