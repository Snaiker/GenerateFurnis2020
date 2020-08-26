namespace GenerateFurnis
{
    public class Furnis
    {
        public string className { get; set; }

        public string publicName { get; set; }

        public string descName { get; set; }

        public Furnis(string className, string publicName, string descName)
        {
            this.className = className;
            this.publicName = publicName;
            this.descName = descName;
        }
    }
}
