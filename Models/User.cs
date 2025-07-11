using System.Text.Json.Serialization;

namespace Blog.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        [JsonIgnore] // para não aparecer no get json, (segurança).
        public string PasswordHash { get; set; }
        public string Image { get; set; }
        public string Slug { get; set; }
        public string Bio { get; set; }

        public IList<Post> Posts { get; set; }
        public IList<Role> Roles { get; set; }
    }
}