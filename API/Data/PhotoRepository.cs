using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class PhotoRepository : IPhotoRepository
    {
        private readonly DataContext context;
        private readonly IMapper mapper;

        public PhotoRepository(DataContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        public async Task<Photo> GetPhotoById(int id)
        {
            return await context.Photos
                .IgnoreQueryFilters()
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<PhotoForApprovalDTO>> GetUnapprovedPhotos()
        {
            return await context.Photos
                .IgnoreQueryFilters()
                .Where(p => !p.IsApproved)
                .ProjectTo<PhotoForApprovalDTO>(mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public void RemovePhoto(Photo photo)
        {
            context.Photos.Remove(photo);
        }
    }
}