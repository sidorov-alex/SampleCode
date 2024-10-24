using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Lers.Core.NodeJobs.Request;
using Lers.Server.Api;
using Lers.Interop;
using Microsoft.AspNetCore.Mvc;

namespace Lers.Server.Api_v1.Core.NodeJobs
{
	/// <summary>
	/// API работ на объектах учёта.
	/// </summary>
	[ApiVersion("1")]
	[Route(Routes.CorePrefix + "[controller]")]
	public class NodeJobsController : LersControllerBase
	{
		private readonly GetNodeJobRequestProcessor _getHandler;
		private readonly IMapper _mapper;

		public NodeJobsController(GetNodeJobRequestProcessor getHandler,
			IMapper mapper)
		{
			_getHandler = getHandler;
			_mapper = mapper;
		}

		/// <summary>
		/// Возвращает список работ на объектах учёта.
		/// </summary>
		/// <param name="incomplete">Указывает, что нужно вернуть только незавершённые работы.</param>
		/// <returns></returns>
		[HttpGet]
		public IEnumerable<NodeJobDTO> GetList([FromQuery]bool incomplete = false)
		{
			var response = _getHandler.HandleGetList(CurrentAccount, incomplete);

			var accounts = response.AccountList.ToDictionary(x => x.Id);
			var types = response.NodeJobTypeList.ToDictionary(x => x.Id);
			var resolutions = response.ResolutionList.ToDictionary(x => x.Id);

			return response.NodeJobList.Select(x =>
			{
				var dto = _mapper.Map<NodeJobDTO>(x);

				dto.Type = types[x.Type];

				if (x.CreatorAccountId.HasValue)
				{
					dto.CreatorAccount = accounts[x.CreatorAccountId.Value];
				}

				if (x.PerformerAccountId.HasValue)
				{
					dto.PerformerAccount = accounts[x.PerformerAccountId.Value];
				}
				
				if (x.ResolutionId.HasValue)
				{
					dto.Resolution = resolutions[x.ResolutionId.Value];
				}

				return dto;
			});
		}
	}
}
