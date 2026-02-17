using AutoMapper;
using Server.Api.Domain.Service.StatmentOrchestration.Model.GroupedModel;

namespace Server.Api.Infrastructure.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<StatementResponse, RecoveredData>();
            CreateMap<RecoveredData, StatementResponse>();
            // Adicione outros mapeamentos conforme necessário
        }
    }
}