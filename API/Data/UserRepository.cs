using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext context;
        private readonly SignInManager<AppUser> signinManager;

        public IMapper mapper { get; }

        public UserRepository(DataContext context, IMapper mapper, SignInManager<AppUser> signinManager)
        {
            this.mapper = mapper;
            this.signinManager = signinManager;
            this.context = context;
        }

        public async Task<MemberDTO> GetMemberAsync(string username)
        {
            var query = context.Users.AsQueryable();
            var currentUsername = signinManager.Context.User.GetUsername();
            if (currentUsername == username) {
                query = query.IgnoreQueryFilters();
            }
            query = query.Where(x => x.UserName == username);
            return await query.ProjectTo<MemberDTO>(mapper.ConfigurationProvider).SingleOrDefaultAsync();
        }

        public async Task<PagedList<MemberDTO>> GetMembersAsync(UserParams userParams)
        {
            var query = context.Users.AsQueryable();
            query = query.Where(u => u.UserName != userParams.CurrentUserName);
            query = query.Where(u => u.Gender == userParams.Gender);
            var minDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
            var maxDob = DateTime.Today.AddYears(-userParams.MinAge);
            query = query.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);
            query = userParams.OrderBy switch {
                "created" => query.OrderByDescending(u => u.Created),
                _ => query.OrderByDescending(u => u.LastActive)
            };

            return await PagedList<MemberDTO>.CreateAsync(
                query.ProjectTo<MemberDTO>(mapper.ConfigurationProvider).AsNoTracking(), 
                userParams.PageNumber, userParams.PageSize
            );
        }

        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            return await context.Users.FindAsync(id);
        }

        public async Task<AppUser> GetUserByUsernameAsync(string username)
        {
            var query = context.Users.AsQueryable();
            if (signinManager.Context.User.GetUsername() == username) {
                query = query.IgnoreQueryFilters();
            }
            return await query
                .Include(p => p.Photos)
                .SingleOrDefaultAsync(x => x.UserName == username);
        }

        public async Task<string> GetUserGender(string username) {
            return await context.Users.Where(x => x.UserName == username).Select(x => x.Gender).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<AppUser>> GetUsersAsync()
        {
            return await context.Users
                .Include(p => p.Photos)
                .ToListAsync();
        }

        public void Update(AppUser user)
        {
            context.Entry(user).State = EntityState.Modified;
        }

        public async Task<AppUser> GetUserByPhotoId(int id)
        {
            return await context.Users
                .IgnoreQueryFilters()
                .Include(p => p.Photos)
                .Where(u => u.Photos.Where(p => p.Id == id).Count() > 0)
                .FirstOrDefaultAsync();
        }
    }
}