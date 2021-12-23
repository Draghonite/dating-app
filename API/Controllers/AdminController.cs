using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AdminController : BaseApiController
    {
        private readonly UserManager<AppUser> userManager;
        private readonly IUnitOfWork unitOfWork;

        public AdminController(UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
        {
            this.userManager = userManager;
            this.unitOfWork = unitOfWork;
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUsersWithRoles() {
            var users = await userManager.Users
                .Include(r => r.UserRoles)
                .ThenInclude(r => r.Role)
                .OrderBy(u => u.UserName)
                .Select(u => new {
                    u.Id,
                    Username = u.UserName,
                    Roles = u.UserRoles.Select(r => r.Role.Name).ToList()
                })
                .ToListAsync();
            return Ok(users);
        }

        [HttpPost("edit-roles/{username}")]
        public async Task<ActionResult> EditRoles(string username, [FromQuery] string roles) {
            var selectedRoles = roles.Split(",").ToArray();
            var user = await userManager.FindByNameAsync(username);
            if (user == null) {
                return NotFound();
            }
            var userRoles = await userManager.GetRolesAsync(user);
            var result = await userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
            if (!result.Succeeded) {
                return BadRequest("Failed to add to roles");
            }
            result = await userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));
            if (!result.Succeeded) {
                return BadRequest("Failed to remove from roles");
            }
            return Ok(await userManager.GetRolesAsync(user));
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photos-to-moderate")]
        public async Task<ActionResult> GetPhotosForApproval() {
            var photosToApprove = await unitOfWork.PhotoRepository.GetUnapprovedPhotos();
            return Ok(photosToApprove);
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("approve-photo/{id}")]
        public async Task<ActionResult> ApprovePhotoById(int id) {
            var photo = await unitOfWork.PhotoRepository.GetPhotoById(id);
            if (photo == null) {
                return NotFound();
            }
            photo.IsApproved = true;
            var user = await unitOfWork.UserRepository.GetUserByPhotoId(id);
            if (user != null && user.Photos.Where(p => p.IsMain).Count() == 0) {
                photo.IsMain = true;
            }
            return Ok(await unitOfWork.Complete());
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("reject-photo/{id}")]
        public async Task<ActionResult> RejectPhotoById(int id) {
            var photo = await unitOfWork.PhotoRepository.GetPhotoById(id);
            if (photo == null) {
                return NotFound();
            }
            unitOfWork.PhotoRepository.RemovePhoto(photo);
            return Ok(await unitOfWork.Complete());
        }
    }
}