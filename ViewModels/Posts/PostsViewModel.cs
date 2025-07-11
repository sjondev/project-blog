namespace Blog.ViewModels.Posts;

public class PostsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime LastUpdateDate { get; set; }
    public string Slug { get; set; }
    public string Category { get; set; }
    public string Author { get; set; }
}