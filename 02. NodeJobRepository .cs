using Lers.Administration;
using Lers.Core;
using Lers.Core.NodeJobs;
using Lers.Dal;
using Lers.Interop;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lers
{
	class INodeJobRepository : INodeJobRepository
	{
		private readonly INodeJobDataAccess DataAccess;

		private readonly NodeJobTypeManager _nodeJobTypeRepo;

		private readonly IAccountManager _accountRepo;

		public INodeJobRepository(IDataAccess dal, NodeJobTypeManager jobTypeRepo,
			IAccountManager accountRepo)
		{
			DataAccess = dal.NodeJobData;
			_nodeJobTypeRepo = jobTypeRepo;
			_accountRepo = accountRepo;
		}

		public IList<NodeJob> GetListWithAccessCheck(IAccount account)
		{
			if (account == null)
				throw new ArgumentNullException(nameof(account));

			IEnumerable<Models.NodeJob> nodeJobList = DataAccess.GetListWithAccessCheck(account.Id, account.IsGranted(AccessRight.ViewAccountNodeJob));

			return nodeJobList.Select(x => CreateInstance(x)).ToList();
		}

		private NodeJob CreateInstance(Models.NodeJob model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			return new NodeJob(model, null, _nodeJobTypeRepo.GetByIdChecked(model.Type));
		}
	}
}
