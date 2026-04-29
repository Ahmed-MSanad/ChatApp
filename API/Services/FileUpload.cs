namespace API.Services;

public class FileUpload
{
    public static async Task<string> Upload(IFormFile file) // Upload Method to Store the Uploaded File on the Server and just store the File Name in the Database
    {
        var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads"); // Upload Folder Path
        if(!Directory.Exists(uploadFolder))
            Directory.CreateDirectory(uploadFolder); // Create the Upload Folder if not exist using Upload Folder Path

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName); // File Name using Guid and File Extension of the Uploaded File

        var filePath = Path.Combine(uploadFolder, fileName); // File Path using Upload Folder Path and File Name
        await using var stream = new FileStream(filePath, FileMode.Create); // Create a File Stream using File Path and File Mode Create

        await file.CopyToAsync(stream); // Copy the Uploaded File to the File Stream asynchronously

        return fileName;
    }
}
