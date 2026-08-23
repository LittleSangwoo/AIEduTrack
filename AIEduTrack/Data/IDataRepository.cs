using AIEduTrack.Models;

namespace AIEduTrack.Data
{
    //ЗАГЛУШКИ
    public interface IDataRepository
    {
        UserProfile GetProfile(string userId);
        List<Course> GetAvailableCourses();
        
        // ДОБАВЛЯЕМ ЭТУ СТРОКУ:
        List<UserProfile> GetAllUsers();

        // ДОБАВЛЯЕМ МЕТОД ДЛЯ ПАРСИНГА ИЗ UI:
        void UpdateData(Stream historyStream, Stream catalogStream);
    }

    
}
