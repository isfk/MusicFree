namespace MusicFree.Model
{
    struct Music
    {
        public string Mid { get; set; }
        public string Name { get; set; }
        public string Src { get; set; }
        public string Img { get; set; }
        public string Lrc { get; set; }
        public List<string> Artist { get; set; }
        public Album Album { get; set; }
    }

    struct Album
    {
        public string Name { get; set; }
    }
}
