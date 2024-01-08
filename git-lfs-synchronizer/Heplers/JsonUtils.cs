using Newtonsoft.Json.Linq;

namespace git_lfs_synchronizer.Heplers
{
    public static class JsonUtils
    {
        public static bool CheckJsonFile(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"File {path} is not found.");
                return false;
            }

            var data = File.ReadAllText(path);

            if (string.IsNullOrWhiteSpace(data))
            {
                Console.WriteLine($"File {path} is empty.");
                return false;
            }

            try
            {
                var jToken = JObject.Parse(data);
                if (jToken is JObject)
                {
                    return true;
                }

                Console.WriteLine($"Contents of file {path} shall be a json object");
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine($"File to parse file {path} as json: {e.Message}");
                return false;
            }
        }
    }
}
