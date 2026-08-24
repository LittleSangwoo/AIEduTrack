using AIEduTrack.Models;

namespace AIEduTrack.Data
{
    public interface IDataRepository
    {
        UserProfile GetProfile(string userId);
        List<Course> GetAvailableCourses();
        List<UserProfile> GetAllUsers();

        void UpdateData(Stream historyStream, Stream catalogStream);

        void LoadCatalogFile(Stream stream, string fileName);
        void LoadHistoryFile(Stream stream, string fileName);
        void LoadBookletFile(Stream pdfStream); // ППК из буклета "Линейка программ"
        void LoadFromDirectory(string directoryPath);
        void ClearAll();
    }
}