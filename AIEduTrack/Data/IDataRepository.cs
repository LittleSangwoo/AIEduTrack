using AIEduTrack.Models;

namespace AIEduTrack.Data
{
    //ЗАГЛУШКИ
    public interface IDataRepository
    {
        UserProfile GetProfile(string userId);
        List<Course> GetAvailableCourses();
    }

    public interface IMockDataRepository : IDataRepository
    {
        // Специфичные методы для тестовой заглушки, если понадобятся
        List<Course> GetCatalog();
        UserProfile GetUserProfile(string userId);
    }
}
