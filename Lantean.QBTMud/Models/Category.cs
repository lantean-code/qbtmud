namespace Lantean.QBTMud.Models
{
    public record Category
    {
        public Category(string name, string savePath)
        {
            Name = name;
            SavePath = savePath;
        }

        public string Name { get; set; }
        public string SavePath { get; set; }
    }
}