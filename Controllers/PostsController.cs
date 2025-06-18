using Blog.Data;
using Blog.Models;
using Blog.ViewModels;
using Blog.ViewModels.Posts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Blog.Controllers;

[ApiController]
public class PostsController : ControllerBase
{
    [HttpGet("v1/posts")]
    public async Task<IActionResult> Get(
        [FromServices] BlogDataContext context,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 25)
    {
        try
        {
            var count = await context.Posts.AsNoTracking().CountAsync();
            var postsData = await context
                .Posts
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.Author)
                .Select(x => new PostsViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    LastUpdateDate = x.LastUpdateDate,
                    Slug = x.Slug,
                    Category = x.Category.Name,
                    Author = $"{x.Author.Name} ({x.Author.Email})",
                })
                .Skip(page * pageSize)
                .Take(pageSize)
                .OrderBy(x => x.LastUpdateDate)
                .ToListAsync();
            return Ok(new ResultViewModel<dynamic>(new
            {
                total = count,
                page,
                pageSize,
                postsData
            }));
        }
        catch (Exception e)
        {
            return StatusCode(500, new ResultViewModel<Post>("5x04 - Falha interna no servidor!"));
        }
    }
    
    [HttpGet("v1/posts/{id:int}")]
    public async Task<IActionResult> DetailsAsync(
        [FromServices] BlogDataContext context,
        [FromRoute] int id
        )
    {
        try
        {
            var count = await context.Posts.AsNoTracking().CountAsync();
            var postsData = await context
                .Posts
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.Author)
                .ThenInclude(x => x.Roles)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (postsData == null)
                return NotFound(new ResultViewModel<Post>("Conteúdo não encontrado!"));
            
            return Ok(new ResultViewModel<Post>(postsData));
        }
        catch (Exception e)
        {
            return StatusCode(500, new ResultViewModel<Post>("5x04 - Falha interna no servidor!"));
        }
    }
    
    [HttpGet("v1/posts/category/{category}")]
    public async Task<IActionResult> GetByCategoryAsync(
        [FromRoute] string category,
        [FromServices] BlogDataContext context,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 25)
    {
        try
        {
           var count = await context.Posts.AsNoTracking().CountAsync();
           var posts = await context.Posts
               .AsNoTracking()
               .Include(x => x.Category)
               .Include(x => x.Author)
               .Where(x => x.Category.Name == category)
               .Select(x => new PostsViewModel
               {
                   Id = x.Id,
                   Title = x.Title,
                   LastUpdateDate = x.LastUpdateDate,
                   Slug = x.Slug,
                   Category = x.Category.Name,
                   Author = $"{x.Author.Name} ({x.Author.Email})",
               })
               .Skip(page * pageSize)
               .Take(pageSize)
               .OrderByDescending(x => x.LastUpdateDate)
               .ToListAsync();

           return Ok(new ResultViewModel<dynamic>(new
           {
               total = count,
               page,
               pageSize,
               posts
           }));

        }
        catch (Exception e)
        {
            return StatusCode(500, new ResultViewModel<Post>("5x04 - Falha interna no servidor!"));
        }
    }
}