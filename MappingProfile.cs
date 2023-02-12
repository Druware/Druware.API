using System;
using AutoMapper;
using Druware.Server.Entities;
using Druware.Server.Models;

namespace Druware.API
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<UserRegistrationModel, User>()
                .ForMember(u => u.UserName, opt => opt.MapFrom(x => x.Email));
        }
    }
}

