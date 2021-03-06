using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using C485.DataverseClientProxy.Interfaces;
using C485.DataverseClientProxy.Models;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;

namespace C485.DataverseClientProxy;

public class Connection : IConnection
{
	private readonly CrmServiceClient _connection;
	private readonly object _lockObj;
	private readonly OrganizationServiceContext _xrmServiceContext;
	private bool _disableLockingCheck;

	public Connection(CrmServiceClient connection)
	{
		_lockObj = new object();
		_connection = Guard
		   .Against
		   .Null(connection, nameof(connection));

		_xrmServiceContext = new OrganizationServiceContext(connection);
		_connection
		   .DisableCrossThreadSafeties = true;

		_connection
		   .MaxRetryCount = 10;

		_connection
		   .RetryPauseTime = TimeSpan.FromSeconds(5);
	}

	public IQueryable<Entity> CreateQuery_Unsafe_Unprotected(
		string entityLogicalName,
		OrganizationServiceContextSettings organizationServiceContextSettings = default)
	{
		organizationServiceContextSettings ??= OrganizationServiceContextSettings.Default;

		if (organizationServiceContextSettings.ClearChangesEveryTime)
		{
			_xrmServiceContext
			   .ClearChanges();
		}

		Guard
		   .Against
		   .NullOrEmpty(entityLogicalName, nameof(entityLogicalName));

		_xrmServiceContext
		   .ClearChanges();

		return _xrmServiceContext
		   .CreateQuery(entityLogicalName);
	}

	public IQueryable<T> CreateQuery_Unsafe_Unprotected<T>(
		OrganizationServiceContextSettings organizationServiceContextSettings = default) where T : Entity
	{
		organizationServiceContextSettings ??= OrganizationServiceContextSettings.Default;

		if (organizationServiceContextSettings.ClearChangesEveryTime)
		{
			_xrmServiceContext
			   .ClearChanges();
		}

		_xrmServiceContext
		   .ClearChanges();

		return _xrmServiceContext
		   .CreateQuery<T>();
	}

	public Guid CreateRecord(Entity record, RequestSettings requestSettings)
	{
		Guard
		   .Against
		   .NullOrInvalidInput(record, nameof(record), p => p.Id == Guid.Empty && !string.IsNullOrEmpty(p.LogicalName));

		Guard
		   .Against
		   .Null(requestSettings, nameof(requestSettings));

		if (!_disableLockingCheck && !IsLockedByThisThread())
		{
			throw new ArgumentException("Lock not set for used connection.");
		}

		_connection
		   .CallerId = requestSettings.ImpersonateAsUserByDataverseId ?? Guid.Empty;

		_connection
		   .BypassPluginExecution = requestSettings.SkipPluginExecution;

		return _connection
		   .Create(record);
	}

	public async Task<Guid> CreateRecordAsync(Entity record, RequestSettings requestSettings)
	{
		return await Task
		   .Run(() => CreateRecord(record, requestSettings));
	}

	public void DeleteRecord(string logicalName, Guid id, RequestSettings requestSettings)
	{
		Guard
		   .Against
		   .NullOrEmpty(logicalName, nameof(logicalName));

		Guard
		   .Against
		   .Default(id, nameof(id));

		Guard
		   .Against
		   .Null(requestSettings, nameof(requestSettings));

		if (!_disableLockingCheck && !IsLockedByThisThread())
		{
			throw new ArgumentException("Lock not set for used connection.");
		}

		_connection
		   .CallerId = requestSettings.ImpersonateAsUserByDataverseId ?? Guid.Empty;

		_connection
		   .BypassPluginExecution = requestSettings.SkipPluginExecution;

		_connection
		   .Delete(logicalName, id);
	}

	public void DeleteRecord(EntityReference entityReference, RequestSettings requestSettings)
	{
		Guard
		   .Against
		   .Null(entityReference, nameof(entityReference));

		DeleteRecord(entityReference.LogicalName, entityReference.Id, requestSettings);
	}

	public async Task DeleteRecordAsync(string logicalName, Guid id, RequestSettings requestSettings)
	{
		await Task
		   .Run(() => DeleteRecord(logicalName, id, requestSettings));
	}

	public async Task DeleteRecordAsync(EntityReference entityReference, RequestSettings requestSettings)
	{
		await Task
		   .Run(() => DeleteRecord(entityReference, requestSettings));
	}

	public void DisableLockingCheck()
	{
		_disableLockingCheck = true;
	}

	public OrganizationResponse Execute(OrganizationRequest request, RequestSettings requestSettings)
	{
		Guard
		   .Against
		   .Null(request, nameof(request));

		Guard
		   .Against
		   .Null(requestSettings, nameof(requestSettings));

		if (!_disableLockingCheck && !IsLockedByThisThread())
		{
			throw new ArgumentException("Lock not set for this connection.");
		}

		_connection
		   .CallerId = requestSettings.ImpersonateAsUserByDataverseId ?? Guid.Empty;

		_connection
		   .BypassPluginExecution = requestSettings.SkipPluginExecution;

		return _connection
		   .Execute(request);
	}

	public OrganizationResponse Execute(ExecuteMultipleRequestBuilder executeMultipleRequestBuilder)
	{
		Guard
		   .Against
		   .Null(executeMultipleRequestBuilder, nameof(executeMultipleRequestBuilder));

		return Execute(executeMultipleRequestBuilder.RequestWithResults,
			new RequestSettings
			{
				ImpersonateAsUserByDataverseId = executeMultipleRequestBuilder.ImpersonateAsUserById,
				SkipPluginExecution = executeMultipleRequestBuilder.SkipPluginExecution
			});
	}

	public async Task<OrganizationResponse> ExecuteAsync(OrganizationRequest request, RequestSettings requestSettings)
	{
		return await Task
		   .Run(() => Execute(request, requestSettings));
	}

	public async Task<OrganizationResponse> ExecuteAsync(ExecuteMultipleRequestBuilder executeMultipleRequestBuilder)
	{
		return await Task
		   .Run(() => Execute(executeMultipleRequestBuilder));
	}

	public bool IsLockedByThisThread()
	{
		return Monitor
		   .IsEntered(_lockObj);
	}

	public Entity[] QueryMultiple(
		string entityLogicalName,
		Func<IQueryable<Entity>, IQueryable<Entity>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default)
	{
		organizationServiceContextSettings ??= OrganizationServiceContextSettings.Default;
		Guard
		   .Against
		   .NullOrEmpty(entityLogicalName, nameof(entityLogicalName));

		Guard
		   .Against
		   .Null(queryBuilder, nameof(queryBuilder));

		if (!_disableLockingCheck && !IsLockedByThisThread())
		{
			throw new ArgumentException("Lock not set for used connection.");
		}

		if (organizationServiceContextSettings.ClearChangesEveryTime)
		{
			_xrmServiceContext.ClearChanges();
		}

		IQueryable<Entity> query = _xrmServiceContext
		   .CreateQuery(entityLogicalName);

		Entity[] queryResults = queryBuilder(query)
		   .ToArray();

		if (!organizationServiceContextSettings.DetachRetrievedRecords)
		{
			return queryResults;
		}

		foreach (Entity entity in queryResults)
		{
			_xrmServiceContext
			   .Detach(entity, true);
		}

		return queryResults;
	}

	public T[] QueryMultiple<T>(
		Func<IQueryable<T>, IQueryable<T>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default) where T : Entity
	{
		organizationServiceContextSettings ??= OrganizationServiceContextSettings.Default;

		Guard
		   .Against
		   .Null(queryBuilder, nameof(queryBuilder));

		if (!_disableLockingCheck && !IsLockedByThisThread())
		{
			throw new ArgumentException("Lock not set for used connection.");
		}

		if (organizationServiceContextSettings.ClearChangesEveryTime)
		{
			_xrmServiceContext.ClearChanges();
		}

		IQueryable<T> query = _xrmServiceContext
		   .CreateQuery<T>();

		T[] queryResults = queryBuilder(query)
		   .ToArray();

		if (!organizationServiceContextSettings.DetachRetrievedRecords)
		{
			return queryResults;
		}

		foreach (T entity in queryResults)
		{
			_xrmServiceContext
			   .Detach(entity, true);
		}

		return queryResults;
	}

	public async Task<Entity[]> QueryMultipleAsync(
		string entityLogicalName,
		Func<IQueryable<Entity>, IQueryable<Entity>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default)
	{
		return await Task
		   .Run(() => QueryMultiple(entityLogicalName, queryBuilder, organizationServiceContextSettings));
	}

	public async Task<T[]> QueryMultipleAsync<T>(
		Func<IQueryable<T>, IQueryable<T>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default) where T : Entity
	{
		return await Task
		   .Run(() => QueryMultiple(queryBuilder, organizationServiceContextSettings));
	}

	public Entity QuerySingle(
		string entityLogicalName,
		Func<IQueryable<Entity>, IQueryable<Entity>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default)
	{
		organizationServiceContextSettings ??= OrganizationServiceContextSettings.Default;

		if (organizationServiceContextSettings.ClearChangesEveryTime)
		{
			_xrmServiceContext
			   .ClearChanges();
		}

		Guard
		   .Against
		   .NullOrEmpty(entityLogicalName, nameof(entityLogicalName));

		Guard
		   .Against
		   .Null(queryBuilder, nameof(queryBuilder));

		if (!_disableLockingCheck && !IsLockedByThisThread())
		{
			throw new ArgumentException("Lock not set for used connection.");
		}

		IQueryable<Entity> query = _xrmServiceContext
		   .CreateQuery(entityLogicalName);

		Entity[] queryResults = queryBuilder(query)
		   .ToArray();

		if (!organizationServiceContextSettings.DetachRetrievedRecords)
		{
			Guard
			   .Against
			   .InvalidInput(queryResults,
					nameof(queryResults),
					p => p.Length == 1,
					$"Expected one record, retrieved {queryResults.Length}.");

			return queryResults[0];
		}

		foreach (Entity entity in queryResults)
		{
			_xrmServiceContext
			   .Detach(entity, true);
		}

		Guard
		   .Against
		   .InvalidInput(queryResults,
				nameof(queryResults),
				p => p.Length == 1,
				$"Expected one record, retrieved {queryResults.Length}.");

		return queryResults[0];
	}

	public T QuerySingle<T>(
		Func<IQueryable<T>, IQueryable<T>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default) where T : Entity
	{
		organizationServiceContextSettings ??= OrganizationServiceContextSettings.Default;

		if (organizationServiceContextSettings.ClearChangesEveryTime)
		{
			_xrmServiceContext
			   .ClearChanges();
		}

		Guard
		   .Against
		   .Null(queryBuilder, nameof(queryBuilder));

		if (!_disableLockingCheck && !IsLockedByThisThread())
		{
			throw new ArgumentException("Lock not set for used connection.");
		}

		IQueryable<T> query = _xrmServiceContext
		   .CreateQuery<T>();

		T[] queryResults = queryBuilder(query)
		   .ToArray();

		if (!organizationServiceContextSettings.DetachRetrievedRecords)
		{
			Guard
			   .Against
			   .InvalidInput(queryResults,
					nameof(queryResults),
					p => p.Length == 1,
					$"Expected one record, retrieved {queryResults.Length}.");

			return queryResults[0];
		}

		foreach (T entity in queryResults)
		{
			_xrmServiceContext
			   .Detach(entity, true);
		}

		Guard
		   .Against
		   .InvalidInput(queryResults,
				nameof(queryResults),
				p => p.Length == 1,
				$"Expected one record, retrieved {queryResults.Length}.");

		return queryResults[0];
	}

	public async Task<Entity> QuerySingleAsync(
		string entityLogicalName,
		Func<IQueryable<Entity>, IQueryable<Entity>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default)
	{
		return await Task
		   .Run(() => QuerySingle(entityLogicalName, queryBuilder, organizationServiceContextSettings));
	}

	public async Task<T> QuerySingleAsync<T>(
		Func<IQueryable<T>, IQueryable<T>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default) where T : Entity
	{
		return await Task
		   .Run(() => QuerySingle(queryBuilder, organizationServiceContextSettings));
	}

	public Entity QuerySingleOrDefault(
		string entityLogicalName,
		Func<IQueryable<Entity>, IQueryable<Entity>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default)
	{
		organizationServiceContextSettings ??= OrganizationServiceContextSettings.Default;

		if (organizationServiceContextSettings.ClearChangesEveryTime)
		{
			_xrmServiceContext
			   .ClearChanges();
		}

		Guard
		   .Against
		   .NullOrEmpty(entityLogicalName, nameof(entityLogicalName));

		Guard
		   .Against
		   .Null(queryBuilder, nameof(queryBuilder));

		if (!_disableLockingCheck && !IsLockedByThisThread())
		{
			throw new ArgumentException("Lock not set for used connection.");
		}

		IQueryable<Entity> query = _xrmServiceContext
		   .CreateQuery(entityLogicalName);

		Entity[] queryResults = queryBuilder(query)
		   .ToArray();

		if (!organizationServiceContextSettings.DetachRetrievedRecords)
		{
			Guard
			   .Against
			   .InvalidInput(queryResults,
					nameof(queryResults),
					p => p.Length <= 1,
					$"Expected one record, retrieved {queryResults.Length}.");

			return queryResults
			   .SingleOrDefault();
		}

		Guard
		   .Against
		   .InvalidInput(queryResults,
				nameof(queryResults),
				p => p.Length <= 1,
				$"Expected one record, retrieved {queryResults.Length}.");

		foreach (Entity entity in queryResults)
		{
			_xrmServiceContext
			   .Detach(entity, true);
		}

		return queryResults
		   .SingleOrDefault();
	}

	public T QuerySingleOrDefault<T>(
		Func<IQueryable<T>, IQueryable<T>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default) where T : Entity
	{
		organizationServiceContextSettings ??= OrganizationServiceContextSettings.Default;

		if (organizationServiceContextSettings.ClearChangesEveryTime)
		{
			_xrmServiceContext
			   .ClearChanges();
		}

		Guard
		   .Against
		   .Null(queryBuilder, nameof(queryBuilder));

		if (!_disableLockingCheck && !IsLockedByThisThread())
		{
			throw new ArgumentException("Lock not set for used connection.");
		}

		IQueryable<T> query = _xrmServiceContext
		   .CreateQuery<T>();

		T[] queryResults = queryBuilder(query)
		   .ToArray();

		if (!organizationServiceContextSettings.DetachRetrievedRecords)
		{
			Guard
			   .Against
			   .InvalidInput(queryResults,
					nameof(queryResults),
					p => p.Length <= 1,
					$"Expected one record, retrieved {queryResults.Length}.");

			return queryResults
			   .SingleOrDefault();
		}

		Guard
		   .Against
		   .InvalidInput(queryResults,
				nameof(queryResults),
				p => p.Length <= 1,
				$"Expected one record, retrieved {queryResults.Length}.");

		foreach (T entity in queryResults)
		{
			_xrmServiceContext
			   .Detach(entity, true);
		}

		return queryResults
		   .SingleOrDefault();
	}

	public async Task<Entity> QuerySingleOrDefaultAsync(
		string entityLogicalName,
		Func<IQueryable<Entity>, IQueryable<Entity>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default)
	{
		return await Task
		   .Run(() => QuerySingleOrDefault(entityLogicalName, queryBuilder, organizationServiceContextSettings));
	}

	public async Task<T> QuerySingleOrDefaultAsync<T>(
		Func<IQueryable<T>, IQueryable<T>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default) where T : Entity
	{
		return await Task
		   .Run(() => QuerySingleOrDefault(queryBuilder, organizationServiceContextSettings));
	}

	public Entity RefreshRecord(Entity record)
	{
		Guard
		   .Against
		   .NullOrInvalidInput(record, nameof(record), p => p.Id != Guid.Empty && !string.IsNullOrEmpty(p.LogicalName));

		if (!_disableLockingCheck && !IsLockedByThisThread())
		{
			throw new ArgumentException("Lock not set for this connection.");
		}

		ColumnSet columns = new(false);

		foreach (string fieldName in record.Attributes.Keys)
		{
			columns
			   .AddColumn(fieldName);
		}

		return _connection
		   .Retrieve(record.LogicalName, record.Id, columns);
	}

	public async Task<Entity> RefreshRecordAsync(Entity record)
	{
		return await Task
		   .Run(() => RefreshRecord(record));
	}

	public void ReleaseLock()
	{
		Monitor
		   .Exit(_lockObj);
	}

	public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
	{
		if (!_disableLockingCheck && !IsLockedByThisThread())
		{
			throw new ArgumentException("Lock not set for this connection.");
		}

		Guard
		   .Against
		   .NullOrEmpty(entityName, nameof(entityName));

		Guard
		   .Against
		   .Default(id, nameof(id));

		Guard
		   .Against
		   .Null(columnSet, nameof(columnSet));

		return _connection
		   .Retrieve(entityName, id, columnSet);
	}

	public async Task<Entity> RetrieveAsync(string entityName, Guid id, ColumnSet columnSet)
	{
		return await Task
		   .Run(() => RetrieveAsync(entityName, id, columnSet));
	}

	public Entity[] RetrieveMultiple(QueryExpression queryExpression)
	{
		Guard
		   .Against
		   .Null(queryExpression, nameof(queryExpression));

		if (!_disableLockingCheck && !IsLockedByThisThread())
		{
			throw new ArgumentException("Lock not set for this connection.");
		}

		return InnerRetrieveMultiple()
		   .ToArray();

		IEnumerable<Entity> InnerRetrieveMultiple()
		{
			queryExpression.PageInfo = new PagingInfo
			{
				Count = 5000,
				PageNumber = 1,
				PagingCookie = null
			};

			while (true)
			{
				EntityCollection retrieveMultipleResult = _connection
				   .RetrieveMultiple(queryExpression);

				foreach (Entity record in retrieveMultipleResult.Entities)
				{
					yield return record;
				}

				if (!retrieveMultipleResult.MoreRecords)
				{
					break;
				}

				queryExpression.PageInfo.PageNumber++;
				queryExpression.PageInfo.PagingCookie = retrieveMultipleResult.PagingCookie;
			}
		}
	}

	public async Task<Entity[]> RetrieveMultipleAsync(QueryExpression queryExpression)
	{
		return await Task
		   .Run(() => RetrieveMultiple(queryExpression));
	}

	public bool Test()
	{
		WhoAmIResponse response = (WhoAmIResponse)_connection
		   .Execute(new WhoAmIRequest());

		return response != null
			&& response.UserId != Guid.Empty;
	}

	public async Task<bool> TestAsync()
	{
		return await Task
		   .Run(Test);
	}

	public bool TryLock()
	{
		return Monitor
		   .TryEnter(_lockObj);
	}

	public Guid UpdateRecord(Entity record, RequestSettings requestSettings)
	{
		Guard
		   .Against
		   .NullOrInvalidInput(record, nameof(record), p => p.Id != Guid.Empty && !string.IsNullOrEmpty(p.LogicalName));

		Guard
		   .Against
		   .Null(requestSettings, nameof(requestSettings));

		if (!_disableLockingCheck && !IsLockedByThisThread())
		{
			throw new ArgumentException("Lock not set for used connection.");
		}

		_connection
		   .CallerId = requestSettings.ImpersonateAsUserByDataverseId ?? Guid.Empty;

		_connection
		   .BypassPluginExecution = requestSettings.SkipPluginExecution;

		_connection
		   .Update(record);

		return record
		   .Id;
	}

	public async Task<Guid> UpdateRecordAsync(Entity record, RequestSettings requestSettings)
	{
		return await Task
		   .Run(() => UpdateRecord(record, requestSettings));
	}

	public EntityReference UpsertRecord(Entity record, RequestSettings requestSettings)
	{
		Guard
		   .Against
		   .NullOrInvalidInput(record, nameof(record), p => string.IsNullOrEmpty(p.LogicalName));

		Guard
		   .Against
		   .Null(requestSettings, nameof(requestSettings));

		if (!_disableLockingCheck && !IsLockedByThisThread())
		{
			throw new ArgumentException("Lock not set for used connection.");
		}

		_connection
		   .CallerId = requestSettings.ImpersonateAsUserByDataverseId ?? Guid.Empty;

		_connection
		   .BypassPluginExecution = requestSettings.SkipPluginExecution;

		UpsertRequest request = new()
		{
			Target = record
		};

		UpsertResponse executeResponse = (UpsertResponse)Execute(request, requestSettings);

		return Guard
		   .Against
		   .Null(executeResponse, nameof(executeResponse))
		   .Target;
	}

	public async Task<EntityReference> UpsertRecordAsync(Entity record, RequestSettings requestSettings)
	{
		return await Task
		   .Run(() => UpsertRecord(record, requestSettings));
	}
}