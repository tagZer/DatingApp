using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.Api.Data;
using DatingApp.Api.Helpers;
using DatingApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.Api.Controllers
{
    [Authorize]
    [Route("api/users/{userId}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private readonly IMapper _mapper;
        private readonly IDatingRepository _repository;
        private Cloudinary _cloudinary;

        public PhotosController(IDatingRepository repository, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _repository = repository;
            _mapper = mapper;
            _cloudinaryConfig = cloudinaryConfig;

            //cloudinary api
            Account acc = new Account (
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }

        [HttpGet("{id}", Name ="GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photoFromRepo = await _repository.GetPhoto(id);

            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);

            return Ok(photo);
        }

        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm] PhotoForCreationDto photoForCreationDto)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var userFromRepo = await _repository.GetUser(userId);

            if (photoForCreationDto.File == null)
            {
                return BadRequest("You've sent an empty file");
            }
            
            var file = photoForCreationDto.File;

            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                    };

                    uploadResult = _cloudinary.Upload(uploadParams);
                }
            }
            // uri obsolete, trying url
            photoForCreationDto.Url = uploadResult.Url.ToString();
            photoForCreationDto.PublicId = uploadResult.PublicId;

            var photo = _mapper.Map<Photo>(photoForCreationDto);

            if (!userFromRepo.Photos.Any(p => p.IsMain))
                photo.IsMain = true;

            userFromRepo.Photos.Add(photo);

            if (await _repository.SaveAll())
            {
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                return CreatedAtRoute("GetPhoto", new { userId = userId, id = photo.Id}, photoToReturn);
            }

            return BadRequest("Could not add the photo");
        }

        [HttpPost("{photoId}/setMain")]
        public async Task<IActionResult> SetMainPhoto (int userId, int photoId)
        {
             if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
             {
                return Unauthorized();
             }

            var userFromRepo = await _repository.GetUser(userId);

            if (!userFromRepo.Photos.Any( p => p.Id == photoId))
            {
                return Unauthorized();
            }

            var photoFromRepository = await _repository.GetPhoto(photoId);

            if (photoFromRepository.IsMain)
            {
                return BadRequest("This is already main photo!");
            }

            var currentMainPhoto = await _repository.GetMainPhotoForUser(userId);

            currentMainPhoto.IsMain = false;
            photoFromRepository.IsMain = true;

            if (await _repository.SaveAll())
            {
                return NoContent();
            }

            return BadRequest("Could not set photo as main");
        }

        [HttpDelete("{photoId}")]
        public async Task<IActionResult> DeletePhoto(int userId, int photoId)
        {
             if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
             {
                return Unauthorized();
             }

            var userFromRepo = await _repository.GetUser(userId);

            if (!userFromRepo.Photos.Any( p => p.Id == photoId))
            {
                return Unauthorized();
            }

            var photoFromRepository = await _repository.GetPhoto(photoId);

            if (photoFromRepository.IsMain)
            {
                return BadRequest("This is main photo, you can't delete it!");
            }

            if (photoFromRepository.PublicId != null) 
            {
                var deleteParams = new DeletionParams(photoFromRepository.PublicId);

                var result = _cloudinary.Destroy(deleteParams);

                if (result.Result == "ok")
                {
                    _repository.Delete(photoFromRepository);
                }
            }

            if (photoFromRepository.PublicId == null)
            {
                _repository.Delete(photoFromRepository);
            }   

            if (await _repository.SaveAll())
            {
                return Ok();
            }

            return BadRequest("Failed to delete the photo!");
        } 
    }
}