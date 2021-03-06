using System;
using System.Linq;
using System.Threading.Tasks;
using C485.DataverseClientProxy.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace C485.DataverseClientProxy.Interfaces;

public interface IDataverseConnectionLayer
{
	IQueryable<Entity> CreateQuery_Unsafe_Unprotected(
		string entityLogicalName,
		OrganizationServiceContextSettings organizationServiceContextSettings = default);

	IQueryable<T> CreateQuery_Unsafe_Unprotected<T>(
		OrganizationServiceContextSettings organizationServiceContextSettings = default) where T : Entity;

	Guid CreateRecord(Entity record, RequestSettings requestSettings);

	Task<Guid> CreateRecordAsync(Entity record, RequestSettings requestSettings);

	void DeleteRecord(string logicalName, Guid id, RequestSettings requestSettings);

	void DeleteRecord(EntityReference entityReference, RequestSettings requestSettings);

	Task DeleteRecordAsync(string logicalName, Guid id, RequestSettings requestSettings);

	Task DeleteRecordAsync(EntityReference entityReference, RequestSettings requestSettings);

	OrganizationResponse Execute(OrganizationRequest request, RequestSettings requestSettings);

	OrganizationResponse Execute(ExecuteMultipleRequestBuilder executeMultipleRequestBuilder);

	Task<OrganizationResponse> ExecuteAsync(OrganizationRequest request, RequestSettings requestSettings);

	Task<OrganizationResponse> ExecuteAsync(ExecuteMultipleRequestBuilder executeMultipleRequestBuilder);

	Entity[] QueryMultiple(
		string entityLogicalName,
		Func<IQueryable<Entity>, IQueryable<Entity>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default);

	T[] QueryMultiple<T>(
		Func<IQueryable<T>, IQueryable<T>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default) where T : Entity;

	Task<Entity[]> QueryMultipleAsync(
		string entityLogicalName,
		Func<IQueryable<Entity>, IQueryable<Entity>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default);

	Task<T[]> QueryMultipleAsync<T>(
		Func<IQueryable<T>, IQueryable<T>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default) where T : Entity;

	Entity QuerySingle(
		string entityLogicalName,
		Func<IQueryable<Entity>, IQueryable<Entity>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default);

	T QuerySingle<T>(
		Func<IQueryable<T>, IQueryable<T>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default) where T : Entity;

	Task<Entity> QuerySingleAsync(
		string entityLogicalName,
		Func<IQueryable<Entity>, IQueryable<Entity>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default);

	Task<T> QuerySingleAsync<T>(
		Func<IQueryable<T>, IQueryable<T>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default) where T : Entity;

	Entity QuerySingleOrDefault(
		string entityLogicalName,
		Func<IQueryable<Entity>, IQueryable<Entity>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default);

	T QuerySingleOrDefault<T>(
		Func<IQueryable<T>, IQueryable<T>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default) where T : Entity;

	Task<Entity> QuerySingleOrDefaultAsync(
		string entityLogicalName,
		Func<IQueryable<Entity>, IQueryable<Entity>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default);

	Task<T> QuerySingleOrDefaultAsync<T>(
		Func<IQueryable<T>, IQueryable<T>> queryBuilder,
		OrganizationServiceContextSettings organizationServiceContextSettings = default) where T : Entity;

	Entity RefreshRecord(Entity record);

	Task<Entity> RefreshRecordAsync(Entity record);

	Entity Retrieve(string entityName, Guid id, ColumnSet columnSet);

	Task<Entity> RetrieveAsync(string entityName, Guid id, ColumnSet columnSet);

	Entity[] RetrieveMultiple(QueryExpression queryExpression);

	Task<Entity[]> RetrieveMultipleAsync(QueryExpression queryExpression);

	Guid UpdateRecord(Entity record, RequestSettings requestSettings);

	Task<Guid> UpdateRecordAsync(Entity record, RequestSettings requestSettings);

	EntityReference UpsertRecord(Entity record, RequestSettings requestSettings);

	Task<EntityReference> UpsertRecordAsync(Entity record, RequestSettings requestSettings);
}