using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;

namespace API.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DataContext context;
        private readonly IMapper mapper;
        private readonly SignInManager<AppUser> signInManager;

        public UnitOfWork(DataContext context, IMapper mapper, SignInManager<AppUser> signInManager)
        {
            this.context = context;
            this.mapper = mapper;
            this.signInManager = signInManager;
        }

        public IUserRepository UserRepository => new UserRepository(context, mapper, signInManager);

        public IMessageRepository MessageRepository => new MessageRepository(context, mapper);

        public ILikesRepository LikesRepository => new LikesRepository(context);

        public IPhotoRepository PhotoRepository => new PhotoRepository(context, mapper);

        public async Task<bool> Complete()
        {
            return await context.SaveChangesAsync() > 0;
        }

        public bool HasChanges()
        {
            return context.ChangeTracker.HasChanges();
        }
    }
}