using Microsoft.EntityFrameworkCore;
using System.Net;
using VSMSWebClient.Data;
using VSMSWebClient.Models;

namespace VSMSWebClient.Services
{
    public class RequestRepositoryService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RequestRepositoryService> _logger;

        public RequestRepositoryService(AppDbContext context, ILogger<RequestRepositoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddRequestAsync(Request request)
        {
            _context.Requests.Add(request);
            await _context.SaveChangesAsync();
        }

        public async Task<Request?> GetRequestByUuidAsync(string uuid)
        {
            return await _context.Requests.FirstOrDefaultAsync(r => r.Uuid == uuid);
        }

        public async Task UpdateRequestAsync(Request request)
        {
            _context.Requests.Update(request);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Request>> GetAllRequestsAsync()
        {
            return await _context.Requests.ToListAsync();
        }

        public async Task<bool> UpdateRequestStatusAsync(string uuid, string status)
        {
            // Finding the record by UUID
            var request = await _context.Requests
                .FirstOrDefaultAsync(r => r.Uuid == uuid);

            if (request == null)
            {
                return false; // The record was not found
            }

            // Updating the status
            request.Status = status;

            // Saving changes in the database
            await _context.SaveChangesAsync();

            return true;
        }

        // For requestsFromServer table

        public async Task AddRequestFromServerAsync(RequestFromServer request)
        {
            _context.RequestsFromServer.Add(request);
            await _context.SaveChangesAsync();
        }

        public async Task AddBulkRequestsFromServerAsync(List<RequestFromServer> requests)
        {
            _context.RequestsFromServer.AddRange(requests);
            await _context.SaveChangesAsync();
        }

        public async Task<List<RequestFromServer>> GetAllRequestsFromServerAsync()
        {
            return await _context.RequestsFromServer.ToListAsync();
        }

        public async Task<RequestFromServer?> GetRequestFromServerByUuidAsync(string uuid)
        {
            return await _context.RequestsFromServer.FirstOrDefaultAsync(r => r.Uuid == uuid);
        }

        public async Task ClearRequestsFromServerAsync()
        {
            var allRequests = await _context.RequestsFromServer.ToListAsync();
            _context.RequestsFromServer.RemoveRange(allRequests);
            await _context.SaveChangesAsync();
        }

        public async Task<int> SyncRequestsFromServerAsync(List<RequestFromServer> newRequests)
        {
            _logger.LogInformation("Starting sync with {Count} new requests", newRequests.Count);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Getting the existing UUIDs for comparison (excluding NULL)
                var existingUuids = await _context.RequestsFromServer
                    .Where(r => r.Uuid != null)
                    .Select(r => r.Uuid)
                    .ToListAsync();

                _logger.LogInformation("Existing UUIDs in DB: {Count}", existingUuids.Count);
                _logger.LogInformation("New UUIDs from server: {Count}", newRequests.Count);

                // 2. We determine which records need to be added/updated/deleted
                var newUuids = newRequests.Select(r => r.Uuid).ToList();

                // Records to delete (there are in the database, but not in the new data)
                var uuidsToDelete = existingUuids
                    .Where(uuid => uuid != null)
                    .Except(newUuids)
                    .ToList();

                // Entries to add (there are in the new data, but not in the database)
                var requestsToAdd = newRequests
                    .Where(r => r.Uuid != null && !existingUuids.Contains(r.Uuid))
                    .Select(r => new RequestFromServer
                    {
                        // DON'T COPY THE ID!
                        FirstName = r.FirstName,
                        SecondName = r.SecondName,
                        LastName = r.LastName,
                        PhoneNumber = r.PhoneNumber,
                        Uuid = r.Uuid,
                        Status = r.Status,
                        Message = r.Message,
                        SendTime = r.SendTime
                    })
                    .ToList();

                // Entries for updating (available in both)
                var requestsToUpdate = newRequests
                    .Where(r => r.Uuid != null && existingUuids.Contains(r.Uuid))
                    .ToList();

                _logger.LogInformation("To add: {AddCount}, To update: {UpdateCount}, To delete: {DeleteCount}",
                    requestsToAdd.Count, requestsToUpdate.Count, uuidsToDelete.Count);

                // 3. Performing operations
                if (uuidsToDelete.Count > 0)
                {
                    var toDelete = await _context.RequestsFromServer
                        .Where(r => r.Uuid != null && uuidsToDelete.Contains(r.Uuid))
                        .ToListAsync();
                    _context.RequestsFromServer.RemoveRange(toDelete);
                    _logger.LogInformation("Removed {Count} records", toDelete.Count);
                }

                if (requestsToAdd.Count > 0)
                {
                    await _context.RequestsFromServer.AddRangeAsync(requestsToAdd);
                    _logger.LogInformation("Added {Count} new records", requestsToAdd.Count);
                }

                if (requestsToUpdate.Count > 0)
                {
                    foreach (var newRequest in requestsToUpdate)
                    {
                        var existing = await _context.RequestsFromServer
                            .FirstOrDefaultAsync(r => r.Uuid == newRequest.Uuid);
                        if (existing != null)
                        {
                            // DON'T COPY THE ID!
                            existing.FirstName = newRequest.FirstName;
                            existing.SecondName = newRequest.SecondName;
                            existing.LastName = newRequest.LastName;
                            existing.PhoneNumber = newRequest.PhoneNumber;
                            existing.Status = newRequest.Status;
                            existing.Message = newRequest.Message;
                            existing.SendTime = newRequest.SendTime;

                            _context.RequestsFromServer.Update(existing);
                        }
                    }
                    _logger.LogInformation("Updated {Count} records", requestsToUpdate.Count);
                }

                var changes = await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Sync completed with {Changes} changes", changes);
                return changes;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during sync");
                throw;
            }
        }
    }
}