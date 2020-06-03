namespace GenerateFurnis
{
    public class Furnis
    {
        public string className { get; set; }

        public string publicName { get; set; }

        public string descName { get; set; }

        public Furnis(string cN, string pN, string dN)
        {
            this.className = cN;
            this.publicName = pN;
            this.descName = dN;
        }
    }
}
