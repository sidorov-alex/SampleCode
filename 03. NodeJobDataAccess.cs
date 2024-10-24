using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Z.EntityFramework.Plus;
using Lers.Utils;

namespace Lers.Dal
{
	class NodeJobDataAccess : DataAccessImplementBase, INodeJobDataAccess
	{
		public NodeJobDataAccess(DataAccessCreateContext context) : base(context)
		{
		}

		public List<Models.NodeJob> GetListWithAccessCheck(EntityIdentifier accountId, bool canViewAllJobs)
		{
			var query =
				from nodeJob in DbContext.NodeJobs
				join an in DbContext.AllowedNodes(accountId, null) on nodeJob.NodeId equals an.Id
				join amp in DbContext.GetAllowedMeasurePoints(accountId) on nodeJob.MeasurePointId equals amp.Id into ampJoin
				from accountMeasurePoint in ampJoin.DefaultIfEmpty()
				where
					(nodeJob.MeasurePointId == null || nodeJob.MeasurePointId == accountMeasurePoint.Id)
					&& (canViewAllJobs || nodeJob.PerformerAccountId == accountId.Value || nodeJob.CreatorAccountId == accountId.Value)
				select nodeJob;

			return query.ToList();
		}
	}
}