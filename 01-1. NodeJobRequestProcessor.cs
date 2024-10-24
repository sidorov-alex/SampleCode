using System.Collections.Generic;
using System.Linq;
using Lers.Administration;
using Lers.Interop;
using Lers.Utils;

namespace Lers.Core.NodeJobs.Request
{
	/// <summary>
	/// Обработчик запроса получения работ на объектах.
	/// </summary>
	public class NodeJobRequestProcessor
	{
		private static readonly ILogger logger = Logger.CreateForCurrentClass();

		private readonly INodeJobRepository _repository;
		private readonly Dal.IDataAccess _dal;
		private readonly IAccountManager _accountManager;

		public NodeJobRequestProcessor(
			INodeJobRepository repository,
			Dal.IDataAccess dal, IAccountManager accountManager)
		{
			_repository = repository;
			_dal = dal;
			_accountManager = accountManager;
		}


		/// <summary>
		/// Обрабатывает запрос на получение списка записей журнала работ.
		/// </summary>		
		public GetNodeJobResponseParameters HandleGetList(IAccount account, bool getActive)
		{
			logger.Info("Обработка запроса на получение списка записей журнала работ на объектах учета.");

			// Получаем полный список записей в журнале работ для всех объектов учета доступных текущей учетной записи.

			var nodeJobs = _repository.GetListWithAccessCheck(account);

			if (getActive)
			{
				// Фильтруем только незавершённые работы, если их запросили.

				nodeJobs = nodeJobs.Where(x => x.State != NodeJobState.Completed).ToArray();
			}

			return PrepareNodeJobListResponse(nodeJobs);
		}

		private GetNodeJobResponseParameters PrepareNodeJobListResponse(IEnumerable<NodeJob> nodeJobs)
		{
			var accountList = new List<IAccount>();
			var incidentList = new List<Models.Incident>();
			var resolutionList = new List<Models.NodeJobResolution>();
			var nodeJobTypeList = new List<Models.NodeJobType>();

			// Формируем список объектов учета, точек учета и учетных записей без повторов,
			// и только те, которые есть в списке работ.
			FillUniqueLists(nodeJobs, accountList, incidentList, resolutionList, nodeJobTypeList);

			var jobNodes = nodeJobs.Select(x => x.NodeId)
				.Distinct();

			// Выбираем объекты, связанные с этими работами.

			var nodes = from n in _dal.NodeData.GetList()
						join nj in jobNodes
						on n.Id equals nj
						select n;
			
			// Получаем полные списки объектов и точек учета доступных текущей учетной записи и список учетных записей
			var accounts = _accountManager.GetList().ToDictionary(x => x.Id);
			var incidents = _dal.IncidentData.GetIncidentsForNodeJobs().ToDictionary(x => x.Id);
			var resolutions = _dal.NodeJobData.GetResolutionList().ToDictionary(x => x.Id);

			nodeJobTypeList.AddRange(_dal.NodeJobTypeData.GetList().ToList());

			// Формируем список объектов учета, точек учета и учетных записей только тех, которые есть в списке работ.
			foreach (NodeJob job in nodeJobs)
			{
				// Нужны уникальные учетные записи
				if (job.CreatorAccountId.HasValue || job.PerformerAccountId.HasValue)
				{
					if (job.CreatorAccountId.HasValue)
					{
						var account = accounts[job.CreatorAccountId.Value];

						if (!accountList.Contains(account))
						{
							accountList.Add(account);
						}
					}

					if (job.PerformerAccountId.HasValue && (!job.CreatorAccountId.HasValue || (job.CreatorAccountId.Value != job.PerformerAccountId.Value)))
					{
						var account = accounts[job.PerformerAccountId.Value];

						if (!accountList.Contains(account))
						{
							accountList.Add(account);
						}
					}
				}

				if (job.IncidentId.HasValue)
				{
					if (incidents.TryGetValue(job.IncidentId.Value, out Models.Incident incident))
					{
						if (!incidentList.Contains(incident))
						{
							incidentList.Add(incident);
						}
					}
				}

				if (job.ResolutionId.HasValue)
				{
					if (resolutions.TryGetValue(job.ResolutionId.Value, out Models.NodeJobResolution resolution))
					{
						if (!resolutionList.Contains(resolution))
						{
							resolutionList.Add(resolution);
						}
					}
				}
			}
		}
	}
}
