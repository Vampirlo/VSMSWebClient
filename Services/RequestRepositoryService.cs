using Microsoft.EntityFrameworkCore;
using VSMSWebClient.Data;
using VSMSWebClient.Models;

namespace VSMSWebClient.Services
{
    public class RequestRepositoryService
    {
        private readonly AppDbContext _context;

        public RequestRepositoryService(AppDbContext context)
        {
            _context = context;
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
            // Находим запись по UUID
            var request = await _context.Requests
                .FirstOrDefaultAsync(r => r.Uuid == uuid);

            if (request == null)
            {
                return false; // Запись не найдена
            }

            // Обновляем статус
            request.Status = status;

            // Сохраняем изменения в базе данных
            await _context.SaveChangesAsync();

            return true; // Успешное обновление
        }
    }
}