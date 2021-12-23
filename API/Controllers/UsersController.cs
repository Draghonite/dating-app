using System.Security.Claims;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        public IPhotoService photoService { get; }
        public UsersController(IUnitOfWork unitOfWork, IMapper mapper, IPhotoService photoService)
        {
            this.photoService = photoService;
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;           
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDTO>>> GetUsers([FromQuery]UserParams userParams) {
            var gender = await unitOfWork.UserRepository.GetUserGender(User.GetUsername());
            userParams.CurrentUserName = User.GetUsername();
            if (String.IsNullOrEmpty(userParams.Gender)) {
                userParams.Gender = gender == "male" ? "female" : "male";
            }
            var users = await unitOfWork.UserRepository.GetMembersAsync(userParams);
            Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);
            return Ok(users);
        }

        [HttpGet("{username}", Name = "GetUser")]
        public async Task<ActionResult<MemberDTO>> GetUser(string username) {
            return await unitOfWork.UserRepository.GetMemberAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDTO memberUpdateDTO) {
            var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());
            mapper.Map(memberUpdateDTO, user);
            unitOfWork.UserRepository.Update(user);
            if (await unitOfWork.Complete()) {
                return NoContent();
            }
            return BadRequest("Failed to udpate user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDTO>> AddPhoto(IFormFile file) {
            var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());
            var result = await photoService.AddPhotoAsync(file);
            if (result.Error != null) {
                return BadRequest(result.Error.Message);
            }
            var photo = new Photo {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };
            user.Photos.Add(photo);
            if (await unitOfWork.Complete()) {
                return CreatedAtRoute("GetUser", new { username = user.UserName }, mapper.Map<PhotoDTO>(photo));
            }
            return BadRequest("Problem adding photo");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId) {
            var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());
            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if (photo.IsMain) {
                return BadRequest("This is already your main photo");
            }
            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
            if (currentMain != null) {
                currentMain.IsMain = false;
            }
            photo.IsMain = true;
            if (await unitOfWork.Complete()) {
                return NoContent();
            }
            return BadRequest("Failed to set main photo");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId) {
            var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());
            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if (photo == null) {
                return NotFound();
            }
            if (photo.IsMain) {
                return BadRequest("You cannot delete your main photo");
            }
            if (photo.PublicId != null) {
                var result = await photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error != null) {
                    return BadRequest(result.Error.Message);
                }
            }
            user.Photos.Remove(photo);
            if (await unitOfWork.Complete()) {
                return Ok();
            }
            return BadRequest("Failed to delete the photo");
        }
    }
}