using CSharpFunctionalExtensions;
using DIT.IntegrationService.Domain.Converters;
using DIT.IntegrationService.Domain.Crm;
using DIT.IntegrationService.Domain.OData;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DIT.IntegrationService.Data
{
    public interface ICrmRepository
    {
        Task<Result<T>> GetAsync<T>(
            string entityPluralName, Guid entiyId, params string[] cols) where T : BaseEntity;
        Task<Result<T>> GetWithFetchXmlAsync<T>(
            string entityPluralName, string fetchXml) where T : BaseEntity;
        Task<Result<IEnumerable<T>>> GetMultiple_WithFetchXmlAsync<T>(
            string entityPluralName, string fetchXml) where T : BaseEntity;
        Task<Result<Guid>> CreateAsync<T>(string entityPluralName, T item) where T : BaseEntity;
        Task<Result<Guid>> UpdateAsync<T>(string entityPluralName, T updatedItem, Guid entityId) where T : BaseEntity;
        Task<Result> DeleteAsync<T>(string entityPluralName, Guid entityId) where T : BaseEntity;
    }

    public class CrmRepository : ICrmRepository
    {
        private readonly HttpClient _httpClient;

        public CrmRepository(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<Result<T>> GetAsync<T>(
            string entityPluralName, Guid entiyId, params string[] cols) where T : BaseEntity
        {
            string url = $"{_httpClient.BaseAddress.AbsoluteUri}/{entityPluralName}({entiyId})";
            if (cols != null && cols.Any()) url = $"{url}?$select={string.Join(",", cols)}";

            HttpResponseMessage response = null;
            var settings = new JsonSerializerSettings
            {
                Converters = new JsonConverter[] { new JsonDynamicNameConverter() },
                Formatting = Formatting.None,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
            try
            {
                response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var entity = JsonConvert.DeserializeObject<T>(content, settings);
                    return Result.Ok(entity);
                }
                throw new CrmHttpResponseException(response.Content);
            }
            catch (Exception ex)
            {
                Log.Error(
                    "Failed to get D365 entity (Type= {@EntityType}). EntityId: {@EntityId}.  Error: {@Exception}",
                    nameof(T), entiyId, ex
                );
                return Result.Failure<T>(ex.Message);
            }
            finally
            {
                response?.Dispose();
            }
        }

        public async Task<Result<T>> GetWithFetchXmlAsync<T>(string entityPluralName, string fetchXml) where T : BaseEntity
        {
            string url = @$"{_httpClient.BaseAddress.AbsoluteUri}/{entityPluralName}?fetchXml={fetchXml}";

            HttpResponseMessage response = null;
            var settings = new JsonSerializerSettings
            {
                Converters = new JsonConverter[] { new JsonDynamicNameConverter() },
                Formatting = Formatting.None,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
            try
            {
                response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    ODataResponse<T> odataResp = JsonConvert.DeserializeObject<ODataResponse<T>>(content, settings);
                    var values = odataResp?.Value;
                    return Result.Success<T>(values?.FirstOrDefault());
                }

                throw new CrmHttpResponseException(response.Content);
            }
            catch (Exception ex)
            {
                Log.Error(
                    "Failed to get D365 entity (Type= {@EntityType}). FetchXml: {@FetchXml}.  Error: {@Exception}",
                    nameof(T), fetchXml, ex
                );
                return Result.Failure<T>(ex.Message);
            }
            finally
            {
                response?.Dispose();
            }
        }

        public async Task<Result<IEnumerable<T>>> GetMultiple_WithFetchXmlAsync<T>(
            string entityPluralName, string fetchXml) where T : BaseEntity
        {
            string url = $"{_httpClient.BaseAddress.AbsoluteUri}/{entityPluralName}?fetchXml={fetchXml}";

            HttpResponseMessage response = null;
            var settings = new JsonSerializerSettings
            {
                Converters = new JsonConverter[] { new JsonDynamicNameConverter() },
                Formatting = Formatting.None,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
            try
            {
                response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    ODataResponse<T> odataResp = JsonConvert.DeserializeObject<ODataResponse<T>>(content, settings);
                    var values = odataResp?.Value;
                    return Result.Success<IEnumerable<T>>(values);
                }

                throw new CrmHttpResponseException(response.Content);
            }
            catch (Exception ex)
            {
                Log.Error(
                   "Failed to get multiple D365 entities (Type= {@EntityType}). FetchXml: {@FetchXml}.  Error: {@Exception}",
                   nameof(T), fetchXml, ex
                );
                return Result.Failure<IEnumerable<T>>(ex.Message);
            }
            finally
            {
                response?.Dispose();
            }
        }

        public async Task<Result<Guid>> CreateAsync<T>(string entityPluralName, T item) where T : BaseEntity
        {
            string url = $"{_httpClient.BaseAddress.AbsoluteUri}/{entityPluralName}";
            var settings = new JsonSerializerSettings
            {
                Converters = new JsonConverter[] { new JsonDynamicNameConverter() },
                Formatting = Formatting.None,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
            var json = JsonConvert.SerializeObject(item, settings);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            HttpResponseMessage response = null;
            try
            {
                response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var oDataEntityId = response.Headers.GetValues("OData-EntityId").FirstOrDefault();
                    if (!string.IsNullOrEmpty(oDataEntityId))
                    {
                        string pattern = @"([a-z0-9]{8}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{12})";
                        var result = Regex.Match(oDataEntityId, pattern);
                        if (result.Success)
                        {
                            Log.Information(
                                "D365 Entity (Type= @Entity) successfully created. EntityId: @EntityId",
                                nameof(T), result.Value
                            );
                            return Result.Success(new Guid(result.Value));
                        }
                    }
                    throw new Exception("Could not parse created record Id.");
                }

                throw new CrmHttpResponseException(response.Content);
            }
            catch (Exception ex)
            {
                Log.Error(
                    "Failed to create D365 entity (Type= {@EntityType}). Entity: {@Entity}.  Error: {@Exception}",
                    nameof(T), item, ex
                );
                return Result.Failure<Guid>(ex.Message);
            }
            finally
            {
                request.Dispose();
                response?.Dispose();
            }
        }

        public async Task<Result<Guid>> UpdateAsync<T>(string entityPluralName, T updatedItem, Guid entityId) where T : BaseEntity
        {
            string url = $"{_httpClient.BaseAddress.AbsoluteUri}/{entityPluralName}({entityId})";
            var settings = new JsonSerializerSettings
            {
                Converters = new JsonConverter[] { new JsonDynamicNameConverter() },
                Formatting = Formatting.None,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
            var json = JsonConvert.SerializeObject(updatedItem, settings);
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("Patch"), url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            HttpResponseMessage response = null;
            try
            {
                response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    Log.Information(
                       "D365 Entity (Type= {@Entity}) successfully updated. EntityId: {@EntityId}",
                       nameof(T), entityId
                    );
                    return Result.Success(entityId);
                }

                throw new CrmHttpResponseException(response.Content);
            }
            catch (Exception ex)
            {
                Log.Error(
                    "Failed to update D365 entity (Type= {@EntityType}). Entity: {@Entity}. Error: {@Exception}",
                    nameof(T), updatedItem, ex
                );
                return Result.Failure<Guid>(ex.Message);
            }
            finally
            {
                request.Dispose();
                response?.Dispose();
            }
        }

        public async Task<Result> DeleteAsync<T>(string entityPluralName, Guid entityId) where T : BaseEntity
        {
            string url = $"{_httpClient.BaseAddress.AbsoluteUri}/{entityPluralName}({entityId})";

            HttpResponseMessage response = null;
            try
            {
                response = await _httpClient.DeleteAsync(url);
                if (response.IsSuccessStatusCode)
                    return Result.Success();
                throw new CrmHttpResponseException(response.Content);
            }
            catch (Exception ex)
            {
                Log.Error(
                    "Failed to delete D365 entity (Type= {@EntityType}). EntityId: {@EntityId}. Error: {@Exception}",
                    nameof(T), entityId, ex
                );
                return Result.Failure(ex.Message);
            }
            finally
            {
                response?.Dispose();
            }
        }
    }
}
