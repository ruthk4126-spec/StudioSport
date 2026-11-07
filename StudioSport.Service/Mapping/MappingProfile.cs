using AutoMapper;
using StudioSport.Core.DTOs.Classes;
using StudioSport.Core.DTOs.Enrollments;
using StudioSport.Core.DTOs.Subscriptions;
using StudioSport.Core.DTOs.Auth;
using StudioSport.Core.Entities;

namespace StudioSport.Service.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // שיעור -> תשובה
        CreateMap<ClassSession, ClassResponseDto>()
            .ForMember(d => d.TrainerName,
                o => o.MapFrom(s => s.Trainer != null ? s.Trainer.Name : string.Empty));

        // יצירה ועדכון -> ישות
        CreateMap<ClassCreateDto, ClassSession>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.EnrolledCount, o => o.Ignore())
            .ForMember(d => d.Version, o => o.Ignore())
            .ForMember(d => d.Trainer, o => o.Ignore())
            .ForMember(d => d.Enrollments, o => o.Ignore());

        CreateMap<ClassUpdateDto, ClassSession>()
            .ForMember(d => d.EnrolledCount, o => o.Ignore())
            .ForMember(d => d.Version, o => o.Ignore())
            .ForMember(d => d.Trainer, o => o.Ignore())
            .ForMember(d => d.Enrollments, o => o.Ignore());

        // רישום -> תשובה
        CreateMap<Enrollment, EnrollmentResponseDto>()
            .ForMember(d => d.ClassId,
                o => o.MapFrom(s => s.ClassSessionId))
            .ForMember(d => d.ClassName,
                o => o.MapFrom(s => s.ClassSession != null ? s.ClassSession.Name : string.Empty))
            .ForMember(d => d.StartTime,
                o => o.MapFrom(s => s.ClassSession != null ? s.ClassSession.StartTime : default))
            .ForMember(d => d.EquipmentNotes,
                o => o.MapFrom(s => s.ClassSession != null ? s.ClassSession.EquipmentNotes : null))
            .ForMember(d => d.Status,
                o => o.MapFrom(s => s.Status.ToString()));

        // מנוי -> תשובה
        CreateMap<Subscription, SubscriptionResponseDto>()
            .ForMember(d => d.Status,
                o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.IsActive,
                o => o.MapFrom(s => s.Status == Core.Enums.SubscriptionStatus.Active
                                     && s.EndDate >= DateTime.UtcNow));
    }
}